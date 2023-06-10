using Fractural.Commons;
using Godot;

namespace Fractural.NodeVars
{
    [RegisteredType(nameof(PropagatedNodeVarContainer3D), "res://addons/FracturalNodeVars/Assets/dependency-container-3d.svg", nameof(Spatial))]
    [Tool]
    public class PropagatedNodeVarContainer3D : NodeVarContainer3D, IPropagatedNodeVarContainer
    {
        [Export]
        private NodePath _sourcePath;
        public INodeVarContainer Source => GetNode<INodeVarContainer>(_sourcePath);
    }
}
