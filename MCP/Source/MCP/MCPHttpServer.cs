using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Verse;

namespace MCP
{
    public static class MCPHttpServer
    {
        private static HttpListener _listener;
        private static Thread _thread;

        internal static readonly ConcurrentQueue<PendingRequest> Queue =
            new ConcurrentQueue<PendingRequest>();

        public static void Start(int port = 8080)
        {
            if (_listener != null) return;

            if (!HttpListener.IsSupported)
            {
                Log.Error("[MCP] HttpListener is not supported on this platform.");
                return;
            }

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            _listener.Start();

            _thread = new Thread(ListenLoop)
            {
                IsBackground = true,
                Name = "MCP_HttpServer"
            };
            _thread.Start();

            Log.Message($"[MCP] HTTP bridge listening on http://127.0.0.1:{port}/");
        }

        public static void Stop()
        {
            try { _listener?.Stop(); _listener?.Close(); }
            catch { /* shutdown */ }
            _listener = null;
        }

        private static void ListenLoop()
        {
            while (_listener != null && _listener.IsListening)
            {
                HttpListenerContext ctx;
                try { ctx = _listener.GetContext(); }
                catch { break; }
                ThreadPool.QueueUserWorkItem(_ => HandleContext(ctx));
            }
        }

        private static void HandleContext(HttpListenerContext ctx)
        {
            string body = "";
            if (ctx.Request.HasEntityBody)
            {
                using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
                body = reader.ReadToEnd();
            }

            var pending = new PendingRequest
            {
                Method      = ctx.Request.HttpMethod,
                Path        = ctx.Request.Url.AbsolutePath,
                QueryString = ctx.Request.Url.Query,
                Body        = body
            };

            Queue.Enqueue(pending);

            if (!pending.Done.Wait(TimeSpan.FromSeconds(15)))
            {
                pending.StatusCode = 503;
                pending.Response   = "{\"error\":\"Game thread timeout\"}";
            }

            var resp = ctx.Response;
            resp.StatusCode   = pending.StatusCode;
            resp.ContentType  = "application/json; charset=utf-8";
            resp.Headers.Add("Access-Control-Allow-Origin", "*");
            var bytes = Encoding.UTF8.GetBytes(pending.Response ?? "{}");
            resp.ContentLength64 = bytes.Length;
            resp.OutputStream.Write(bytes, 0, bytes.Length);
            resp.OutputStream.Close();
        }
    }
}
