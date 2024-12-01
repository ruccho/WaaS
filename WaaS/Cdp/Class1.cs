using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Cdp
{
    public class StandardServerTransport : IServerTransport
    {
        private CancellationTokenSource cts = new();

        public StandardServerTransport()
        {
            ProcessHttpAsync();
        }

        private async void ProcessHttpAsync()
        {
            using var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            listener.Start();

            while (true)
            {
                var context = await listener.GetContextAsync();
            }
        }

        public Task<HttpListenerContext> GetHttpContextAsync()
        {
        }

        public Task<ICommand<ICommandParams>> GetCommandAsync()
        {
        }

        public void Dispose()
        {
        }
    }

    public interface IServerTransport : IDisposable
    {
        Task<HttpListenerContext> GetHttpContextAsync();
        Task<ICommand<ICommandParams>> GetCommandAsync();
    }

    public interface IHttpListenerContext
    {
    }

    public class Server
    {
        private IServerTransport transport;
        private IEnumerable<IDomain> domains;

        public Server()
        {
        }

        private async Task ProcessHttpAsync()
        {
            var context = await transport.GetHttpContextAsync();
            var req = context.Request;
            var method = req.HttpMethod;
            var endpoint = req.Url.LocalPath;

            switch (endpoint)
            {
                case "/json/version":
                {
                    context.
                }
            }
        }

        private async Task<ICommandResponse<ICommandReturns>> ProcessCommandAsync(ICommand<ICommandParams> command)
        {
            var method = command.Method;
            var dot = method.IndexOf('.');
            if (dot == -1) throw new InvalidOperationException();
            var domain = method[..dot];
            var commandName = method[(dot + 1)..];
            
        }
    }

    public interface ICommand<out TParams> where TParams : ICommandParams
    {
        int Number { get; }
        string Method { get; }
        TParams Params { get; }
    }

    public interface ICommandResponse<out TReturns> where TReturns : ICommandReturns
    {
        int Number { get; }
        TReturns Params { get; }
    }

    public interface ICommandParams : IObject
    {
    }

    public interface ICommandReturns : IObject
    {
    }

    public interface IEventParams : IObject
    {
    }

    public interface IObject
    {
    }

    public interface IHttpRequest
    {
    }

    public interface IVersionResponse
    {
        string Browser { get; }
        string ProtocolVersion { get; }
        string UserAgent { get; }
        string V8Version { get; }
        string WebKitVersion { get; }
        string WebSocketDebuggerUrl { get; }
    }

    public interface IListResponseElement
    {
        string Description { get; }
        string DevtoolsFrontendUrl { get; }
        string Id { get; }
        string Title { get; }
        string Type { get; }
        string Url { get; }
        string WebSocketDebuggerUrl { get; }
    }

    public interface IProtocolResponse
    {
        IEnumerable<IDomainDefinition> Domains { get; }
    }

    public interface IDomainDefinition
    {
        string Domain { get; }
        bool Experimental { get; }

        IEnumerable<string> Dependencies { get; }
        // TODO
    }

    public interface IDomain
    {
    }
}