using Fractural.Plugin;
using Fractural.Plugin.AssetsRegistry;
using Fractural.Utils;
using Godot;
using System;
using System.Linq;

#if TOOLS
namespace Fractural.NodeVars
{
    [Tool]
    public class PointerNodeVarStrategyDisplay : NodeVarStrategyDisplay<PointerNodeVarStrategy>
    {
        private NodeVarPointerSelect _nodeVarPointerSelect;

        public PointerNodeVarStrategyDisplay() { }
        public PointerNodeVarStrategyDisplay(Control topRow, INodeVarContainer propagationSource, IAssetsRegistry assetsRegistry, PackedSceneDefaultValuesRegistry defaultValuesRegistry, Node sceneRoot, Node relativeToNode)
        {
            _nodeVarPointerSelect = new NodeVarPointerSelect(
                propagationSource,
                assetsRegistry,
                defaultValuesRegistry,
                sceneRoot,
                relativeToNode,
                (container, nodeVar) => NodeVarUtils.IsNodeVarValidPointer(
                    container,
                    relativeToNode,
                    sceneRoot,
                    nodeVar,
                    Data.Operation,
                    Data.ValueType
                )
            );
            _nodeVarPointerSelect.VarNameChanged += OnContainerVarNameSelected;
            _nodeVarPointerSelect.NodePathChanged += OnNodePathChanged;

            topRow.AddChild(_nodeVarPointerSelect);
        }

        public override void UpdateDisabledAndFixedUI(bool isFixed, bool disabled, bool privateDisabled, bool nonSetDisabled)
        {
            _nodeVarPointerSelect.Disabled = disabled || privateDisabled || nonSetDisabled;
        }

        private void OnContainerVarNameSelected(string name)
        {
            Strategy.ContainerVarName = name;
            InvokeDataChanged();
        }

        public override void SetData(NodeVarData value, NodeVarData defaultData = null)
        {
            base.SetData(value, defaultData);

            _nodeVarPointerSelect.SetValue(Strategy.ContainerPath, Strategy.ContainerVarName);
        }

        private void OnNodePathChanged(NodePath newValue)
        {
            Strategy.ContainerPath = newValue;
            InvokeDataChanged();
        }
    }
}
#endif