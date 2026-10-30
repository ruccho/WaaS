namespace WaaS.Runtime
{
    /// <summary>
    ///     A trap raised when the fuel of an <see cref="ExecutionContext" /> runs out.
    ///     See <see cref="ExecutionContext.Fuel" />.
    /// </summary>
    public class FuelExhaustedException : TrapException
    {
        public FuelExhaustedException() : base("All fuel has been consumed.")
        {
        }
    }
}