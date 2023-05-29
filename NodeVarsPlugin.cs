using Fractural.Plugin;
using Godot;

#if TOOLS
namespace Fractural.NodeVars
{
    [Tool]
    public class NodeVarsPlugin : ExtendedPlugin
    {
        public override string PluginName => "Fractural Node Vars";

        protected override void Load()
        {
            AddManagedInspectorPlugin(new DictNodeVarsInspectorPlugin(this));
        }
    }
}
#endif