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
    public class NodeVarPointerSelect : HBoxContainer, ISerializationListener
    {
        public event Action<NodePath> NodePathChanged;
        public event Action<string> VarNameChanged;

        private bool _disabled;
        public bool Disabled
        {
            get => _disabled;
            set
            {
                _disabled = value;
                if (IsInsideTree())
                    UpdateDisabledUI();
            }
        }

        private PopupSearch _containerVarPopupSearch;
        private Button _containerVarSelectButton;
        private NodePathValueProperty _containerPathProperty;
        private Node _relativeToNode;

        public NodePath ContainerPath { get; private set; }
        public string VarName { get; private set; }
        public Func<NodeVarData, bool> NodeVarConditionFunc { get; set; }

        public NodeVarPointerSelect() { }
        public NodeVarPointerSelect(IAssetsRegistry assetsRegistry, Node sceneRoot, Node relativeToNode, Func<NodeVarData, bool> conditionFunc = null)
        {
            SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;

            _relativeToNode = relativeToNode;

            if (conditionFunc != null)
                NodeVarConditionFunc = conditionFunc;
            else
                NodeVarConditionFunc = (var) => true;

            _containerVarSelectButton = new Button();
            _containerVarSelectButton.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            _containerVarSelectButton.ClipText = true;
            _containerVarSelectButton.Connect("pressed", this, nameof(OnContainerVarSelectPressed));

            _containerVarPopupSearch = new PopupSearch();
            _containerVarPopupSearch.ItemListLineHeight = (int)(_containerVarPopupSearch.ItemListLineHeight * assetsRegistry.Scale);
            _containerVarPopupSearch.Connect(nameof(PopupSearch.EntrySelected), this, nameof(OnContainerVarNameSelected));

            _containerPathProperty = new NodePathValueProperty(sceneRoot, (node) => node is INodeVarContainer);
            _containerPathProperty.ValueChanged += OnContainerPathChanged;
            _containerPathProperty.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            _containerPathProperty.RelativeToNode = relativeToNode;

            AddChild(_containerVarPopupSearch);
            AddChild(_containerPathProperty);
            AddChild(_containerVarSelectButton);
        }

        public void SetValue(NodePath containerPath, string varName)
        {
            ContainerPath = containerPath;
            VarName = varName;

            _containerPathProperty.SetValue(containerPath, false);
            UpdateDisabledUI();
        }

        private void UpdateDisabledUI()
        {
            _containerPathProperty.Disabled = _disabled;
            _containerVarSelectButton.Disabled = _disabled;

            _containerVarSelectButton.Text = VarName ?? "[Empty]";
            var containerNode = _relativeToNode.GetNodeOrNull(ContainerPath ?? new NodePath()) as INodeVarContainer;
            _containerVarSelectButton.Disabled = _disabled || containerNode == null;
        }

        private void OnContainerPathChanged(NodePath path)
        {
            ContainerPath = path;
            if (ContainerPath == null || ContainerPath.IsEmpty())
                OnContainerVarNameSelected(null);
            else
                UpdateDisabledUI();
            NodePathChanged?.Invoke(path);
        }

        private void OnContainerVarNameSelected(string name)
        {
            VarName = name;
            UpdateDisabledUI();
            VarNameChanged?.Invoke(name);
        }

        private void OnContainerVarSelectPressed()
        {
            var container = _relativeToNode.GetNode<INodeVarContainer>(ContainerPath);
            _containerVarPopupSearch.SearchEntries = container.GetNodeVarsList().Where(x => NodeVarConditionFunc(x)).Select(x => x.Name).ToArray();
            _containerVarPopupSearch.Popup_(_containerVarSelectButton.GetGlobalRect());
        }

        public void OnBeforeSerialize()
        {
            NodeVarConditionFunc = null;
        }

        public void OnAfterDeserialize() { }
    }
}
#endif