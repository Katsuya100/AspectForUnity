using System;
using System.Collections;
using NUnit.Framework;

namespace Katuusagi.AspectForUnity.Tests
{
    public class ParameterArrayTest
    {
        [Test]
        public void IndexerAndEnumeratorUseLogicalLength()
        {
            var backing = new object[16];
            backing[0] = "first";
            backing[1] = 22;
            backing[2] = "third";
            backing[3] = "outside";
            backing[15] = "also outside";

            var parameters = new ParameterArray(3, backing);

            Assert.AreEqual(3, parameters.Length);
            Assert.AreEqual("first", parameters[0]);
            Assert.AreEqual(22, parameters[1]);
            Assert.AreEqual("third", parameters[2]);
            AssertInvalidIndex(backing, -1);
            AssertInvalidIndex(backing, 3);
            AssertInvalidIndex(backing, 15);

            var count = 0;
            foreach (var parameter in parameters)
            {
                Assert.AreNotEqual("outside", parameter);
                Assert.AreNotEqual("also outside", parameter);
                count++;
            }

            Assert.AreEqual(3, count);
        }

        private static object ReadParameter(ParameterArray parameters, int index)
        {
            return parameters[index];
        }

        private static void AssertInvalidIndex(object[] backing, int index)
        {
            try
            {
                ReadParameter(new ParameterArray(3, backing), index);
            }
            catch (IndexOutOfRangeException)
            {
                return;
            }

            Assert.Fail($"Expected IndexOutOfRangeException for index {index}.");
        }
    }
}
