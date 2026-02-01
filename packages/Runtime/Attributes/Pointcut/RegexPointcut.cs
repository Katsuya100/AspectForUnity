using System;
using System.Text.RegularExpressions;

namespace Katuusagi.AspectForUnity
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = true)]
    public class RegexPointcut: PointcutBase
    {
        public Regex Pattern { get; private set; }
        public PointcutNameFlag PointcutNameFlag { get; private set; }

        public RegexPointcut(string pattern, PointcutNameFlag pointcutNameFlag = PointcutNameFlag.Simple)
        {
            Pattern = new Regex(pattern);
            PointcutNameFlag = pointcutNameFlag;
        }
    }
}
