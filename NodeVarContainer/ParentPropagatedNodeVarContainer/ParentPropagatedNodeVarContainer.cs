using Fractural.Commons;
using Godot;

namespace Fractural.NodeVars
{
    [RegisteredType(nameof(ParentPropagatedNodeVarContainer), "res://addons/FracturalNodeVars/Assets/dependency-container.svg", nameof(Node))]
    [Tool]
    public class ParentPropagatedNodeVarContainer : NodeVarContainer, IPropagatedNodeVarContainer
    {
        public INodeVarContainer Source => GetParent() as INodeVarContainer;
    }
}
