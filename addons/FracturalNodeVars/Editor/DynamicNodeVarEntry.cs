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
    public class DynamicNodeVarEntry : NodeVarEntry<DynamicNodeVarData>, ISerializationListener
    {
        private class ValueTypeData
        {
            public string Name { get; set; }
            public Type Type { get; set; }
            public Texture Icon { get; set; }
            public int Index { get; set; }
            public bool UseIconOnly { get; set; }
        }

        private class OperationTypeData
        {
            public string Name { get; set; }
            public NodeVarOperation Operation { get; set; }
            public int Index { get; set; }
        }

        private OptionButton _valueTypeButton;
        private OptionButton _operationButton;
        private Button _isPointerButton;
        private MarginContainer _valuePropertyContainer;
        private ValueProperty _valueProperty;
        private ValueTypeData[] _valueTypes;
        private OperationTypeData[] _operationTypes;
        private Node _relativeToNode;
        private IAssetsRegistry _assetsRegistry;
        private NodeVarPointerSelect _nodeVarPointerSelect;

        public DynamicNodeVarEntry() { }
        public DynamicNodeVarEntry(IAssetsRegistry assetsRegistry, PackedSceneDefaultValuesRegistry defaultValuesRegistry, Node sceneRoot, Node relativeToNode) : base()
        {
            _assetsRegistry = assetsRegistry;
            _relativeToNode = relativeToNode;

            var firstRowHBox = new HBoxContainer();
            var secondRowHBox = new HBoxContainer();
            _contentVBox.AddChild(firstRowHBox);
            _contentVBox.AddChild(secondRowHBox);

            _valuePropertyContainer = new MarginContainer();
            _valuePropertyContainer.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;

            _valueTypeButton = new OptionButton();
            _valueTypeButton.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            _valueTypeButton.ClipText = true;
            _valueTypeButton.Connect("item_selected", this, nameof(OnValueTypeSelected));

            _operationButton = new OptionButton();
            _operationButton.SizeFlagsHorizontal = (int)SizeFlags.Fill;
            _operationButton.RectMinSize = new Vector2(80 * _assetsRegistry.Scale, 0);
            _operationButton.Connect("item_selected", this, nameof(OnOperationSelected));

            _isPointerButton = new Button();
            _isPointerButton.SizeFlagsHorizontal = (int)SizeFlags.Fill;
            _isPointerButton.ToggleMode = true;
            _isPointerButton.Connect("toggled", this, nameof(OnIsPointerToggled));

            _nodeVarPointerSelect = new NodeVarPointerSelect(assetsRegistry, defaultValuesRegistry, sceneRoot, relativeToNode, (nodeVar) => NodeVarUtils.CheckNodeVarCompatible(nodeVar, Data.Operation, Data.ValueType));
            _nodeVarPointerSelect.VarNameChanged += OnContainerVarNameSelected;
            _nodeVarPointerSelect.NodePathChanged += OnNodePathChanged;

            firstRowHBox.AddChild(_nameProperty);
            firstRowHBox.AddChild(_valueTypeButton);
            firstRowHBox.AddChild(_operationButton);

            secondRowHBox.AddChild(_isPointerButton);
            secondRowHBox.AddChild(_nodeVarPointerSelect);
            secondRowHBox.AddChild(_valuePropertyContainer);
            secondRowHBox.AddChild(_resetInitialValueButton);
            secondRowHBox.AddChild(_deleteButton);
        }

        public override void _Ready()
        {
            base._Ready();

#if TOOLS
            if (NodeUtils.IsInEditorSceneTab(this))
                return;
#endif
            _isPointerButton.Icon = GetIcon("GuiScrollArrowRightHl", "EditorIcons");

            InitValueTypes();
            InitOperationTypes();
            UpdateDisabledAndFixedUI();
        }

        protected override void UpdateDisabledAndFixedUI()
        {
            base.UpdateDisabledAndFixedUI();
            _valueTypeButton.Disabled = Disabled || IsFixed;
            _operationButton.Disabled = Disabled || IsFixed;
            if (_valueProperty != null)
                _valueProperty.Disabled = Disabled;
            _nodeVarPointerSelect.Disabled = Disabled;
        }

        private void OnContainerVarNameSelected(string name)
        {
            Data.ContainerVarName = name;
            InvokeDataChanged();
        }

        private void SetValueTypeValueDisplay(Type type)
        {
            var valueTypeData = _valueTypes.First(x => x.Type == type);
            _valueTypeButton.Select(valueTypeData.Index);
            _valueTypeButton.SizeFlagsHorizontal = (int)(valueTypeData.UseIconOnly ? SizeFlags.Fill : SizeFlags.ExpandFill);
            if (valueTypeData.UseIconOnly)
                _valueTypeButton.Text = "";
        }

        private void SetOperationsValueDisplay(NodeVarOperation operation)
        {
            var operationTypeData = _operationTypes.First(x => x.Operation == operation);
            _operationButton.Select(operationTypeData.Index);
        }

        public override void SetData(DynamicNodeVarData value, DynamicNodeVarData defaultData = null)
        {
            var oldData = Data;
            base.SetData(value, defaultData);

            if ((oldData == null && Data != null) || (oldData != null && oldData.ValueType != Data.ValueType))
                UpdateValuePropertyType();

            SetValueTypeValueDisplay(Data.ValueType);
            SetOperationsValueDisplay(Data.Operation);
            _valueProperty.SetValue(Data.InitialValue, false);
            _isPointerButton.SetPressedNoSignal(Data.IsPointer);
            UpdatePointerSelectAndVisibility();
        }

        private void UpdatePointerSelectAndVisibility()
        {
            _nodeVarPointerSelect.SetValue(Data.ContainerPath, Data.ContainerVarName);
            _nodeVarPointerSelect.Visible = Data.IsPointer;
            _valuePropertyContainer.Visible = !Data.IsPointer;
        }

        /// <summary>
        /// Recreates the ValueProperty based on the current Data.ValueType.
        /// </summary>
        private void UpdateValuePropertyType()
        {
            // Update the ValueProperty to the new data type if the data type changes.
            _valueProperty?.QueueFree();
            _valueProperty = ValueProperty.CreateValueProperty(Data.ValueType);
            _valueProperty.ValueChanged += (newValue) =>
            {
                Data.InitialValue = newValue;
                InvokeDataChanged();
            };
            _valueProperty.SetValue(Data.InitialValue, false);
            _valuePropertyContainer.AddChild(_valueProperty);
        }

        private void InitOperationTypes()
        {
            _operationTypes = new[] {
                new OperationTypeData()
                {
                    Name = "Get/Set",
                    Operation = NodeVarOperation.GetSet
                },
                new OperationTypeData() {
                    Name = "Get",
                    Operation = NodeVarOperation.Get
                },
                new OperationTypeData() {
                    Name = "Set",
                    Operation = NodeVarOperation.Set
                },
            };
            foreach (var type in _operationTypes)
            {
                var index = _operationButton.GetItemCount();
                _operationButton.AddItem(type.Name);
                type.Index = index;
            }
        }

        private void InitValueTypes()
        {
            _valueTypes = new[] {
                new ValueTypeData() {
                    Name = "int",
                    Type = typeof(int),
                    Icon = GetIcon("int", "EditorIcons"),
                    UseIconOnly = true
                },
                new ValueTypeData() {
                    Name = "float",
                    Type = typeof(float),
                    Icon = GetIcon("float", "EditorIcons"),
                    UseIconOnly = true
                },
                new ValueTypeData() {
                    Name = "bool",
                    Type = typeof(bool),
                    Icon = GetIcon("bool", "EditorIcons"),
                    UseIconOnly = true
                },
                new ValueTypeData() {
                    Name = "string",
                    Type = typeof(string),
                    Icon = GetIcon("String", "EditorIcons"),
                    UseIconOnly = true
                },
                new ValueTypeData() {
                    Name = "Vector2",
                    Type = typeof(Vector2),
                    Icon = GetIcon("Vector2", "EditorIcons"),
                    UseIconOnly = true
                },
                new ValueTypeData() {
                    Name = "Vector3",
                    Type = typeof(Vector3),
                    Icon = GetIcon("Vector3", "EditorIcons"),
                    UseIconOnly = true
                }
            };
            foreach (var type in _valueTypes)
            {
                int currIndex = _valueTypeButton.GetItemCount();
                type.Index = currIndex;
                _valueTypeButton.AddIconItem(type.Icon, type.Name);
            }
        }

        private void OnNodePathChanged(NodePath newValue)
        {
            Data.ContainerPath = newValue;
            InvokeDataChanged();
        }

        private void OnValueTypeSelected(int index)
        {
            var newType = _valueTypes.First(x => x.Index == index).Type;
            if (Data.ValueType == newType)
                return;
            Data.ValueType = newType;
            Data.InitialValue = DefaultValueUtils.GetDefault(Data.ValueType);
            SetValueTypeValueDisplay(Data.ValueType);
            UpdateValuePropertyType();
            InvokeDataChanged();
        }

        private void OnOperationSelected(int index)
        {
            var operation = _operationTypes.First(x => x.Index == index).Operation;
            if (Data.Operation == operation)
                return;
            Data.Operation = operation;
            SetOperationsValueDisplay(Data.Operation);
            InvokeDataChanged();
        }

        private void OnIsPointerToggled(bool isPointer)
        {
            if (isPointer)
            {
                Data.ContainerPath = new NodePath();
            }
            else
            {
                Data.ContainerPath = null;
                Data.ContainerVarName = null;
            }
            UpdatePointerSelectAndVisibility();
            InvokeDataChanged();
        }

        public void OnBeforeSerialize()
        {
            Data = null;
            DefaultData = null;
        }

        public void OnAfterDeserialize() { }
    }
}
#endif