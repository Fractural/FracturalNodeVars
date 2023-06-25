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
    public class NodeVarEntry : HBoxContainer
    {
        /// <summary>
        /// NameChanged(string oldName, Entry entry)
        /// </summary>
        public event Action<string, NodeVarEntry> NameChanged;
        /// <summary>
        /// DataChanged(string name, NodeVarData newValue, bool isDefault)
        /// </summary>
        public event Action<string, NodeVarData, bool> DataChanged;
        /// <summary>
        /// Deleted(string name)
        /// </summary>
        public event Action<string> Deleted;

        public NodeVarData Data { get; set; }
        public NodeVarData DefaultData { get; set; }

        private bool _disabled = false;
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

        protected Button _resetInitialValueButton;
        protected Button _deleteButton;
        protected StringValueProperty _nameProperty;
        protected VBoxContainer _contentVBox;
        protected Node _sceneRoot;
        protected Node _relativeToNode;
        protected OptionButton _operationButton;
        protected OperationTypeData[] _operationTypes;
        protected IAssetsRegistry _assetsRegistry;

        private HBoxContainer _firstRowHBox;
        private HBoxContainer _secondRowHBox;

        /// <summary>
        /// Checks whether the NodeVarContainer containing the DynamicNodeVar is an instance of a scene.
        /// </summary>
        protected bool IsNodeVarContainerInstanced => NodeVarUtils.IsInstancedScene(_relativeToNode, _sceneRoot);
        /// <summary>
        /// NodeVarEntry editing should be disabled if the NodeVar is an instance and the operation is private.
        /// </summary>
        protected bool PrivateDisabled => IsNodeVarContainerInstanced && Data != null && Data.Operation == NodeVarOperation.Private;
        /// <summary>
        /// NodeVarEntry editing should be disabled if the NodeVar is an instance and the operation does not contain a public setter.
        /// </summary>
        protected bool NonSetDisabled => IsNodeVarContainerInstanced && Data != null && !Data.Operation.IsSet();

        public NodeVarEntry() { }
        public NodeVarEntry(Node sceneRoot, Node relativeToNode, IAssetsRegistry assetsRegistry)
        {
            _assetsRegistry = assetsRegistry;
            _sceneRoot = sceneRoot;
            _relativeToNode = relativeToNode;
            _resetInitialValueButton = new Button();
            _resetInitialValueButton.Connect("pressed", this, nameof(OnResetButtonPressed));

            _deleteButton = new Button();
            _deleteButton.Connect("pressed", this, nameof(InvokeDeleted));

            _nameProperty = new StringValueProperty();
            _nameProperty.ValueChanged += OnNameChanged;
            _nameProperty.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            _nameProperty.Validate = (name) => name != "" && name != null && !char.IsDigit(name[0]) && !name.Contains(" ");

            _operationButton = new OptionButton();
            _operationButton.SizeFlagsHorizontal = (int)SizeFlags.Fill;
            _operationButton.RectMinSize = new Vector2(80 * _assetsRegistry.Scale, 0);
            _operationButton.Connect("item_selected", this, nameof(OnOperationSelected));

            var control = new Control();
            control.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            control.SizeFlagsStretchRatio = 0.1f;
            control.RectSize = new Vector2(24, 0);
            AddChild(control);

            _contentVBox = new VBoxContainer();
            _contentVBox.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            _contentVBox.SizeFlagsStretchRatio = 0.9f;
            AddChild(_contentVBox);
        }

        public override void _Ready()
        {
#if TOOLS
            if (NodeUtils.IsInEditorSceneTab(this))
                return;
#endif
            _operationButton.AddColorOverride("font_color_disabled", _operationButton.GetColor("font_color"));
            _deleteButton.Icon = GetIcon("Remove", "EditorIcons");
            _resetInitialValueButton.Icon = GetIcon("Reload", "EditorIcons");
            InitOperationTypes();
        }

        public virtual void ResetName(string oldName)
        {
            Data.Name = oldName;
            _nameProperty.SetValue(oldName, false);
        }

        protected void InitOperationTypes()
        {
            _operationTypes = FetchOperationTypes;
            foreach (var type in _operationTypes)
            {
                var index = _operationButton.GetItemCount();
                _operationButton.AddItem(type.Name);
                type.Index = index;
            }
        }

        protected virtual OperationTypeData[] FetchOperationTypes => NodeVarUtils.GetOperationTypes();

        protected virtual void UpdateDisabledAndFixedUI()
        {
            _contentVBox.Visible = !PrivateDisabled;
            _operationButton.Disabled = Disabled || IsFixed || PrivateDisabled || NonSetDisabled;
            _nameProperty.Disabled = IsFixed || Disabled || PrivateDisabled || NonSetDisabled;
            _deleteButton.Visible = !(IsFixed || PrivateDisabled || NonSetDisabled);
            _deleteButton.Disabled = Disabled;
        }
        protected virtual void InvokeDeleted() => Deleted?.Invoke(Data.Name);
        protected virtual void InvokeDataChanged()
        {
            UpdateResetButton();
            DataChanged?.Invoke(Data.Name, Data, CheckIsSameAsDefault());
        }
        protected virtual bool CheckIsSameAsDefault() => DefaultData != null && Data.Equals(DefaultData);
        protected virtual void InvokeNameChanged(string oldName) => NameChanged?.Invoke(oldName, this);
        public virtual void SetData(NodeVarData data, NodeVarData defaultData = null)
        {
            Data = data.Clone();
            DefaultData = defaultData;
            _nameProperty.SetValue(data.Name, false);
            SetOperationsValueDisplay(Data.Operation);
            UpdateResetButton();
        }

        protected void OnNameChanged(string newName)
        {
            var oldName = Data.Name;
            Data.Name = newName;
            InvokeNameChanged(oldName);
        }

        protected void OnResetButtonPressed()
        {
            SetData(DefaultData.Clone(), DefaultData);
            InvokeDataChanged();
        }

        protected void OnOperationSelected(int index)
        {
            var operation = _operationTypes.First(x => x.Index == index).Operation;
            if (Data.Operation == operation)
                return;
            Data.Operation = operation;
            SetOperationsValueDisplay(Data.Operation);
            InvokeDataChanged();
        }

        protected void SetOperationsValueDisplay(NodeVarOperation operation)
        {
            var operationTypeData = _operationTypes.First(x => x.Operation == operation);
            _operationButton.Select(operationTypeData.Index);
        }

        protected virtual void UpdateResetButton()
        {
            _resetInitialValueButton.Visible = DefaultData != null && !Data.Equals(DefaultData);
        }
    }

    // TODO NOW: Finish porting UI code to use new NodeVarStrategy system
    [Tool]
    public abstract class NodeVarStrategyDisplay : HBoxContainer
    {
        public NodeVarStrategy Strategy { get; set; }
        public NodeVarStrategy DefaultStrategy { get; set; }
        public abstract void SetData(NodeVarStrategy)
        {

        }
    }

    public abstract class NodeVarStrategyDisplay<T> : NodeVarStrategyDisplay where T : NodeVarStrategy
    {
        public new T Strategy
        {
            get => (T)base.Strategy;
            set => base.Strategy = value;
        }
        public new T DefaultStrategy
        {
            get => (T)base.DefaultStrategy;
            set => base.DefaultStrategy = value;
        }

        public override void SetData(NodeVarData data, NodeVarData defaultData = null) => SetData((T)data, (T)defaultData);
        public virtual void SetData(T data, T defaultData = null) => base.SetData(data, defaultData);
    }
}
#endif