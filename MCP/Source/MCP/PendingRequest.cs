using System.Threading;

namespace MCP
{
    internal class PendingRequest
    {
        public string Method;
        public string Path;
        public string QueryString;
        public string Body;
        public string Response;
        public int StatusCode = 200;
        public readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);
    }
}
