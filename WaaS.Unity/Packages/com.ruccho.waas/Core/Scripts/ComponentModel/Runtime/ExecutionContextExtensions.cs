using WaaS.Runtime;

namespace WaaS.ComponentModel.Runtime
{
    public static class ExecutionContextExtensions
    {
        public static ComponentInvocationContext InvokeComponentFunctionScope(this ExecutionContext context,
            IFunction function, out ArgumentLowerer lowerer)
        {
            return new ComponentInvocationContext(context, function, out lowerer);
        }

        public static ComponentInvocationContext.Async InvokeAsyncComponentFunctionScope(this ExecutionContext context,
            IFunction function, out ArgumentLowerer lowerer)
        {
            return new ComponentInvocationContext(context, function, out lowerer).ToAsync();
        }
    }
}