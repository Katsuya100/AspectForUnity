namespace Katuusagi.AspectForUnity.Tests.LocalAssembly
{
    [Aspect]
    public static class SimplePointcutAspect
    {
        public static int AttributeTypeTypeCount;
        public static int AttributeTypeStringCount;
        public static int AttributeTypeMismatchCount;
        public static int DeclaringAttributeTypeTypeCount;
        public static int DeclaringAttributeTypeStringCount;
        public static int DeclaringAttributeTypeMismatchCount;
        public static int MethodNameCount;
        public static int MethodNameMismatchCount;
        public static int DeclaringTypeTypeCount;
        public static int DeclaringTypeStringCount;
        public static int DeclaringTypeMismatchCount;
        public static int ReturnTypeTypeCount;
        public static int ReturnTypeStringCount;
        public static int ReturnTypeMismatchCount;
        public static int GenericParameterNameFirstCount;
        public static int GenericParameterNameSecondCount;
        public static int GenericParameterNameLastCount;
        public static int GenericParameterNameSecondLastCount;
        public static int GenericParameterNameRepeatedCount;
        public static int GenericParameterNamePositiveRepeatedCount;
        public static int GenericParameterNameAnyCount;
        public static int GenericParameterNameMismatchCount;
        public static int ParameterTypeTypeCount;
        public static int ParameterTypeStringCount;
        public static int ParameterTypeLastCount;
        public static int ParameterTypeSecondLastCount;
        public static int ParameterTypeRepeatedCount;
        public static int ParameterTypePositiveRepeatedCount;
        public static int ParameterTypeAnyTypeCount;
        public static int ParameterTypeAnyStringCount;
        public static int ParameterTypeMismatchCount;

        public static void Reset()
        {
            AttributeTypeTypeCount = 0;
            AttributeTypeStringCount = 0;
            AttributeTypeMismatchCount = 0;
            DeclaringAttributeTypeTypeCount = 0;
            DeclaringAttributeTypeStringCount = 0;
            DeclaringAttributeTypeMismatchCount = 0;
            MethodNameCount = 0;
            MethodNameMismatchCount = 0;
            DeclaringTypeTypeCount = 0;
            DeclaringTypeStringCount = 0;
            DeclaringTypeMismatchCount = 0;
            ReturnTypeTypeCount = 0;
            ReturnTypeStringCount = 0;
            ReturnTypeMismatchCount = 0;
            GenericParameterNameFirstCount = 0;
            GenericParameterNameSecondCount = 0;
            GenericParameterNameLastCount = 0;
            GenericParameterNameSecondLastCount = 0;
            GenericParameterNameRepeatedCount = 0;
            GenericParameterNamePositiveRepeatedCount = 0;
            GenericParameterNameAnyCount = 0;
            GenericParameterNameMismatchCount = 0;
            ParameterTypeTypeCount = 0;
            ParameterTypeStringCount = 0;
            ParameterTypeLastCount = 0;
            ParameterTypeSecondLastCount = 0;
            ParameterTypeRepeatedCount = 0;
            ParameterTypePositiveRepeatedCount = 0;
            ParameterTypeAnyTypeCount = 0;
            ParameterTypeAnyStringCount = 0;
            ParameterTypeMismatchCount = 0;
        }

        [Advice(JoinPoint.Before)]
        [AttributeTypePointcut(typeof(SimplePointcutMethodMarkerAttribute))]
        public static void AttributeTypeTypeAdvice()
        {
            AttributeTypeTypeCount++;
        }

        [Advice(JoinPoint.Before)]
        [AttributeTypePointcut("Katuusagi.AspectForUnity.Tests.LocalAssembly.SimplePointcutMethodMarkerAttribute")]
        public static void AttributeTypeStringAdvice()
        {
            AttributeTypeStringCount++;
        }

        [Advice(JoinPoint.Before)]
        [AttributeTypePointcut(typeof(SimplePointcutDeclaringMarkerAttribute))]
        public static void AttributeTypeMismatchAdvice()
        {
            AttributeTypeMismatchCount++;
        }

        [Advice(JoinPoint.Before)]
        [DeclaringAttributeTypePointcut(typeof(SimplePointcutDeclaringMarkerAttribute))]
        public static void DeclaringAttributeTypeTypeAdvice()
        {
            DeclaringAttributeTypeTypeCount++;
        }

        [Advice(JoinPoint.Before)]
        [DeclaringAttributeTypePointcut("Katuusagi.AspectForUnity.Tests.LocalAssembly.SimplePointcutDeclaringMarkerAttribute")]
        public static void DeclaringAttributeTypeStringAdvice()
        {
            DeclaringAttributeTypeStringCount++;
        }

        [Advice(JoinPoint.Before)]
        [DeclaringAttributeTypePointcut(typeof(SimplePointcutMethodMarkerAttribute))]
        public static void DeclaringAttributeTypeMismatchAdvice()
        {
            DeclaringAttributeTypeMismatchCount++;
        }

        [Advice(JoinPoint.Before)]
        [MethodNamePointcut("MatchingMethod")]
        public static void MethodNameAdvice()
        {
            MethodNameCount++;
        }

        [Advice(JoinPoint.Before)]
        [MethodNamePointcut("OtherMethod")]
        public static void MethodNameMismatchAdvice()
        {
            MethodNameMismatchCount++;
        }

        [Advice(JoinPoint.Before)]
        [DeclaringTypePointcut(typeof(SimplePointcutTarget))]
        public static void DeclaringTypeTypeAdvice()
        {
            DeclaringTypeTypeCount++;
        }

        [Advice(JoinPoint.Before)]
        [DeclaringTypePointcut("Katuusagi.AspectForUnity.Tests.LocalAssembly.SimplePointcutTarget")]
        public static void DeclaringTypeStringAdvice()
        {
            DeclaringTypeStringCount++;
        }

        [Advice(JoinPoint.Before)]
        [DeclaringTypePointcut(typeof(string))]
        public static void DeclaringTypeMismatchAdvice()
        {
            DeclaringTypeMismatchCount++;
        }

        [Advice(JoinPoint.Before)]
        [ReturnTypePointcut(typeof(int))]
        public static void ReturnTypeTypeAdvice()
        {
            ReturnTypeTypeCount++;
        }

        [Advice(JoinPoint.Before)]
        [ReturnTypePointcut("System.Int32")]
        public static void ReturnTypeStringAdvice()
        {
            ReturnTypeStringCount++;
        }

        [Advice(JoinPoint.Before)]
        [ReturnTypePointcut(typeof(string))]
        public static void ReturnTypeMismatchAdvice()
        {
            ReturnTypeMismatchCount++;
        }

        [Advice(JoinPoint.Before)]
        [GenericParameterNamePointcut(0, "T")]
        public static void GenericParameterNameFirstAdvice()
        {
            GenericParameterNameFirstCount++;
        }

        [Advice(JoinPoint.Before)]
        [GenericParameterNamePointcut(1, "U")]
        public static void GenericParameterNameSecondAdvice()
        {
            GenericParameterNameSecondCount++;
        }

        [Advice(JoinPoint.Before)]
        [GenericParameterNamePointcut(-1, "U")]
        public static void GenericParameterNameLastAdvice()
        {
            GenericParameterNameLastCount++;
        }

        [Advice(JoinPoint.Before)]
        [GenericParameterNamePointcut(-2, "T")]
        public static void GenericParameterNameSecondLastAdvice()
        {
            GenericParameterNameSecondLastCount++;
        }

        [Advice(JoinPoint.Before)]
        [GenericParameterNamePointcut(-100, "T")]
        public static void GenericParameterNameRepeatedAdvice()
        {
            GenericParameterNameRepeatedCount++;
        }

        [Advice(JoinPoint.Before)]
        [GenericParameterNamePointcut(100, "T")]
        public static void GenericParameterNamePositiveRepeatedAdvice()
        {
            GenericParameterNamePositiveRepeatedCount++;
        }

        [Advice(JoinPoint.Before)]
        [GenericParameterNamePointcut("U")]
        public static void GenericParameterNameAnyAdvice()
        {
            GenericParameterNameAnyCount++;
        }

        [Advice(JoinPoint.Before)]
        [GenericParameterNamePointcut(0, "U")]
        public static void GenericParameterNameMismatchAdvice()
        {
            GenericParameterNameMismatchCount++;
        }

        [Advice(JoinPoint.Before)]
        [ParameterTypePointcut(0, typeof(string))]
        public static void ParameterTypeTypeAdvice()
        {
            ParameterTypeTypeCount++;
        }

        [Advice(JoinPoint.Before)]
        [ParameterTypePointcut(1, "System.Int32")]
        public static void ParameterTypeStringAdvice()
        {
            ParameterTypeStringCount++;
        }

        [Advice(JoinPoint.Before)]
        [ParameterTypePointcut(-1, typeof(int))]
        public static void ParameterTypeLastAdvice()
        {
            ParameterTypeLastCount++;
        }

        [Advice(JoinPoint.Before)]
        [ParameterTypePointcut(-2, typeof(string))]
        public static void ParameterTypeSecondLastAdvice()
        {
            ParameterTypeSecondLastCount++;
        }

        [Advice(JoinPoint.Before)]
        [ParameterTypePointcut(-100, typeof(string))]
        public static void ParameterTypeRepeatedAdvice()
        {
            ParameterTypeRepeatedCount++;
        }

        [Advice(JoinPoint.Before)]
        [ParameterTypePointcut(100, typeof(string))]
        public static void ParameterTypePositiveRepeatedAdvice()
        {
            ParameterTypePositiveRepeatedCount++;
        }

        [Advice(JoinPoint.Before)]
        [ParameterTypePointcut(typeof(int))]
        public static void ParameterTypeAnyTypeAdvice()
        {
            ParameterTypeAnyTypeCount++;
        }

        [Advice(JoinPoint.Before)]
        [ParameterTypePointcut("System.String")]
        public static void ParameterTypeAnyStringAdvice()
        {
            ParameterTypeAnyStringCount++;
        }

        [Advice(JoinPoint.Before)]
        [ParameterTypePointcut(0, typeof(int))]
        public static void ParameterTypeMismatchAdvice()
        {
            ParameterTypeMismatchCount++;
        }
    }
}
