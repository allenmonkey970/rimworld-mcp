using HarmonyLib;
using Verse;

namespace MCP
{
    public class MCPMod : Mod
    {
        public MCPMod(ModContentPack content) : base(content)
        {
            new Harmony("anthonyzolmora.MCP").PatchAll();
            MCPHttpServer.Start(8080);
        }
    }
}
