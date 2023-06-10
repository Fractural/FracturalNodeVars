using Fractural.Commons;
using Godot;

namespace Fractural.NodeVars
{
    [RegisteredType(nameof(PropagatedNodeVarContainerControl), "res://addons/FracturalNodeVars/Assets/dependency-container-control.svg", nameof(Control))]
    [Tool]
    public class PropagatedNodeVarContainerControl : NodeVarContainerControl, IPropagatedNodeVarContainer
    {
        [Export]
        private NodePath _sourcePath;
        public INodeVarContainer Source => GetNode<INodeVarContainer>(_sourcePath);
    }
}
