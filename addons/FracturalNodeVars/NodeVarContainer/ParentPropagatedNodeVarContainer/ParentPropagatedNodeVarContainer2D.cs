using Fractural.Commons;
using Godot;

namespace Fractural.NodeVars
{
    [RegisteredType(nameof(ParentPropagatedNodeVarContainer2D), "res://addons/FracturalNodeVars/Assets/dependency-container-2d.svg", nameof(Node2D))]
    [Tool]
    public class ParentPropagatedNodeVarContainer2D : NodeVarContainer2D, IPropagatedNodeVarContainer
    {
        public INodeVarContainer Source => GetParent() as INodeVarContainer;
    }
}
