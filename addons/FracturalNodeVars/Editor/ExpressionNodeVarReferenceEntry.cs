using Fractural.Plugin;
using Fractural.Plugin.AssetsRegistry;
using Fractural.Utils;
using Godot;
using System;
using static Fractural.NodeVars.ExpressionNodeVarData;

#if TOOLS
namespace Fractural.NodeVars
{
    public class ExpressionNodeVarReferenceEntry : HBoxContainer
    {
        public event Action<string, ExpressionNodeVarReferenceEntry> NameChanged;
        public event Action<string, NodeVarReference> DataChanged;
        public event Action<string> Deleted;

        public NodeVarReference Data { get; private set; }
        public NodeVarReference DefaultData { get; private set; }
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
        private StringValueProperty _nameProperty;
        private Button _deleteButton;

        public ExpressionNodeVarReferenceEntry() { }
        public ExpressionNodeVarReferenceEntry(IAssetsRegistry assetsRegistry, Node sceneRoot, Node relativeToNode, Func<NodeVarData, bool> conditionFunc = null)
        {
            var control = new Control();
            control.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            control.SizeFlagsStretchRatio = 0.1f;
            control.RectSize = new Vector2(24, 0);
            AddChild(control);

            var contentVBox = new VBoxContainer();
            contentVBox.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            contentVBox.SizeFlagsStretchRatio = 0.9f;
            AddChild(contentVBox);

            _nameProperty = new StringValueProperty();
            _nameProperty.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            _nameProperty.ValueChanged += OnNameChanged;

            _nodeVarPointerSelect = new NodeVarPointerSelect(assetsRegistry, sceneRoot, relativeToNode, conditionFunc);
            _nodeVarPointerSelect.NodePathChanged += OnContainerPathChanged;
            _nodeVarPointerSelect.VarNameChanged += OnContainerVarNameChanged;

            _deleteButton = new Button();
            _deleteButton.Connect("pressed", this, nameof(OnDeletePressed));

            var hbox = new HBoxContainer();
            hbox.AddChild(_nameProperty);
            hbox.AddChild(_deleteButton);

            contentVBox.AddChild(hbox);
            contentVBox.AddChild(_nodeVarPointerSelect);
        }

        public override void _Ready()
        {
#if TOOLS
            if (NodeUtils.IsInEditorSceneTab(this))
                return;
#endif

            _deleteButton.Icon = GetIcon("Remove", "EditorIcons");
        }

        public void SetData(NodeVarReference data, NodeVarReference defaultData)
        {
            Data = data;
            DefaultData = data;
            _nodeVarPointerSelect.SetValue(data.ContainerPath, data.ContainerVarName);
            _nameProperty.SetValue(data.Name, false);
        }

        public void ResetName(string oldName)
        {
            Data.Name = oldName;
            _nameProperty.SetValue(oldName, false);
        }

        private void UpdateDisabledAndFixedUI()
        {
            _deleteButton.Disabled = IsFixed || Disabled;
            _nameProperty.Disabled = IsFixed || Disabled;
            _nodeVarPointerSelect.Disabled = Disabled;
        }

        private void OnContainerPathChanged(NodePath path)
        {
            Data.ContainerPath = path;
            InvokeDataChanged();
        }

        private void OnContainerVarNameChanged(string name)
        {
            Data.ContainerVarName = name;
            InvokeDataChanged();
        }

        private void OnNameChanged(string newName)
        {
            var oldName = Data.Name;
            Data.Name = newName;
            NameChanged?.Invoke(oldName, this);
        }

        private void OnDeletePressed() => InvokeDeleted();
        private void InvokeDataChanged() => DataChanged?.Invoke(Data.Name, Data);
        private void InvokeDeleted() => Deleted?.Invoke(Data.Name);
    }
}
#endif