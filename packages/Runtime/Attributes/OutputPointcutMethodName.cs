using System;

namespace Katuusagi.AspectForUnity
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class OutputPointcutMethodName : Attribute
    {
        public PointcutNameFlag PointcutNameFlag { get; private set; }
        public OutputPointcutMethodName(PointcutNameFlag pointcutNameFlag)
        {
            PointcutNameFlag = pointcutNameFlag;
        }
    }
}
