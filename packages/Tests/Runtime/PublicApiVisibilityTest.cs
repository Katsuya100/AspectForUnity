using System.ComponentModel;
using System.Reflection;
using NUnit.Framework;

namespace Katuusagi.AspectForUnity.Tests
{
    public class PublicApiVisibilityTest
    {
        [Test]
        public void RuntimeHelpersRemainPublicAndHiddenFromBrowsableLists()
        {
            AssertPublicAndHidden(typeof(ProceedingRuntime));
            AssertPublicAndHidden(typeof(ProceedingStateBase));
            AssertPublicAndHidden(typeof(IProceedingState));
            AssertPublicAndHidden(typeof(IProceedingState<>));
            AssertPublicAndHidden(typeof(ObjectArrayPool));
            AssertPublicAndHidden(typeof(ProceedingStatePool<>));

            AssertPublicMethods(typeof(ProceedingRuntime),
                "Rent", "Activate", "EnsureProceed", "Return", "CreateContext",
                "CreateUnsafeContext", "GetReturnValue", "GetUnsafeReturnValue");
            AssertPublicMethods(typeof(ProceedingStateBase), "Activate", "Release", "EnsureProceed");
            AssertPublicMethods(typeof(IProceedingState), "Proceed");
            AssertPublicMethods(typeof(IProceedingState<>), "GetReturnValue", "Proceed");
            AssertPublicMethods(typeof(ObjectArrayPool), "Rent", "Return");
            AssertPublicMethods(typeof(ProceedingStatePool<>), "Rent", "Return");
        }

        [Test]
        public void UserFacingApiRemainsBrowsableAndPublic()
        {
            AssertPublicAndBrowsable(typeof(ProceedingContext));
            AssertPublicAndBrowsable(typeof(ProceedingContext<>));
            AssertPublicAndBrowsable(typeof(UnsafeProceedingContext<>));
            AssertPublicAndBrowsable(typeof(ParameterArray));
            AssertPublicAndBrowsable(typeof(JoinPoint));
            AssertPublicAndBrowsable(typeof(AdviceScope));
            AssertPublicAndBrowsable(typeof(GenericBinding));

            AssertPublicAndBrowsable(typeof(Aspect));
            AssertPublicAndBrowsable(typeof(Advice));
            AssertPublicAndBrowsable(typeof(BlockAspect));
            AssertPublicAndBrowsable(typeof(OutputPointcutMethodName));
            AssertPublicAndBrowsable(typeof(PointcutBase));
            AssertPublicAndBrowsable(typeof(TypeNamePointcutBase));
            AssertPublicAndBrowsable(typeof(AttributeTypePointcut));
            AssertPublicAndBrowsable(typeof(DeclaringAttributeTypePointcut));
            AssertPublicAndBrowsable(typeof(DeclaringTypePointcut));
            AssertPublicAndBrowsable(typeof(GenericParameterNamePointcut));
            AssertPublicAndBrowsable(typeof(MethodNamePointcut));
            AssertPublicAndBrowsable(typeof(ParameterTypePointcut));
            AssertPublicAndBrowsable(typeof(ReturnTypePointcut));
            AssertPublicAndBrowsable(typeof(RegexPointcut));
            AssertPublicAndBrowsable(typeof(PointcutProceed));
            AssertPublicAndBrowsable(typeof(PointcutGenericBind));
            AssertPublicAndBrowsable(typeof(PointcutMethod));
            AssertPublicAndBrowsable(typeof(PointcutParameters));
            AssertPublicAndBrowsable(typeof(PointcutReturned));
            AssertPublicAndBrowsable(typeof(PointcutThis));
            AssertPublicAndBrowsable(typeof(PointcutThrown));
        }

        private static void AssertPublicAndHidden(System.Type type)
        {
            Assert.That(type.IsPublic, Is.True, $"{type} must remain public for generated IL compatibility.");
            var attribute = (EditorBrowsableAttribute)System.Attribute.GetCustomAttribute(
                type,
                typeof(EditorBrowsableAttribute),
                inherit: false);
            Assert.That(attribute, Is.Not.Null, $"{type} must have EditorBrowsableAttribute.");
            Assert.That(attribute.State, Is.EqualTo(EditorBrowsableState.Never));
        }

        private static void AssertPublicAndBrowsable(System.Type type)
        {
            Assert.That(type.IsPublic, Is.True, $"{type} is a user-facing public API.");
            var attribute = System.Attribute.GetCustomAttribute(
                type,
                typeof(EditorBrowsableAttribute),
                inherit: false);
            Assert.That(attribute, Is.Null, $"{type} must remain visible in IDE browsing.");
        }

        private static void AssertPublicMethods(System.Type type, params string[] names)
        {
            foreach (var name in names)
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                Assert.That(System.Array.Exists(methods, method => method.Name == name), Is.True,
                    $"{type}.{name} must remain a public method.");
            }
        }
    }
}
