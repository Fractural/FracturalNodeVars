using Fractural.Plugin;
using Fractural.Plugin.AssetsRegistry;
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
            AssetsRegistry = new EditorAssetsRegistry(this);
            AddManagedInspectorPlugin(new DictNodeVarsInspectorPlugin(this));
        }

        protected override void Unload()
        {
            AssetsRegistry = null;
        }
    }
}
#endif