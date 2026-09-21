namespace Katuusagi.AspectForUnity
{
    /// <summary>
    /// Defines the lifetime over which an advice is applied.
    /// </summary>
    public enum AdviceScope
    {
        /// <summary>
        /// Applies to the method invocation boundary. This is the default and
        /// preserves the existing behavior.
        /// </summary>
        Invocation,

        /// <summary>
        /// Applies to one logical asynchronous execution, including await and
        /// yield suspension and resumption points.
        /// </summary>
        AsyncExecutionSequence,
    }
}
