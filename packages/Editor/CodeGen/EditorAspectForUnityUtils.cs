using Katuusagi.ILPostProcessorCommon.Editor;
using Mono.Cecil;
using NUnit.Framework.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Katuusagi.AspectForUnity.Editor
{
    public static class EditorAspectForUnityUtils
    {
        public static string GeneratePointcutMethodName(MethodReference method, PointcutNameFlag nameFlag)
        {
            var methodDef = method.Resolve();
            var assemblyDef = methodDef.Module.Assembly;
            var declaringTypeDef = methodDef.DeclaringType;

            StringBuilder sb = new StringBuilder();

            var isTypeFullName = (nameFlag & PointcutNameFlag.TypeFullName) != 0;
            var isAssemblyFullName = (nameFlag & PointcutNameFlag.AssemblyFullName) != 0;
            var isAncestorDeclaringTypeAttribute = (nameFlag & PointcutNameFlag.AncestorDeclaringTypeAttribute) != 0;
            var isAttributeArguments = (nameFlag & PointcutNameFlag.AttributeArguments) != 0;

            var hasAssemblyName = (nameFlag & PointcutNameFlag.AssemblyName) != 0;
            var hasAssemblyAttribute = (nameFlag & PointcutNameFlag.AssemblyAttribute) != 0;
            var hasModuleAttribute = (nameFlag & PointcutNameFlag.ModuleAttribute) != 0;
            var hasDeclaringTypeAttribute = (nameFlag & PointcutNameFlag.DeclaringTypeAttribute) != 0;
            var hasDeclaringTypeName = (nameFlag & PointcutNameFlag.DeclaringTypeName) != 0;
            var hasDeclaringTypeGenericArgumentAttribute = (nameFlag & PointcutNameFlag.DeclaringTypeGenericArgumentAttribute) != 0;
            var hasDeclaringTypeGenericArgumentName = (nameFlag & PointcutNameFlag.DeclaringTypeGenericArgumentName) != 0;
            var hasDeclaringTypeGenericArguments = declaringTypeDef.HasGenericParameters && (hasDeclaringTypeGenericArgumentAttribute || hasDeclaringTypeGenericArgumentName);
            var hasMethodAccessModifier = (nameFlag & PointcutNameFlag.MethodAccessModifier) != 0;
            var hasMethodStaticModifier = (nameFlag & PointcutNameFlag.MethodStaticModifier) != 0;
            var hasMethodOverrideModifier = (nameFlag & PointcutNameFlag.MethodOverrideModifier) != 0;
            var hasReturnTypeAttribute = (nameFlag & PointcutNameFlag.ReturnTypeAttribute) != 0;
            var hasReturnTypeName = (nameFlag & PointcutNameFlag.ReturnTypeName) != 0;
            var hasMethodAttribute = (nameFlag & PointcutNameFlag.MethodAttribute) != 0;
            var hasMethodName = (nameFlag & PointcutNameFlag.MethodName) != 0;
            var hasGenericArgumentAttribute = (nameFlag & PointcutNameFlag.GenericArgumentAttribute) != 0;
            var hasGenericArgumentName = (nameFlag & PointcutNameFlag.GenericArgumentName) != 0;
            var hasGenericArguments = methodDef.HasGenericParameters && (hasGenericArgumentAttribute || hasGenericArgumentName);
            var hasParameterAttribute = (nameFlag & PointcutNameFlag.ParameterAttribute) != 0;
            var hasParameterTypeName = (nameFlag & PointcutNameFlag.ParameterTypeName) != 0;
            var hasParameterName = (nameFlag & PointcutNameFlag.ParameterName) != 0;
            var hasParameters = hasParameterAttribute || hasParameterTypeName || hasParameterName;

            if (hasAssemblyName)
            {
                if (isAssemblyFullName)
                {
                    sb.Append(methodDef.Module.Assembly.Name.FullName);
                }
                else
                {
                    sb.Append(methodDef.Module.Assembly.Name.Name);
                }
            }

            if (hasAssemblyAttribute)
            {
                AppendCustomAttributes(sb, "assembly", assemblyDef.CustomAttributes, nameFlag);
            }

            if (hasModuleAttribute)
            {
                AppendCustomAttributes(sb, "module", methodDef.Module.CustomAttributes, nameFlag);
            }

            if (hasDeclaringTypeAttribute)
            {
                using (ThreadStaticListPool<CustomAttribute>.Get(out var customAttributes))
                {
                    if (isAncestorDeclaringTypeAttribute)
                    {
                        GetDeclaringTypeAttributesRecursive(declaringTypeDef, customAttributes);
                    }
                    else
                    {
                        customAttributes.AddRange(declaringTypeDef.CustomAttributes);
                    }

                    AppendCustomAttributes(sb, "declaring", customAttributes, nameFlag);
                }
            }

            if (hasReturnTypeAttribute)
            {
                AppendCustomAttributes(sb, "return", methodDef.MethodReturnType.CustomAttributes, nameFlag);
            }

            if (hasMethodAttribute)
            {
                AppendCustomAttributes(sb, string.Empty, methodDef.CustomAttributes, nameFlag);
            }

            if (hasMethodAccessModifier)
            {
                if (IsSeparateNext(sb))
                {
                    sb.Append(" ");
                }

                if (methodDef.IsPublic)
                {
                    sb.Append("public");
                }
                else if (methodDef.IsFamily)
                {
                    sb.Append("protected");
                }
                else if (methodDef.IsAssembly)
                {
                    sb.Append("internal");
                }
                else if (methodDef.IsPrivate)
                {
                    sb.Append("private");
                }
                else if (methodDef.IsFamilyOrAssembly)
                {
                    sb.Append("protected internal");
                }
                else if (methodDef.IsFamilyAndAssembly)
                {
                    sb.Append("private protected");
                }
            }

            if (hasMethodStaticModifier)
            {
                if (IsSeparateNext(sb))
                {
                    sb.Append(" ");
                }

                if (methodDef.IsStatic)
                {
                    sb.Append("static");
                }
            }

            if (hasMethodOverrideModifier)
            {
                if (IsSeparateNext(sb))
                {
                    sb.Append(" ");
                }

                bool isSealed = methodDef.IsFinal && methodDef.IsVirtual;
                if (isSealed)
                {
                    sb.Append("sealed");
                }

                if (IsSeparateNext(sb))
                {
                    sb.Append(" ");
                }

                bool isVirtual = methodDef.IsVirtual && !methodDef.IsFinal;
                bool isOverride = methodDef.IsVirtual && methodDef.IsReuseSlot && !methodDef.IsNewSlot;
                bool isNew = methodDef.IsVirtual && methodDef.IsNewSlot && !methodDef.IsReuseSlot;
                if (isOverride)
                {
                    sb.Append("override");
                }
                else if (isNew)
                {
                    sb.Append("new");
                }
                else if (isVirtual)
                {
                    sb.Append("virtual");
                }
            }

            if (hasReturnTypeName)
            {
                if (IsSeparateNext(sb))
                {
                    sb.Append(" ");
                }

                AppendTypeName(sb, methodDef.MethodReturnType.ReturnType, isTypeFullName);
            }

            if (hasDeclaringTypeName)
            {
                if (IsSeparateNext(sb))
                {
                    sb.Append(" ");
                }

                AppendTypeName(sb, declaringTypeDef, isTypeFullName);
            }

            if (hasDeclaringTypeGenericArguments)
            {
                sb.Append("<");
                for (int i = 0; i < declaringTypeDef.GenericParameters.Count; i++)
                {
                    var gp = declaringTypeDef.GenericParameters[i];
                    if (i > 0)
                    {
                        sb.Append(",");
                    }
                    if (hasDeclaringTypeGenericArgumentAttribute)
                    {
                        AppendCustomAttributes(sb, string.Empty, gp.CustomAttributes, nameFlag);
                    }
                    if (hasDeclaringTypeGenericArgumentName)
                    {
                        AppendTypeName(sb, gp, isTypeFullName);
                    }
                }
                sb.Append(">");
            }

            if (hasMethodName)
            {
                if (hasDeclaringTypeName || hasDeclaringTypeGenericArguments)
                {
                    sb.Append("::");
                }
                else
                {
                    if (IsSeparateNext(sb))
                    {
                        sb.Append(" ");
                    }
                }

                sb.Append(methodDef.Name);
            }

            if (hasGenericArguments)
            {
                sb.Append("<");
                for (int i = 0; i < methodDef.GenericParameters.Count; i++)
                {
                    var gp = methodDef.GenericParameters[i];
                    if (i > 0)
                    {
                        sb.Append(",");
                    }
                    if (hasGenericArgumentAttribute)
                    {
                        AppendCustomAttributes(sb, string.Empty, gp.CustomAttributes, nameFlag);
                    }
                    if (hasGenericArgumentName)
                    {
                        AppendTypeName(sb, gp, isTypeFullName);
                    }
                }
                sb.Append(">");
            }

            if (hasParameters)
            {
                sb.Append("(");
                for (int i = 0; i < methodDef.Parameters.Count; i++)
                {
                    var p = methodDef.Parameters[i];
                    if (i > 0)
                    {
                        sb.Append(",");
                    }
                    if (hasParameterAttribute)
                    {
                        AppendCustomAttributes(sb, string.Empty, p.CustomAttributes, nameFlag);
                    }
                    if (hasParameterTypeName)
                    {
                        AppendTypeName(sb, p.ParameterType, isTypeFullName, p);
                    }
                    if (hasParameterName)
                    {
                        if (sb.Length > 0 &&
                            hasParameterTypeName)
                        {
                            sb.Append(" ");
                        }
                        sb.Append(p.Name);
                    }
                }
                sb.Append(")");
            }

            var methodName = sb.ToString();
            return methodName;
        }

        public static void AppendCustomAttributes(StringBuilder sb, string label, IEnumerable<CustomAttribute> customAttributes, PointcutNameFlag flag)
        {
            if (customAttributes == null || !customAttributes.Any())
            {
                return;
            }

            if (string.IsNullOrEmpty(label))
            {
                sb.Append("[");
            }
            else
            {
                sb.Append($"[{label}:");
            }

            var isTypeFullName = (flag & PointcutNameFlag.TypeFullName) != 0;
            var isAttributeArguments = (flag & PointcutNameFlag.AttributeArguments) != 0;
            var isAttributeProperties = (flag & PointcutNameFlag.AttributeProperties) != 0;

            int i = 0;
            foreach (var ca in customAttributes)
            {
                if (i > 0)
                {
                    sb.Append(",");
                }

                AppendTypeName(sb, ca.AttributeType, isTypeFullName);

                var hasAttributeArguments = isAttributeArguments && ca.HasConstructorArguments;
                var hasAttributeProperties = isAttributeProperties && (ca.HasFields || ca.HasProperties);
                if (hasAttributeArguments || hasAttributeProperties)
                {
                    sb.Append("(");
                    bool isFirst = true;
                    if (hasAttributeArguments)
                    {
                        foreach (var arg in ca.ConstructorArguments)
                        {
                            if (!isFirst)
                            {
                                sb.Append(",");
                            }

                            isFirst = false;

                            AppendObject(sb, arg.Type, arg.Value, isTypeFullName);
                        }
                    }

                    if (hasAttributeProperties)
                    {
                        foreach (var prop in ca.Fields.Concat(ca.Properties))
                        {
                            if (!isFirst)
                            {
                                sb.Append(",");
                            }

                            isFirst = false;

                            sb.Append(prop.Name);
                            sb.Append("=");
                            AppendObject(sb, prop.Argument.Type, prop.Argument.Value, isTypeFullName);
                        }
                    }

                    sb.Append(")");
                }

                ++i;
            }

            sb.Append("]");
        }

        public static void GetDeclaringTypeAttributesRecursive(TypeDefinition type, List<CustomAttribute> result)
        {
            if (type == null)
            {
                return;
            }

            GetDeclaringTypeAttributesRecursive(type.DeclaringType, result);
            result.AddRange(type.CustomAttributes);
        }

        public static void AppendTypeName(StringBuilder sb, TypeReference typeRef, bool isTypeFullName, ParameterDefinition parameter = null)
        {
            if (typeRef is ByReferenceType byRefType)
            {
                if (parameter == null)
                {
                    sb.Append("ref ");
                }
                else if (parameter.IsIn)
                {
                    sb.Append("in ");
                }
                else if (parameter.IsOut)
                {
                    sb.Append("out ");
                }
                else
                {
                    sb.Append("ref ");
                }
                AppendTypeName(sb, byRefType.ElementType, isTypeFullName, parameter);
                return;
            }
            else if (typeRef is PointerType pointerType)
            {
                AppendTypeName(sb, pointerType.ElementType, isTypeFullName, parameter);
                sb.Append("*");
                return;
            }
            else if (typeRef is ArrayType arrayType)
            {
                AppendTypeName(sb, arrayType.ElementType, isTypeFullName, parameter);
                sb.Append("[");
                for (int i = 1; i < arrayType.Rank; i++)
                {
                    sb.Append(",");
                }
                sb.Append("]");
                return;
            }
            else if (typeRef is GenericInstanceType genericInstanceType)
            {
                if (genericInstanceType.Resolve().FullName == typeof(Nullable<>).FullName)
                {
                    AppendTypeName(sb, genericInstanceType.GenericArguments[0], isTypeFullName, parameter);
                    sb.Append("?");
                }
                else
                {
                    AppendTypeName(sb, genericInstanceType.ElementType, isTypeFullName, parameter);
                    sb.Append("<");
                    for (int i = 0; i < genericInstanceType.GenericArguments.Count; i++)
                    {
                        var ga = genericInstanceType.GenericArguments[i];
                        if (i > 0)
                        {
                            sb.Append(",");
                        }
                        AppendTypeName(sb, ga, isTypeFullName, parameter);
                    }
                    sb.Append(">");
                }
                return;
            }
            else if (typeRef is PinnedType pinnedType)
            {
                sb.Append("fixed(");
                AppendTypeName(sb, pinnedType.ElementType, isTypeFullName, parameter);
                sb.Append(")");
                return;
            }
            else if (typeRef is SentinelType sentinelType)
            {
                AppendTypeName(sb, sentinelType.ElementType, isTypeFullName, parameter);
                sb.Append("...");
                return;
            }
            else if (typeRef is OptionalModifierType optionalModifierType)
            {
                AppendTypeName(sb, optionalModifierType.ElementType, isTypeFullName, parameter);
                return;
            }
            else if (typeRef is RequiredModifierType requiredModifierType)
            {
                AppendTypeName(sb, requiredModifierType.ElementType, isTypeFullName, parameter);
                return;
            }
            else if (typeRef is FunctionPointerType functionPointerType)
            {
                sb.Append("delegate*");
                sb.Append("<");
                for (int i = 0; i < functionPointerType.Parameters.Count; i++)
                {
                    var p = functionPointerType.Parameters[i];
                    if (i > 0)
                    {
                        sb.Append(",");
                    }
                    AppendTypeName(sb, p.ParameterType, isTypeFullName, parameter);
                }
                AppendTypeName(sb, functionPointerType.ReturnType, isTypeFullName, parameter);
                sb.Append(">");
                return;
            }
            else if (typeRef is GenericParameter)
            {
                sb.Append(typeRef.Name);
                return;
            }

            var typeDef = typeRef.Resolve();
            if (typeDef != null)
            {
                typeRef = typeDef;
            }

            if (isTypeFullName)
            {
                if (typeRef.DeclaringType != null)
                {
                    AppendTypeName(sb, typeRef.DeclaringType, isTypeFullName, parameter);
                    sb.Append(".");
                }
                else
                {
                    sb.Append(typeRef.Namespace);
                    sb.Append(".");
                }
            }

            string name = typeRef.Name;
            if (typeRef.IsGenericDefinition())
            {
                var splitedName = name.Split('`');
                if (splitedName.Any())
                {
                    name = splitedName[0];
                }
            }

            sb.Append(name);
        }

        public static void AppendObject(StringBuilder sb, TypeReference typeRef, object value, bool isTypeFullName)
        {
            if (value is string)
            {
                sb.Append($"\"{value}\"");
                return;
            }

            if (value is CustomAttributeArgument[] arguments)
            {
                sb.Append("{");
                for (int i = 0; i < arguments.Length; i++)
                {
                    var arg = arguments[i];
                    if (i > 0)
                    {
                        sb.Append(",");
                    }
                    AppendObject(sb, arg.Type, arg.Value, isTypeFullName);
                }
                sb.Append("}");
                return;
            }

            var typeDef = typeRef.Resolve();
            if (typeDef != null)
            {
                if (typeDef.IsEnum)
                {
                    AppendEnum(sb, typeDef, value, isTypeFullName);
                    return;
                }
            }

            sb.Append(value);
        }

        public static void AppendEnum(StringBuilder sb, TypeDefinition enumTypeDef, object value, bool isTypeFullName)
        {
            var remainBit = ConvertToUInt64Safe(value);
            var hasFlagsAttribute = enumTypeDef.HasAttribute(typeof(FlagsAttribute).FullName);
            if (!hasFlagsAttribute || remainBit == 0)
            {
                foreach (var field in enumTypeDef.Fields)
                {
                    if (field.Constant == null || !field.Constant.Equals(value))
                    {
                        continue;
                    }

                    AppendTypeName(sb, enumTypeDef, isTypeFullName);
                    sb.Append($".{field.Name}");
                    return;
                }

                sb.Append(value);
                return;
            }

            using (ThreadStaticListPool<FieldDefinition>.Get(out var usings))
            {
                var enumValues = enumTypeDef.Fields
                        .Where(f => f.Constant != null)
                        .OrderByDescending(v => ConvertToUInt64Safe(v.Constant));
                foreach (var enumValue in enumValues)
                {
                    var bitValue = ConvertToUInt64Safe(enumValue.Constant);
                    if (bitValue == 0)
                    {
                        continue;
                    }

                    if ((remainBit & bitValue) == bitValue)
                    {
                        usings.Add(enumValue);
                        remainBit &= ~bitValue;
                        if (remainBit == 0)
                        {
                            break;
                        }
                    }
                }

                if (remainBit != 0)
                {
                    sb.Append(value);
                    return;
                }

                for (int i = usings.Count - 1; i >= 0; --i)
                {
                    var field = usings[i];
                    if (i < usings.Count - 1)
                    {
                        sb.Append("|");
                    }

                    AppendTypeName(sb, enumTypeDef, isTypeFullName);
                }
            }
        }

        public static ulong ConvertToUInt64Safe(object value)
        {
            if (value == null)
                return 0;

            switch (value)
            {
                case ulong ulongValue:
                    return ulongValue;
                case long longValue:
                    return unchecked((ulong)longValue);
                case uint uintValue:
                    return uintValue;
                case int intValue:
                    return unchecked((ulong)intValue);
                case ushort ushortValue:
                    return ushortValue;
                case short shortValue:
                    return unchecked((ulong)shortValue);
                case byte byteValue:
                    return byteValue;
                case sbyte sbyteValue:
                    return unchecked((ulong)sbyteValue);
                default:
                    // フォールバック：Convert.ToUInt64を使用するが、例外が発生した場合は0を返す
                    try
                    {
                        return Convert.ToUInt64(value);
                    }
                    catch (OverflowException)
                    {
                        // 負の値を含む場合は、uncheckedキャストを試行
                        if (value is IConvertible convertible)
                        {
                            var longValue = convertible.ToInt64(null);
                            return unchecked((ulong)longValue);
                        }
                        else
                        {
                            throw;
                        }
                    }
                    catch
                    {
                        throw;
                    }
            }
        }

        public static bool IsSeparateNext(StringBuilder sb)
        {
            if (sb == null || sb.Length <= 0)
            {
                return false;
            }

            var c = sb[^1];
            return c != '<' &&
                   c != '>' &&
                   c != '[' &&
                   c != ']' &&
                   c != '(' &&
                   c != ')' &&
                   c != '{' &&
                   c != '}' &&
                   c != '"' &&
                   c != '\'' &&
                   c != ':' &&
                   c != ',' &&
                   c != '.' &&
                   c != '=' &&
                   c != '|' &&
                   c != '?' &&
                   c != ' ';
        }
    }
}
