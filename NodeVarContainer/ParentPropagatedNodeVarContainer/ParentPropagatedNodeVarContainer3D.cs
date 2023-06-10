using Fractural.Commons;
using Godot;

namespace Fractural.NodeVars
{
    [RegisteredType(nameof(ParentPropagatedNodeVarContainer3D), "res://addons/FracturalNodeVars/Assets/dependency-container-3d.svg", nameof(Spatial))]
    [Tool]
    public class ParentPropagatedNodeVarContainer3D : NodeVarContainer3D, IPropagatedNodeVarContainer
    {
        public INodeVarContainer Source => GetParent() as INodeVarContainer;
    }
}
