using Godot;
using Fractural.Plugin;
using System;
using Fractural.Utils;
using GDC = Godot.Collections;
using System.Linq;
using System.Collections.Generic;

#if TOOLS
namespace Fractural.NodeVars
{
    public class DictNodeVarsInspectorPlugin : EditorInspectorPlugin, IManagedUnload
    {
        private ExtendedPlugin _plugin;
        private PackedSceneDefaultValuesRegistry _packedSceneDefaultValuesRegistry;

        public DictNodeVarsInspectorPlugin() { }
        public DictNodeVarsInspectorPlugin(ExtendedPlugin plugin)
        {
            _plugin = plugin;
            _packedSceneDefaultValuesRegistry = new PackedSceneDefaultValuesRegistry();
            // We don't want to cache the default value lookups since the
            // user can change them at any time.
            _packedSceneDefaultValuesRegistry.UseCache = false;
            _plugin.AddChild(_packedSceneDefaultValuesRegistry);
        }

        public void Unload()
        {
            _packedSceneDefaultValuesRegistry.QueueFree();
        }

        public override bool CanHandle(Godot.Object @object)
        {
            return true;
        }

        private GDC.Dictionary GetDefaultNodeVarDict(Node node, string path)
        {
            if (node.Filename == "") return null;
            var packedScene = ResourceLoader.Load<PackedScene>(node.Filename);
            var instance = packedScene.Instance();
            instance.QueueFree();
            var dict = _packedSceneDefaultValuesRegistry.GetDefaultValue<GDC.Dictionary>(node, path);
            return dict;
        }

        public override bool ParseProperty(Godot.Object @object, int type, string path, int hint, string hintText, int usage)
        {
            if (!(@object is Node node)) return false;
            var parser = new HintArgsParser(hintText);
            if (parser.TryGetArgs(nameof(HintString.DictNodeVars), out string modeString))
            {
                var objectType = node.GetCSharpType();
                NodeVarData[] localFixedNodeVars = null;
                NodeVarData[] defaultNodeVars = null;
                bool canAddNewVars = false;

                var sceneRoot = _plugin.GetEditorInterface();
                if (sceneRoot != @object)
                {
                    // Inherit default values from original scene file
                    //
                    // If we are not the root of the current edited scene, the we must find the default values
                    // If we are the root, then there's no need to find default values since we are already editing the "default values"
                    var defaultNodeVarDict = GetDefaultNodeVarDict(node, path);
                    if (defaultNodeVarDict != null)
                    {
                        defaultNodeVars = new NodeVarData[defaultNodeVarDict.Count];
                        int index = 0;
                        foreach (string key in defaultNodeVarDict.Keys)
                            defaultNodeVars[index++] = NodeVarData.FromGDDict(defaultNodeVarDict.Get<GDC.Dictionary>(key), key);
                    }
                }

                var mode = (HintString.DictNodeVarsMode)Enum.Parse(typeof(HintString.DictNodeVarsMode), modeString);
                if (mode == HintString.DictNodeVarsMode.Attributes || mode == HintString.DictNodeVarsMode.LocalAttributes)
                    localFixedNodeVars = DictNodeVarsUtils.GetNodeVarsFromAttributes(objectType);
                if (mode == HintString.DictNodeVarsMode.Local || mode == HintString.DictNodeVarsMode.LocalAttributes)
                    canAddNewVars = true;

                AddPropertyEditor(path, new ValueEditorProperty(
                    new DictNodeVarsValueProperty(
                        _plugin.AssetsRegistry,
                        _plugin.GetEditorInterface().GetEditedSceneRoot(),
                        @object as Node,
                        localFixedNodeVars,
                        defaultNodeVars,
                        canAddNewVars)
                    )
                );
                return true;
            }
            return false;
        }
    }
}
#endif