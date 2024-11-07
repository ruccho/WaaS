// <auto-generated />
#nullable enable

namespace Wasi.Http
{
    // interface outgoing-handler
    /// <summary>
    ///     This interface defines a handler of outgoing HTTP Requests. It should be
    ///     imported by components which wish to make HTTP Requests.
    /// </summary>
    [global::WaaS.ComponentModel.Binding.ComponentInterface(@"outgoing-handler")]
    public partial interface IOutgoingHandler
    {
        /// <summary>
        ///     This function is invoked with an outgoing HTTP Request, and it returns
        ///     a resource `future-incoming-response` which represents an HTTP Response
        ///     which may arrive in the future.
        ///     
        ///     The `options` argument accepts optional parameters for the HTTP
        ///     protocol's transport layer.
        ///     
        ///     This function may return an error if the `outgoing-request` is invalid
        ///     or not allowed to be made. Otherwise, protocol errors are reported
        ///     through the `future-incoming-response`.
        /// </summary>
        [global::WaaS.ComponentModel.Binding.ComponentApi(@"handle")]
        global::System.Threading.Tasks.ValueTask<global::WaaS.ComponentModel.Binding.Result<global::WaaS.ComponentModel.Binding.Owned<Wasi.Http.ITypes.IFutureIncomingResponseResourceImpl>, Wasi.Http.ITypes.ErrorCode>> Handle(global::WaaS.ComponentModel.Binding.Owned<Wasi.Http.ITypes.IOutgoingRequestResourceImpl> @request, global::WaaS.ComponentModel.Binding.Owned<Wasi.Http.ITypes.IRequestOptionsResourceImpl>? @options);

    }
}
