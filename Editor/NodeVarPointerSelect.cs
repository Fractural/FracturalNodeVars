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
                    UpdateDisabledAndSelectUI();
            }
        }

        public NodePath ContainerPath { get; private set; }
        public string VarName { get; private set; }
        public Func<NodeVarData, bool> NodeVarConditionFunc { get; set; }

        private PopupSearch _containerVarPopupSearch;
        private Button _containerVarSelectButton;
        private NodePathValueProperty _containerPathProperty;
        private Node _relativeToNode;
        private PackedSceneDefaultValuesRegistry _defaultValuesRegistry;
        private ValueTypeData[] _valueTypes;
        private Texture _expressionIcon;
        private INodeVarContainer _propagationSource;

        public NodeVarPointerSelect() { }
        public NodeVarPointerSelect(INodeVarContainer propagationSource, IAssetsRegistry assetsRegistry, PackedSceneDefaultValuesRegistry defaultValuesRegistry, Node sceneRoot, Node relativeToNode, Func<NodeVarData, bool> conditionFunc = null)
        {
            SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;

            _propagationSource = propagationSource;
            _defaultValuesRegistry = defaultValuesRegistry;
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
            _containerPathProperty.Visible = _propagationSource == null;

            AddChild(_containerVarPopupSearch);
            AddChild(_containerPathProperty);
            AddChild(_containerVarSelectButton);
        }

        public override void _Ready()
        {
            _valueTypes = NodeVarUtils.GetValueTypes(this);
            _expressionIcon = GetIcon("SceneUniqueName", "EditorIcons");
        }

        public void SetValue(NodePath containerPath, string varName)
        {
            ContainerPath = containerPath;
            VarName = varName;

            _containerPathProperty.SetValue(containerPath, false);
            if (containerPath != null && _propagationSource != null)
            {
                var propagationSourcePath = _relativeToNode.GetPathTo(_propagationSource as Node);
                if (!Equals(containerPath, propagationSourcePath))
                {
                    _containerPathProperty.SetValue(propagationSourcePath, false);
                    OnContainerPathChanged(propagationSourcePath);
                }
            }

            UpdateSearchEntries();
            UpdateDisabledAndSelectUI();
        }

        private void UpdateDisabledAndSelectUI()
        {
            _containerPathProperty.Disabled = _disabled;
            _containerVarSelectButton.Disabled = _disabled;

            _containerVarSelectButton.Text = VarName ?? "[Empty]";
            _containerVarSelectButton.Icon = _containerVarPopupSearch.SearchEntries.FirstOrDefault(x => x.Text == VarName).Icon;
            var containerNode = _relativeToNode.GetNodeOrNull(ContainerPath ?? new NodePath()) as INodeVarContainer;
            _containerVarSelectButton.Disabled = _disabled || containerNode == null;
        }

        private void UpdateSearchEntries()
        {
            if (ContainerPath == null) return;
            var container = _relativeToNode.GetNodeOrNull<INodeVarContainer>(ContainerPath);
            if (container == null) return;
            _containerVarPopupSearch.SearchEntries = container.GetNodeVarsList(_defaultValuesRegistry)
                .Where(x => NodeVarConditionFunc(x))
                .Select(x =>
                {
                    var entry = new SearchEntry(x.Name);
                    if (x is ITypedNodeVar typedVar)
                        entry.Icon = _valueTypes.FirstOrDefault(v => v.Type == typedVar.ValueType)?.Icon;
                    else if (x is ExpressionNodeVarData)
                        entry.Icon = _expressionIcon;
                    return entry;
                })
                .ToArray();
        }

        private void OnContainerPathChanged(NodePath path)
        {
            ContainerPath = path;
            if (ContainerPath == null || ContainerPath.IsEmpty())
                OnContainerVarNameSelected(null);
            else
                UpdateDisabledAndSelectUI();
            NodePathChanged?.Invoke(path);
        }

        private void OnContainerVarNameSelected(string name)
        {
            VarName = name;
            UpdateDisabledAndSelectUI();
            VarNameChanged?.Invoke(name);
        }

        private void OnContainerVarSelectPressed() => _containerVarPopupSearch.Popup_(_containerVarSelectButton.GetGlobalRect());

        public void OnBeforeSerialize()
        {
            _valueTypes = null;
            NodeVarConditionFunc = null;
        }

        public void OnAfterDeserialize() { }
    }
}
#endif