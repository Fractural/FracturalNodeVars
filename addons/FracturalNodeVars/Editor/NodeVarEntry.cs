using Fractural.Plugin;
using Fractural.Plugin.AssetsRegistry;
using Fractural.Utils;
using Godot;
using System;
using System.Linq;

#if TOOLS
namespace Fractural.NodeVars
{
    public class ValueTypeData
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public Texture Icon { get; set; }
        public int Index { get; set; }
        public bool UseIconOnly { get; set; }

        public override string ToString()
        {
            return $"{Name}, {Type.Name}, {Icon}, [{Index}], {UseIconOnly}";
        }
    }

    public class OperationTypeData
    {
        public string Name { get; set; }
        public NodeVarOperation Operation { get; set; }
        public int Index { get; set; }

        public override string ToString()
        {
            return $"{Name}, {Operation}, [{Index}]";
        }
    }

    public class StrategyTypeData
    {
        public Type StrategyType { get; set; }
        public Type StrategyDisplayType { get; set; }
        public Texture Icon { get; set; }
        public Func<NodeVarStrategy> BuildStrategy { get; set; }
        public Func<NodeVarStrategyDisplay> BuildDisplay { get; set; }
        public NodeVarOperation[] ValidOperations { get; set; }

        public override string ToString()
        {
            return $"{StrategyType.Name}, {StrategyDisplayType}, {Icon}, [{string.Join(", ", ValidOperations)}]";
        }
    }

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

        private Button _resetInitialValueButton;
        private Button _deleteButton;
        private StringValueProperty _nameProperty;
        private VBoxContainer _contentVBox;
        private OptionButton _operationButton;
        private HBoxContainer _firstRowHBox;
        private HBoxContainer _secondRowHBox;
        private OptionButton _valueTypeButton;

        private OperationTypeData[] _operationTypes;
        private ValueTypeData[] _valueTypes;
        private StrategyTypeData[] _strategyTypes;
        private StrategyTypeData[] _validStrategyTypes;

        private HBoxContainer _strategyDisplayTopRow;
        private VBoxContainer _strategyDisplayBottomRow;
        private NodeVarStrategyDisplay _strategyDisplay;
        private Button _strategyToggleButton;

        private INodeVarContainer _propagationSource;
        private IAssetsRegistry _assetsRegistry;
        private PackedSceneDefaultValuesRegistry _defaultValueRegistry;
        private Node _sceneRoot;
        private Node _relativeToNode;

        /// <summary>
        /// Checks whether the NodeVarContainer containing the DynamicNodeVar is an instance of a scene.
        /// </summary>
        private bool IsNodeVarContainerInstanced => NodeVarUtils.IsInstancedScene(_relativeToNode, _sceneRoot);
        /// <summary>
        /// NodeVarEntry editing should be disabled if the NodeVar is an instance and the operation is private.
        /// </summary>
        private bool PrivateDisabled => IsNodeVarContainerInstanced && Data != null && Data.Operation.IsPrivate();
        /// <summary>
        /// NodeVarEntry editing should be disabled if the NodeVar is an instance and the operation does not contain a public setter.
        /// </summary>
        private bool NonSetDisabled => IsNodeVarContainerInstanced && Data != null && !Data.Operation.IsSet();

        private bool IsSameAsDefault => DefaultData != null && Data.Equals(DefaultData);

        public NodeVarEntry() { }
        public NodeVarEntry(INodeVarContainer propagationSource, IAssetsRegistry assetsRegistry, PackedSceneDefaultValuesRegistry defaultValuesRegistry, Node sceneRoot, Node relativeToNode)
        {
            _propagationSource = propagationSource;
            _assetsRegistry = assetsRegistry;
            _defaultValueRegistry = defaultValuesRegistry;
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

            _valueTypeButton = new OptionButton();
            _valueTypeButton.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            _valueTypeButton.ClipText = true;
            _valueTypeButton.Connect("item_selected", this, nameof(OnValueTypeSelected));

            _operationButton = new OptionButton();
            _operationButton.SizeFlagsHorizontal = (int)SizeFlags.Fill;
            _operationButton.RectMinSize = new Vector2(90 * _assetsRegistry.Scale, 0);
            _operationButton.Connect("item_selected", this, nameof(OnOperationSelected));

            var control = new Control();
            control.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            control.SizeFlagsStretchRatio = 0.1f;
            control.RectSize = new Vector2(24, 0);
            AddChild(control);

            _firstRowHBox = new HBoxContainer();
            _secondRowHBox = new HBoxContainer();

            _strategyDisplayTopRow = new HBoxContainer();
            _strategyDisplayTopRow.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            _strategyDisplayBottomRow = new VBoxContainer();
            _strategyDisplayBottomRow.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;

            _strategyToggleButton = new Button();
            _strategyToggleButton.SizeFlagsHorizontal = (int)SizeFlags.Fill;
            _strategyToggleButton.Connect("pressed", this, nameof(OnStrategyTogglePressed));

            _firstRowHBox.AddChild(_nameProperty);
            _firstRowHBox.AddChild(_valueTypeButton);
            _firstRowHBox.AddChild(_operationButton);

            _secondRowHBox.AddChild(_strategyToggleButton);
            _secondRowHBox.AddChild(_strategyDisplayTopRow);
            _secondRowHBox.AddChild(_resetInitialValueButton);
            _secondRowHBox.AddChild(_deleteButton);

            _contentVBox = new VBoxContainer();
            _contentVBox.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            _contentVBox.SizeFlagsStretchRatio = 0.9f;
            _contentVBox.AddChild(_firstRowHBox);
            _contentVBox.AddChild(_secondRowHBox);
            _contentVBox.AddChild(_strategyDisplayBottomRow);
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
            InitValueTypes();
            InitStrategyTypes();
        }

        public virtual void SetData(NodeVarData data, NodeVarData defaultData = null)
        {
            Data = data.Clone();
            DefaultData = defaultData;
            _nameProperty.SetValue(data.Name, false);
            UpdateValueTypeValueDisplay();
            UpdateOperationsValueDisplay();
            UpdateStrategyDisplay();
            UpdateResetButton();
            UpdateDisabledAndFixedUI();
        }

        public virtual void ResetName(string oldName)
        {
            Data.Name = oldName;
            _nameProperty.SetValue(oldName, false);
        }

        private void InitOperationTypes()
        {
            _operationTypes = NodeVarUtils.GetOperationTypes();
            foreach (var type in _operationTypes)
            {
                var index = _operationButton.GetItemCount();
                _operationButton.AddItem(type.Name);
                type.Index = index;
            }
        }

        private void InitValueTypes()
        {
            _valueTypes = NodeVarUtils.GetValueTypes(this);
            foreach (var type in _valueTypes)
            {
                int currIndex = _valueTypeButton.GetItemCount();
                type.Index = currIndex;
                _valueTypeButton.AddIconItem(type.Icon, type.Name);
            }
        }

        private void InitStrategyTypes()
        {
            _strategyTypes = new StrategyTypeData[]
            {
                new StrategyTypeData()
                {
                    Icon = GetIcon("FixedMaterial", "EditorIcons"),
                    StrategyType = typeof(ValueNodeVarStrategy),
                    StrategyDisplayType = typeof(ValueNodeVarStrategyDisplay),
                    BuildStrategy = () => new ValueNodeVarStrategy() {
                        InitialValue = DefaultValueUtils.GetDefault(Data.ValueType)
                    },
                    BuildDisplay = () => {
                        return new ValueNodeVarStrategyDisplay(_strategyDisplayTopRow);
                    },
                    ValidOperations = new ValueNodeVarStrategy().ValidOperations
                },
                new StrategyTypeData()
                {
                    Icon = GetIcon("GuiScrollArrowRightHl", "EditorIcons"),
                    StrategyType = typeof(PointerNodeVarStrategy),
                    StrategyDisplayType = typeof(PointerNodeVarStrategyDisplay),
                    BuildStrategy = () => new PointerNodeVarStrategy(),
                    BuildDisplay = () => {
                        return new PointerNodeVarStrategyDisplay(_strategyDisplayTopRow, _propagationSource, _assetsRegistry, _defaultValueRegistry, _sceneRoot, _relativeToNode);
                    },
                    ValidOperations = new PointerNodeVarStrategy().ValidOperations
                    },
                new StrategyTypeData()
                {
                    Icon = GetIcon("SceneUniqueName", "EditorIcons"),
                    StrategyType = typeof(ExpressionNodeVarStrategy),
                    StrategyDisplayType = typeof(ExpressionNodeVarStrategyDisplay),
                    BuildStrategy = () => new ExpressionNodeVarStrategy(),
                    BuildDisplay = () => {
                        return new ExpressionNodeVarStrategyDisplay(_strategyDisplayTopRow, _strategyDisplayBottomRow, _propagationSource, _assetsRegistry, _defaultValueRegistry, _sceneRoot, _relativeToNode);
                    },
                    ValidOperations = new ExpressionNodeVarStrategy().ValidOperations
                }
            };
        }

        private void UpdateDisabledAndFixedUI()
        {
            _contentVBox.Visible = !PrivateDisabled;
            _operationButton.Disabled = Disabled || IsFixed || PrivateDisabled || NonSetDisabled;
            _nameProperty.Disabled = IsFixed || Disabled || PrivateDisabled || NonSetDisabled;
            _deleteButton.Visible = !(IsFixed || PrivateDisabled || NonSetDisabled);
            _deleteButton.Disabled = Disabled;
            _strategyDisplay.UpdateDisabledAndFixedUI(IsFixed, Disabled, PrivateDisabled, NonSetDisabled);

            _secondRowHBox.Visible = !NonSetDisabled;
            _strategyDisplayBottomRow.Visible = !NonSetDisabled;
        }

        private void UpdateResetButton()
        {
            _resetInitialValueButton.Visible = DefaultData != null && !Data.Equals(DefaultData);
        }

        private void UpdateOperationsValueDisplay()
        {
            var operationTypeData = _operationTypes.First(x => x.Operation == Data.Operation);
            _operationButton.Select(operationTypeData.Index);

            _validStrategyTypes = _strategyTypes.Where(x => x.ValidOperations.Contains(Data.Operation)).ToArray();
            if (_validStrategyTypes.Length == 0)
            {
                Data.Strategy = null;
                UpdateStrategyDisplay();
                InvokeDataChanged();
            }
            else if (!_validStrategyTypes.Any(x => x.StrategyType == Data.Strategy.GetType()))
            {
                Data.Strategy = _validStrategyTypes.FirstOrDefault()?.BuildStrategy();
                UpdateStrategyDisplay();
                InvokeDataChanged();
            }
        }

        private void UpdateValueTypeValueDisplay()
        {
            var valueTypeData = _valueTypes.First(x => x.Type == Data.ValueType);
            _valueTypeButton.Select(valueTypeData.Index);
            _valueTypeButton.SizeFlagsHorizontal = (int)(valueTypeData.UseIconOnly ? SizeFlags.Fill : SizeFlags.ExpandFill);
            if (valueTypeData.UseIconOnly)
                _valueTypeButton.Text = "";
        }

        private void ClearStrategyDisplay()
        {
            // Regenerate NodeVarStrategyDisplay if 
            // the current NodeVar type is different from
            // the existing display's NodeVar type
            if (_strategyDisplay != null)
            {
                _strategyDisplay.DataChanged -= InvokeDataChanged;
                _strategyDisplay.QueueFree();
            }
            foreach (Node child in _strategyDisplayTopRow.GetChildren())
                child.QueueFree();
            foreach (Node child in _strategyDisplayBottomRow.GetChildren())
                child.QueueFree();
        }

        private void UpdateStrategyDisplay()
        {
            if (Data?.Strategy == null)
                ClearStrategyDisplay();
            else
            {
                var strategyType = _validStrategyTypes.FirstOrDefault(x => x.StrategyType == Data.Strategy.GetType());
                _strategyToggleButton.Icon = strategyType.Icon;

                if (_strategyDisplay?.GetType() != strategyType.StrategyDisplayType)
                {
                    ClearStrategyDisplay();

                    _strategyDisplay = strategyType.BuildDisplay();
                    AddChild(_strategyDisplay);
                }

                _strategyDisplay.SetData(Data, DefaultData);
                _strategyDisplay.DataChanged += InvokeDataChanged;
            }
        }

        private void InvokeDeleted() => Deleted?.Invoke(Data.Name);

        private void InvokeDataChanged()
        {
            UpdateResetButton();
            DataChanged?.Invoke(Data.Name, Data, IsSameAsDefault);
        }

        private void InvokeNameChanged(string oldName) => NameChanged?.Invoke(oldName, this);

        private void OnStrategyTogglePressed()
        {
            if (_validStrategyTypes.Length <= 1)
                return;

            int index = Array.FindIndex(_validStrategyTypes, (type) => type.StrategyType == Data.Strategy.GetType());
            if (index < 0)
            {
                GD.PushError($"{nameof(NodeVarEntry)}: Could not find strategy of type \"{Data.Strategy.GetType().Name}\".");
                return;
            }

            StrategyTypeData nextStrategyType = _validStrategyTypes[(index + 1) % _validStrategyTypes.Length];
            Data.Strategy = nextStrategyType.BuildStrategy();
            UpdateStrategyDisplay();

            InvokeDataChanged();
        }

        private void OnNameChanged(string newName)
        {
            var oldName = Data.Name;
            Data.Name = newName;
            InvokeNameChanged(oldName);
        }

        private void OnResetButtonPressed()
        {
            SetData(DefaultData.Clone(), DefaultData);
            InvokeDataChanged();
        }

        private void OnOperationSelected(int index)
        {
            var operation = _operationTypes.First(x => x.Index == index).Operation;
            if (Data.Operation == operation)
                return;
            Data.Operation = operation;
            UpdateOperationsValueDisplay();
            InvokeDataChanged();
        }

        private void OnValueTypeSelected(int index)
        {
            var newType = _valueTypes.First(x => x.Index == index).Type;
            if (Data.ValueType == newType)
                return;
            Data.ValueType = newType;
            UpdateValueTypeValueDisplay();
            UpdateStrategyDisplay();
            InvokeDataChanged();
        }
    }
}
#endif