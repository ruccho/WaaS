namespace WaaS.Runtime
{
    public enum StackFrameState
    {
        /// <summary>
        ///     The stack frame is ready for evaluation.
        /// </summary>
        Ready,

        /// <summary>
        ///     The stack frame is busy and should not be evaluated until the waker is triggered.
        /// </summary>
        Pending,

        /// <summary>
        ///     The stack frame has completed execution
        /// </summary>
        Completed
    }
}