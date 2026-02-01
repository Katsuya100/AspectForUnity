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
        private MethodReference _objectArrayPoolRent;
        private MethodReference _objectArrayPoolReturn;
        private MethodReference _getMethodFromHandle = null;
        private TypeReference _exceptionType;

        private struct AdviceInfo
        {
            public bool HasPointcutMethod;
            public bool HasPointcutParameters;
            public TypeReference PointcutThisType;
            public TypeReference PointcutReturnedType;
            public TypeReference PointcutThrownType;
            public MethodReference Method;
            public JoinPoint JoinPoint;
            public bool UnsafeInjection;
            public IReadOnlyList<IPointcutInfo> Pointcuts;
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

                    using (ThreadStaticArrayPool.Get(out var allTypes, assembly.Modules.SelectMany(v => v.Types).GetAllTypes()))
                    {
                        foreach (var type in allTypes)
                        {
                            OutputPointcutMethodNameProcessor(type);
                        }

                        // 定義しているdll内でのみAdviceをValidateする
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

                        // 参照しているアセンブリ一覧
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
                                        method.HasAttribute(typeof(BlockAspect).FullName))
                                    {
                                        continue;
                                    }

                                    AdviceProcessor(type, method);

                                    var body = method.Body;
                                    ILPPUtils.ResolveInstructionOpCode(body.Instructions);
                                    body.Optimize();
                                }
                            }
                        }

                        return compiledAssembly.GetResult(assembly);
                    }
                }
            }
            catch (Exception e)
            {
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
                // TODO: 一部ここを通ってしまうので何らか解決すべき
                ILPPUtils.Log($"{_mainModule.Name}: resolve failed. {methodRef.FullName}");
                return;
            }

            var advice = method.GetAttribute(typeof(Advice).FullName);
            if (advice == null)
            {
                return;
            }

            var joinPoint = (JoinPoint)(int)advice.ConstructorArguments[0].Value;
            var unsafeInjection = (bool)advice.ConstructorArguments[1].Value;
            var pointcuts = CreatePointcutInfos(method);
            var hasPointcutMethod = method.Parameters.Any(HasPointcutMethod);
            var hasPointcutParameters = method.Parameters.Any(HasPointcutParameters);

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
                PointcutThisType = pointcutThisType,
                PointcutReturnedType = pointcutReturnedType,
                PointcutThrownType = pointcutThrownType,
                Method = methodRef,
                JoinPoint = joinPoint,
                UnsafeInjection = unsafeInjection,
                Pointcuts = pointcuts,
            };

            result.Add(adviceInfo);
        }

        private static IPointcutInfo CreatePointcutInfo(CustomAttribute pointcut)
        {
            if (pointcut.AttributeType.FullName == typeof(RegexPointcut).FullName)
            {
                return new RegexPointcutInfo(pointcut);
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
            using (ThreadStaticArrayPool.Get(out var advices, _advices.Where(v => v.Pointcuts.All(p => p.IsMatch(method)))))
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

                // オリジナルの処理を別メソッドにコピー
                var methodAttributes = method.Attributes;
                methodAttributes &= ~(Mono.Cecil.MethodAttributes.Virtual | Mono.Cecil.MethodAttributes.Abstract);
                var aspectInstances = new Dictionary<TypeReference, VariableDefinition>(TypeReferenceComparer.Default);
                var copyMethod = new MethodDefinition($"$Katuusagi.AspectForUnity${method.Name}", methodAttributes, method.ReturnType);
                {
                    foreach (var ca in method.CustomAttributes)
                    {
                        copyMethod.CustomAttributes.Add(ca);
                    }

                    foreach (var g in method.GenericParameters)
                    {
                        var genericParameter = new GenericParameter(g.Name, copyMethod);
                        copyMethod.GenericParameters.Add(genericParameter);
                        foreach (var ca in g.CustomAttributes)
                        {
                            genericParameter.CustomAttributes.Add(ca);
                        }
                    }

                    foreach (var p in method.Parameters)
                    {
                        var parameterType = p.ParameterType;
                        if (p.ParameterType is GenericParameter genericParameter &&
                            genericParameter.DeclaringMethod.Is(method))
                        {
                            parameterType = copyMethod.GetGenericParameter(parameterType.Name);
                        }

                        var parameter = new ParameterDefinition(p.Name, p.Attributes, parameterType);
                        copyMethod.Parameters.Add(parameter);
                        foreach (var ca in p.CustomAttributes)
                        {
                            parameter.CustomAttributes.Add(ca);
                        }
                    }

                    {
                        if (method.ReturnType is GenericParameter genericParameter &&
                            genericParameter.DeclaringMethod.Is(method))
                        {
                            copyMethod.ReturnType = copyMethod.GetGenericParameter(copyMethod.ReturnType.Name);
                        }

                        foreach (var ca in method.MethodReturnType.CustomAttributes)
                        {
                            copyMethod.MethodReturnType.CustomAttributes.Add(ca);
                        }
                    }

                    copyMethod.ImplAttributes = method.ImplAttributes;
                    copyMethod.SemanticsAttributes = method.SemanticsAttributes;
                    // copyMethod.CustomAttributes = ...
                    copyMethod.DebugInformation = method.DebugInformation;
                    copyMethod.AggressiveInlining = true;
                    copyMethod.NoInlining = false;
                    copyMethod.AggressiveOptimization = method.AggressiveOptimization;
                    copyMethod.HasThis = method.HasThis;
                    copyMethod.ExplicitThis = method.ExplicitThis;
                    copyMethod.CallingConvention = method.CallingConvention;
                    copyMethod.Body.InitLocals = body.InitLocals;
                    copyMethod.Body.MaxStackSize = body.MaxStackSize;

                    foreach (var v in body.Variables)
                    {
                        copyMethod.Body.Variables.Add(v);
                    }
                    body.Variables.Clear();

                    foreach (var instruction in body.Instructions)
                    {
                        if (instruction.Operand is ParameterReference p)
                        {
                            instruction.Operand = copyMethod.Parameters[p.Index];
                        }
                        if (instruction.Operand is GenericParameter gp &&
                            gp.DeclaringMethod.Is(method))
                        {
                            instruction.Operand = copyMethod.GetGenericParameter(gp.Name);
                        }

                        copyMethod.Body.Instructions.Add(instruction);
                    }
                    body.Instructions.Clear();

                    foreach (var eh in body.ExceptionHandlers)
                    {
                        copyMethod.Body.ExceptionHandlers.Add(eh);
                    }
                    body.ExceptionHandlers.Clear();
                }

                type.Methods.Add(copyMethod);

                // オリジナルメソッドの再構築
                {
                    var ilProcessor = body.GetILProcessor();
                    var hasTry = afterThrowingAdvices.Any() || afterAdvices.Any();
                    var hasResultVariable = method.HasReturn() && afterReturningAdvices.Any(v => v.PointcutReturnedType != null);

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
                            methodInstance.HasThis = copyMethod.HasThis;
                            methodInstance.ExplicitThis = copyMethod.ExplicitThis;
                            methodInstance.CallingConvention = copyMethod.CallingConvention;
                            foreach (var genericParameter in copyMethod.GenericParameters)
                            {
                                methodInstance.GenericParameters.Add(new GenericParameter(genericParameter.Name, methodInstance));
                            }
                            foreach (var p in copyMethod.Parameters)
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
                    if (hasPointcutParameters)
                    {
                        parameterArrayTmp = new VariableDefinition(_objectArray);
                        method.Body.Variables.Add(parameterArrayTmp);

                        ilProcessor.Append(ILPPUtils.LoadLiteral(method.Parameters.Count));
                        ilProcessor.Emit(OpCodes.Call, _objectArrayPoolRent);
                        ilProcessor.Append(ILPPUtils.SetLocal(parameterArrayTmp));
                    }

                    var processStart = Instruction.Create(OpCodes.Nop);
                    ilProcessor.Append(processStart);

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

                    // Before advice
                    foreach (var advice in beforeAdvices)
                    {
                        AppendCallAdvice(ilProcessor, method, advice, aspectInstances, methodBase, null, parameterArray, null);
                    }

                    // 本体の呼び出し
                    var callStart = Instruction.Create(OpCodes.Nop);
                    ilProcessor.Append(callStart);

                    if (method.HasThis)
                    {
                        ilProcessor.Emit(OpCodes.Ldarg_0);
                    }

                    foreach (var parameter in method.Parameters)
                    {
                        ilProcessor.Append(ILPPUtils.LoadArgument(parameter));
                    }

                    TypeReference callDeclaringType = copyMethod.DeclaringType;
                    MethodReference callMethod = copyMethod;
                    if (copyMethod.DeclaringType.IsGenericDefinition())
                    {
                        callDeclaringType = callDeclaringType.MakeGenericInstanceType(copyMethod.DeclaringType.GenericParameters);
                        callDeclaringType = _mainModule.ImportReference(callDeclaringType);
                        callMethod = new MethodReference(callMethod.Name, callMethod.ReturnType, callDeclaringType);
                        callMethod.HasThis = copyMethod.HasThis;
                        callMethod.ExplicitThis = copyMethod.ExplicitThis;
                        callMethod.CallingConvention = copyMethod.CallingConvention;
                        foreach (var genericParameter in copyMethod.GenericParameters)
                        {
                            callMethod.GenericParameters.Add(new GenericParameter(genericParameter.Name, callMethod));
                        }
                        foreach (var p in copyMethod.Parameters)
                        {
                            callMethod.Parameters.Add(new ParameterDefinition(p.Name, p.Attributes, p.ParameterType));
                        }
                    }

                    if (copyMethod.IsGenericDefinition())
                    {
                        callMethod = callMethod.MakeGenericInstanceMethod(method.GenericParameters);
                        callMethod = _mainModule.ImportReference(callMethod);
                    }
                    ilProcessor.Emit(OpCodes.Call, callMethod);
                    VariableDefinition resultVariable = null;
                    if (hasResultVariable)
                    {
                        resultVariable = new VariableDefinition(method.ReturnType);
                        method.Body.Variables.Add(resultVariable);
                        ilProcessor.Append(ILPPUtils.SetLocal(resultVariable));
                    }

                    // AfterReturning advice
                    foreach (var advice in afterReturningAdvices)
                    {
                        AppendCallAdvice(ilProcessor, method, advice, aspectInstances, methodBase, resultVariable, parameterArray, null);
                    }

                    var methodEnd = Instruction.Create(OpCodes.Nop);
                    var processEnd = Instruction.Create(OpCodes.Nop);
                    if (hasTry || hasPointcutParameters)
                    {
                        ilProcessor.Emit(OpCodes.Leave, processEnd);
                    }

                    var callEnd = Instruction.Create(OpCodes.Nop);
                    ilProcessor.Append(callEnd);
                    var handleStart = callEnd;

                    // AfterThrowing advice
                    if (afterThrowingAdvices.Any())
                    {
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

                        var catchStart = handleStart;
                        var catchEnd = methodEnd;
                        if (afterAdvices.Any())
                        {
                            catchEnd = Instruction.Create(OpCodes.Nop);
                            ilProcessor.Append(catchEnd);
                            handleStart = catchEnd;
                        }

                        var exceptionHandler = new ExceptionHandler(ExceptionHandlerType.Catch)
                        {
                            TryStart = callStart,
                            TryEnd = callEnd,
                            HandlerStart = catchStart,
                            HandlerEnd = catchEnd,
                            CatchType = _exceptionType,
                        };
                        body.ExceptionHandlers.Add(exceptionHandler);
                    }

                    // After advice
                    if (afterAdvices.Any())
                    {
                        foreach (var advice in afterAdvices)
                        {
                            AppendCallAdvice(ilProcessor, method, advice, aspectInstances, methodBase, null, parameterArray, null);
                        }

                        ilProcessor.Emit(OpCodes.Endfinally);

                        var finallyStart = handleStart;
                        var finallyEnd = methodEnd;
                        var exceptionHandler = new ExceptionHandler(ExceptionHandlerType.Finally)
                        {
                            TryStart = callStart,
                            TryEnd = finallyStart,
                            HandlerStart = finallyStart,
                            HandlerEnd = finallyEnd,
                        };
                        body.ExceptionHandlers.Add(exceptionHandler);
                    }

                    ilProcessor.Append(methodEnd);

                    if (hasTry)
                    {
                        handleStart = methodEnd;
                    }

                    // PointcutParamterを持っている場合は解放処理
                    if (hasPointcutParameters)
                    {
                        ilProcessor.Append(ILPPUtils.LoadLocal(parameterArrayTmp));
                        ilProcessor.Emit(OpCodes.Call, _objectArrayPoolReturn);
                        ilProcessor.Emit(OpCodes.Endfinally);
                        var finallyStart = handleStart;
                        var finallyEnd = processEnd;
                        var exceptionHandler = new ExceptionHandler(ExceptionHandlerType.Finally)
                        {
                            TryStart = processStart,
                            TryEnd = finallyStart,
                            HandlerStart = finallyStart,
                            HandlerEnd = finallyEnd,
                        };
                        body.ExceptionHandlers.Add(exceptionHandler);
                    }

                    ilProcessor.Append(processEnd);

                    if (hasResultVariable)
                    {
                        ilProcessor.Emit(OpCodes.Ldloc, resultVariable);
                    }

                    ilProcessor.Emit(OpCodes.Ret);
                }
            }
        }

        private TypeReference ExceptionType(AdviceInfo advice)
        {
            if (advice.PointcutThrownType == null)
            {
                return advice.Method.Module.TypeSystem.Object;
            }

            return advice.PointcutThrownType;
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
                    ilProcessor.Append(ILPPUtils.LoadLocal(instanceVariable));
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
                method.Body.Instructions.Add(ILPPUtils.SetLocal(instanceVariable));
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
                // TODO: 一部ここを通ってしまうので何らか解決すべき
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
                // TODO: 一部ここを通ってしまうので何らか解決すべき
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
                if (adviceMethod.Parameters.Any(v => !v.IsOut && !v.IsIn && v.ParameterType.IsByReference))
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

            var parameters = adviceMethod.Parameters.Where(v => HasPointcutThis(v) || HasPointcutReturned(v) || !HasPointcutAccessorAttribute(v));
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
            bool hasError = false;
            ILPPUtils.LogError("ASPECT1601", "AspectForUnity", $"Around advice is not supported yet in method \"{adviceInfo.Method.FullName}\".", adviceInfo.Method);
            hasError = true;
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

        private bool ValidateAdvice(MethodDefinition method, AdviceInfo adviceInfo)
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
                    hasError = ValidateAfterReturning(method, adviceInfo) || hasError;
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

        private bool ValidateAfterReturning(MethodDefinition method, AdviceInfo adviceInfo)
        {
            bool hasError = false;

            var methodReturnType = method.ReturnType;
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
            bool hasError = false;
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
