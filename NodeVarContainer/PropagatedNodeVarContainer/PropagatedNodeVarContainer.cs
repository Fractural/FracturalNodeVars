using Fractural.Commons;
using Godot;

namespace Fractural.NodeVars
{
    [RegisteredType(nameof(PropagatedNodeVarContainer), "res://addons/FracturalNodeVars/Assets/dependency-container.svg", nameof(Node))]
    [Tool]
    public class PropagatedNodeVarContainer : NodeVarContainer, IPropagatedNodeVarContainer
    {
        [Export]
        private NodePath _sourcePath;
        public INodeVarContainer Source => GetNode<INodeVarContainer>(_sourcePath);
    }
}
