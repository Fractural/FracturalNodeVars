using Fractural.Commons;
using Godot;

namespace Fractural.NodeVars
{
    [RegisteredType(nameof(ParentPropagatedNodeVarContainerControl), "res://addons/FracturalNodeVars/Assets/dependency-container-control.svg", nameof(Control))]
    [Tool]
    public class ParentPropagatedNodeVarContainerControl : NodeVarContainerControl, IPropagatedNodeVarContainer
    {
        public INodeVarContainer Source => GetParent() as INodeVarContainer;
    }
}
