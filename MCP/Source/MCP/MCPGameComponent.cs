using System;
using Verse;

namespace MCP
{
    public class MCPGameComponent : GameComponent
    {
        public MCPGameComponent(Game game) { }

        public override void GameComponentUpdate()
        {
            while (MCPHttpServer.Queue.TryDequeue(out var req))
            {
                try
                {
                    (req.StatusCode, req.Response) = RequestRouter.Handle(req);
                }
                catch (Exception ex)
                {
                    req.StatusCode = 500;
                    req.Response   = $"{{\"error\":{Newtonsoft.Json.JsonConvert.ToString(ex.Message)}}}";
                }
                finally
                {
                    req.Done.Set();
                }
            }
        }
    }
}
