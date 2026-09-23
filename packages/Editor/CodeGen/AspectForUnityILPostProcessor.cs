using Katuusagi.ILPostProcessorCommon.Editor;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace Katuusagi.AspectForUnity.Editor
{
    internal class AspectForUnityILPostProcessor : ILPostProcessor
    {
        private static HashSet<string> IgnoreAssembly = new HashSet<string>()
        {
            "Katuusagi.AspectForUnity",
            "Katuusagi.ILPostProcessorCommon",
        };

        private List<AdviceInfo> _advices;
        private ModuleDefinition _mainModule;
        private TypeReference _methodBase;
        private TypeReference _objectArray;
        private TypeReference _parameterArray;
        private MethodReference _parameterArrayCtor;
        private TypeReference _proceedingContext;
        private TypeReference _proceedingContextGeneric;
        private TypeReference _unsafeProceedingContextGeneric;
        private TypeReference _proceedingStatePoolGeneric;
        private TypeReference _proceedingRuntime;
        private MethodReference _objectArrayPoolRent;
        private MethodReference _objectArrayPoolReturn;
        private MethodReference _getMethodFromHandle = null;
        private TypeReference _exceptionType;

        private readonly Dictionary<MethodDefinition, AsyncExecutionInfo> _asyncExecutionInfos = new Dictionary<MethodDefinition, AsyncExecutionInfo>();
        private readonly HashSet<MethodDefinition> _asyncMoveNextMethods = new HashSet<MethodDefinition>();

        private enum AsyncExecutionKind
        {
            Async,
            Iterator,
        }

        private sealed class AsyncExecutionInfo
        {
            public MethodDefinition SourceMethod;
            public TypeDefinition StateMachineType;
            public MethodDefinition MoveNext;
            public AsyncExecutionKind Kind;
            public FieldDefinition StartedField;
            public FieldDefinition CompletedField;
            public FieldDefinition BuilderField;
            public FieldDefinition ThisField;
            public Dictionary<string, FieldDefinition> ParameterFields;
            public TypeReference AsyncResultType;
        }

        private struct AdviceInfo
        {
            public bool HasPointcutMethod;
            public bool HasPointcutParameters;
            public bool HasPointcutProceed;
            public TypeReference PointcutThisType;
            public TypeReference PointcutProceedType;
            public TypeReference PointcutReturnedType;
            public TypeReference PointcutThrownType;
            public MethodReference Method;
            public JoinPoint JoinPoint;
            public bool UnsafeInjection;
            public AdviceScope Scope;
            public IReadOnlyList<IPointcutInfo> Pointcuts;
        }

        private sealed class AroundGenerationInfo
        {
            public TypeDefinition StateType;
            public TypeReference StateTypeInstance;
            public TypeReference CallStateTypeInstance;
            public MethodReference Dispatcher;
            public MethodReference Initialize;
            public MethodReference SetMetadata;
            public MethodReference CopyBack;
            public MethodReference[] SetAspects;
            public MethodDefinition[] InvokeAround;
            public Dictionary<GenericParameter, TypeReference> GenericParameterMap;
            public TypeReference ReturnElementType;
            public bool ReturnsByReference;
            public bool HasReturn;
            public bool HasPointcutParameters;
            public bool HasPointcutMethod;
            public FieldDefinition TargetField;
            public FieldDefinition[] ParameterFields;
            public FieldDefinition MethodBaseField;
            public FieldDefinition ParametersField;
            public FieldDefinition ParameterCountField;
            public FieldDefinition ReturnValueField;
            public Dictionary<TypeReference, FieldDefinition> AspectFields;
        }

        public override ILPostProcessor GetInstance() => this;
        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            return !IgnoreAssembly.Contains(compiledAssembly.Name) &&
                   compiledAssembly.References.Select(Path.GetFileNameWithoutExtension).Contains("Katuusagi.AspectForUnity");
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            if (!WillProcess(compiledAssembly))
            {
                return null;
            }

            try
            {
                ILPPUtils.InitLog<AspectForUnityILPostProcessor>(compiledAssembly);
                using (var assembly = ILPPUtils.LoadAssemblyDefinition(compiledAssembly))
                {
                    _mainModule = assembly.MainModule;

                    _exceptionType = _mainModule.ImportReference(typeof(Exception));
                    _methodBase = _mainModule.ImportReference(typeof(System.Reflection.MethodBase));
                    _objectArray = _mainModule.ImportReference(typeof(object[]));
                    _parameterArray = _mainModule.ImportReference(typeof(ParameterArray));
                    _parameterArrayCtor = _mainModule.ImportReference(typeof(ParameterArray).GetConstructors().FirstOrDefault());
                    _proceedingContext = _mainModule.ImportReference(typeof(ProceedingContext));
                    _proceedingContextGeneric = _mainModule.ImportReference(typeof(ProceedingContext<>));
                    _unsafeProceedingContextGeneric = _mainModule.ImportReference(typeof(UnsafeProceedingContext<>));
                    _proceedingStatePoolGeneric = _mainModule.ImportReference(typeof(ProceedingStatePool<>));
                    _proceedingRuntime = _mainModule.ImportReference(typeof(ProceedingRuntime));
                    _objectArrayPoolRent = _mainModule.ImportReference(typeof(ObjectArrayPool).GetMethod(nameof(ObjectArrayPool.Rent)));
                    _objectArrayPoolReturn = _mainModule.ImportReference(typeof(ObjectArrayPool).GetMethod(nameof(ObjectArrayPool.Return)));
                    _getMethodFromHandle = _mainModule.ImportReference(((Func<RuntimeMethodHandle, RuntimeTypeHandle, System.Reflection.MethodBase>)System.Reflection.MethodBase.GetMethodFromHandle).Method);

                    if (_advices == null)
                    {
                        _advices = new List<AdviceInfo>();
                    }
                    else
                    {
                        _advices.Clear();
                    }

                    _asyncExecutionInfos.Clear();
                    _asyncMoveNextMethods.Clear();

                    using (ThreadStaticArrayPool.Get(out var allTypes, assembly.Modules.SelectMany(v => v.Types).GetAllTypes()))
                    {
                        foreach (var type in allTypes)
                        {
                            OutputPointcutMethodNameProcessor(type);
                        }

                        // Validate Advice declarations in this assembly.
                        foreach (var type in allTypes)
                        {
                            if (!type.HasAttribute(typeof(Aspect).FullName) ||
                                ValidateAspect(type) ||
                                !type.HasMethods)
                            {
                                continue;
                            }

                            foreach (var method in type.Methods)
                            {
                                ImportAdvice(method, _advices);
                            }
                        }

                        if (ValidateAdvices(_advices) ||
                            compiledAssembly.Name == "AspectEntry")
                        {
                            return compiledAssembly.GetNullResult();
                        }

                        // Collect references of the compiled assembly.
                        var references = compiledAssembly.References.Select(v => ILPPUtils.CopyAssemblySymbols(compiledAssembly.Name, v)).ToArray();
                        var referencesQuery = references.Where(v =>
                        {
                            var asmName = Path.GetFileNameWithoutExtension(v);
                            return !IgnoreAssembly.Contains(asmName) &&
                                    asmName != "AspectEntry" &&
                                    asmName != assembly.FullName;
                        })
                        .Distinct()
                        .Select(v => ILPPUtils.LoadAssemblyDefinition(v, references))
                        .Where(v => v != null);
                        using (ThreadStaticListPool.Get(out var referenceAssemblies, referencesQuery))
                        {
                            try
                            {
                                using (ThreadStaticArrayPool.Get(out var referenceTypes, referenceAssemblies.SelectMany(v => v.Modules).SelectMany(v => v.Types).GetAllTypes()))
                                {
                                    foreach (var type in referenceTypes)
                                    {
                                        if (!type.HasAttribute(typeof(Aspect).FullName) ||
                                            !type.HasMethods)
                                        {
                                            continue;
                                        }

                                        foreach (var method in type.Methods)
                                        {
                                            var tmp = _mainModule.ImportReference(method);
                                            ImportAdvice(tmp, _advices);
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                foreach (var asm in referenceAssemblies)
                                {
                                    asm.Dispose();
                                }
                            }
                        }

                        ImportGlobalAdvices(_advices);


                        if (assembly.HasAttribute(typeof(BlockAspect).FullName) ||
                            assembly.Modules.Any(v => v.HasAttribute(typeof(BlockAspect).FullName)))
                        {
                            return compiledAssembly.GetNullResult();
                        }

                        var hasAsyncExecutionError = CollectAsyncExecutionInfos(allTypes);
                        foreach (var type in allTypes)
                        {
                            if (!type.HasMethods ||
                                type.HasAttribute(typeof(BlockAspect).FullName))
                            {
                                continue;
                            }

                            using (ThreadStaticArrayPool.Get(out var methods, type.Methods))
                            {
                                foreach (var method in methods)
                                {
                                    if (!method.HasBody ||
                                        method.HasAttribute(typeof(BlockAspect).FullName) ||
                                        _asyncMoveNextMethods.Contains(method))
                                    {
                                        continue;
                                    }

                                    AdviceProcessor(type, method);

                                    if (_asyncExecutionInfos.TryGetValue(method, out var asyncExecutionInfo))
                                    {
                                        hasAsyncExecutionError |= AsyncExecutionSequenceAdviceProcessor(asyncExecutionInfo);
                                    }

                                    var body = method.Body;
                                    ILPPUtils.ResolveInstructionOpCode(body.Instructions);
                                    body.Optimize();
                                }
                            }
                        }

                        if (hasAsyncExecutionError)
                        {
                            return compiledAssembly.GetNullResult();
                        }

                        return compiledAssembly.GetResult(assembly);
                    }
                }
            }
            catch (Exception e)
            {
                ILPPUtils.Log($"{_mainModule?.Name}: AspectForUnity ILPP failure: {e}");
                ILPPUtils.LogException(e);
            }

            return compiledAssembly.GetNullResult();
        }

        private void ImportGlobalAdvices(List<AdviceInfo> result)
        {
            var runtimeConstructs = ILPPUtils.FindConstructs<Advice>(typeof(AspectEntry).Assembly);
            foreach (var constructor in runtimeConstructs)
            {
                var methodRef = _mainModule.ImportReference(constructor);
                ImportAdvice(methodRef, result);
            }

            var runtimeMethods = ILPPUtils.FindMethods<Advice>(typeof(AspectEntry).Assembly);
            foreach (var method in runtimeMethods)
            {
                var methodRef = _mainModule.ImportReference(method);
                ImportAdvice(methodRef, result);
            }
        }

        private void ImportAdvice(MethodReference methodRef, List<AdviceInfo> result)
        {
            var method = methodRef.Resolve();
            if (method == null)
            {
                // TODO: Resolve references that may originate in another assembly.
                ILPPUtils.Log($"{_mainModule.Name}: resolve failed. {methodRef.FullName}");
                return;
            }

            var advice = method.GetAttribute(typeof(Advice).FullName);
            if (advice == null)
            {
                return;
            }

            var joinPoint = (JoinPoint)(int)advice.ConstructorArguments[0].Value;
            var unsafeInjection = false;
            var scope = AdviceScope.Invocation;
            if (advice.ConstructorArguments.Count >= 2)
            {
                var secondParameterType = advice.Constructor.Parameters.Count >= 2
                    ? advice.Constructor.Parameters[1].ParameterType.FullName
                    : null;
                if (secondParameterType == typeof(AdviceScope).FullName)
                {
                    scope = (AdviceScope)(int)advice.ConstructorArguments[1].Value;
                }
                else
                {
                    unsafeInjection = Convert.ToBoolean(advice.ConstructorArguments[1].Value);
                    if (advice.ConstructorArguments.Count >= 3)
                    {
                        scope = (AdviceScope)(int)advice.ConstructorArguments[2].Value;
                    }
                }
            }
            var pointcuts = CreatePointcutInfos(method);
            var hasPointcutMethod = method.Parameters.Any(HasPointcutMethod);
            var hasPointcutParameters = method.Parameters.Any(HasPointcutParameters);
            var pointcutProceedParam = method.Parameters.FirstOrDefault(HasPointcutProceed);
            var hasPointcutProceed = pointcutProceedParam != null;
            TypeReference pointcutProceedType = null;
            if (pointcutProceedParam != null)
            {
                pointcutProceedType = pointcutProceedParam.ParameterType.ContainsGenericParameter
                    ? pointcutProceedParam.ParameterType
                    : _mainModule.ImportReference(pointcutProceedParam.ParameterType);
            }

            var pointcutThisParam = method.Parameters.FirstOrDefault(HasPointcutThis);
            TypeReference pointcutThisType = null;
            if (pointcutThisParam != null)
            {
                if (pointcutThisParam.ParameterType.ContainsGenericParameter)
                {
                    pointcutThisType = pointcutThisParam.ParameterType;
                }
                else
                {
                    pointcutThisType = _mainModule.ImportReference(pointcutThisParam.ParameterType);
                }
            }

            var pointcutResultParam = method.Parameters.FirstOrDefault(HasPointcutReturned);
            TypeReference pointcutReturnedType = null;
            if (pointcutResultParam != null)
            {
                if (pointcutResultParam.ParameterType.ContainsGenericParameter)
                {
                    pointcutReturnedType = pointcutResultParam.ParameterType;
                }
                else
                {
                    pointcutReturnedType = _mainModule.ImportReference(pointcutResultParam.ParameterType);
                }
            }

            var PointcutThrownParam = method.Parameters.FirstOrDefault(HasPointcutThrown);
            TypeReference pointcutThrownType = null;
            if (PointcutThrownParam != null)
            {
                if (PointcutThrownParam.ParameterType.ContainsGenericParameter)
                {
                    pointcutThrownType = PointcutThrownParam.ParameterType;
                }
                else
                {
                    pointcutThrownType = _mainModule.ImportReference(PointcutThrownParam.ParameterType);
                }
            }

            var adviceInfo = new AdviceInfo()
            {
                HasPointcutMethod = hasPointcutMethod,
                HasPointcutParameters = hasPointcutParameters,
                HasPointcutProceed = hasPointcutProceed,
                PointcutThisType = pointcutThisType,
                PointcutProceedType = pointcutProceedType,
                PointcutReturnedType = pointcutReturnedType,
                PointcutThrownType = pointcutThrownType,
                Method = methodRef,
                JoinPoint = joinPoint,
                UnsafeInjection = unsafeInjection,
                Scope = scope,
                Pointcuts = pointcuts,
            };

            result.Add(adviceInfo);
        }

        private bool CollectAsyncExecutionInfos(IEnumerable<TypeDefinition> allTypes)
        {
            var hasError = false;
            const string asyncStateMachineAttribute = "System.Runtime.CompilerServices.AsyncStateMachineAttribute";
            const string iteratorStateMachineAttribute = "System.Runtime.CompilerServices.IteratorStateMachineAttribute";
            const string asyncIteratorStateMachineAttribute = "System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute";

            foreach (var type in allTypes)
            {
                if (!type.HasMethods)
                {
                    continue;
                }

                foreach (var method in type.Methods)
                {
                    if (!method.HasBody ||
                        !_advices.Any(v => v.Scope == AdviceScope.AsyncExecutionSequence &&
                                           v.Pointcuts.All(p => p.IsMatch(method))))
                    {
                        continue;
                    }

                    var attribute = method.GetAttribute(asyncStateMachineAttribute);
                    var kind = AsyncExecutionKind.Async;
                    if (attribute == null)
                    {
                        attribute = method.GetAttribute(iteratorStateMachineAttribute);
                        kind = AsyncExecutionKind.Iterator;
                    }

                    if (attribute == null)
                    {
                        if (method.GetAttribute(asyncIteratorStateMachineAttribute) != null)
                        {
                            ILPPUtils.LogError("ASPECT2601", "AspectForUnity", $"Advice for asynchronous execution sequence is not supported for async iterator method \"{method.FullName}\".", method);
                        }
                        else
                        {
                            ILPPUtils.LogError("ASPECT2602", "AspectForUnity", $"Advice for asynchronous execution sequence was applied to method \"{method.FullName}\", but the method is not an async method or coroutine.", method);
                        }

                        hasError = true;
                        continue;
                    }

                    if (attribute.ConstructorArguments.Count <= 0 ||
                        !(attribute.ConstructorArguments[0].Value is TypeReference stateMachineTypeReference))
                    {
                        ILPPUtils.LogError("ASPECT2603", "AspectForUnity", $"Cannot resolve the asynchronous execution type for method \"{method.FullName}\".", method);
                        hasError = true;
                        continue;
                    }

                    var stateMachineType = stateMachineTypeReference.Resolve();
                    var moveNext = stateMachineType?.Methods.FirstOrDefault(v => v.Name == "MoveNext" && !v.IsStatic);
                    if (stateMachineType == null || moveNext == null || !moveNext.HasBody)
                    {
                        ILPPUtils.LogError("ASPECT2604", "AspectForUnity", $"Cannot find MoveNext for asynchronous method \"{method.FullName}\".", method);
                        hasError = true;
                        continue;
                    }

                    var builderField = stateMachineType.Fields.FirstOrDefault(v => v.Name == "<>t__builder");
                    if (kind == AsyncExecutionKind.Async && builderField == null)
                    {
                        ILPPUtils.LogError("ASPECT2614",
                                           "AspectForUnity",
                                           $"Cannot find the asynchronous method builder field for method \"{method.FullName}\".",
                                           method);
                        hasError = true;
                        continue;
                    }

                    var info = new AsyncExecutionInfo()
                    {
                        SourceMethod = method,
                        StateMachineType = stateMachineType,
                        MoveNext = moveNext,
                        Kind = kind,
                        StartedField = AddGeneratedField(stateMachineType,
                                                         $"$AspectForUnityStarted_{method.MetadataToken.ToInt32():X8}",
                                                         _mainModule.TypeSystem.Boolean),
                        CompletedField = AddGeneratedField(stateMachineType,
                                                           $"$AspectForUnityCompleted_{method.MetadataToken.ToInt32():X8}",
                                                           _mainModule.TypeSystem.Boolean),
                        BuilderField = builderField,
                        ThisField = FindStateMachineThisField(stateMachineType),
                        ParameterFields = FindStateMachineParameterFields(method, stateMachineType),
                        AsyncResultType = FindAsyncResultType(method, moveNext, kind, builderField),
                    };

                    _asyncExecutionInfos.Add(method, info);
                    _asyncMoveNextMethods.Add(moveNext);
                }
            }

            return hasError;
        }

        private FieldDefinition AddGeneratedField(TypeDefinition type, string baseName, TypeReference fieldType)
        {
            var name = baseName;
            var suffix = 0;
            while (type.Fields.Any(v => v.Name == name))
            {
                suffix++;
                name = $"{baseName}{suffix}";
            }

            var field = new FieldDefinition(name, FieldAttributes.Private, _mainModule.ImportReference(fieldType));
            type.Fields.Add(field);
            return field;
        }

        private FieldReference CreateStateFieldReference(FieldDefinition field, TypeDefinition stateType)
        {
            TypeReference declaringType = stateType;
            if (stateType.HasGenericParameters)
            {
                var genericInstance = new GenericInstanceType(stateType);
                foreach (var genericParameter in stateType.GenericParameters)
                {
                    genericInstance.GenericArguments.Add(genericParameter);
                }

                declaringType = genericInstance;
            }

            return new FieldReference(field.Name, field.FieldType, declaringType);
        }

        private static FieldDefinition FindStateMachineThisField(TypeDefinition stateMachineType)
        {
            return stateMachineType.Fields.FirstOrDefault(v => v.Name == "<>4__this" ||
                                                                  v.Name.IndexOf("__this", StringComparison.Ordinal) >= 0);
        }

        private static Dictionary<string, FieldDefinition> FindStateMachineParameterFields(MethodDefinition method, TypeDefinition stateMachineType)
        {
            var result = new Dictionary<string, FieldDefinition>(StringComparer.Ordinal);
            foreach (var parameter in method.Parameters)
            {
                var field = stateMachineType.Fields.FirstOrDefault(v => v.Name == parameter.Name ||
                                                                          v.Name == $"<>3__{parameter.Name}" ||
                                                                          v.Name.StartsWith($"<{parameter.Name}>", StringComparison.Ordinal));
                if (field != null)
                {
                    result[parameter.Name] = field;
                }
            }

            return result;
        }

        private TypeReference FindAsyncResultType(MethodDefinition sourceMethod,
                                                  MethodDefinition moveNext,
                                                  AsyncExecutionKind kind,
                                                  FieldDefinition builderField)
        {
            if (kind != AsyncExecutionKind.Async)
            {
                return null;
            }

            if (sourceMethod.ReturnType is GenericInstanceType returnType &&
                returnType.GenericArguments.Count == 1 &&
                (returnType.ElementType.FullName == "System.Threading.Tasks.Task`1" ||
                 returnType.ElementType.FullName == "System.Threading.Tasks.ValueTask`1"))
            {
                return _mainModule.ImportReference(returnType.GenericArguments[0]);
            }

            var setResult = moveNext.Body.Instructions
                .Select(v => v.Operand)
                .OfType<MethodReference>()
                .FirstOrDefault(v => IsAsyncBuilderCompletionMethod(v, builderField, "SetResult") &&
                                     v.Parameters.Count == 1);
            if (setResult == null)
            {
                return null;
            }

            var resultType = setResult.Parameters[0].ParameterType;
            if (resultType.ContainsGenericParameter &&
                setResult.DeclaringType is GenericInstanceType builderInstance)
            {
                var genericParameterMap = new Dictionary<GenericParameter, TypeReference>();
                for (int i = 0; i < builderInstance.ElementType.GenericParameters.Count &&
                             i < builderInstance.GenericArguments.Count; i++)
                {
                    genericParameterMap[builderInstance.ElementType.GenericParameters[i]] = builderInstance.GenericArguments[i];
                }

                resultType = SubstituteType(resultType, genericParameterMap);
            }

            if (resultType.ContainsGenericParameter)
            {
                return null;
            }

            return _mainModule.ImportReference(resultType);
        }

        private bool IsAsyncBuilderCompletionMethod(MethodReference method,
                                                    FieldDefinition builderField,
                                                    string methodName)
        {
            if (method == null ||
                builderField == null ||
                !method.HasThis ||
                method.Name != methodName)
            {
                return false;
            }

            var methodDeclaringType = method.DeclaringType;
            var builderType = builderField.FieldType;
            var methodElementType = methodDeclaringType is GenericInstanceType methodGenericType
                ? methodGenericType.ElementType
                : methodDeclaringType;
            var builderElementType = builderType is GenericInstanceType builderGenericType
                ? builderGenericType.ElementType
                : builderType;

            if (methodDeclaringType.FullName != builderType.FullName &&
                methodElementType.FullName != builderElementType.FullName)
            {
                return false;
            }

            if (methodName == "SetException")
            {
                return method.Parameters.Count == 1 &&
                       method.Parameters[0].ParameterType.FullName == _exceptionType.FullName;
            }

            return method.Parameters.Count <= 1;
        }

        private static IPointcutInfo CreatePointcutInfo(CustomAttribute pointcut)
        {
            var pointcutType = pointcut.AttributeType.FullName;
            if (pointcutType == typeof(RegexPointcut).FullName)
            {
                return new RegexPointcutInfo(pointcut);
            }

            if (pointcutType == typeof(AttributeTypePointcut).FullName)
            {
                return new AttributeTypePointcutInfo(pointcut);
            }

            if (pointcutType == typeof(DeclaringAttributeTypePointcut).FullName)
            {
                return new DeclaringAttributeTypePointcutInfo(pointcut);
            }

            if (pointcutType == typeof(MethodNamePointcut).FullName)
            {
                return new MethodNamePointcutInfo(pointcut);
            }

            if (pointcutType == typeof(DeclaringTypePointcut).FullName)
            {
                return new DeclaringTypePointcutInfo(pointcut);
            }

            if (pointcutType == typeof(ReturnTypePointcut).FullName)
            {
                return new ReturnTypePointcutInfo(pointcut);
            }

            if (pointcutType == typeof(GenericParameterNamePointcut).FullName)
            {
                return new GenericParameterNamePointcutInfo(pointcut);
            }

            if (pointcutType == typeof(ParameterTypePointcut).FullName)
            {
                return new ParameterTypePointcutInfo(pointcut);
            }

            return null;
        }

        private static IReadOnlyList<IPointcutInfo> CreatePointcutInfos(MethodDefinition method)
        {
            var declaringAttributes = GetDeclaringAttributes(method);
            var attributes = method.CustomAttributes;
            var pointcuts = declaringAttributes.Concat(attributes)
                                              .Select(CreatePointcutInfo)
                                              .Where(v => v != null)
                                              .ToArray();
            return pointcuts;
        }

        private static IEnumerable<CustomAttribute> GetDeclaringAttributes(MethodDefinition method)
        {
            var type = method.DeclaringType;
            while (type != null)
            {
                foreach (var attr in type.CustomAttributes)
                {
                    yield return attr;
                }
                type = type.DeclaringType;
            }
        }

        private void OutputPointcutMethodNameProcessor(TypeDefinition type)
        {
            if (!type.HasMethods)
            {
                return;
            }

            var result = new StringBuilder();
            using (ThreadStaticArrayPool.Get(out var methods, type.Methods))
            {
                foreach (var method in methods)
                {
                    var outputAttrs = method.CustomAttributes;
                    using (ThreadStaticListPool.Get<PointcutNameFlag>(out var flags))
                    {
                        foreach (var outputAttr in outputAttrs)
                        {
                            if (outputAttr.AttributeType.FullName != typeof(OutputPointcutMethodName).FullName)
                            {
                                continue;
                            }

                            var flag = (PointcutNameFlag)(ulong)outputAttr.ConstructorArguments[0].Value;
                            flags.Add(flag);
                        }

                        if (!flags.Any())
                        {
                            continue;
                        }

                        var all = EditorAspectForUnityUtils.GeneratePointcutMethodName(method, PointcutNameFlag.All);
                        result.AppendLine(all);
                        foreach (var flag in flags)
                        {
                            var methodName = EditorAspectForUnityUtils.GeneratePointcutMethodName(method, flag);
                            result.AppendLine($"-> {flag.ToString()}:{methodName}");
                        }
                        result.AppendLine();
                    }
                }
            }

            if (result.Length <= 0)
            {
                return;
            }

            var assemblyName = type.Module.Assembly.Name.Name;
            var directoryPath = Path.Combine("Logs/PointcutMethodName", assemblyName);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var invalidFileNameChars = Path.GetInvalidFileNameChars();
            var fileName = type.FullName;
            foreach (var c in invalidFileNameChars)
            {
                fileName = fileName.Replace(c, '_');
            }

            var logPath = Path.Combine(directoryPath, $"{fileName}.txt");
            File.WriteAllText(logPath, result.ToString());
        }


        private void AdviceProcessor(TypeDefinition type, MethodDefinition method)
        {
            var body = method.Body;
            using (ThreadStaticArrayPool.Get(out var advices, _advices.Where(v => v.Scope == AdviceScope.Invocation &&
                                                                                   v.Pointcuts.All(p => p.IsMatch(method)))))
            {
                if (advices.Length <= 0 ||
                    ValidateAdvices(method, advices))
                {
                    return;
                }

                var hasPointcutParameters = advices.Any(v => v.HasPointcutParameters);
                var adviceGroups = advices.GroupBy(v => v.JoinPoint);
                var beforeAdvices = Array.Empty<AdviceInfo>();
                var aroundAdvices = Array.Empty<AdviceInfo>();
                var afterReturningAdvices = Array.Empty<AdviceInfo>();
                var afterThrowingAdvices = Array.Empty<AdviceInfo>();
                var afterAdvices = Array.Empty<AdviceInfo>();
                foreach (var adviceGroup in adviceGroups)
                {
                    var sorted = adviceGroup.OrderBy(v =>
                    {
                        var adviceMethod = v.Method.Resolve();
                        return adviceMethod.IsConstructor ? 0 : 1;
                    })
                    .ThenBy(v =>
                    {
                        return v.PointcutReturnedType?.IsByReference ?? false ? 1 : 0;
                    })
                    .ThenBy(v =>
                    {
                        int parameterBitMask = 0;
                        var adviceMethod = v.Method.Resolve();
                        for (int i = 0; i < method.Parameters.Count && i < 32; i++)
                        {
                            var methodParam = method.Parameters[i];
                            var adviceParam = adviceMethod.GetParameter(methodParam.Name);
                            if (adviceParam == null || HasPointcutAccessorAttribute(adviceParam))
                            {
                                continue;
                            }

                            if (adviceParam.ParameterType.IsByReference == true)
                            {
                                parameterBitMask |= (1 << i);
                            }
                        }
                        return parameterBitMask;
                    });
                    switch (adviceGroup.Key)
                    {
                        case JoinPoint.Before:
                            beforeAdvices = sorted.ToArray();
                            break;
                        case JoinPoint.Around:
                            aroundAdvices = sorted.ToArray();
                            break;
                        case JoinPoint.AfterReturning:
                            afterReturningAdvices = sorted.ToArray();
                            break;
                        case JoinPoint.AfterThrowing:
                            afterThrowingAdvices = sorted.ToArray();
                            break;
                        case JoinPoint.After:
                            afterAdvices = sorted.ToArray();
                            break;

                    }
                }

                // Transform the original method in place.
                var aspectInstances = new Dictionary<TypeReference, VariableDefinition>(TypeReferenceComparer.Default);

                // Rebuild the method body with advice code.
                {
                    var hasAfterReturning = afterReturningAdvices.Any();
                    var hasAfterThrowing = afterThrowingAdvices.Any();
                    var hasAfter = afterAdvices.Any();
                    var needsNormalExit = hasAfterReturning || hasAfter || hasPointcutParameters;
                    var hasResultVariable = method.HasReturn() && needsNormalExit;

                    VariableDefinition resultVariable = null;
                    if (hasResultVariable)
                    {
                        resultVariable = new VariableDefinition(method.ReturnType);
                        method.Body.Variables.Add(resultVariable);
                    }

                    var originalInstructions = body.Instructions.ToArray();
                    if (originalInstructions.Length <= 0)
                    {
                        return;
                    }

                    AroundGenerationInfo aroundInfo = null;
                    if (aroundAdvices.Length > 0)
                    {
                        var originalMethod = ExtractOriginalMethod(type, method);
                        aroundInfo = CreateAroundGenerationInfo(type, method, originalMethod, aroundAdvices);
                    }

                    // Keep the original instructions and exception handlers in the original method.
                    // Rebuild only the instruction collection so prefix code can be emitted first.
                    body.Instructions.Clear();
                    if (aroundInfo != null)
                    {
                        body.ExceptionHandlers.Clear();
                    }
                    var ilProcessor = body.GetILProcessor();

                    VariableDefinition methodBase = null;
                    if (advices.Any(v => v.HasPointcutMethod))
                    {
                        TypeReference declaringTypeInstance = method.DeclaringType;
                        MethodReference methodInstance = method;
                        if (method.DeclaringType.IsGenericDefinition())
                        {
                            declaringTypeInstance = declaringTypeInstance.MakeGenericInstanceType(method.DeclaringType.GenericParameters);
                            declaringTypeInstance = _mainModule.ImportReference(declaringTypeInstance);
                            methodInstance = new MethodReference(method.Name, method.ReturnType, declaringTypeInstance);
                            methodInstance.HasThis = method.HasThis;
                            methodInstance.ExplicitThis = method.ExplicitThis;
                            methodInstance.CallingConvention = method.CallingConvention;
                            foreach (var genericParameter in method.GenericParameters)
                            {
                                methodInstance.GenericParameters.Add(new GenericParameter(genericParameter.Name, methodInstance));
                            }
                            foreach (var p in method.Parameters)
                            {
                                methodInstance.Parameters.Add(new ParameterDefinition(p.Name, p.Attributes, p.ParameterType));
                            }
                        }

                        if (method.IsGenericDefinition())
                        {
                            methodInstance = methodInstance.MakeGenericInstanceMethod(method.GenericParameters);
                            methodInstance = _mainModule.ImportReference(methodInstance);
                        }

                        ilProcessor.Emit(OpCodes.Ldtoken, methodInstance);
                        ilProcessor.Emit(OpCodes.Ldtoken, declaringTypeInstance);
                        ilProcessor.Emit(OpCodes.Call, _getMethodFromHandle);
                        methodBase = new VariableDefinition(_methodBase);
                        method.Body.Variables.Add(methodBase);
                        ilProcessor.Append(ILPPUtils.SetLocal(methodBase));
                    }

                    VariableDefinition parameterArrayTmp = null;
                    Instruction processStart = null;
                    if (hasPointcutParameters)
                    {
                        parameterArrayTmp = new VariableDefinition(_objectArray);
                        method.Body.Variables.Add(parameterArrayTmp);

                        ilProcessor.Append(ILPPUtils.LoadLiteral(method.Parameters.Count));
                        ilProcessor.Emit(OpCodes.Call, _objectArrayPoolRent);
                        ilProcessor.Append(ILPPUtils.SetLocal(parameterArrayTmp));

                        processStart = Instruction.Create(OpCodes.Nop);
                        ilProcessor.Append(processStart);
                    }

                    VariableDefinition parameterArray = null;
                    if (hasPointcutParameters)
                    {
                        parameterArray = new VariableDefinition(_parameterArray);
                        method.Body.Variables.Add(parameterArray);

                        ilProcessor.Append(ILPPUtils.LoadLiteral(method.Parameters.Count));
                        ilProcessor.Append(ILPPUtils.LoadLocal(parameterArrayTmp));
                        for (int i = 0; i < method.Parameters.Count; i++)
                        {
                            var parameter = method.Parameters[i];
                            ilProcessor.Emit(OpCodes.Dup);
                            ilProcessor.Append(ILPPUtils.LoadLiteral(i));
                            ilProcessor.Append(ILPPUtils.LoadArgument(parameter));
                            if (parameter.ParameterType.IsValueType || parameter.ParameterType.IsGenericParameter)
                            {
                                ilProcessor.Emit(OpCodes.Box, parameter.ParameterType);
                            }

                            ilProcessor.Emit(OpCodes.Stelem_Ref);
                        }

                        ilProcessor.Emit(OpCodes.Newobj, _parameterArrayCtor);
                        ilProcessor.Append(ILPPUtils.SetLocal(parameterArray));
                    }

                    if (aroundInfo != null)
                    {
                        EnsureAroundAspectInstances(ilProcessor,
                                                    method,
                                                    advices,
                                                    aroundAdvices,
                                                    aspectInstances,
                                                    methodBase,
                                                    parameterArray);
                    }

                    // Before advice
                    foreach (var advice in beforeAdvices)
                    {
                        if (aroundInfo != null && advice.Method.Resolve()?.IsConstructor == true)
                        {
                            continue;
                        }

                        AppendCallAdvice(ilProcessor, method, advice, aspectInstances, methodBase, null, parameterArray, null);
                    }

                    // Append the original method body, or the generated Around dispatcher.
                    Instruction originalStart;
                    if (aroundInfo != null)
                    {
                        originalStart = Instruction.Create(OpCodes.Nop);
                        ilProcessor.Append(originalStart);
                        originalInstructions = AppendAroundDispatcherCall(ilProcessor, method, aroundInfo, aspectInstances, methodBase, parameterArrayTmp, originalStart);
                    }
                    else
                    {
                        originalStart = originalInstructions[0];
                        foreach (var instruction in originalInstructions)
                        {
                            ilProcessor.Append(instruction);
                        }
                    }

                    var normalExit = Instruction.Create(OpCodes.Nop);
                    var processEnd = Instruction.Create(OpCodes.Nop);
                    if (needsNormalExit || hasAfterThrowing)
                    {
                        if (needsNormalExit)
                        {
                            RewriteReturns(method, originalInstructions, resultVariable, normalExit, hasAfterThrowing);
                        }

                        ilProcessor.Append(normalExit);
                    }

                    foreach (var advice in afterReturningAdvices)
                    {
                        AppendCallAdvice(ilProcessor, method, advice, aspectInstances, methodBase, resultVariable, parameterArray, null);
                    }

                    if (hasAfter || hasPointcutParameters)
                    {
                        ilProcessor.Emit(OpCodes.Leave, processEnd);
                    }
                    else if (needsNormalExit)
                    {
                        if (resultVariable != null)
                        {
                            ilProcessor.Append(ILPPUtils.LoadLocal(resultVariable));
                        }

                        ilProcessor.Emit(OpCodes.Ret);
                    }

                    Instruction afterThrowingStart = null;
                    Instruction afterThrowingEnd = null;

                    if (hasAfterThrowing)
                    {
                        afterThrowingStart = Instruction.Create(OpCodes.Nop);
                        ilProcessor.Append(afterThrowingStart);

                        var throwingGroups = afterThrowingAdvices.GroupBy(ExceptionType, TypeReferenceComparer.Default);
                        foreach (var throwingGroup in throwingGroups)
                        {
                            Instruction adviceEnd = null;
                            VariableDefinition exceptionVariable = null;
                            var hasExceptionResult = !throwingGroup.Key.Is(method.Module.TypeSystem.Object);
                            if (hasExceptionResult)
                            {
                                exceptionVariable = new VariableDefinition(throwingGroup.Key);
                                method.Body.Variables.Add(exceptionVariable);
                                adviceEnd = Instruction.Create(OpCodes.Nop);

                                ilProcessor.Emit(OpCodes.Dup);
                                ilProcessor.Emit(OpCodes.Isinst, throwingGroup.Key);
                                ilProcessor.Append(ILPPUtils.SetLocal(exceptionVariable));
                                ilProcessor.Append(ILPPUtils.LoadLocal(exceptionVariable));
                                ilProcessor.Emit(OpCodes.Brfalse, adviceEnd);
                            }

                            foreach (var advice in throwingGroup)
                            {
                                AppendCallAdvice(ilProcessor, method, advice, aspectInstances, methodBase, null, parameterArray, exceptionVariable);
                            }

                            if (hasExceptionResult)
                            {
                                ilProcessor.Append(adviceEnd);
                            }
                        }

                        ilProcessor.Emit(OpCodes.Pop);
                        ilProcessor.Emit(OpCodes.Rethrow);
                        if (hasAfter || hasPointcutParameters)
                        {
                            afterThrowingEnd = Instruction.Create(OpCodes.Nop);
                            ilProcessor.Append(afterThrowingEnd);
                        }
                    }

                    Instruction afterTryEnd = null;
                    Instruction parameterCleanupStart = null;
                    if (hasAfter)
                    {
                        afterTryEnd = Instruction.Create(OpCodes.Nop);
                        ilProcessor.Append(afterTryEnd);
                        foreach (var advice in afterAdvices)
                        {
                            AppendCallAdvice(ilProcessor, method, advice, aspectInstances, methodBase, null, parameterArray, null);
                        }

                        ilProcessor.Emit(OpCodes.Endfinally);
                        if (hasPointcutParameters)
                        {
                            parameterCleanupStart = Instruction.Create(OpCodes.Nop);
                            ilProcessor.Append(parameterCleanupStart);
                        }
                    }
                    else if (hasPointcutParameters)
                    {
                        if (afterThrowingEnd != null)
                        {
                            parameterCleanupStart = afterThrowingEnd;
                        }
                        else
                        {
                            parameterCleanupStart = Instruction.Create(OpCodes.Nop);
                            ilProcessor.Append(parameterCleanupStart);
                        }
                    }

                    if (hasPointcutParameters)
                    {
                        ilProcessor.Append(ILPPUtils.LoadLocal(parameterArrayTmp));
                        ilProcessor.Emit(OpCodes.Call, _objectArrayPoolReturn);
                        ilProcessor.Emit(OpCodes.Endfinally);
                    }

                    if (hasAfter || hasPointcutParameters)
                    {
                        ilProcessor.Append(processEnd);
                        if (resultVariable != null)
                        {
                            ilProcessor.Append(ILPPUtils.LoadLocal(resultVariable));
                        }

                        ilProcessor.Emit(OpCodes.Ret);
                    }

                    if (hasAfterThrowing)
                    {
                        body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
                        {
                            TryStart = originalStart,
                            TryEnd = normalExit,
                            HandlerStart = afterThrowingStart,
                            HandlerEnd = afterThrowingEnd,
                            CatchType = _exceptionType,
                        });
                    }

                    if (hasAfter)
                    {
                        body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally)
                        {
                            TryStart = originalStart,
                            TryEnd = afterTryEnd,
                            HandlerStart = afterTryEnd,
                            HandlerEnd = hasPointcutParameters ? parameterCleanupStart : processEnd,
                        });
                    }

                    if (hasPointcutParameters)
                    {
                        body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally)
                        {
                            TryStart = processStart,
                            TryEnd = parameterCleanupStart,
                            HandlerStart = parameterCleanupStart,
                            HandlerEnd = processEnd,
                        });
                    }
                }
            }
        }

        private bool AsyncExecutionSequenceAdviceProcessor(AsyncExecutionInfo info)
        {
            var hasError = false;
            var sourceMethod = info.SourceMethod;
            using (ThreadStaticArrayPool.Get(out var advices, _advices.Where(v => v.Scope == AdviceScope.AsyncExecutionSequence &&
                                                                                   v.Pointcuts.All(p => p.IsMatch(sourceMethod)))))
            {
                if (advices.Length <= 0 || ValidateAsyncExecutionAdvices(info, advices))
                {
                    return advices.Length > 0;
                }

                SplitAdvices(sourceMethod, advices,
                             out var beforeAdvices,
                             out var aroundAdvices,
                             out var afterReturningAdvices,
                             out var afterThrowingAdvices,
                             out var afterAdvices);
                if (aroundAdvices.Length > 0)
                {
                    ILPPUtils.LogError("ASPECT2610", "AspectForUnity", $"Around advice is not supported for AsyncExecutionSequence method \"{sourceMethod.FullName}\".", aroundAdvices[0].Method);
                    return true;
                }

                var body = info.MoveNext.Body;
                var originalInstructions = body.Instructions.ToArray();
                if (originalInstructions.Length <= 0)
                {
                    return false;
                }

                NormalizeShortBranches(body);

                var aspectFields = new Dictionary<TypeReference, FieldDefinition>(TypeReferenceComparer.Default);
                var ilProcessor = body.GetILProcessor();
                var originalStart = originalInstructions[0];

                var resume = Instruction.Create(OpCodes.Nop);
                ilProcessor.InsertBefore(originalStart, Instruction.Create(OpCodes.Ldarg_0));
                ilProcessor.InsertBefore(originalStart, Instruction.Create(OpCodes.Ldfld, info.StartedField));
                ilProcessor.InsertBefore(originalStart, Instruction.Create(OpCodes.Brtrue, resume));
                ilProcessor.InsertBefore(originalStart, Instruction.Create(OpCodes.Ldarg_0));
                ilProcessor.InsertBefore(originalStart, Instruction.Create(OpCodes.Ldc_I4_1));
                ilProcessor.InsertBefore(originalStart, Instruction.Create(OpCodes.Stfld, info.StartedField));

                VariableDefinition beforeParameters = null;
                VariableDefinition beforeObjectArray = null;
                Instruction beforeParameterTryStart = null;
                if (beforeAdvices.Any(v => v.HasPointcutParameters))
                {
                    beforeParameters = AppendStateMachineParameterArrayBefore(ilProcessor, body, originalStart, info,
                                                                              out beforeObjectArray,
                                                                              out beforeParameterTryStart);
                }

                foreach (var advice in beforeAdvices)
                {
                    AppendCallStateMachineAdvice(ilProcessor, info, advice, aspectFields, null, beforeParameters, null, originalStart);
                }

                ilProcessor.InsertBefore(originalStart, resume);
                if (beforeParameters != null)
                {
                    AppendStateMachineParameterArrayReturnFinally(ilProcessor, body,
                                                                  beforeObjectArray,
                                                                  beforeParameterTryStart,
                                                                  resume);
                }

                if (info.Kind == AsyncExecutionKind.Async)
                {
                    hasError = InjectAsyncCompletionAdvice(info,
                                                           aspectFields,
                                                           afterReturningAdvices,
                                                           afterThrowingAdvices,
                                                           afterAdvices);
                }
                else
                {
                    InjectIteratorCompletionAdvice(info, aspectFields, afterAdvices);
                    if (afterThrowingAdvices.Any())
                    {
                        InjectIteratorExceptionAdvice(info, aspectFields, afterThrowingAdvices, afterAdvices);
                    }
                }
            }

            return hasError;
        }

        private static void NormalizeShortBranches(MethodBody body)
        {
            foreach (var instruction in body.Instructions)
            {
                switch (instruction.OpCode.Code)
                {
                    case Code.Br_S:
                        instruction.OpCode = OpCodes.Br;
                        break;
                    case Code.Brfalse_S:
                        instruction.OpCode = OpCodes.Brfalse;
                        break;
                    case Code.Brtrue_S:
                        instruction.OpCode = OpCodes.Brtrue;
                        break;
                    case Code.Beq_S:
                        instruction.OpCode = OpCodes.Beq;
                        break;
                    case Code.Bge_S:
                        instruction.OpCode = OpCodes.Bge;
                        break;
                    case Code.Bge_Un_S:
                        instruction.OpCode = OpCodes.Bge_Un;
                        break;
                    case Code.Bgt_S:
                        instruction.OpCode = OpCodes.Bgt;
                        break;
                    case Code.Bgt_Un_S:
                        instruction.OpCode = OpCodes.Bgt_Un;
                        break;
                    case Code.Ble_S:
                        instruction.OpCode = OpCodes.Ble;
                        break;
                    case Code.Ble_Un_S:
                        instruction.OpCode = OpCodes.Ble_Un;
                        break;
                    case Code.Blt_S:
                        instruction.OpCode = OpCodes.Blt;
                        break;
                    case Code.Blt_Un_S:
                        instruction.OpCode = OpCodes.Blt_Un;
                        break;
                    case Code.Bne_Un_S:
                        instruction.OpCode = OpCodes.Bne_Un;
                        break;
                    case Code.Leave_S:
                        instruction.OpCode = OpCodes.Leave;
                        break;
                }
            }
        }

        private bool ValidateAsyncExecutionAdvices(AsyncExecutionInfo info, IEnumerable<AdviceInfo> advices)
        {
            var hasError = false;
            foreach (var advice in advices)
            {
                if (ValidateAdvice(info.SourceMethod, advice, info.AsyncResultType))
                {
                    hasError = true;
                }

                var adviceMethod = advice.Method.Resolve();
                if (info.SourceMethod.Parameters.Any(v => v.ParameterType.IsByReference) &&
                    adviceMethod.Parameters.Any(v => !HasPointcutAccessorAttribute(v)))
                {
                    ILPPUtils.LogError("ASPECT2605", "AspectForUnity", $"Advice \"{advice.Method.FullName}\" cannot bind by-reference parameters across an asynchronous execution sequence.", advice.Method);
                    hasError = true;
                }

                if (advice.PointcutThisType != null && info.ThisField == null)
                {
                    ILPPUtils.LogError("ASPECT2606", "AspectForUnity", $"Cannot bind PointcutThis for asynchronous method \"{info.SourceMethod.FullName}\" because its instance is not captured by the generated asynchronous execution.", advice.Method);
                    hasError = true;
                }

                foreach (var parameter in adviceMethod.Parameters.Where(v => !HasPointcutAccessorAttribute(v)))
                {
                    if (!info.ParameterFields.ContainsKey(info.SourceMethod.GetParameter(parameter.Name)?.Name ?? parameter.Name))
                    {
                        ILPPUtils.LogError("ASPECT2607", "AspectForUnity", $"Cannot bind parameter \"{parameter.Name}\" for asynchronous method \"{info.SourceMethod.FullName}\".", advice.Method);
                        hasError = true;
                    }
                }

                if (info.Kind == AsyncExecutionKind.Iterator && advice.PointcutReturnedType != null)
                {
                    ILPPUtils.LogError("ASPECT2608", "AspectForUnity", $"PointcutReturned is not supported for coroutine advice \"{advice.Method.FullName}\".", advice.Method);
                    hasError = true;
                }

                if (advice.JoinPoint == JoinPoint.AfterReturning &&
                    advice.PointcutReturnedType != null &&
                    info.Kind == AsyncExecutionKind.Async &&
                    info.AsyncResultType == null)
                {
                    ILPPUtils.LogError("ASPECT2609", "AspectForUnity", $"PointcutReturned cannot be used because asynchronous method \"{info.SourceMethod.FullName}\" has no result value.", advice.Method);
                    hasError = true;
                }
            }

            return hasError;
        }

        private void SplitAdvices(MethodDefinition method,
                                  IEnumerable<AdviceInfo> advices,
                                  out AdviceInfo[] beforeAdvices,
                                  out AdviceInfo[] aroundAdvices,
                                  out AdviceInfo[] afterReturningAdvices,
                                  out AdviceInfo[] afterThrowingAdvices,
                                  out AdviceInfo[] afterAdvices)
        {
            beforeAdvices = Array.Empty<AdviceInfo>();
            aroundAdvices = Array.Empty<AdviceInfo>();
            afterReturningAdvices = Array.Empty<AdviceInfo>();
            afterThrowingAdvices = Array.Empty<AdviceInfo>();
            afterAdvices = Array.Empty<AdviceInfo>();

            foreach (var adviceGroup in advices.GroupBy(v => v.JoinPoint))
            {
                var sorted = adviceGroup.OrderBy(v =>
                {
                    var adviceMethod = v.Method.Resolve();
                    return adviceMethod.IsConstructor ? 0 : 1;
                })
                .ThenBy(v => v.PointcutReturnedType?.IsByReference ?? false ? 1 : 0)
                .ThenBy(v =>
                {
                    var parameterBitMask = 0;
                    var adviceMethod = v.Method.Resolve();
                    for (int i = 0; i < method.Parameters.Count && i < 32; i++)
                    {
                        var methodParam = method.Parameters[i];
                        var adviceParam = adviceMethod.GetParameter(methodParam.Name);
                        if (adviceParam == null || HasPointcutAccessorAttribute(adviceParam))
                        {
                            continue;
                        }

                        if (adviceParam.ParameterType.IsByReference)
                        {
                            parameterBitMask |= 1 << i;
                        }
                    }

                    return parameterBitMask;
                });

                switch (adviceGroup.Key)
                {
                    case JoinPoint.Before:
                        beforeAdvices = sorted.ToArray();
                        break;
                    case JoinPoint.Around:
                        aroundAdvices = sorted.ToArray();
                        break;
                    case JoinPoint.AfterReturning:
                        afterReturningAdvices = sorted.ToArray();
                        break;
                    case JoinPoint.AfterThrowing:
                        afterThrowingAdvices = sorted.ToArray();
                        break;
                    case JoinPoint.After:
                        afterAdvices = sorted.ToArray();
                        break;
                }
            }
        }

        private bool InjectAsyncCompletionAdvice(AsyncExecutionInfo info,
                                                 Dictionary<TypeReference, FieldDefinition> aspectFields,
                                                 IReadOnlyList<AdviceInfo> afterReturningAdvices,
                                                 IReadOnlyList<AdviceInfo> afterThrowingAdvices,
                                                 IReadOnlyList<AdviceInfo> afterAdvices)
        {
            if (!afterReturningAdvices.Any() && !afterThrowingAdvices.Any() && !afterAdvices.Any())
            {
                return false;
            }

            var body = info.MoveNext.Body;
            var ilProcessor = body.GetILProcessor();
            foreach (var instruction in body.Instructions.ToArray())
            {
                if (!(instruction.Operand is MethodReference calledMethod))
                {
                    continue;
                }

                var isSetResult = IsAsyncBuilderCompletionMethod(calledMethod, info.BuilderField, "SetResult");
                var isSetException = IsAsyncBuilderCompletionMethod(calledMethod, info.BuilderField, "SetException");
                if (!isSetResult && !isSetException)
                {
                    continue;
                }

                var relevantAdvices = isSetResult
                    ? afterReturningAdvices.Concat(afterAdvices).ToArray()
                    : afterThrowingAdvices.Concat(afterAdvices).ToArray();
                if (relevantAdvices.Length <= 0)
                {
                    continue;
                }

                var argumentTypeOverrides = isSetResult &&
                                             calledMethod.Parameters.Count == 1 &&
                                             info.AsyncResultType != null
                    ? new[] { info.AsyncResultType }
                    : null;
                var argumentLocals = StoreCallArgumentsBefore(ilProcessor,
                                                               body,
                                                               instruction,
                                                               calledMethod,
                                                               argumentTypeOverrides,
                                                               keepValueTypeReceiverOnStack: false,
                                                               discardValueTypeReceiver: calledMethod.HasThis &&
                                                                                         calledMethod.DeclaringType.IsValueType);

                var completionEnd = Instruction.Create(OpCodes.Nop);
                InsertBefore(ilProcessor, instruction,
                             Instruction.Create(OpCodes.Ldarg_0),
                             Instruction.Create(OpCodes.Ldfld, info.CompletedField),
                             Instruction.Create(OpCodes.Brtrue, completionEnd),
                             Instruction.Create(OpCodes.Ldarg_0),
                             Instruction.Create(OpCodes.Ldc_I4_1),
                             Instruction.Create(OpCodes.Stfld, info.CompletedField));

                VariableDefinition parameters = null;
                VariableDefinition objectArray = null;
                Instruction parameterTryStart = null;
                if (relevantAdvices.Any(v => v.HasPointcutParameters))
                {
                    parameters = AppendStateMachineParameterArrayBefore(ilProcessor, body, instruction, info,
                                                                        out objectArray,
                                                                        out parameterTryStart);
                }

                VariableDefinition returned = null;
                VariableDefinition exception = null;
                if (isSetResult && calledMethod.Parameters.Count == 1)
                {
                    returned = argumentLocals[calledMethod.HasThis ? 1 : 0];
                }
                else if (isSetException && calledMethod.Parameters.Count == 1)
                {
                    exception = argumentLocals[calledMethod.HasThis ? 1 : 0];
                }

                if (isSetResult)
                {
                    foreach (var advice in afterReturningAdvices)
                    {
                        AppendCallStateMachineAdvice(ilProcessor, info, advice, aspectFields, returned, parameters, null, instruction);
                    }

                    foreach (var advice in afterAdvices)
                    {
                        AppendCallStateMachineAdvice(ilProcessor, info, advice, aspectFields, returned, parameters, null, instruction);
                    }
                }
                else
                {
                    foreach (var advice in afterThrowingAdvices)
                    {
                        AppendCallStateMachineAdvice(ilProcessor, info, advice, aspectFields, null, parameters, exception, instruction);
                    }

                    foreach (var advice in afterAdvices)
                    {
                        AppendCallStateMachineAdvice(ilProcessor, info, advice, aspectFields, null, parameters, null, instruction);
                    }
                }

                ilProcessor.InsertBefore(instruction, completionEnd);
                if (parameters != null)
                {
                    AppendStateMachineParameterArrayReturnFinally(ilProcessor, body,
                                                                  objectArray,
                                                                  parameterTryStart,
                                                                  completionEnd);
                }

                if (calledMethod.HasThis &&
                    calledMethod.DeclaringType.IsValueType &&
                    argumentLocals.Count > 0 &&
                    argumentLocals[0] == null)
                {
                    AppendLoadStateMachineField(ilProcessor, info.BuilderField, true, instruction);
                }

                ReloadCallArgumentsBefore(ilProcessor, instruction, calledMethod, argumentLocals);
            }

            return false;
        }

        private void InjectIteratorCompletionAdvice(AsyncExecutionInfo info,
                                                    Dictionary<TypeReference, FieldDefinition> aspectFields,
                                                    IReadOnlyList<AdviceInfo> afterAdvices)
        {
            if (!afterAdvices.Any())
            {
                return;
            }

            var body = info.MoveNext.Body;
            var ilProcessor = body.GetILProcessor();
            foreach (var instruction in body.Instructions.ToArray())
            {
                if (instruction.OpCode != OpCodes.Ret)
                {
                    continue;
                }

                var index = body.Instructions.IndexOf(instruction);
                if (index <= 0 || !IsLoadZero(body.Instructions[index - 1]))
                {
                    continue;
                }

                var completionInstruction = body.Instructions[index - 1];
                var reload = Instruction.Create(OpCodes.Nop);
                InsertBefore(ilProcessor, completionInstruction,
                             Instruction.Create(OpCodes.Ldarg_0),
                             Instruction.Create(OpCodes.Ldfld, info.CompletedField),
                             Instruction.Create(OpCodes.Brtrue, reload),
                             Instruction.Create(OpCodes.Ldarg_0),
                             Instruction.Create(OpCodes.Ldc_I4_1),
                             Instruction.Create(OpCodes.Stfld, info.CompletedField));

                VariableDefinition parameters = null;
                VariableDefinition objectArray = null;
                Instruction parameterTryStart = null;
                if (afterAdvices.Any(v => v.HasPointcutParameters))
                {
                    parameters = AppendStateMachineParameterArrayBefore(ilProcessor, body, completionInstruction, info,
                                                                        out objectArray,
                                                                        out parameterTryStart);
                }

                foreach (var advice in afterAdvices)
                {
                    AppendCallStateMachineAdvice(ilProcessor, info, advice, aspectFields, null, parameters, null, completionInstruction);
                }

                ilProcessor.InsertBefore(completionInstruction, reload);
                if (parameters != null)
                {
                    AppendStateMachineParameterArrayReturnFinally(ilProcessor, body,
                                                                  objectArray,
                                                                  parameterTryStart,
                                                                  reload);
                }
            }
        }

        private void InjectIteratorExceptionAdvice(AsyncExecutionInfo info,
                                                   Dictionary<TypeReference, FieldDefinition> aspectFields,
                                                   IReadOnlyList<AdviceInfo> afterThrowingAdvices,
                                                   IReadOnlyList<AdviceInfo> afterAdvices)
        {
            if (!afterThrowingAdvices.Any() && !afterAdvices.Any())
            {
                return;
            }

            var body = info.MoveNext.Body;
            var ilProcessor = body.GetILProcessor();
            var tryStart = body.Instructions[0];
            var tryEnd = Instruction.Create(OpCodes.Nop);
            var handlerStart = Instruction.Create(OpCodes.Nop);
            var handlerEnd = Instruction.Create(OpCodes.Nop);
            var returnLabel = Instruction.Create(OpCodes.Nop);
            var returnValue = new VariableDefinition(info.MoveNext.ReturnType);
            body.Variables.Add(returnValue);

            foreach (var instruction in body.Instructions.ToArray())
            {
                if (instruction.OpCode != OpCodes.Ret)
                {
                    continue;
                }

                ilProcessor.InsertBefore(instruction, ILPPUtils.SetLocal(returnValue));
                instruction.OpCode = OpCodes.Leave;
                instruction.Operand = returnLabel;
            }

            var exception = new VariableDefinition(_exceptionType);
            body.Variables.Add(exception);

            ilProcessor.Append(tryEnd);
            ilProcessor.Append(handlerStart);
            ilProcessor.Emit(OpCodes.Stloc, exception);
            ilProcessor.Append(handlerEnd);

            var skip = Instruction.Create(OpCodes.Nop);
            InsertBefore(ilProcessor, handlerEnd,
                         Instruction.Create(OpCodes.Ldarg_0),
                         Instruction.Create(OpCodes.Ldfld, info.CompletedField),
                         Instruction.Create(OpCodes.Brtrue, skip),
                         Instruction.Create(OpCodes.Ldarg_0),
                         Instruction.Create(OpCodes.Ldc_I4_1),
                         Instruction.Create(OpCodes.Stfld, info.CompletedField));

            VariableDefinition parameters = null;
            VariableDefinition objectArray = null;
            Instruction parameterTryStart = null;
            if (afterThrowingAdvices.Concat(afterAdvices).Any(v => v.HasPointcutParameters))
            {
                parameters = AppendStateMachineParameterArrayBefore(ilProcessor, body, handlerEnd, info,
                                                                    out objectArray,
                                                                    out parameterTryStart);
            }

            foreach (var advice in afterThrowingAdvices)
            {
                AppendCallStateMachineAdvice(ilProcessor, info, advice, aspectFields, null, parameters, exception, handlerEnd);
            }

            foreach (var advice in afterAdvices)
            {
                AppendCallStateMachineAdvice(ilProcessor, info, advice, aspectFields, null, parameters, null, handlerEnd);
            }

            ilProcessor.InsertBefore(handlerEnd, skip);
            if (parameters != null)
            {
                AppendStateMachineParameterArrayReturnFinally(ilProcessor, body,
                                                              objectArray,
                                                              parameterTryStart,
                                                              skip);
            }
            ilProcessor.InsertBefore(handlerEnd, Instruction.Create(OpCodes.Rethrow));
            ilProcessor.Append(returnLabel);
            ilProcessor.Append(ILPPUtils.LoadLocal(returnValue));
            ilProcessor.Emit(OpCodes.Ret);
            body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                TryStart = tryStart,
                TryEnd = tryEnd,
                HandlerStart = handlerStart,
                HandlerEnd = handlerEnd,
                CatchType = _exceptionType,
            });
        }

        private VariableDefinition AppendStateMachineParameterArrayBefore(ILProcessor ilProcessor,
                                                                            MethodBody body,
                                                                            Instruction before,
                                                                            AsyncExecutionInfo info,
                                                                            out VariableDefinition objectArray,
                                                                            out Instruction tryStart)
        {
            objectArray = new VariableDefinition(_objectArray);
            body.Variables.Add(objectArray);
            InsertBefore(ilProcessor, before,
                         ILPPUtils.LoadLiteral(info.SourceMethod.Parameters.Count),
                         Instruction.Create(OpCodes.Call, _objectArrayPoolRent),
                         ILPPUtils.SetLocal(objectArray));
            tryStart = Instruction.Create(OpCodes.Nop);
            ilProcessor.InsertBefore(before, tryStart);

            for (int i = 0; i < info.SourceMethod.Parameters.Count; i++)
            {
                var parameter = info.SourceMethod.Parameters[i];
                var field = info.ParameterFields[parameter.Name];
                InsertBefore(ilProcessor, before,
                             ILPPUtils.LoadLocal(objectArray),
                             ILPPUtils.LoadLiteral(i));
                var loadInstructions = new List<Instruction>();
                loadInstructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                loadInstructions.Add(Instruction.Create(OpCodes.Ldfld, field));
                var fieldType = field.FieldType.IsByReference ? ((ByReferenceType)field.FieldType).ElementType : field.FieldType;
                if (fieldType.IsValueType || fieldType.IsGenericParameter)
                {
                    loadInstructions.Add(Instruction.Create(OpCodes.Box, fieldType));
                }

                loadInstructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
                InsertBefore(ilProcessor, before, loadInstructions.ToArray());
            }

            var parameters = new VariableDefinition(_parameterArray);
            body.Variables.Add(parameters);
            InsertBefore(ilProcessor, before,
                         ILPPUtils.LoadLiteral(info.SourceMethod.Parameters.Count),
                         ILPPUtils.LoadLocal(objectArray),
                         Instruction.Create(OpCodes.Newobj, _parameterArrayCtor),
                         ILPPUtils.SetLocal(parameters));
            return parameters;
        }

        private void AppendStateMachineParameterArrayReturnFinally(ILProcessor ilProcessor,
                                                                     MethodBody body,
                                                                     VariableDefinition objectArray,
                                                                     Instruction tryStart,
                                                                     Instruction end)
        {
            var finallyStart = Instruction.Create(OpCodes.Nop);
            InsertBefore(ilProcessor, end,
                         Instruction.Create(OpCodes.Leave, end),
                         finallyStart,
                         ILPPUtils.LoadLocal(objectArray),
                         Instruction.Create(OpCodes.Call, _objectArrayPoolReturn),
                         Instruction.Create(OpCodes.Endfinally));

            body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally)
            {
                TryStart = tryStart,
                TryEnd = finallyStart,
                HandlerStart = finallyStart,
                HandlerEnd = end,
            });
        }

        private static bool IsLoadZero(Instruction instruction)
        {
            return instruction.OpCode == OpCodes.Ldc_I4_0;
        }

        private static void InsertBefore(ILProcessor ilProcessor, Instruction target, params Instruction[] instructions)
        {
            foreach (var instruction in instructions)
            {
                ilProcessor.InsertBefore(target, instruction);
            }
        }

        private List<VariableDefinition> StoreCallArgumentsBefore(ILProcessor ilProcessor,
                                                                    MethodBody body,
                                                                    Instruction before,
                                                                    MethodReference method,
                                                                    IReadOnlyList<TypeReference> parameterTypeOverrides = null,
                                                                    bool keepValueTypeReceiverOnStack = false,
                                                                    bool discardValueTypeReceiver = false)
        {
            var result = new List<VariableDefinition>();
            for (int i = method.Parameters.Count - 1; i >= 0; i--)
            {
                var parameter = method.Parameters[i];
                var localType = parameterTypeOverrides != null && i < parameterTypeOverrides.Count && parameterTypeOverrides[i] != null
                    ? parameterTypeOverrides[i]
                    : parameter.ParameterType;
                var local = new VariableDefinition(localType);
                body.Variables.Add(local);
                ilProcessor.InsertBefore(before, ILPPUtils.SetLocal(local));
                result.Insert(0, local);
            }

            if (method.HasThis && keepValueTypeReceiverOnStack && method.DeclaringType.IsValueType)
            {
                result.Insert(0, null);
            }
            else if (method.HasThis && discardValueTypeReceiver && method.DeclaringType.IsValueType)
            {
                ilProcessor.InsertBefore(before, Instruction.Create(OpCodes.Pop));
                result.Insert(0, null);
            }
            else if (method.HasThis)
            {
                var receiverType = method.DeclaringType.IsValueType
                    ? new ByReferenceType(method.DeclaringType)
                    : method.DeclaringType;
                var receiver = new VariableDefinition(receiverType);
                body.Variables.Add(receiver);
                ilProcessor.InsertBefore(before, ILPPUtils.SetLocal(receiver));
                result.Insert(0, receiver);
            }

            return result;
        }

        private static void ReloadCallArgumentsBefore(ILProcessor ilProcessor,
                                                       Instruction before,
                                                       MethodReference method,
                                                       IReadOnlyList<VariableDefinition> locals)
        {
            var offset = 0;
            if (method.HasThis)
            {
                var receiver = locals[offset++];
                if (receiver != null)
                {
                    ilProcessor.InsertBefore(before, ILPPUtils.LoadLocal(receiver));
                }
            }

            for (int i = 0; i < method.Parameters.Count; i++)
            {
                ilProcessor.InsertBefore(before, ILPPUtils.LoadLocal(locals[offset++]));
            }
        }

        private void AppendLoadStateMachineField(ILProcessor ilProcessor,
                                                 FieldDefinition field,
                                                 bool address,
                                                 Instruction insertBefore = null)
        {
            var loadThis = Instruction.Create(OpCodes.Ldarg_0);
            var loadField = Instruction.Create(address ? OpCodes.Ldflda : OpCodes.Ldfld, field);
            if (insertBefore == null)
            {
                ilProcessor.Append(loadThis);
                ilProcessor.Append(loadField);
            }
            else
            {
                ilProcessor.InsertBefore(insertBefore, loadThis);
                ilProcessor.InsertBefore(insertBefore, loadField);
            }
        }

        private void AppendCallStateMachineAdvice(ILProcessor ilProcessor,
                                                   AsyncExecutionInfo info,
                                                   AdviceInfo adviceInfo,
                                                   Dictionary<TypeReference, FieldDefinition> aspectFields,
                                                   VariableDefinition returned,
                                                   VariableDefinition parameters,
                                                   VariableDefinition exception,
                                                   Instruction insertBefore = null)
        {
            void Add(Instruction instruction)
            {
                if (insertBefore == null)
                {
                    ilProcessor.Append(instruction);
                }
                else
                {
                    ilProcessor.InsertBefore(insertBefore, instruction);
                }
            }

            var sourceMethod = info.SourceMethod;
            var adviceMethod = adviceInfo.Method.Resolve();
            var adviceMethodRef = adviceInfo.Method;
            var aspectType = sourceMethod.Module.ImportReference(adviceMethod.DeclaringType);

            if (adviceMethod.HasGenericParameters)
            {
                using (ThreadStaticListPool<TypeReference>.Get(out var genericArguments))
                {
                    foreach (var adviceGenericParameter in adviceMethod.GenericParameters)
                    {
                        var binding = GetBinding(adviceGenericParameter);
                        TypeReference genericArgument = null;
                        switch (binding)
                        {
                            case GenericBinding.GenericParameterName:
                                genericArgument = FindBoundGeneicArgumentByName(sourceMethod, adviceGenericParameter.Name);
                                break;
                            case GenericBinding.ParameterType:
                                using (ThreadStaticListPool<TypeReference>.Get(out var genericArgumentsByParameter))
                                {
                                    FindBoundGeneicArgumentByParameterType(sourceMethod, adviceMethod, adviceGenericParameter, genericArgumentsByParameter);
                                    genericArgument = genericArgumentsByParameter.FirstOrDefault();
                                }
                                break;
                        }

                        if (genericArgument == null)
                        {
                            return;
                        }

                        genericArguments.Add(genericArgument);
                    }

                    adviceMethodRef = adviceMethodRef.MakeGenericInstanceMethod(genericArguments);
                    adviceMethodRef = _mainModule.ImportReference(adviceMethodRef);
                }
            }

            FieldDefinition instanceField = null;
            if (adviceMethod.HasThis)
            {
                if (!aspectFields.TryGetValue(aspectType, out instanceField))
                {
                    if (!adviceMethod.IsConstructor)
                    {
                        return;
                    }

                    instanceField = AddGeneratedField(info.StateMachineType,
                                                      $"$AspectForUnityAspect_{info.SourceMethod.MetadataToken.ToInt32():X8}_{aspectFields.Count}",
                                                      aspectType);
                    aspectFields.Add(aspectType, instanceField);
                }

                if (!adviceMethod.IsConstructor)
                {
                    AppendLoadStateMachineField(ilProcessor,
                                                instanceField,
                                                aspectType.IsValueType,
                                                insertBefore);
                }
            }

            foreach (var adviceParameter in adviceMethod.Parameters)
            {
                var adviceParameterType = adviceParameter.ParameterType;
                TypeReference parameterType;
                if (HasPointcutReturned(adviceParameter))
                {
                    if (returned == null)
                    {
                        return;
                    }

                    if (returned.VariableType.IsByReference == adviceParameterType.IsByReference)
                    {
                        Add(ILPPUtils.LoadLocal(returned));
                    }
                    else if (returned.VariableType is ByReferenceType returnedByReference)
                    {
                        Add(ILPPUtils.LoadLocal(returned));
                        Add(ILPPUtils.LoadIndirect(returnedByReference.ElementType));
                    }
                    else
                    {
                        Add(ILPPUtils.LoadLocalAddress(returned));
                    }

                    parameterType = returned.VariableType;
                }
                else if (HasPointcutThrown(adviceParameter))
                {
                    if (exception == null)
                    {
                        return;
                    }

                    Add(ILPPUtils.LoadLocal(exception));
                    parameterType = exception.VariableType;
                }
                else if (HasPointcutMethod(adviceParameter))
                {
                    AppendLoadSourceMethodBase(ilProcessor, sourceMethod, insertBefore);
                    parameterType = adviceParameter.ParameterType;
                }
                else if (HasPointcutParameters(adviceParameter))
                {
                    if (parameters == null)
                    {
                        return;
                    }

                    Add(ILPPUtils.LoadLocal(parameters));
                    parameterType = adviceParameter.ParameterType;
                }
                else if (HasPointcutThis(adviceParameter))
                {
                    if (info.ThisField == null)
                    {
                        return;
                    }

                    AppendLoadStateMachineField(ilProcessor, info.ThisField, false, insertBefore);
                    parameterType = info.ThisField.FieldType;
                }
                else
                {
                    var parameter = sourceMethod.GetParameter(adviceParameter.Name);
                    if (parameter == null || !info.ParameterFields.TryGetValue(parameter.Name, out var field))
                    {
                        return;
                    }

                    AppendLoadStateMachineField(ilProcessor, field, adviceParameterType.IsByReference, insertBefore);
                    parameterType = field.FieldType;
                }

                if (parameterType is ByReferenceType byReferenceType)
                {
                    parameterType = byReferenceType.ElementType;
                }

                if (!adviceParameterType.IsByReference && parameterType.IsBoxingRequired(adviceParameterType))
                {
                    Add(Instruction.Create(OpCodes.Box, parameterType));
                }
            }

            if (adviceMethod.IsConstructor)
            {
                Add(Instruction.Create(OpCodes.Newobj, adviceMethodRef));
                var instance = new VariableDefinition(aspectType);
                info.MoveNext.Body.Variables.Add(instance);
                Add(ILPPUtils.SetLocal(instance));
                Add(Instruction.Create(OpCodes.Ldarg_0));
                Add(ILPPUtils.LoadLocal(instance));
                Add(Instruction.Create(OpCodes.Stfld, instanceField));
            }
            else
            {
                Add(Instruction.Create(OpCodes.Call, adviceMethodRef));
            }
        }

        private void AppendLoadSourceMethodBase(ILProcessor ilProcessor, MethodDefinition method, Instruction insertBefore = null)
        {
            void Add(Instruction instruction)
            {
                if (insertBefore == null)
                {
                    ilProcessor.Append(instruction);
                }
                else
                {
                    ilProcessor.InsertBefore(insertBefore, instruction);
                }
            }

            TypeReference declaringTypeInstance = method.DeclaringType;
            MethodReference methodInstance = method;
            if (method.DeclaringType.IsGenericDefinition())
            {
                declaringTypeInstance = _mainModule.ImportReference(declaringTypeInstance.MakeGenericInstanceType(method.DeclaringType.GenericParameters));
                methodInstance = new MethodReference(method.Name, method.ReturnType, declaringTypeInstance)
                {
                    HasThis = method.HasThis,
                    ExplicitThis = method.ExplicitThis,
                    CallingConvention = method.CallingConvention,
                };
                foreach (var genericParameter in method.GenericParameters)
                {
                    methodInstance.GenericParameters.Add(new GenericParameter(genericParameter.Name, methodInstance));
                }

                foreach (var parameter in method.Parameters)
                {
                    methodInstance.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, parameter.ParameterType));
                }
            }

            if (method.IsGenericDefinition())
            {
                methodInstance = _mainModule.ImportReference(methodInstance.MakeGenericInstanceMethod(method.GenericParameters));
            }

            Add(Instruction.Create(OpCodes.Ldtoken, methodInstance));
            Add(Instruction.Create(OpCodes.Ldtoken, declaringTypeInstance));
            Add(Instruction.Create(OpCodes.Call, _getMethodFromHandle));
        }

        private void RewriteReturns(MethodDefinition method,
                                    IReadOnlyList<Instruction> originalInstructions,
                                    VariableDefinition result,
                                    Instruction normalExit,
                                    bool forceLeave)
        {
            var ilProcessor = method.Body.GetILProcessor();
            foreach (var instruction in originalInstructions)
            {
                if (instruction.OpCode != OpCodes.Ret)
                {
                    continue;
                }

                var branchOpCode = forceLeave || IsInExceptionRegion(method.Body, instruction)
                    ? OpCodes.Leave
                    : OpCodes.Br;
                if (result != null)
                {
                    instruction.OpCode = OpCodes.Stloc;
                    instruction.Operand = result;
                    ilProcessor.InsertAfter(instruction,
                        Instruction.Create(branchOpCode, normalExit));
                }
                else
                {
                    instruction.OpCode = branchOpCode;
                    instruction.Operand = normalExit;
                }
            }
        }

        private bool IsInExceptionRegion(MethodBody body, Instruction instruction)
        {
            foreach (var exceptionHandler in body.ExceptionHandlers)
            {
                if (IsInExceptionRange(body, instruction,
                                       exceptionHandler.TryStart, exceptionHandler.TryEnd) ||
                    IsInExceptionRange(body, instruction,
                                       exceptionHandler.HandlerStart, exceptionHandler.HandlerEnd) ||
                    IsInExceptionRange(body, instruction,
                                       exceptionHandler.FilterStart, exceptionHandler.HandlerStart))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsInExceptionRange(MethodBody body,
                                        Instruction instruction,
                                        Instruction start,
                                        Instruction end)
        {
            if (start == null || end == null)
            {
                return false;
            }

            var instructionIndex = body.Instructions.IndexOf(instruction);
            var startIndex = body.Instructions.IndexOf(start);
            var endIndex = body.Instructions.IndexOf(end);
            return startIndex >= 0 && endIndex >= 0 &&
                   instructionIndex >= startIndex && instructionIndex < endIndex;
        }

        private TypeReference ExceptionType(AdviceInfo advice)
        {
            if (advice.PointcutThrownType == null)
            {
                return advice.Method.Module.TypeSystem.Object;
            }

            return advice.PointcutThrownType;
        }

        private MethodDefinition ExtractOriginalMethod(TypeDefinition declaringType, MethodDefinition method)
        {
            var originalName = $"$AspectOriginal_{method.Name}_{method.MetadataToken.ToInt32():X8}";
            var original = new MethodDefinition(originalName,
                                                 MethodAttributes.Assembly | MethodAttributes.HideBySig | (method.IsStatic ? MethodAttributes.Static : 0),
                                                 method.ReturnType)
            {
                ImplAttributes = method.ImplAttributes,
                CallingConvention = method.CallingConvention,
            };

            var genericParameterMap = new Dictionary<GenericParameter, TypeReference>();
            foreach (var genericParameter in method.GenericParameters)
            {
                var clone = new GenericParameter(genericParameter.Name, original)
                {
                    Attributes = genericParameter.Attributes,
                };
                original.GenericParameters.Add(clone);
                genericParameterMap.Add(genericParameter, clone);
            }

            original.ReturnType = SubstituteType(method.ReturnType, genericParameterMap);
            foreach (var parameter in method.Parameters)
            {
                original.Parameters.Add(new ParameterDefinition(parameter.Name,
                                                                 parameter.Attributes,
                                                                 SubstituteType(parameter.ParameterType, genericParameterMap)));
            }

            for (int i = 0; i < method.GenericParameters.Count; i++)
            {
                foreach (var constraint in method.GenericParameters[i].Constraints)
                {
                    original.GenericParameters[i].Constraints.Add(new GenericParameterConstraint(SubstituteType(constraint.ConstraintType, genericParameterMap)));
                }
            }

            var parameterMap = new Dictionary<ParameterDefinition, ParameterDefinition>();
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                parameterMap.Add(method.Parameters[i], original.Parameters[i]);
            }

            var variableMap = new Dictionary<VariableDefinition, VariableDefinition>();
            foreach (var variable in method.Body.Variables)
            {
                var clone = new VariableDefinition(SubstituteType(variable.VariableType, genericParameterMap));
                original.Body.Variables.Add(clone);
                variableMap.Add(variable, clone);
            }

            var instructionMap = new Dictionary<Instruction, Instruction>();
            foreach (var instruction in method.Body.Instructions)
            {
                var clone = CreateInstruction(instruction.OpCode, instruction.Operand);
                instructionMap.Add(instruction, clone);
                original.Body.Instructions.Add(clone);
            }

            foreach (var instruction in method.Body.Instructions)
            {
                var clone = instructionMap[instruction];
                clone.Operand = CloneInstructionOperand(instruction.Operand,
                                                         instructionMap,
                                                         parameterMap,
                                                         variableMap,
                                                         genericParameterMap,
                                                         method,
                                                         original);
            }

            foreach (var exceptionHandler in method.Body.ExceptionHandlers)
            {
                original.Body.ExceptionHandlers.Add(new ExceptionHandler(exceptionHandler.HandlerType)
                {
                    CatchType = SubstituteType(exceptionHandler.CatchType, genericParameterMap),
                    TryStart = instructionMap[exceptionHandler.TryStart],
                    TryEnd = exceptionHandler.TryEnd == null ? null : instructionMap[exceptionHandler.TryEnd],
                    FilterStart = exceptionHandler.FilterStart == null ? null : instructionMap[exceptionHandler.FilterStart],
                    HandlerStart = instructionMap[exceptionHandler.HandlerStart],
                    HandlerEnd = exceptionHandler.HandlerEnd == null ? null : instructionMap[exceptionHandler.HandlerEnd],
                });
            }

            original.Body.InitLocals = method.Body.InitLocals;
            original.Body.MaxStackSize = method.Body.MaxStackSize;
            declaringType.Methods.Add(original);
            return original;
        }

        private Instruction CreateInstruction(OpCode opcode, object operand)
        {
            switch (opcode.OperandType)
            {
                case OperandType.InlineNone:
                    return Instruction.Create(opcode);
                case OperandType.ShortInlineI:
                    return Instruction.Create(opcode, Convert.ToSByte(operand));
                case OperandType.InlineI:
                    return Instruction.Create(opcode, Convert.ToInt32(operand));
                case OperandType.InlineI8:
                    return Instruction.Create(opcode, Convert.ToInt64(operand));
                case OperandType.ShortInlineR:
                    return Instruction.Create(opcode, Convert.ToSingle(operand));
                case OperandType.InlineR:
                    return Instruction.Create(opcode, Convert.ToDouble(operand));
                case OperandType.ShortInlineBrTarget:
                case OperandType.InlineBrTarget:
                    return Instruction.Create(opcode, (Instruction)operand);
                case OperandType.InlineSwitch:
                    return Instruction.Create(opcode, (Instruction[])operand);
                case OperandType.InlineString:
                    return Instruction.Create(opcode, (string)operand);
                case OperandType.InlineField:
                    return Instruction.Create(opcode, (FieldReference)operand);
                case OperandType.InlineMethod:
                    if (operand is not MethodReference methodReference)
                    {
                        throw new InvalidOperationException($"InlineMethod operand was not a method for opcode {opcode}.");
                    }

                    return Instruction.Create(opcode, methodReference);
                case OperandType.InlineSig:
                    return Instruction.Create(opcode, (CallSite)operand);
                case OperandType.InlineType:
                    return Instruction.Create(opcode, (TypeReference)operand);
                case OperandType.InlineTok:
                    if (operand is TypeReference type)
                    {
                        return Instruction.Create(opcode, type);
                    }

                    if (operand is FieldReference field)
                    {
                        return Instruction.Create(opcode, field);
                    }

                    return Instruction.Create(opcode, (MethodReference)operand);
                case OperandType.ShortInlineVar:
                case OperandType.InlineVar:
                    if (operand is ParameterDefinition parameter)
                    {
                        return Instruction.Create(opcode, parameter);
                    }

                    return Instruction.Create(opcode, (VariableDefinition)operand);
                default:
                    throw new NotSupportedException($"Unsupported Cecil operand type: {opcode.OperandType}.");
            }
        }

        private object CloneInstructionOperand(object operand,
                                                Dictionary<Instruction, Instruction> instructions,
                                                Dictionary<ParameterDefinition, ParameterDefinition> parameters,
                                                Dictionary<VariableDefinition, VariableDefinition> variables,
                                                IDictionary<GenericParameter, TypeReference> genericParameters,
                                                MethodDefinition sourceMethod,
                                                MethodDefinition clonedMethod)
        {
            if (operand == null)
            {
                return null;
            }

            if (operand is Instruction instruction)
            {
                return instructions[instruction];
            }

            if (operand is Instruction[] instructionArray)
            {
                return instructionArray.Select(v => instructions[v]).ToArray();
            }

            if (operand is ParameterDefinition parameter && parameters.TryGetValue(parameter, out var clonedParameter))
            {
                return clonedParameter;
            }

            if (operand is VariableDefinition variable && variables.TryGetValue(variable, out var clonedVariable))
            {
                return clonedVariable;
            }

            if (operand is MethodReference methodReference)
            {
                if (methodReference.Resolve() == sourceMethod)
                {
                    return CreateMethodReference(clonedMethod, clonedMethod.DeclaringType, new Dictionary<GenericParameter, TypeReference>());
                }

                return CloneMethodReference(methodReference, genericParameters);
            }

            if (operand is FieldReference fieldReference)
            {
                return new FieldReference(fieldReference.Name,
                                          SubstituteType(fieldReference.FieldType, genericParameters),
                                          SubstituteType(fieldReference.DeclaringType, genericParameters));
            }

            if (operand is TypeReference typeReference)
            {
                return SubstituteType(typeReference, genericParameters);
            }

            return operand;
        }

        private MethodReference CloneMethodReference(MethodReference source,
                                                      IDictionary<GenericParameter, TypeReference> genericParameters)
        {
            if (source is GenericInstanceMethod genericInstanceMethod)
            {
                var element = CloneMethodReference(genericInstanceMethod.ElementMethod, genericParameters);
                var result = new GenericInstanceMethod(element);
                foreach (var argument in genericInstanceMethod.GenericArguments)
                {
                    result.GenericArguments.Add(SubstituteType(argument, genericParameters));
                }

                return result;
            }

            var resultMethod = new MethodReference(source.Name,
                                                   SubstituteType(source.ReturnType, genericParameters),
                                                   SubstituteType(source.DeclaringType, genericParameters))
            {
                HasThis = source.HasThis,
                ExplicitThis = source.ExplicitThis,
                CallingConvention = source.CallingConvention,
            };
            var localGenericParameters = new Dictionary<GenericParameter, TypeReference>(genericParameters);
            foreach (var genericParameter in source.GenericParameters)
            {
                var clone = new GenericParameter(genericParameter.Name, resultMethod)
                {
                    Attributes = genericParameter.Attributes,
                };
                resultMethod.GenericParameters.Add(clone);
                localGenericParameters[genericParameter] = clone;
            }

            resultMethod.ReturnType = SubstituteType(source.ReturnType, localGenericParameters);
            foreach (var parameter in source.Parameters)
            {
                resultMethod.Parameters.Add(new ParameterDefinition(parameter.Name,
                                                                    parameter.Attributes,
                                                                    SubstituteType(parameter.ParameterType, localGenericParameters)));
            }

            return resultMethod;
        }

        private AroundGenerationInfo CreateAroundGenerationInfo(TypeDefinition declaringType,
                                                                MethodDefinition method,
                                                                MethodDefinition originalMethod,
                                                                AdviceInfo[] aroundAdvices)
        {
            var stateType = new TypeDefinition(declaringType.Namespace,
                                                $"$AspectAroundState_{method.Name}_{method.MetadataToken.ToInt32():X8}",
                                                TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                                                _mainModule.ImportReference(typeof(ProceedingStateBase)));
            _mainModule.Types.Add(stateType);

            var genericParameterMap = new Dictionary<GenericParameter, TypeReference>();
            foreach (var genericParameter in declaringType.GenericParameters.Concat(method.GenericParameters))
            {
                var clone = new GenericParameter(genericParameter.Name, stateType)
                {
                    Attributes = genericParameter.Attributes,
                };
                stateType.GenericParameters.Add(clone);
                genericParameterMap.Add(genericParameter, clone);
            }

            foreach (var genericParameter in declaringType.GenericParameters.Concat(method.GenericParameters))
            {
                var clone = (GenericParameter)genericParameterMap[genericParameter];
                foreach (var constraint in genericParameter.Constraints)
                {
                    clone.Constraints.Add(new GenericParameterConstraint(SubstituteType(constraint.ConstraintType, genericParameterMap)));
                }
            }

            TypeReference stateTypeInstance = stateType;
            if (stateType.HasGenericParameters)
            {
                var genericInstance = new GenericInstanceType(stateType);
                foreach (var genericParameter in stateType.GenericParameters)
                {
                    genericInstance.GenericArguments.Add(genericParameter);
                }

                stateTypeInstance = genericInstance;
            }

            TypeReference callStateTypeInstance = stateType;
            if (stateType.HasGenericParameters)
            {
                var genericInstance = new GenericInstanceType(stateType);
                foreach (var genericParameter in declaringType.GenericParameters.Concat(method.GenericParameters))
                {
                    genericInstance.GenericArguments.Add(genericParameter);
                }

                callStateTypeInstance = genericInstance;
            }

            var result = new AroundGenerationInfo
            {
                StateType = stateType,
                StateTypeInstance = stateTypeInstance,
                CallStateTypeInstance = callStateTypeInstance,
                GenericParameterMap = genericParameterMap,
                HasReturn = method.HasReturn(),
                HasPointcutParameters = aroundAdvices.Any(v => v.HasPointcutParameters),
                HasPointcutMethod = aroundAdvices.Any(v => v.HasPointcutMethod),
                ReturnsByReference = method.ReturnType is ByReferenceType,
                AspectFields = new Dictionary<TypeReference, FieldDefinition>(TypeReferenceComparer.Default),
            };

            result.ReturnElementType = method.ReturnType is ByReferenceType methodReturnByReference
                ? SubstituteType(methodReturnByReference.ElementType, genericParameterMap)
                : SubstituteType(method.ReturnType, genericParameterMap);

            result.TargetField = method.HasThis
                ? AddGeneratedField(stateType, "$target", SubstituteType(declaringType, genericParameterMap))
                : null;
            result.ParameterFields = method.Parameters.Select((parameter, index) =>
            {
                var fieldType = parameter.ParameterType is ByReferenceType byReference
                    ? SubstituteType(byReference.ElementType, genericParameterMap)
                    : SubstituteType(parameter.ParameterType, genericParameterMap);
                return AddGeneratedField(stateType, $"$arg{index}", fieldType);
            }).ToArray();
            result.ReturnValueField = result.HasReturn
                ? AddGeneratedField(stateType, "$returnValue", result.ReturnElementType)
                : null;
            result.MethodBaseField = AddGeneratedField(stateType, "$methodBase", _methodBase);
            result.ParametersField = AddGeneratedField(stateType, "$parameters", _objectArray);
            result.ParameterCountField = AddGeneratedField(stateType, "$parameterCount", _mainModule.TypeSystem.Int32);

            foreach (var advice in aroundAdvices.Where(v => v.Method.HasThis))
            {
                var aspectType = _mainModule.ImportReference(advice.Method.Resolve().DeclaringType);
                if (!result.AspectFields.Keys.Any(v => v.FullName == aspectType.FullName))
                {
                    result.AspectFields.Add(aspectType, AddGeneratedField(stateType, $"$aspect{result.AspectFields.Count}", aspectType));
                }
            }

            if (!result.HasReturn)
            {
                stateType.Interfaces.Add(new InterfaceImplementation(_mainModule.ImportReference(typeof(IProceedingState))));
            }
            else
            {
                var proceedingState = new GenericInstanceType(_mainModule.ImportReference(typeof(IProceedingState<>)));
                proceedingState.GenericArguments.Add(result.ReturnElementType);
                stateType.Interfaces.Add(new InterfaceImplementation(proceedingState));
            }

            CreateStateConstructor(stateType);
            result.Initialize = CreateStateInitialize(stateType, method, result);
            result.SetMetadata = CreateStateSetMetadata(stateType, result);
            result.CopyBack = CreateStateCopyBack(stateType, method, result);
            result.SetAspects = result.AspectFields.Values.Select(v => CreateStateSetAspect(stateType, v)).ToArray();
            if (result.HasReturn)
            {
                CreateStateGetReturnValue(stateType, result);
            }
            CreateStateClear(stateType, result);

            result.InvokeAround = aroundAdvices.Select((advice, index) => CreateInvokeAround(stateType, method, result, advice, index)).ToArray();
            CreateStateProceed(stateType, method, originalMethod, result, result.InvokeAround);
            result.Dispatcher = CreateAroundDispatcher(stateType, method, result, originalMethod);
            return result;
        }

        private MethodDefinition CreateStateConstructor(TypeDefinition stateType)
        {
            var constructor = new MethodDefinition(".ctor",
                                                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                                                    _mainModule.TypeSystem.Void);
            stateType.Methods.Add(constructor);
            var il = constructor.Body.GetILProcessor();
            var baseConstructor = typeof(ProceedingStateBase).GetConstructor(System.Reflection.BindingFlags.Instance |
                                                                               System.Reflection.BindingFlags.Public |
                                                                               System.Reflection.BindingFlags.NonPublic,
                                                                               binder: null,
                                                                               types: Type.EmptyTypes,
                                                                               modifiers: null);
            if (baseConstructor == null)
            {
                throw new InvalidOperationException("ProceedingStateBase parameterless constructor was not found.");
            }

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, _mainModule.ImportReference(baseConstructor));
            il.Emit(OpCodes.Ret);
            return constructor;
        }

        private MethodDefinition CreateStateGetReturnValue(TypeDefinition stateType, AroundGenerationInfo info)
        {
            var getter = new MethodDefinition("GetReturnValue",
                                               MethodAttributes.Public |
                                               MethodAttributes.Virtual |
                                               MethodAttributes.NewSlot |
                                               MethodAttributes.Final |
                                               MethodAttributes.HideBySig,
                                               new ByReferenceType(info.ReturnElementType));
            stateType.Methods.Add(getter);
            var il = getter.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, CreateStateFieldReference(info.ReturnValueField, stateType));
            il.Emit(OpCodes.Ret);
            return getter;
        }

        private MethodDefinition CreateStateInitialize(TypeDefinition stateType, MethodDefinition method, AroundGenerationInfo info)
        {
            var initialize = new MethodDefinition("Initialize", MethodAttributes.Public | MethodAttributes.HideBySig, _mainModule.TypeSystem.Void);
            stateType.Methods.Add(initialize);
            var parameters = new List<ParameterDefinition>();
            if (method.HasThis)
            {
                parameters.Add(new ParameterDefinition("target", ParameterAttributes.None, info.TargetField.FieldType));
            }

            foreach (var parameter in method.Parameters)
            {
                parameters.Add(new ParameterDefinition(parameter.Name,
                                                        parameter.Attributes,
                                                        SubstituteType(parameter.ParameterType, info.GenericParameterMap)));
            }

            foreach (var parameter in parameters)
            {
                initialize.Parameters.Add(parameter);
            }

            var il = initialize.Body.GetILProcessor();
            int argumentIndex = 0;
            if (method.HasThis)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Append(ILPPUtils.LoadArgument(parameters[argumentIndex++]));
                il.Emit(OpCodes.Stfld, CreateStateFieldReference(info.TargetField, stateType));
            }

            for (int i = 0; i < method.Parameters.Count; i++, argumentIndex++)
            {
                var parameter = method.Parameters[i];
                var generatedParameter = parameters[argumentIndex];
                il.Emit(OpCodes.Ldarg_0);
                if (parameter.IsOut)
                {
                    il.Emit(OpCodes.Ldflda, CreateStateFieldReference(info.ParameterFields[i], stateType));
                    il.Emit(OpCodes.Initobj, info.ParameterFields[i].FieldType);
                }
                else if (parameter.ParameterType is ByReferenceType byReference)
                {
                    il.Append(ILPPUtils.LoadArgument(generatedParameter));
                    il.Append(ILPPUtils.LoadIndirect(SubstituteType(byReference.ElementType, info.GenericParameterMap)));
                    il.Emit(OpCodes.Stfld, CreateStateFieldReference(info.ParameterFields[i], stateType));
                }
                else
                {
                    il.Append(ILPPUtils.LoadArgument(generatedParameter));
                    il.Emit(OpCodes.Stfld, CreateStateFieldReference(info.ParameterFields[i], stateType));
                }
            }

            il.Emit(OpCodes.Ret);
            return initialize;
        }

        private MethodDefinition CreateStateSetMetadata(TypeDefinition stateType, AroundGenerationInfo info)
        {
            var setter = new MethodDefinition("SetMetadata", MethodAttributes.Public | MethodAttributes.HideBySig, _mainModule.TypeSystem.Void);
            setter.Parameters.Add(new ParameterDefinition("method", ParameterAttributes.None, _methodBase));
            setter.Parameters.Add(new ParameterDefinition("parameters", ParameterAttributes.None, _objectArray));
            setter.Parameters.Add(new ParameterDefinition("parameterCount", ParameterAttributes.None, _mainModule.TypeSystem.Int32));
            stateType.Methods.Add(setter);
            var il = setter.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, CreateStateFieldReference(info.MethodBaseField, stateType));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Stfld, CreateStateFieldReference(info.ParametersField, stateType));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Stfld, CreateStateFieldReference(info.ParameterCountField, stateType));
            il.Emit(OpCodes.Ret);
            return setter;
        }

        private MethodDefinition CreateStateCopyBack(TypeDefinition stateType, MethodDefinition method, AroundGenerationInfo info)
        {
            var copyBack = new MethodDefinition("CopyBack", MethodAttributes.Public | MethodAttributes.HideBySig, _mainModule.TypeSystem.Void);
            foreach (var parameter in method.Parameters.Where(v => v.ParameterType is ByReferenceType))
            {
                var elementType = ((ByReferenceType)parameter.ParameterType).ElementType;
                copyBack.Parameters.Add(new ParameterDefinition(parameter.Name,
                                                                 parameter.Attributes,
                                                                 new ByReferenceType(SubstituteType(elementType, info.GenericParameterMap))));
            }

            stateType.Methods.Add(copyBack);
            var il = copyBack.Body.GetILProcessor();
            foreach (var parameter in copyBack.Parameters)
            {
                var sourceParameter = method.GetParameter(parameter.Name);
                if (sourceParameter.IsIn)
                {
                    continue;
                }

                var field = info.ParameterFields[method.Parameters.IndexOf(sourceParameter)];
                il.Append(ILPPUtils.LoadArgument(parameter));
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, CreateStateFieldReference(field, stateType));
                AppendStoreIndirect(il, field.FieldType);
            }

            il.Emit(OpCodes.Ret);
            return copyBack;
        }

        private MethodDefinition CreateStateSetAspect(TypeDefinition stateType, FieldDefinition field)
        {
            var setter = new MethodDefinition($"Set{field.Name.Substring(1)}", MethodAttributes.Public | MethodAttributes.HideBySig, _mainModule.TypeSystem.Void);
            setter.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, field.FieldType));
            stateType.Methods.Add(setter);
            var il = setter.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, CreateStateFieldReference(field, stateType));
            il.Emit(OpCodes.Ret);
            return setter;
        }

        private MethodDefinition CreateStateClear(TypeDefinition stateType, AroundGenerationInfo info)
        {
            var clear = new MethodDefinition("ClearState", MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig, _mainModule.TypeSystem.Void);
            stateType.Methods.Add(clear);
            var il = clear.Body.GetILProcessor();
            var fields = new List<FieldDefinition>();
            if (info.TargetField != null) fields.Add(info.TargetField);
            fields.AddRange(info.ParameterFields);
            if (info.ReturnValueField != null) fields.Add(info.ReturnValueField);
            fields.Add(info.MethodBaseField);
            fields.Add(info.ParametersField);
            fields.Add(info.ParameterCountField);
            fields.AddRange(info.AspectFields.Values);
            foreach (var field in fields)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldflda, CreateStateFieldReference(field, stateType));
                il.Emit(OpCodes.Initobj, field.FieldType);
            }

            il.Emit(OpCodes.Ret);
            return clear;
        }

        private MethodDefinition CreateInvokeAround(TypeDefinition stateType,
                                                     MethodDefinition method,
                                                     AroundGenerationInfo info,
                                                     AdviceInfo advice,
                                                     int slot)
        {
            var returnType = info.HasReturn && info.ReturnsByReference
                ? new ByReferenceType(info.ReturnElementType)
                : _mainModule.TypeSystem.Void;
            var invoke = new MethodDefinition($"InvokeAround{slot}", MethodAttributes.Private | MethodAttributes.HideBySig, returnType);
            invoke.Parameters.Add(new ParameterDefinition("generation", ParameterAttributes.None, _mainModule.TypeSystem.Int32));
            stateType.Methods.Add(invoke);

            var il = invoke.Body.GetILProcessor();
            VariableDefinition context = null;
            if (info.HasReturn)
            {
                var contextDefinition = advice.UnsafeInjection
                    ? typeof(UnsafeProceedingContext<>).GetGenericTypeDefinition()
                    : typeof(ProceedingContext<>).GetGenericTypeDefinition();
                var contextType = new GenericInstanceType(_mainModule.ImportReference(contextDefinition));
                contextType.GenericArguments.Add(info.ReturnElementType);
                context = new VariableDefinition(contextType);
                invoke.Body.Variables.Add(context);
                il.Emit(OpCodes.Ldarg_0);
                il.Append(ILPPUtils.LoadLiteral(slot));
                il.Emit(OpCodes.Ldarg_1);
                if (info.ReturnsByReference)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldflda, CreateStateFieldReference(info.ReturnValueField, stateType));
                }
                il.Emit(OpCodes.Newobj, CreateContextConstructor(contextType, info.ReturnsByReference));
                il.Append(ILPPUtils.SetLocal(context));
            }
            else
            {
                context = new VariableDefinition(_proceedingContext);
                invoke.Body.Variables.Add(context);
                il.Emit(OpCodes.Ldarg_0);
                il.Append(ILPPUtils.LoadLiteral(slot));
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Newobj, CreateContextConstructor(_proceedingContext, false));
                il.Append(ILPPUtils.SetLocal(context));
            }

            AppendAroundAdviceCall(il, method, info, advice, context);
            il.Emit(OpCodes.Ldarg_0);
            il.Append(ILPPUtils.LoadLiteral(slot));
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, CreateRuntimeMethod(nameof(ProceedingRuntime.EnsureProceed), _mainModule.TypeSystem.Void, _mainModule.TypeSystem.Object, _mainModule.TypeSystem.Int32, _mainModule.TypeSystem.Int32));

            if (info.HasReturn)
            {
                if (info.ReturnsByReference)
                {
                    il.Emit(OpCodes.Ldloca, context);
                    il.Emit(OpCodes.Call, CreateReturnValueGetter(context.VariableType));
                }
            }

            il.Emit(OpCodes.Ret);
            return invoke;
        }

        private void AppendAroundAdviceCall(ILProcessor il,
                                             MethodDefinition method,
                                             AroundGenerationInfo info,
                                             AdviceInfo advice,
                                             VariableDefinition context)
        {
            var adviceMethod = advice.Method.Resolve();
            var adviceMethodReference = CreateAdviceMethodReferenceForWrapper(method, advice, info.GenericParameterMap);
            var aspectType = _mainModule.ImportReference(adviceMethod.DeclaringType);
            if (adviceMethod.HasThis)
            {
                var aspectField = info.AspectFields.First(v => v.Key.FullName == aspectType.FullName).Value;
                il.Emit(OpCodes.Ldarg_0);
                if (aspectField.FieldType.IsValueType)
                {
                    il.Emit(OpCodes.Ldflda, aspectField);
                }
                else
                {
                    il.Emit(OpCodes.Ldfld, aspectField);
                }
            }

            foreach (var adviceParameter in adviceMethod.Parameters)
            {
                var adviceParameterType = adviceParameter.ParameterType;
                TypeReference parameterType;
                if (HasPointcutProceed(adviceParameter))
                {
                    if (adviceParameterType is ByReferenceType)
                    {
                        il.Emit(OpCodes.Ldloca, context);
                    }
                    else
                    {
                        il.Append(ILPPUtils.LoadLocal(context));
                    }
                    parameterType = adviceParameterType;
                }
                else if (HasPointcutMethod(adviceParameter))
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, CreateStateFieldReference(info.MethodBaseField, info.StateType));
                    parameterType = _methodBase;
                }
                else if (HasPointcutParameters(adviceParameter))
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, CreateStateFieldReference(info.ParameterCountField, info.StateType));
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, CreateStateFieldReference(info.ParametersField, info.StateType));
                    il.Emit(OpCodes.Newobj, _parameterArrayCtor);
                    parameterType = _parameterArray;
                }
                else if (HasPointcutThis(adviceParameter))
                {
                    il.Emit(OpCodes.Ldarg_0);
                    if (adviceParameterType.IsByReference)
                    {
                        il.Emit(OpCodes.Ldflda, CreateStateFieldReference(info.TargetField, info.StateType));
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldfld, CreateStateFieldReference(info.TargetField, info.StateType));
                    }

                    parameterType = info.TargetField.FieldType;
                }
                else
                {
                    var parameter = method.GetParameter(adviceParameter.Name);
                    var field = info.ParameterFields[method.Parameters.IndexOf(parameter)];
                    il.Emit(OpCodes.Ldarg_0);
                    if (adviceParameterType.IsByReference)
                    {
                        il.Emit(OpCodes.Ldflda, CreateStateFieldReference(field, info.StateType));
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldfld, CreateStateFieldReference(field, info.StateType));
                    }

                    parameterType = field.FieldType;
                }

                if (parameterType is ByReferenceType byReferenceType)
                {
                    parameterType = byReferenceType.ElementType;
                }

                if (!adviceParameterType.IsByReference && parameterType.IsBoxingRequired(adviceParameterType))
                {
                    il.Emit(OpCodes.Box, parameterType);
                }
            }

            il.Emit(OpCodes.Call, adviceMethodReference);
        }

        private MethodDefinition CreateStateProceed(TypeDefinition stateType,
                                                     MethodDefinition method,
                                                     MethodDefinition originalMethod,
                                                     AroundGenerationInfo info,
                                                     MethodDefinition[] invokeAround)
        {
            var returnType = info.HasReturn
                ? new ByReferenceType(info.ReturnElementType)
                : _mainModule.TypeSystem.Void;
            var proceed = new MethodDefinition("Proceed",
                                               MethodAttributes.Public |
                                               MethodAttributes.Virtual |
                                               MethodAttributes.NewSlot |
                                               MethodAttributes.Final |
                                               MethodAttributes.HideBySig,
                                               returnType);
            proceed.Parameters.Add(new ParameterDefinition("slot", ParameterAttributes.None, _mainModule.TypeSystem.Int32));
            proceed.Parameters.Add(new ParameterDefinition("generation", ParameterAttributes.None, _mainModule.TypeSystem.Int32));
            stateType.Methods.Add(proceed);

            var il = proceed.Body.GetILProcessor();
            VariableDefinition referenceReturn = null;
            if (info.ReturnsByReference)
            {
                referenceReturn = new VariableDefinition(returnType);
                proceed.Body.Variables.Add(referenceReturn);
            }
            var beginProceed = typeof(ProceedingStateBase).GetMethod("BeginProceed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var failProceed = typeof(ProceedingStateBase).GetMethod("FailProceed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (beginProceed == null || failProceed == null)
            {
                throw new InvalidOperationException("ProceedingStateBase Proceed methods were not found.");
            }

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, _mainModule.ImportReference(beginProceed));

            var tryStart = Instruction.Create(OpCodes.Nop);
            var handlerStart = Instruction.Create(OpCodes.Nop);
            il.Append(tryStart);
            for (int i = 0; i < invokeAround.Length; i++)
            {
                var next = Instruction.Create(OpCodes.Nop);
                il.Emit(OpCodes.Ldarg_1);
                il.Append(ILPPUtils.LoadLiteral(1));
                il.Emit(OpCodes.Add);
                il.Append(ILPPUtils.LoadLiteral(i));
                il.Emit(OpCodes.Bne_Un, next);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Call, CreateMethodReference(invokeAround[i], info.StateTypeInstance, info.GenericParameterMap));
                if (info.HasReturn && !info.ReturnsByReference)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldflda, CreateStateFieldReference(info.ReturnValueField, stateType));
                }
                il.Emit(OpCodes.Ret);
                il.Append(next);
            }

            var preserveStateOnStack = info.HasReturn || !method.HasThis;
            if (preserveStateOnStack)
            {
                il.Emit(OpCodes.Ldarg_0);
            }

            if (method.HasThis)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, CreateStateFieldReference(info.TargetField, stateType));
            }

            for (int i = 0; i < info.ParameterFields.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_0);
                if (method.Parameters[i].ParameterType is ByReferenceType)
                {
                    il.Emit(OpCodes.Ldflda, CreateStateFieldReference(info.ParameterFields[i], stateType));
                }
                else
                {
                    il.Emit(OpCodes.Ldfld, CreateStateFieldReference(info.ParameterFields[i], stateType));
                }
            }

            il.Emit(OpCodes.Call, CreateMethodReference(originalMethod, SubstituteType(method.DeclaringType, info.GenericParameterMap), info.GenericParameterMap));
            if (!info.HasReturn)
            {
                if (preserveStateOnStack)
                {
                    il.Emit(OpCodes.Pop);
                }

                il.Emit(OpCodes.Ret);
            }
            else if (info.ReturnsByReference)
            {
                il.Append(ILPPUtils.SetLocal(referenceReturn));
                il.Emit(OpCodes.Pop);
                il.Append(ILPPUtils.LoadLocal(referenceReturn));
                il.Emit(OpCodes.Ret);
            }
            else
            {
                il.Emit(OpCodes.Stfld, CreateStateFieldReference(info.ReturnValueField, stateType));
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldflda, CreateStateFieldReference(info.ReturnValueField, stateType));
                il.Emit(OpCodes.Ret);
            }

            il.Append(handlerStart);
            var exception = new VariableDefinition(_exceptionType);
            proceed.Body.Variables.Add(exception);
            il.Append(ILPPUtils.SetLocal(exception));
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, _mainModule.ImportReference(failProceed));
            il.Append(ILPPUtils.LoadLocal(exception));
            il.Emit(OpCodes.Throw);
            proceed.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                TryStart = tryStart,
                TryEnd = handlerStart,
                HandlerStart = handlerStart,
                HandlerEnd = null,
                CatchType = _exceptionType,
            });
            return proceed;
        }

        private MethodReference CreateAroundDispatcher(TypeDefinition stateType,
                                                         MethodDefinition method,
                                                         AroundGenerationInfo info,
                                                         MethodDefinition originalMethod)
        {
            var dispatcher = new MethodDefinition("Invoke",
                                                  MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
                                                  SubstituteType(method.ReturnType, info.GenericParameterMap));
            if (method.HasThis)
            {
                dispatcher.Parameters.Add(new ParameterDefinition("target", ParameterAttributes.None, SubstituteType(method.DeclaringType, info.GenericParameterMap)));
            }

            foreach (var parameter in method.Parameters)
            {
                dispatcher.Parameters.Add(new ParameterDefinition(parameter.Name,
                                                                  parameter.Attributes,
                                                                  SubstituteType(parameter.ParameterType, info.GenericParameterMap)));
            }

            dispatcher.Parameters.Add(new ParameterDefinition("method", ParameterAttributes.None, _methodBase));
            dispatcher.Parameters.Add(new ParameterDefinition("parameters", ParameterAttributes.None, _objectArray));
            dispatcher.Parameters.Add(new ParameterDefinition("parameterCount", ParameterAttributes.None, _mainModule.TypeSystem.Int32));
            foreach (var field in info.AspectFields.Values)
            {
                dispatcher.Parameters.Add(new ParameterDefinition(field.Name, ParameterAttributes.None, field.FieldType));
            }

            stateType.Methods.Add(dispatcher);
            var state = new VariableDefinition(info.StateTypeInstance);
            var generation = new VariableDefinition(_mainModule.TypeSystem.Int32);
            dispatcher.Body.Variables.Add(state);
            dispatcher.Body.Variables.Add(generation);
            VariableDefinition returnValue = null;
            if (info.HasReturn)
            {
                returnValue = new VariableDefinition(info.ReturnsByReference
                    ? new ByReferenceType(info.ReturnElementType)
                    : info.ReturnElementType);
                dispatcher.Body.Variables.Add(returnValue);
            }

            var il = dispatcher.Body.GetILProcessor();
            il.Emit(OpCodes.Ldtoken, info.StateTypeInstance);
            il.Emit(OpCodes.Call, _mainModule.ImportReference(typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), new[] { typeof(RuntimeTypeHandle) })));
            il.Emit(OpCodes.Call, CreatePoolMethod("Rent", info));
            il.Emit(OpCodes.Castclass, info.StateTypeInstance);
            il.Append(ILPPUtils.SetLocal(state));
            il.Append(ILPPUtils.LoadLocal(state));
            il.Emit(OpCodes.Call, CreateRuntimeMethod(nameof(ProceedingRuntime.Activate), _mainModule.TypeSystem.Int32, _mainModule.TypeSystem.Object));
            il.Append(ILPPUtils.SetLocal(generation));

            il.Append(ILPPUtils.LoadLocal(state));
            int parameterIndex = 0;
            if (method.HasThis)
            {
                il.Append(ILPPUtils.LoadArgument(dispatcher.Parameters[parameterIndex++]));
            }
            foreach (var parameter in method.Parameters)
            {
                il.Append(ILPPUtils.LoadArgument(dispatcher.Parameters[parameterIndex++]));
            }

            il.Emit(OpCodes.Call, CreateMethodReference(info.Initialize, info.StateTypeInstance, info.GenericParameterMap));
            il.Append(ILPPUtils.LoadLocal(state));
            il.Append(ILPPUtils.LoadArgument(dispatcher.Parameters[parameterIndex++]));
            il.Append(ILPPUtils.LoadArgument(dispatcher.Parameters[parameterIndex++]));
            il.Append(ILPPUtils.LoadArgument(dispatcher.Parameters[parameterIndex++]));
            il.Emit(OpCodes.Call, CreateMethodReference(info.SetMetadata, info.StateTypeInstance, info.GenericParameterMap));

            foreach (var setter in info.SetAspects)
            {
                il.Append(ILPPUtils.LoadLocal(state));
                il.Append(ILPPUtils.LoadArgument(dispatcher.Parameters[parameterIndex++]));
                il.Emit(OpCodes.Call, CreateMethodReference(setter, info.StateTypeInstance, info.GenericParameterMap));
            }

            var tryStart = Instruction.Create(OpCodes.Nop);
            var handlerStart = Instruction.Create(OpCodes.Nop);
            il.Append(tryStart);
            il.Append(ILPPUtils.LoadLocal(state));
            il.Append(ILPPUtils.LoadLocal(generation));
            il.Emit(OpCodes.Call, CreateMethodReference(info.InvokeAround[0], info.StateTypeInstance, info.GenericParameterMap));
            if (info.HasReturn)
            {
                if (info.ReturnsByReference)
                {
                    il.Append(ILPPUtils.SetLocal(returnValue));
                }
                else
                {
                    il.Append(ILPPUtils.LoadLocal(state));
                    il.Emit(OpCodes.Ldflda, CreateStateFieldReference(info.ReturnValueField, stateType));
                    il.Append(ILPPUtils.LoadIndirect(info.ReturnElementType));
                    il.Append(ILPPUtils.SetLocal(returnValue));
                }
            }

            AppendDispatcherCopyBack(il, dispatcher, method, info, state);
            il.Append(ILPPUtils.LoadLocal(state));
            il.Emit(OpCodes.Call, CreatePoolMethod("Return", info));
            if (info.HasReturn)
            {
                il.Append(ILPPUtils.LoadLocal(returnValue));
            }

            il.Emit(OpCodes.Ret);

            il.Append(handlerStart);
            var exception = new VariableDefinition(_exceptionType);
            dispatcher.Body.Variables.Add(exception);
            il.Append(ILPPUtils.SetLocal(exception));
            AppendDispatcherCopyBack(il, dispatcher, method, info, state);
            il.Append(ILPPUtils.LoadLocal(state));
            il.Emit(OpCodes.Call, CreatePoolMethod("Return", info));
            il.Append(ILPPUtils.LoadLocal(exception));
            il.Emit(OpCodes.Throw);
            dispatcher.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                TryStart = tryStart,
                TryEnd = handlerStart,
                HandlerStart = handlerStart,
                HandlerEnd = null,
                CatchType = _exceptionType,
            });
            return CreateMethodReference(dispatcher, info.CallStateTypeInstance, info.GenericParameterMap);
        }

        private void EnsureAroundAspectInstances(ILProcessor ilProcessor,
                                                 MethodDefinition method,
                                                 IEnumerable<AdviceInfo> allAdvices,
                                                 IEnumerable<AdviceInfo> aroundAdvices,
                                                 Dictionary<TypeReference, VariableDefinition> aspectInstances,
                                                 VariableDefinition methodBase,
                                                 VariableDefinition parameterArray)
        {
            foreach (var advice in aroundAdvices.Where(v => v.Method.HasThis))
            {
                var aspectType = method.Module.ImportReference(advice.Method.Resolve().DeclaringType);
                if (aspectInstances.Keys.Any(v => v.FullName == aspectType.FullName))
                {
                    continue;
                }

                var constructor = allAdvices.FirstOrDefault(v => v.Method.Resolve()?.IsConstructor == true &&
                                                                  method.Module.ImportReference(v.Method.Resolve().DeclaringType).Is(aspectType));
                if (constructor.Method != null)
                {
                    AppendCallAdvice(ilProcessor,
                                     method,
                                     constructor,
                                     aspectInstances,
                                     methodBase,
                                     null,
                                     parameterArray,
                                     null);
                }
            }

        }

        private Instruction[] AppendAroundDispatcherCall(ILProcessor ilProcessor,
                                                          MethodDefinition method,
                                                          AroundGenerationInfo info,
                                                          Dictionary<TypeReference, VariableDefinition> aspectInstances,
                                                          VariableDefinition methodBase,
                                                          VariableDefinition parameterArray,
                                                          Instruction originalStart)
        {
            var instructions = new List<Instruction> { originalStart };
            void Add(Instruction instruction)
            {
                ilProcessor.Append(instruction);
                instructions.Add(instruction);
            }

            if (method.HasThis)
            {
                Add(Instruction.Create(OpCodes.Ldarg_0));
            }

            foreach (var parameter in method.Parameters)
            {
                Add(ILPPUtils.LoadArgument(parameter));
            }

            if (methodBase != null)
            {
                Add(ILPPUtils.LoadLocal(methodBase));
            }
            else
            {
                Add(Instruction.Create(OpCodes.Ldnull));
            }

            if (parameterArray != null)
            {
                Add(ILPPUtils.LoadLocal(parameterArray));
            }
            else
            {
                Add(Instruction.Create(OpCodes.Ldnull));
            }

            Add(ILPPUtils.LoadLiteral(method.Parameters.Count));
            foreach (var field in info.AspectFields.Values)
            {
                if (!aspectInstances.TryGetValue(method.Module.ImportReference(field.FieldType), out var instance))
                {
                    instance = aspectInstances.FirstOrDefault(v => v.Key.Is(field.FieldType)).Value;
                }

                Add(ILPPUtils.LoadLocal(instance));
            }

            Add(Instruction.Create(OpCodes.Call, info.Dispatcher));
            var ret = Instruction.Create(OpCodes.Ret);
            Add(ret);
            return instructions.ToArray();
        }

        private void AppendDispatcherCopyBack(ILProcessor il,
                                               MethodDefinition dispatcher,
                                               MethodDefinition method,
                                               AroundGenerationInfo info,
                                               VariableDefinition state)
        {
            if (!method.Parameters.Any(v => v.ParameterType is ByReferenceType))
            {
                return;
            }

            il.Append(ILPPUtils.LoadLocal(state));
            foreach (var parameter in dispatcher.Parameters.Where(v => v.ParameterType is ByReferenceType))
            {
                il.Append(ILPPUtils.LoadArgument(parameter));
            }

            var copyBack = info.CopyBack;
            il.Emit(OpCodes.Call, CreateMethodReference(copyBack, info.StateTypeInstance, info.GenericParameterMap));
        }

        private MethodReference CreatePoolMethod(string name, AroundGenerationInfo info)
        {
            return name == "Rent"
                ? CreateRuntimeMethod(nameof(ProceedingRuntime.Rent), _mainModule.TypeSystem.Object, _mainModule.ImportReference(typeof(Type)))
                : CreateRuntimeMethod(nameof(ProceedingRuntime.Return), _mainModule.TypeSystem.Void, _mainModule.TypeSystem.Object);
        }

        private MethodReference CreateRuntimeMethod(string name, TypeReference returnType, params TypeReference[] parameterTypes)
        {
            var method = typeof(ProceedingRuntime).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .First(v => v.Name == name && v.GetParameters().Length == parameterTypes.Length);
            var result = new MethodReference(name, returnType, _proceedingRuntime)
            {
                HasThis = false,
                CallingConvention = MethodCallingConvention.Default,
            };
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                result.Parameters.Add(new ParameterDefinition($"arg{i}", ParameterAttributes.None, parameterTypes[i]));
            }

            return result;
        }

        private MethodReference CreateContextConstructor(TypeReference contextType, bool hasReturnReference)
        {
            System.Reflection.ConstructorInfo constructor;
            if (contextType is GenericInstanceType genericContext)
            {
                var contextDefinition = genericContext.ElementType.FullName == _unsafeProceedingContextGeneric.FullName
                    ? typeof(UnsafeProceedingContext<>)
                    : typeof(ProceedingContext<>);
                constructor = contextDefinition.GetConstructors(System.Reflection.BindingFlags.Instance |
                                                                 System.Reflection.BindingFlags.Public |
                                                                 System.Reflection.BindingFlags.NonPublic)
                    .Single(v => v.GetParameters().Length == (hasReturnReference ? 4 : 3));
            }
            else
            {
                constructor = typeof(ProceedingContext).GetConstructor(new[] { typeof(object), typeof(int), typeof(int) });
            }

            var result = _mainModule.ImportReference(constructor);
            result.DeclaringType = contextType;
            return result;
        }

        private MethodReference CreateReturnValueGetter(TypeReference contextType)
        {
            var genericContext = (GenericInstanceType)contextType;
            var isUnsafe = genericContext.ElementType.FullName == _unsafeProceedingContextGeneric.FullName;
            var contextDefinition = isUnsafe ? typeof(UnsafeProceedingContext<>) : typeof(ProceedingContext<>);
            var getter = contextDefinition.GetProperty(nameof(ProceedingContext<int>.ReturnValue)).GetGetMethod();
            var importedGetter = _mainModule.ImportReference(getter);
            var contextGenericParameter = genericContext.ElementType.GenericParameters[0];
            return new MethodReference(importedGetter.Name,
                                       CreateClosedReturnValueType(importedGetter.ReturnType,
                                                                    contextGenericParameter,
                                                                    new Dictionary<GenericParameter, TypeReference>(),
                                                                    isUnsafe),
                                       contextType)
            {
                HasThis = true,
                CallingConvention = importedGetter.CallingConvention,
            };
        }

        private TypeReference CreateClosedReturnValueType(TypeReference source,
                                                           TypeReference elementType,
                                                           IDictionary<GenericParameter, TypeReference> genericMap,
                                                           bool isUnsafe)
        {
            if (source is RequiredModifierType requiredModifier)
            {
                return new RequiredModifierType(SubstituteType(requiredModifier.ModifierType, genericMap),
                                                new ByReferenceType(elementType));
            }

            return isUnsafe
                ? new ByReferenceType(elementType)
                : new RequiredModifierType(_mainModule.ImportReference(typeof(System.Runtime.InteropServices.InAttribute)),
                                           new ByReferenceType(elementType));
        }

        private MethodReference CreateContextFactory(bool unsafeInjection, TypeReference returnElementType)
        {
            var method = typeof(ProceedingRuntime).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Single(v => v.Name == (unsafeInjection ? nameof(ProceedingRuntime.CreateUnsafeContext) : nameof(ProceedingRuntime.CreateContext)) &&
                             v.IsGenericMethodDefinition);
            return _mainModule.ImportReference(method).MakeGenericInstanceMethod(new[] { returnElementType });
        }

        private MethodReference CreateMethodReference(MethodDefinition method,
                                                       TypeReference declaringType,
                                                       Dictionary<GenericParameter, TypeReference> genericParameterMap)
        {
            if (method.HasGenericParameters)
            {
                var genericMethod = CloneMethodReference(method, genericParameterMap);
                var genericArguments = method.GenericParameters
                    .Select(genericParameter => SubstituteType(genericParameter, genericParameterMap))
                    .ToArray();
                return genericMethod.MakeGenericInstanceMethod(genericArguments);
            }

            var result = new MethodReference(method.Name,
                                              SubstituteType(method.ReturnType, genericParameterMap),
                                              declaringType)
            {
                HasThis = method.HasThis,
                ExplicitThis = method.ExplicitThis,
                CallingConvention = method.CallingConvention,
            };
            foreach (var parameter in method.Parameters)
            {
                result.Parameters.Add(new ParameterDefinition(parameter.Name,
                                                               parameter.Attributes,
                                                               SubstituteType(parameter.ParameterType, genericParameterMap)));
            }

            return result;
        }

        private MethodReference CreateMethodReference(MethodReference method,
                                                       TypeReference declaringType,
                                                       Dictionary<GenericParameter, TypeReference> genericParameterMap)
        {
            var result = new MethodReference(method.Name,
                                              SubstituteType(method.ReturnType, genericParameterMap),
                                              declaringType)
            {
                HasThis = method.HasThis,
                ExplicitThis = method.ExplicitThis,
                CallingConvention = method.CallingConvention,
            };
            foreach (var parameter in method.Parameters)
            {
                result.Parameters.Add(new ParameterDefinition(parameter.Name,
                                                               parameter.Attributes,
                                                               SubstituteType(parameter.ParameterType, genericParameterMap)));
            }

            return result;
        }

        private MethodReference CreateAdviceMethodReferenceForWrapper(MethodDefinition method,
                                                                      AdviceInfo advice,
                                                                      Dictionary<GenericParameter, TypeReference> genericParameterMap)
        {
            var adviceMethod = advice.Method.Resolve();
            var result = _mainModule.ImportReference(advice.Method);
            if (!adviceMethod.HasGenericParameters)
            {
                return result;
            }

            var genericArguments = new List<TypeReference>();
            foreach (var genericParameter in adviceMethod.GenericParameters)
            {
                var binding = GetBinding(genericParameter);
                TypeReference argument;
                if (binding == GenericBinding.ReturnType)
                {
                    argument = method.ReturnType is ByReferenceType returnByReference
                        ? returnByReference.ElementType
                        : method.ReturnType;
                }
                else if (binding == GenericBinding.ParameterType)
                {
                    using (ThreadStaticListPool<TypeReference>.Get(out var argumentsByParameter))
                    {
                        FindBoundGeneicArgumentByParameterType(method, adviceMethod, genericParameter, argumentsByParameter);
                        argument = argumentsByParameter.FirstOrDefault();
                    }
                }
                else
                {
                    argument = FindBoundGeneicArgumentByName(method, genericParameter.Name);
                }

                genericArguments.Add(SubstituteType(argument, genericParameterMap));
            }

            var importedAdvice = _mainModule.ImportReference(advice.Method);
            var closedElement = new MethodReference(importedAdvice.Name,
                                                     importedAdvice.ReturnType,
                                                     importedAdvice.DeclaringType)
            {
                HasThis = importedAdvice.HasThis,
                ExplicitThis = importedAdvice.ExplicitThis,
                CallingConvention = importedAdvice.CallingConvention,
            };
            var adviceGenericMap = new Dictionary<GenericParameter, TypeReference>();
            foreach (var genericParameter in importedAdvice.GenericParameters)
            {
                var clone = new GenericParameter(genericParameter.Name, closedElement)
                {
                    Attributes = genericParameter.Attributes,
                };
                closedElement.GenericParameters.Add(clone);
                adviceGenericMap[genericParameter] = clone;
            }

            closedElement.ReturnType = SubstituteType(importedAdvice.ReturnType, adviceGenericMap);
            foreach (var parameter in importedAdvice.Parameters)
            {
                closedElement.Parameters.Add(new ParameterDefinition(parameter.Name,
                                                                      parameter.Attributes,
                                                                      SubstituteType(parameter.ParameterType, adviceGenericMap)));
            }

            return _mainModule.ImportReference(closedElement.MakeGenericInstanceMethod(genericArguments));
        }

        private TypeReference SubstituteType(TypeReference source, IDictionary<GenericParameter, TypeReference> map)
        {
            if (source == null)
            {
                return null;
            }

            if (source is GenericParameter genericParameter &&
                (map.TryGetValue(genericParameter, out var replacement) ||
                 (replacement = map.FirstOrDefault(v => v.Key.Position == genericParameter.Position).Value) != null))
            {
                return replacement;
            }

            if (source is RequiredModifierType requiredModifier)
            {
                return new RequiredModifierType(SubstituteType(requiredModifier.ModifierType, map),
                                                SubstituteType(requiredModifier.ElementType, map));
            }

            if (source is OptionalModifierType optionalModifier)
            {
                return new OptionalModifierType(SubstituteType(optionalModifier.ModifierType, map),
                                                SubstituteType(optionalModifier.ElementType, map));
            }

            if (source is ByReferenceType byReference)
            {
                return new ByReferenceType(SubstituteType(byReference.ElementType, map));
            }

            if (source is PointerType pointer)
            {
                return new PointerType(SubstituteType(pointer.ElementType, map));
            }

            if (source is ArrayType array)
            {
                return new ArrayType(SubstituteType(array.ElementType, map), array.Rank);
            }

            if (source is GenericInstanceType genericInstance)
            {
                var result = new GenericInstanceType(SubstituteType(genericInstance.ElementType, map));
                foreach (var argument in genericInstance.GenericArguments)
                {
                    result.GenericArguments.Add(SubstituteType(argument, map));
                }

                return result;
            }

            if (source is TypeDefinition typeDefinition && typeDefinition.HasGenericParameters)
            {
                var result = new GenericInstanceType(typeDefinition);
                foreach (var typeGenericParameter in typeDefinition.GenericParameters)
                {
                    result.GenericArguments.Add(SubstituteType(typeGenericParameter, map));
                }

                return result;
            }

            return source;
        }

        private void AppendStoreIndirect(ILProcessor il, TypeReference type)
        {
            if (type.IsByReference)
            {
                type = ((ByReferenceType)type).ElementType;
            }

            if (!type.IsValueType || type.FullName == _mainModule.TypeSystem.IntPtr.FullName || type.FullName == _mainModule.TypeSystem.UIntPtr.FullName)
            {
                il.Emit(OpCodes.Stind_Ref);
                return;
            }

            if (type.MetadataType == MetadataType.Boolean || type.MetadataType == MetadataType.SByte)
            {
                il.Emit(OpCodes.Stind_I1);
            }
            else if (type.MetadataType == MetadataType.Int16 || type.MetadataType == MetadataType.Char)
            {
                il.Emit(OpCodes.Stind_I2);
            }
            else if (type.MetadataType == MetadataType.Int32 || type.MetadataType == MetadataType.UInt32)
            {
                il.Emit(OpCodes.Stind_I4);
            }
            else if (type.MetadataType == MetadataType.Int64 || type.MetadataType == MetadataType.UInt64)
            {
                il.Emit(OpCodes.Stind_I8);
            }
            else if (type.MetadataType == MetadataType.Single)
            {
                il.Emit(OpCodes.Stind_R4);
            }
            else if (type.MetadataType == MetadataType.Double)
            {
                il.Emit(OpCodes.Stind_R8);
            }
            else
            {
                il.Emit(OpCodes.Stobj, type);
            }
        }

        private void AppendCallAdvice(ILProcessor ilProcessor, MethodDefinition method, AdviceInfo adviceInfo, Dictionary<TypeReference, VariableDefinition> aspectInstances, VariableDefinition methodBase, VariableDefinition returned, VariableDefinition parameters, VariableDefinition exception)
        {
            var adviceMethod = adviceInfo.Method.Resolve();
            var adviceMethodRef = adviceInfo.Method;
            var aspectType = method.Module.ImportReference(adviceMethod.DeclaringType);

            if (adviceMethod.HasGenericParameters)
            {
                using (ThreadStaticListPool<TypeReference>.Get(out var genericArguments))
                {
                    foreach (var adviceGenericParameter in adviceMethod.GenericParameters)
                    {
                        var binding = GetBinding(adviceGenericParameter);
                        TypeReference genericArgument = null;
                        switch (binding)
                        {
                            case GenericBinding.GenericParameterName:
                                {
                                    genericArgument = FindBoundGeneicArgumentByName(method, adviceGenericParameter.Name);
                                }
                                break;
                            case GenericBinding.ParameterType:
                                {
                                    using (ThreadStaticListPool<TypeReference>.Get(out var genericArgumentsByParameter))
                                    {
                                        FindBoundGeneicArgumentByParameterType(method, adviceMethod, adviceGenericParameter, genericArgumentsByParameter);
                                        genericArgument = genericArgumentsByParameter.FirstOrDefault();
                                    }
                                }
                                break;
                            case GenericBinding.ReturnType:
                                {
                                    genericArgument = method.ReturnType;
                                    if (genericArgument is ByReferenceType returnByReferenceType)
                                    {
                                        genericArgument = returnByReferenceType.ElementType;
                                    }
                                }
                                break;
                        }

                        if (genericArgument == null)
                        {
                            ILPPUtils.Log($"missing genericArgument.");
                            return;
                        }

                        genericArguments.Add(genericArgument);
                    }

                    adviceMethodRef = adviceMethodRef.MakeGenericInstanceMethod(genericArguments);
                    adviceMethodRef = _mainModule.ImportReference(adviceMethodRef);
                }
            }

            if (adviceMethod.HasThis)
            {
                if (aspectInstances.TryGetValue(aspectType, out var instanceVariable))
                {
                    var loadInstance = aspectType.IsValueType
                        ? ILPPUtils.LoadLocalAddress(instanceVariable)
                        : ILPPUtils.LoadLocal(instanceVariable);
                    ilProcessor.Append(loadInstance);
                }
            }

            foreach (var adviceParameter in adviceMethod.Parameters)
            {
                var adviceParameterType = adviceParameter.ParameterType;
                TypeReference parameterType;
                if (HasPointcutReturned(adviceParameter))
                {
                    if (returned == null)
                    {
                        ILPPUtils.Log("missing pointcut returned variable.");
                        return;
                    }

                    var returnedType = returned.VariableType;
                    if (returnedType.IsByReference == adviceParameterType.IsByReference)
                    {
                        ilProcessor.Append(ILPPUtils.LoadLocal(returned));
                    }
                    else if (returnedType is ByReferenceType refResultType)
                    {
                        ilProcessor.Append(ILPPUtils.LoadLocal(returned));
                        ilProcessor.Append(ILPPUtils.LoadIndirect(refResultType.ElementType));
                    }
                    else
                    {
                        ilProcessor.Append(ILPPUtils.LoadLocalAddress(returned));
                    }
                    parameterType = method.ReturnType;
                }
                else if (HasPointcutThrown(adviceParameter))
                {
                    ilProcessor.Append(ILPPUtils.LoadLocal(exception));
                    parameterType = exception.VariableType;
                }
                else if (HasPointcutMethod(adviceParameter))
                {
                    ilProcessor.Append(ILPPUtils.LoadLocal(methodBase));
                    parameterType = adviceParameter.ParameterType;
                }
                else if (HasPointcutParameters(adviceParameter))
                {
                    ilProcessor.Append(ILPPUtils.LoadLocal(parameters));
                    parameterType = adviceParameter.ParameterType;
                }
                else if (HasPointcutThis(adviceParameter))
                {
                    ilProcessor.Emit(OpCodes.Ldarg_0);
                    parameterType = method.DeclaringType;
                }
                else
                {
                    var parameter = method.GetParameter(adviceParameter.Name);
                    if (parameter.ParameterType.IsByReference == adviceParameterType.IsByReference)
                    {
                        ilProcessor.Append(ILPPUtils.LoadArgument(parameter));
                    }
                    else if (parameter.ParameterType is ByReferenceType refParameterType)
                    {
                        ilProcessor.Append(ILPPUtils.LoadArgument(parameter));
                        ilProcessor.Append(ILPPUtils.LoadIndirect(refParameterType.ElementType));
                    }
                    else
                    {
                        ilProcessor.Append(ILPPUtils.LoadArgumentAddress(parameter));
                    }

                    parameterType = parameter.ParameterType;
                }

                if (parameterType is ByReferenceType byRefParameterType)
                {
                    parameterType = byRefParameterType.ElementType;
                }

                if (!adviceParameterType.IsByReference &&
                    parameterType.IsBoxingRequired(adviceParameterType))
                {
                    ilProcessor.Emit(OpCodes.Box, parameterType);
                }
            }

            if (adviceMethod.IsConstructor)
            {
                ilProcessor.Emit(OpCodes.Newobj, adviceMethodRef);
                var instanceVariable = new VariableDefinition(aspectType);
                aspectInstances.Add(aspectType, instanceVariable);
                method.Body.Variables.Add(instanceVariable);
                ilProcessor.Append(ILPPUtils.SetLocal(instanceVariable));
            }
            else
            {
                ilProcessor.Emit(OpCodes.Call, adviceMethodRef);
            }
        }

        private GenericBinding GetBinding(GenericParameter genericParameter)
        {
            var genericBind = genericParameter.GetAttribute(typeof(PointcutGenericBind).FullName);
            var binding = GenericBinding.GenericParameterName;
            if (genericBind != null)
            {
                binding = (GenericBinding)(int)genericBind.ConstructorArguments[0].Value;
            }

            return binding;
        }

        private TypeReference FindBoundGeneicArgumentByName(MethodDefinition method, string name)
        {
            var genericArgument = method.GetGenericParameter(name);
            if (genericArgument == null)
            {
                genericArgument = method.DeclaringType.GetGenericParameter(name);
            }

            return genericArgument;
        }

        private void FindBoundGeneicArgumentByParameterType(MethodDefinition method, MethodDefinition advice, GenericParameter targetGenericParameter, ICollection<TypeReference> results)
        {
            foreach (var adviceParameter in advice.Parameters)
            {
                TypeReference methodParameterType;
                TypeReference adviceParameterType = adviceParameter.ParameterType;
                if (adviceParameterType is ByReferenceType byRefType)
                {
                    adviceParameterType = byRefType.ElementType;
                }

                if (HasPointcutReturned(adviceParameter))
                {
                    methodParameterType = method.ReturnType;
                }
                else if (HasPointcutThis(adviceParameter))
                {
                    methodParameterType = method.DeclaringType;
                    if (methodParameterType.IsGenericDefinition())
                    {
                        methodParameterType = methodParameterType.MakeGenericInstanceType(method.DeclaringType.GenericParameters);
                    }
                }
                else if (HasPointcutAccessorAttribute(adviceParameter))
                {
                    continue;
                }
                else
                {
                    var methodParameter = method.GetParameter(adviceParameter.Name);
                    if (methodParameter == null)
                    {
                        continue;
                    }

                    methodParameterType = methodParameter.ParameterType;
                }

                var result = FindBoundGeneicArgumentByParameterType(methodParameterType, adviceParameterType, targetGenericParameter);
                if (result == null)
                {
                    continue;
                }

                results.Add(method.Module.ImportReference(result));
            }
        }

        private TypeReference FindBoundGeneicArgumentByParameterType(TypeReference methodParameterTypeRef, TypeReference adviceParameterTypeRef, GenericParameter targetGenericParameter)
        {
            if (adviceParameterTypeRef is GenericParameter adviceGenericParameter)
            {
                if (adviceGenericParameter.Is(targetGenericParameter))
                {
                    return methodParameterTypeRef;
                }
            }
            else if (adviceParameterTypeRef is GenericInstanceType adviceGenericInstanceType)
            {
                if (methodParameterTypeRef is GenericInstanceType methodGenericInstanceType)
                {
                    for (int i = 0; i < adviceGenericInstanceType.GenericArguments.Count; ++i)
                    {
                        var adviceGenericArg = adviceGenericInstanceType.GenericArguments[i];
                        var methodGenericArg = methodGenericInstanceType.GenericArguments[i];

                        var result = FindBoundGeneicArgumentByParameterType(methodGenericArg, adviceGenericArg, targetGenericParameter);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
            }
            else if (adviceParameterTypeRef is OptionalModifierType adviceModOptType)
            {
                if (methodParameterTypeRef is OptionalModifierType methodModOptType)
                {
                    var result = FindBoundGeneicArgumentByParameterType(methodModOptType.ElementType, adviceModOptType.ElementType, targetGenericParameter);
                    if (result != null)
                    {
                        return result;
                    }

                    result = FindBoundGeneicArgumentByParameterType(methodModOptType.ModifierType, adviceModOptType.ModifierType, targetGenericParameter);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            else if (adviceParameterTypeRef is RequiredModifierType adviceModReqType)
            {
                if (methodParameterTypeRef is RequiredModifierType methodModReqType)
                {
                    var result = FindBoundGeneicArgumentByParameterType(methodModReqType.ElementType, adviceModReqType.ElementType, targetGenericParameter);
                    if (result != null)
                    {
                        return result;
                    }

                    result = FindBoundGeneicArgumentByParameterType(methodModReqType.ModifierType, adviceModReqType.ModifierType, targetGenericParameter);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            else if (adviceParameterTypeRef is FunctionPointerType functionPointerType)
            {
                if (methodParameterTypeRef is FunctionPointerType methodFunctionPointerType)
                {
                    var result = FindBoundGeneicArgumentByParameterType(methodFunctionPointerType.ReturnType, functionPointerType.ReturnType, targetGenericParameter);
                    if (result != null)
                    {
                        return result;
                    }

                    for (int i = 0; i < functionPointerType.Parameters.Count; ++i)
                    {
                        var adviceParamType = functionPointerType.Parameters[i].ParameterType;
                        var methodParamType = methodFunctionPointerType.Parameters[i].ParameterType;
                        result = FindBoundGeneicArgumentByParameterType(methodParamType, adviceParamType, targetGenericParameter);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
            }
            else if (adviceParameterTypeRef is TypeSpecification adviceTypeSpec)
            {
                if (methodParameterTypeRef is TypeSpecification methodTypeSpec)
                {
                    var result = FindBoundGeneicArgumentByParameterType(methodTypeSpec.ElementType, adviceTypeSpec.ElementType, targetGenericParameter);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            using (ThreadStaticListPool<TypeReference>.Get(out var baseTypes))
            {
                methodParameterTypeRef.GetBaseTypeAndInterfaces(baseTypes);
                foreach (var baseType in baseTypes)
                {
                    var result = FindBoundGeneicArgumentByParameterType(baseType, adviceParameterTypeRef, targetGenericParameter);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        private bool ContainsGenericParameter(TypeReference typeRef, GenericParameter genericParameter)
        {
            if (typeRef is GenericParameter gp)
            {
                if (gp.Is(genericParameter))
                {
                    return true;
                }

                return false;
            }

            if (typeRef is GenericInstanceType genericInstanceType)
            {
                foreach (var genericArg in genericInstanceType.GenericArguments)
                {
                    if (ContainsGenericParameter(genericArg, genericParameter))
                    {
                        return true;
                    }
                }

                return false;
            }

            if (typeRef is OptionalModifierType modOptType)
            {
                return ContainsGenericParameter(modOptType.ElementType, genericParameter) ||
                       ContainsGenericParameter(modOptType.ModifierType, genericParameter);
            }

            if (typeRef is RequiredModifierType modReqType)
            {
                return ContainsGenericParameter(modReqType.ElementType, genericParameter) ||
                       ContainsGenericParameter(modReqType.ModifierType, genericParameter);
            }

            if (typeRef is FunctionPointerType functionPointerType)
            {
                if (ContainsGenericParameter(functionPointerType.ReturnType, genericParameter))
                {
                    return true;
                }
                foreach (var param in functionPointerType.Parameters)
                {
                    if (ContainsGenericParameter(param.ParameterType, genericParameter))
                    {
                        return true;
                    }
                }

                return false;
            }

            if (typeRef is TypeSpecification typeSpec)
            {
                return ContainsGenericParameter(typeSpec.ElementType, genericParameter);
            }
            return false;
        }

        private bool ValidateAspect(TypeReference typeRef)
        {
            var typeDef = typeRef.Resolve();
            if (typeDef == null)
            {
                // TODO: Resolve references that may originate in another assembly.
                ILPPUtils.Log($"{_mainModule.Name}: resolve failed. {typeRef.FullName}");
                return true;
            }

            bool hasError = false;
            if (!typeRef.IsStaticType())
            {
                var hasAdviceConstructor = typeDef.Methods.Any(v => v.IsConstructor && v.HasAttribute(typeof(Advice).FullName));
                if (!hasAdviceConstructor)
                {
                    ILPPUtils.LogError("ASPECT1001", "AspectForUnity", $"Instance aspect \"{typeDef.FullName}\" must have a advice constructor.", typeRef);
                    hasError = true;
                }

                var isAbstract = typeDef.IsAbstract;
                if (isAbstract)
                {
                    ILPPUtils.LogError("ASPECT1002", "AspectForUnity", $"Instance aspect \"{typeDef.FullName}\" cannot be abstract.", typeRef);
                    hasError = true;
                }

                var isGeneric = typeDef.IsGenericDefinition();
                if (isGeneric)
                {
                    ILPPUtils.LogError("ASPECT1003", "AspectForUnity", $"Instance aspect \"{typeDef.FullName}\" cannot be a generic definition.", typeRef);
                    hasError = true;
                }
            }

            return hasError;
        }

        private bool ValidateAdvices(IEnumerable<AdviceInfo> adviceInfos)
        {
            bool hasError = false;
            foreach (var adviceInfo in adviceInfos)
            {
                if (ValidateAdvice(adviceInfo))
                {
                    hasError = true;
                }
            }

            return hasError;
        }

        private bool ValidateAdvice(AdviceInfo adviceInfo)
        {
            var adviceMethod = adviceInfo.Method.Resolve();
            if (adviceMethod == null)
            {
                // TODO: Resolve references that may originate in another assembly.
                ILPPUtils.Log($"{_mainModule.Name}: resolve failed. {adviceInfo.Method.FullName}");
                return true;
            }

            bool hasError = false;
            if (!adviceInfo.Pointcuts.Any())
            {
                ILPPUtils.LogError("ASPECT1101", "AspectForUnity", $"Advice method \"{adviceMethod.FullName}\" must have a Pointcut attribute, either on the method itself or on its declaring type.", adviceInfo.Method);
                hasError = true;
            }

            var declaringType = adviceMethod.DeclaringType;
            if (!declaringType.HasAttribute(typeof(Aspect).FullName))
            {
                ILPPUtils.LogError("ASPECT1102", "AspectForUnity", $"Advice method \"{adviceMethod.FullName}\" must be declared in a class with Aspect attribute.", adviceInfo.Method);
                hasError = true;
            }

            if (!adviceMethod.IsPublic)
            {
                ILPPUtils.LogError("ASPECT1103", "AspectForUnity", $"Advice method \"{adviceMethod.FullName}\" must be public.", adviceInfo.Method);
                hasError = true;
            }

            if (adviceMethod.Parameters.Any(v => v.IsOut))
            {
                ILPPUtils.LogError("ASPECT1104", "AspectForUnity", $"Advice method \"{adviceMethod.FullName}\" cannot have out parameters.", adviceInfo.Method);
                hasError = true;
            }

            if (adviceMethod.HasReturn())
            {
                ILPPUtils.LogError("ASPECT1105", "AspectForUnity", $"Advice method \"{adviceMethod.FullName}\" must have void return type.", adviceInfo.Method);
                hasError = true;
            }

            if (!adviceInfo.UnsafeInjection)
            {
                if (adviceMethod.Parameters.Any(v => !v.IsOut &&
                                                     !v.IsIn &&
                                                     v.ParameterType.IsByReference &&
                                                     !(adviceInfo.JoinPoint == JoinPoint.Around && HasPointcutProceed(v))))
                {
                    ILPPUtils.LogError("ASPECT1106", "AspectForUnity", $"Advice method \"{adviceMethod.FullName}\" cannot have ref parameters unless UnsafeInjection is true.", adviceInfo.Method);
                    hasError = true;
                }
            }

            if (adviceInfo.HasPointcutMethod)
            {
                var pointcutMethod = adviceMethod.Parameters.FirstOrDefault(HasPointcutMethod);
                if (pointcutMethod.ParameterType.FullName != typeof(System.Reflection.MethodBase).FullName)
                {
                    ILPPUtils.LogError("ASPECT1107", "AspectForUnity", $"PointcutMethod parameter \"{pointcutMethod.Name}\" must be of type MethodBase.", adviceInfo.Method);
                    hasError = true;
                }
            }

            if (adviceInfo.HasPointcutParameters)
            {
                var pointcutParameters = adviceMethod.Parameters.FirstOrDefault(HasPointcutParameters);
                if (pointcutParameters.ParameterType.FullName != typeof(ParameterArray).FullName)
                {
                    ILPPUtils.LogError("ASPECT1108", "AspectForUnity", $"PointcutParameters parameter \"{pointcutParameters.Name}\" cannot be of type ParameterArray.", adviceInfo.Method);
                    hasError = true;
                }
            }

            var parameters = adviceMethod.Parameters.Where(v => HasPointcutThis(v) || HasPointcutReturned(v) || HasPointcutProceed(v) || !HasPointcutAccessorAttribute(v));
            using (ThreadStaticArrayPool.Get(out var methodParts, parameters.Select(v => v.ParameterType)))
            {
                foreach (var genericParameter in adviceMethod.GenericParameters)
                {
                    if (genericParameter.GetNullableContextStatus() == NullableStatus.Nullable ||
                        genericParameter.Constraints.Any(v => v.GetNullableStatus() == NullableStatus.Nullable))
                    {
                        ILPPUtils.LogError("ASPECT1109", "AspectForUnity", $"Generic parameter \"{genericParameter.Name}\" in advice method \"{adviceMethod.FullName}\" cannot be nullable.", adviceInfo.Method);
                    }
                    
                    if (genericParameter.Constraints.Any(v => v.ConstraintType.ContainsGenericParameter))
                    {
                        ILPPUtils.LogError("ASPECT1110", "AspectForUnity", $"Generic parameter \"{genericParameter.Name}\" in advice method \"{adviceMethod.FullName}\" cannot have constraints that contain generic parameters.", adviceInfo.Method);
                    }

                    var binding = GetBinding(genericParameter);
                    switch (binding)
                    {
                        case GenericBinding.ParameterType:
                            {
                                if (methodParts.All(v => !ContainsGenericParameter(v, genericParameter)))
                                {
                                    ILPPUtils.LogError("ASPECT1111", "AspectForUnity", $"Generic parameter \"{genericParameter.Name}\" in advice method \"{adviceMethod.FullName}\" is not bound to any parameter types.", adviceInfo.Method);
                                    hasError = true;
                                }
                            }
                            break;
                    }
                }
            }

            switch (adviceInfo.JoinPoint)
            {
                case JoinPoint.Before:
                    hasError = ValidateBefore(adviceInfo) || hasError;
                    break;
                case JoinPoint.AfterReturning:
                    hasError = ValidateAfterReturning(adviceInfo) || hasError;
                    break;
                case JoinPoint.AfterThrowing:
                    hasError = ValidateAfterThrowing(adviceInfo) || hasError;
                    break;
                case JoinPoint.After:
                    hasError = ValidateAfter(adviceInfo) || hasError;
                    break;
                case JoinPoint.Around:
                    hasError = ValidateAround(adviceInfo) || hasError;
                    break;
            }

            return hasError;
        }

        private bool ValidateBefore(AdviceInfo adviceInfo)
        {
            bool hasError = false;
            if (adviceInfo.PointcutReturnedType != null)
            {
                ILPPUtils.LogError("ASPECT1201", "AspectForUnity", $"Before advice method \"{adviceInfo.Method.FullName}\" cannot have PointcutReturn parameter.", adviceInfo.Method);
                hasError = true;
            }

            if (adviceInfo.PointcutThrownType != null)
            {
                ILPPUtils.LogError("ASPECT1202", "AspectForUnity", $"Before advice method \"{adviceInfo.Method.FullName}\" cannot have PointcutThrown parameter.", adviceInfo.Method);
                hasError = true;
            }

            return hasError;
        }

        private bool ValidateAfterReturning(AdviceInfo adviceInfo)
        {
            bool hasError = false;
            var adviceMethod = adviceInfo.Method.Resolve();
            if (adviceInfo.PointcutThrownType != null)
            {
                ILPPUtils.LogError("ASPECT1301", "AspectForUnity", $"Before advice method \"{adviceInfo.Method.FullName}\" cannot have PointcutThrown parameter.", adviceInfo.Method);
                hasError = true;
            }

            if (adviceMethod.IsConstructor)
            {
                ILPPUtils.LogError("ASPECT1302", "AspectForUnity", $"AfterReturning advice method \"{adviceInfo.Method.FullName}\" cannot be a constructor.", adviceInfo.Method);
                hasError = true;
            }

            return hasError;
        }

        private bool ValidateAfterThrowing(AdviceInfo adviceInfo)
        {
            bool hasError = false;
            var adviceMethod = adviceInfo.Method.Resolve();
            if (adviceInfo.PointcutReturnedType != null)
            {
                ILPPUtils.LogError("ASPECT1401", "AspectForUnity", $"Before advice method \"{adviceInfo.Method.FullName}\" cannot have PointcutReturn parameter.", adviceInfo.Method);
                hasError = true;
            }

            if (adviceMethod.Parameters.Count(HasPointcutThrown) > 1)
            {
                ILPPUtils.LogError("ASPECT1402", "AspectForUnity", $"AfterThrowing advice method \"{adviceInfo.Method.FullName}\" cannot have more than one PointcutThrown parameter.", adviceInfo.Method);
                hasError = true;
            }

            if (adviceInfo.PointcutThrownType != null &&
                !adviceInfo.PointcutThrownType.IsCompatible(_exceptionType))
            {
                ILPPUtils.LogError("ASPECT1403", "AspectForUnity", $"AfterThrowing advice method \"{adviceInfo.Method.FullName}\" may only declare a PointcutThrown parameter of type Exception.", adviceInfo.Method);
                hasError = true;
            }

            if (adviceMethod.IsConstructor)
            {
                ILPPUtils.LogError("ASPECT1404", "AspectForUnity", $"AfterThrowing advice method \"{adviceInfo.Method.FullName}\" cannot be a constructor.", adviceInfo.Method);
                hasError = true;
            }

            return hasError;
        }

        private static bool ValidateAfter(AdviceInfo adviceInfo)
        {
            bool hasError = false;
            var adviceMethod = adviceInfo.Method.Resolve();
            if (adviceInfo.PointcutReturnedType != null)
            {
                ILPPUtils.LogError("ASPECT1501", "AspectForUnity", $"After advice method \"{adviceInfo.Method.FullName}\" cannot have PointcutReturn parameter.", adviceInfo.Method);
                hasError = true;
            }

            if (adviceInfo.PointcutThrownType != null)
            {
                ILPPUtils.LogError("ASPECT1502", "AspectForUnity", $"Before advice method \"{adviceInfo.Method.FullName}\" cannot have PointcutThrown parameter.", adviceInfo.Method);
                hasError = true;
            }

            if (adviceMethod.IsConstructor)
            {
                ILPPUtils.LogError("ASPECT1503", "AspectForUnity", $"After advice method \"{adviceInfo.Method.FullName}\" cannot be a constructor.", adviceInfo.Method);
                hasError = true;
            }

            return hasError;
        }

        private bool ValidateAround(AdviceInfo adviceInfo)
        {
            var hasError = false;
            var adviceMethod = adviceInfo.Method.Resolve();
            var proceedParameters = adviceMethod.Parameters.Where(HasPointcutProceed).ToArray();
            if (proceedParameters.Length != 1)
            {
                ILPPUtils.LogError("ASPECT1602", "AspectForUnity", $"Around advice method \"{adviceInfo.Method.FullName}\" must have exactly one PointcutProceed parameter.", adviceInfo.Method);
                hasError = true;
            }

            if (adviceInfo.PointcutReturnedType != null)
            {
                ILPPUtils.LogError("ASPECT1603", "AspectForUnity", $"Around advice method \"{adviceInfo.Method.FullName}\" cannot have a PointcutReturned parameter. Use ProceedingContext.ReturnValue instead.", adviceInfo.Method);
                hasError = true;
            }

            if (adviceInfo.PointcutThrownType != null)
            {
                ILPPUtils.LogError("ASPECT1604", "AspectForUnity", $"Around advice method \"{adviceInfo.Method.FullName}\" cannot have a PointcutThrown parameter.", adviceInfo.Method);
                hasError = true;
            }

            if (adviceMethod.IsConstructor)
            {
                ILPPUtils.LogError("ASPECT1605", "AspectForUnity", $"Around advice method \"{adviceInfo.Method.FullName}\" cannot be a constructor.", adviceInfo.Method);
                hasError = true;
            }

            if (proceedParameters.Length != 1)
            {
                return hasError;
            }

            var proceedParameter = proceedParameters[0];
            var contextType = proceedParameter.ParameterType is ByReferenceType proceedByReference
                ? proceedByReference.ElementType
                : proceedParameter.ParameterType;
            var unsafeContext = contextType as GenericInstanceType;
            var isUnsafeContext = unsafeContext != null &&
                                  unsafeContext.ElementType.FullName == _unsafeProceedingContextGeneric.FullName;
            var isVoidContext = contextType.FullName == _proceedingContext.FullName;
            var valueContext = contextType as GenericInstanceType;
            var isValueContext = valueContext != null &&
                                 valueContext.ElementType.FullName == _proceedingContextGeneric.FullName;

            if (!isVoidContext && !isValueContext && !isUnsafeContext)
            {
                ILPPUtils.LogError("ASPECT1609", "AspectForUnity", $"PointcutProceed parameter \"{proceedParameter.Name}\" in Around advice method \"{adviceInfo.Method.FullName}\" has an invalid context type.", adviceInfo.Method);
                hasError = true;
            }

            if (!adviceInfo.UnsafeInjection && isUnsafeContext)
            {
                ILPPUtils.LogError("ASPECT1607", "AspectForUnity", $"Around advice method \"{adviceInfo.Method.FullName}\" may use UnsafeProceedingContext only when unsafeInjection is true.", adviceInfo.Method);
                hasError = true;
            }

            if (adviceInfo.UnsafeInjection && !isUnsafeContext && !isVoidContext)
            {
                ILPPUtils.LogError("ASPECT1608", "AspectForUnity", $"Around advice method \"{adviceInfo.Method.FullName}\" must use UnsafeProceedingContext for a return value when unsafeInjection is true.", adviceInfo.Method);
                hasError = true;
            }

            return hasError;
        }

        private bool ValidateAdvices(MethodDefinition method, IEnumerable<AdviceInfo> adviceInfos)
        {
            bool hasError = false;
            var declaringTypeToValidInstanceAdvices = new Dictionary<TypeReference, List<AdviceInfo>>(TypeReferenceComparer.Default);
            foreach (var adviceInfo in adviceInfos)
            {
                if (ValidateAdvice(method, adviceInfo))
                {
                    hasError = true;
                }

                if (adviceInfo.Method.HasThis)
                {
                    if (!declaringTypeToValidInstanceAdvices.TryGetValue(adviceInfo.Method.DeclaringType, out var list))
                    {
                        list = new List<AdviceInfo>();
                        declaringTypeToValidInstanceAdvices.Add(adviceInfo.Method.DeclaringType, list);
                    }
                    list.Add(adviceInfo);
                }
            }

            foreach (var pair in declaringTypeToValidInstanceAdvices)
            {
                if (ValidateAspectAdvices(method, pair.Key, pair.Value))
                {
                    hasError = true;
                }
            }

            return hasError;
        }

        private bool ValidateAdvice(MethodDefinition method,
                                     AdviceInfo adviceInfo,
                                     TypeReference returnedTypeOverride = null)
        {
            bool hasError = false;
            var adviceMethodRef = adviceInfo.Method;
            var adviceMethod = adviceMethodRef.Resolve();

            foreach (var adviceGenericParameter in adviceMethod.GenericParameters)
            {
                var binding = GetBinding(adviceGenericParameter);
                TypeReference genericArgument = null;
                switch (binding)
                {
                    case GenericBinding.GenericParameterName:
                        {
                            genericArgument = FindBoundGeneicArgumentByName(method, adviceGenericParameter.Name);
                            if (genericArgument == null)
                            {
                                ILPPUtils.LogError("ASPECT2101", "AspectForUnity", $"Generic parameter \"{adviceGenericParameter.Name}\" is not defined in \"{method.FullName}\".", adviceInfo.Method);
                                hasError = true;
                                continue;
                            }
                        }
                        break;
                    case GenericBinding.ParameterType:
                        {
                            using (ThreadStaticListPool<TypeReference>.Get(out var genericArgumentsByParameter))
                            {
                                FindBoundGeneicArgumentByParameterType(method, adviceMethod, adviceGenericParameter, genericArgumentsByParameter);
                                var genericArgumentsByParameterDistinct = genericArgumentsByParameter.Distinct(TypeReferenceComparer.Default);
                                if (genericArgumentsByParameterDistinct.Count() > 1)
                                {
                                    var genericArgumentsList = string.Join(", ", genericArgumentsByParameterDistinct.Select(v => v.FullName));
                                    ILPPUtils.LogError("ASPECT2102", "AspectForUnity", $"Multiple bound types found for generic parameter \"{adviceGenericParameter.Name}\" in \"{method.FullName}\": {genericArgumentsList}.", adviceInfo.Method);
                                    hasError = true;
                                }
                                genericArgument = genericArgumentsByParameter.FirstOrDefault();
                            }
                            if (genericArgument == null)
                            {
                                ILPPUtils.LogError("ASPECT2103", "AspectForUnity", $"Cannot find bound type for generic parameter \"{adviceGenericParameter.Name}\" in \"{method.FullName}\".", adviceInfo.Method);
                                hasError = true;
                                continue;
                            }
                        }
                        break;
                    case GenericBinding.ReturnType:
                        {
                            genericArgument = returnedTypeOverride ?? method.ReturnType;
                            if (genericArgument is ByReferenceType returnByReferenceType)
                            {
                                genericArgument = returnByReferenceType.ElementType;
                            }
                        }
                        break;
                }

                if (!genericArgument.IsCompatible(adviceGenericParameter))
                {
                    ILPPUtils.LogError("ASPECT2104", "AspectForUnity", $"Type mismatch for generic parameter \"{adviceGenericParameter.Name}\" in \"{method.FullName}\".", adviceInfo.Method);
                    hasError = true;
                    continue;
                }
            }

            foreach (var adviceParameter in adviceMethod.Parameters.Where(v => !HasPointcutAccessorAttribute(v)))
            {
                var parameter = method.GetParameter(adviceParameter.Name);
                if (parameter == null)
                {
                    ILPPUtils.LogError("ASPECT2105", "AspectForUnity", $"\"{adviceParameter.Name}\" is not defined  in \"{method.FullName}\".", adviceInfo.Method);
                    hasError = true;
                    continue;
                }

                var parameterType = parameter.ParameterType;
                if (parameterType is ByReferenceType byReferenceType)
                {
                    parameterType = byReferenceType.ElementType;
                }

                var adviceParameterType = adviceParameter.ParameterType;
                if (adviceParameterType is ByReferenceType adviceByReferenceType)
                {
                    if (!parameterType.IsCompatibleToByReference(adviceByReferenceType))
                    {
                        ILPPUtils.LogError("ASPECT2106", "AspectForUnity", $"Type mismatch for parameter \"{adviceParameter.Name}\" in \"{method.FullName}\". Expected type: {parameter.ParameterType.FullName}, but found: {adviceParameter.ParameterType.FullName}.", adviceInfo.Method);
                        hasError = true;
                        continue;
                    }
                }
                else
                {
                    if (!parameterType.IsCompatible(adviceParameterType))
                    {
                        ILPPUtils.LogError("ASPECT2107", "AspectForUnity", $"Type mismatch for parameter \"{adviceParameter.Name}\" in \"{method.FullName}\". Expected type: {parameter.ParameterType.FullName}, but found: {adviceParameter.ParameterType.FullName}.", adviceInfo.Method);
                        hasError = true;
                        continue;
                    }
                }
            }

            if (adviceInfo.PointcutThisType != null)
            {
                var declaringType = method.DeclaringType;
                if (!declaringType.IsCompatible(adviceInfo.PointcutThisType))
                {
                    ILPPUtils.LogError("ASPECT2108", "AspectForUnity", $"Type mismatch for PointcutThis parameter in advice method \"{adviceInfo.Method.FullName}\". Expected type: {declaringType.FullName}, but found: {adviceInfo.PointcutThisType.FullName}.", adviceInfo.Method);
                    hasError = true;
                }
            }

            switch (adviceInfo.JoinPoint)
            {
                case JoinPoint.Before:
                    hasError = ValidateBefore(method, adviceInfo) || hasError;
                    break;
                case JoinPoint.AfterReturning:
                    hasError = ValidateAfterReturning(method, adviceInfo, returnedTypeOverride) || hasError;
                    break;
                case JoinPoint.AfterThrowing:
                    hasError = ValidateAfterThrowing(method, adviceInfo) || hasError;
                    break;
                case JoinPoint.After:
                    hasError = ValidateAfter(method, adviceInfo) || hasError;
                    break;
                case JoinPoint.Around:
                    hasError = ValidateAround(method, adviceInfo) || hasError;
                    break;
            }

            return hasError;
        }

        private bool ValidateBefore(MethodDefinition method, AdviceInfo adviceInfo)
        {
            bool hasError = false;
            var adviceMethodRef = adviceInfo.Method;
            var adviceMethod = adviceMethodRef.Resolve();

            foreach (var adviceParameter in adviceMethod.Parameters.Where(v => !HasPointcutAccessorAttribute(v)))
            {
                var parameter = method.GetParameter(adviceParameter.Name);
                if (parameter != null && parameter.IsOut)
                {
                    ILPPUtils.LogError("ASPECT2201", "AspectForUnity", $"Before advice method \"{adviceInfo.Method.FullName}\" cannot bind to out parameter \"{parameter.Name}\" of method \"{method.FullName}\".", adviceInfo.Method);
                    hasError = true;
                }
            }

            return hasError;
        }

        private bool ValidateAfterReturning(MethodDefinition method,
                                             AdviceInfo adviceInfo,
                                             TypeReference returnedTypeOverride = null)
        {
            bool hasError = false;

            var methodReturnType = returnedTypeOverride ?? method.ReturnType;
            if (methodReturnType is ByReferenceType byReferenceType)
            {
                methodReturnType = byReferenceType.ElementType;
            }

            var pointcutReturnedType = adviceInfo.PointcutReturnedType;
            if (pointcutReturnedType is ByReferenceType pointcutReturnedByReferenceType)
            {
                if (!methodReturnType.IsCompatibleToByReference(pointcutReturnedByReferenceType))
                {
                    ILPPUtils.LogError("ASPECT2301", "AspectForUnity", $"Type mismatch for PointcutReturn parameter in AfterReturning advice method \"{adviceInfo.Method.FullName}\". Expected type: {method.ReturnType.FullName}, but found: {adviceInfo.PointcutReturnedType.FullName}.", adviceInfo.Method);
                    hasError = true;
                }
            }
            else
            {
                if (pointcutReturnedType != null &&
                    !methodReturnType.IsCompatible(pointcutReturnedType))
                {
                    ILPPUtils.LogError("ASPECT2302", "AspectForUnity", $"Type mismatch for PointcutReturn parameter in AfterReturning advice method \"{adviceInfo.Method.FullName}\". Expected type: {method.ReturnType.FullName}, but found: {adviceInfo.PointcutReturnedType.FullName}.", adviceInfo.Method);
                    hasError = true;
                }
            }

            if (pointcutReturnedType != null &&
                returnedTypeOverride == null &&
                !method.HasReturn())
            {
                ILPPUtils.LogError("ASPECT2303", "AspectForUnity", $"AfterReturning advice method \"{adviceInfo.Method.FullName}\" cannot have PointcutReturn parameter when applied to void method \"{method.FullName}\".", adviceInfo.Method);
                hasError = true;
            }

            return hasError;
        }

        private bool ValidateAfterThrowing(MethodDefinition method, AdviceInfo adviceInfo)
        {
            bool hasError = false;

            var adviceMethodRef = adviceInfo.Method;
            var adviceMethod = adviceMethodRef.Resolve();
            foreach (var adviceParameter in adviceMethod.Parameters.Where(v => !HasPointcutAccessorAttribute(v)))
            {
                var parameter = method.GetParameter(adviceParameter.Name);
                if (parameter != null && parameter.IsOut)
                {
                    ILPPUtils.LogError("ASPECT2401", "AspectForUnity", $"AfterThrowing advice method \"{adviceInfo.Method.FullName}\" cannot bind to out parameter \"{parameter.Name}\" of method \"{method.FullName}\".", adviceInfo.Method);
                    hasError = true;
                }
            }
            return hasError;
        }

        private bool ValidateAfter(MethodDefinition method, AdviceInfo adviceInfo)
        {
            bool hasError = false;
            var adviceMethodRef = adviceInfo.Method;
            var adviceMethod = adviceMethodRef.Resolve();
            foreach (var adviceParameter in adviceMethod.Parameters.Where(v => !HasPointcutAccessorAttribute(v)))
            {
                var parameter = method.GetParameter(adviceParameter.Name);
                if (parameter != null && parameter.IsOut)
                {
                    ILPPUtils.LogError("ASPECT2501", "AspectForUnity", $"After advice method \"{adviceInfo.Method.FullName}\" cannot bind to out parameter \"{parameter.Name}\" of method \"{method.FullName}\".", adviceInfo.Method);
                    hasError = true;
                }
            }
            return hasError;
        }

        private bool ValidateAround(MethodDefinition method, AdviceInfo adviceInfo)
        {
            var hasError = false;
            var adviceMethod = adviceInfo.Method.Resolve();
            if (method.IsConstructor)
            {
                ILPPUtils.LogError("ASPECT1613",
                                   "AspectForUnity",
                                   $"Around advice cannot be applied to constructor method \"{method.FullName}\".",
                                   adviceInfo.Method);
                hasError = true;
            }

            var proceedParameter = adviceMethod.Parameters.FirstOrDefault(HasPointcutProceed);
            if (proceedParameter == null)
            {
                return true;
            }

            var contextType = proceedParameter.ParameterType is ByReferenceType proceedByReference
                ? proceedByReference.ElementType
                : proceedParameter.ParameterType;
            var unsafeContext = contextType as GenericInstanceType;
            var isUnsafeContext = unsafeContext != null &&
                                  unsafeContext.ElementType.FullName == _unsafeProceedingContextGeneric.FullName;
            var isVoidContext = contextType.FullName == _proceedingContext.FullName;
            var valueContext = contextType as GenericInstanceType;
            var isValueContext = valueContext != null &&
                                 valueContext.ElementType.FullName == _proceedingContextGeneric.FullName;
            var methodReturnType = method.ReturnType;
            if (methodReturnType is ByReferenceType methodReturnByReferenceType)
            {
                methodReturnType = methodReturnByReferenceType.ElementType;
            }

            if (!method.HasReturn())
            {
                if (!isVoidContext)
                {
                    ILPPUtils.LogError("ASPECT1610", "AspectForUnity", $"Around advice method \"{adviceInfo.Method.FullName}\" must use ProceedingContext for a void target.", adviceInfo.Method);
                    hasError = true;
                }
            }
            else
            {
                if ((!isValueContext && !isUnsafeContext) || valueContext == null)
                {
                    ILPPUtils.LogError("ASPECT1611", "AspectForUnity", $"Around advice method \"{adviceInfo.Method.FullName}\" must use a generic ProceedingContext for a return value.", adviceInfo.Method);
                    hasError = true;
                }
                else
                {
                    var contextReturnType = valueContext.GenericArguments[0];
                    var isReturnTypeBinding = contextReturnType is GenericParameter genericParameter &&
                                              GetBinding(genericParameter) == GenericBinding.ReturnType;
                    if (!isReturnTypeBinding && !methodReturnType.IsCompatible(contextReturnType))
                    {
                        ILPPUtils.LogError("ASPECT1612", "AspectForUnity", $"Type mismatch for ProceedingContext return value in Around advice method \"{adviceInfo.Method.FullName}\".", adviceInfo.Method);
                        hasError = true;
                    }
                }
            }

            if (isUnsafeContext && !adviceInfo.UnsafeInjection)
            {
                ILPPUtils.LogError("ASPECT1607", "AspectForUnity", $"Around advice method \"{adviceInfo.Method.FullName}\" may use UnsafeProceedingContext only when unsafeInjection is true.", adviceInfo.Method);
                hasError = true;
            }

            if (adviceInfo.UnsafeInjection && method.HasReturn() && !isUnsafeContext)
            {
                ILPPUtils.LogError("ASPECT1608", "AspectForUnity", $"Around advice method \"{adviceInfo.Method.FullName}\" must use UnsafeProceedingContext for a return value when unsafeInjection is true.", adviceInfo.Method);
                hasError = true;
            }

            return hasError;
        }

        private bool ValidateAspectAdvices(MethodDefinition method, TypeReference aspect, IEnumerable<AdviceInfo> adviceInfos)
        {
            bool hasError = false;
            var constructorCount = adviceInfos.Count(v => v.Method.Resolve().IsConstructor);
            if (constructorCount <= 0)
            {
                ILPPUtils.LogError("ASPECT2001", "AspectForUnity", $"Aspect \"{aspect.FullName}\" must have at least one advice constructor to be applied to method \"{method.FullName}\".", aspect);
                hasError = true;
            }

            if (constructorCount >= 2)
            {
                ILPPUtils.LogError("ASPECT2002", "AspectForUnity", $"Aspect \"{aspect.FullName}\" cannot have more than one advice constructor to be applied to method \"{method.FullName}\".", aspect);
                hasError = true;
            }


            return hasError;
        }

        private static bool HasPointcutAccessorAttribute(ParameterDefinition parameter)
        {
            return HasPointcutMethod(parameter) ||
                   HasPointcutParameters(parameter) ||
                   HasPointcutProceed(parameter) ||
                   HasPointcutReturned(parameter) ||
                   HasPointcutThis(parameter) ||
                   HasPointcutThrown(parameter);
        }

        private static bool HasPointcutReturned(ParameterDefinition parameter)
        {
            return parameter.HasAttribute(typeof(PointcutReturned).FullName);
        }

        private static bool HasPointcutMethod(ParameterDefinition parameter)
        {
            return parameter.HasAttribute(typeof(PointcutMethod).FullName);
        }

        private static bool HasPointcutParameters(ParameterDefinition parameter)
        {
            return parameter.HasAttribute(typeof(PointcutParameters).FullName);
        }

        private static bool HasPointcutProceed(ParameterDefinition parameter)
        {
            return parameter.HasAttribute(typeof(PointcutProceed).FullName);
        }

        private static bool HasPointcutThis(ParameterDefinition parameter)
        {
            return parameter.HasAttribute(typeof(PointcutThis).FullName);
        }

        private static bool HasPointcutThrown(ParameterDefinition parameter)
        {
            return parameter.HasAttribute(typeof(PointcutThrown).FullName);
        }
    }
}
