using Fractural.Plugin;
using Fractural.Plugin.AssetsRegistry;
using Fractural.Utils;
using Godot;
using System;
using static Fractural.NodeVars.ExpressionNodeVarData;

#if TOOLS
namespace Fractural.NodeVars
{
    public class NodeVarReferenceEntry : VBoxContainer
    {
        public event Action<string, NodeVarReference> LocalVarAliasChanged;
        public event Action<string, NodeVarReference> DataChanged;
        private event Action<string> Deleted;

        public NodeVarReference Data { get; private set; }
        private bool _isFixed;
        public bool IsFixed
        {
            get => _isFixed;
            set
            {
                _isFixed = value;
                if (IsInsideTree())
                    UpdateDisabledAndFixedUI();
            }
        }
        private bool _disabled;
        public bool Disabled
        {
            get => _disabled;
            set
            {
                _disabled = value;
                if (IsInsideTree())
                    UpdateDisabledAndFixedUI();
            }
        }

        private NodeVarPointerSelect _nodeVarPointerSelect;
        private StringValueProperty _localVarAliasProperty;
        private Button _deleteButton;

        public NodeVarReferenceEntry() { }
        public NodeVarReferenceEntry(IAssetsRegistry assetsRegistry, Node sceneRoot, Node relativeToNode, Func<NodeVarData, bool> conditionFunc = null)
        {
            _localVarAliasProperty = new StringValueProperty();
            _nodeVarPointerSelect = new NodeVarPointerSelect(assetsRegistry, sceneRoot, relativeToNode, conditionFunc);
            _nodeVarPointerSelect.NodePathChanged += (path) =>
            {
                Data.ContainerPath = path;
                InvokeDataChanged();
            };
            _nodeVarPointerSelect.VarNameChanged += (name) =>
            {
                Data.ContainerVarName = name;
                InvokeDataChanged();
            };

            _deleteButton = new Button();
            _deleteButton.Connect("pressed", this, nameof(OnDeletePressed));

            var hbox = new HBoxContainer();
            hbox.AddChild(_localVarAliasProperty);
            hbox.AddChild(_deleteButton);

            AddChild(hbox);
            AddChild(_nodeVarPointerSelect);
        }

        public override void _Ready()
        {
#if TOOLS
            if (NodeUtils.IsInEditorSceneTab(this))
                return;
#endif

            _deleteButton.Icon = GetIcon("Remove", "EditorIcons");
        }

        public void SetData(NodeVarReference data)
        {
            Data = data;
            _nodeVarPointerSelect.SetValue(data.ContainerPath, data.ContainerVarName);
            _localVarAliasProperty.SetValue(data.Name, false);
        }

        public void ResetName(string oldName)
        {
            Data.Name = oldName;
            _localVarAliasProperty.SetValue(oldName, false);
        }

        private void UpdateDisabledAndFixedUI()
        {
            _deleteButton.Disabled = IsFixed || Disabled;
            _localVarAliasProperty.Disabled = IsFixed || Disabled;
            _nodeVarPointerSelect.Disabled = Disabled;
        }

        private void OnDeletePressed() => InvokeDeleted();

        private void InvokeDataChanged() => DataChanged?.Invoke(Data.Name, Data);
        private void InvokeDeleted() => Deleted?.Invoke(Data.Name);
    }
}
#endif