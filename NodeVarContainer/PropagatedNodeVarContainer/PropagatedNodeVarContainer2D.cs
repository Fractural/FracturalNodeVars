using Fractural.Commons;
using Godot;

namespace Fractural.NodeVars
{
    [RegisteredType(nameof(PropagatedNodeVarContainer2D), "res://addons/FracturalNodeVars/Assets/dependency-container-2d.svg", nameof(Node2D))]
    [Tool]
    public class PropagatedNodeVarContainer2D : NodeVarContainer2D, IPropagatedNodeVarContainer
    {
        [Export]
        private NodePath _sourcePath;
        public INodeVarContainer Source => GetNode<INodeVarContainer>(_sourcePath);
    }
}
