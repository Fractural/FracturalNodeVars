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
    }

    public class OperationTypeData
    {
        public string Name { get; set; }
        public NodeVarOperation Operation { get; set; }
        public int Index { get; set; }
    }

    [Tool]
    public class DynamicNodeVarEntry : NodeVarEntry<DynamicNodeVarData>
    {
        private OptionButton _valueTypeButton;
        private Button _isPointerButton;
        private MarginContainer _valuePropertyContainer;
        private ValueProperty _valueProperty;
        private ValueTypeData[] _valueTypes;
        private NodeVarPointerSelect _nodeVarPointerSelect;

        private HBoxContainer _firstRowHBox;
        private HBoxContainer _secondRowHBox;

        public DynamicNodeVarEntry() : base() { }
        public DynamicNodeVarEntry(INodeVarContainer propagationSource, IAssetsRegistry assetsRegistry, PackedSceneDefaultValuesRegistry defaultValuesRegistry, Node sceneRoot, Node relativeToNode) : base(sceneRoot, relativeToNode, assetsRegistry)
        {
            _firstRowHBox = new HBoxContainer();
            _secondRowHBox = new HBoxContainer();
            _contentVBox.AddChild(_firstRowHBox);
            _contentVBox.AddChild(_secondRowHBox);

            _valuePropertyContainer = new MarginContainer();
            _valuePropertyContainer.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;

            _valueTypeButton = new OptionButton();
            _valueTypeButton.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            _valueTypeButton.ClipText = true;
            _valueTypeButton.Connect("item_selected", this, nameof(OnValueTypeSelected));

            _isPointerButton = new Button();
            _isPointerButton.SizeFlagsHorizontal = (int)SizeFlags.Fill;
            _isPointerButton.ToggleMode = true;
            _isPointerButton.Connect("toggled", this, nameof(OnIsPointerToggled));

            _nodeVarPointerSelect = new NodeVarPointerSelect(
                propagationSource,
                assetsRegistry,
                defaultValuesRegistry,
                sceneRoot,
                relativeToNode,
                (container, nodeVar) => NodeVarUtils.IsNodeVarValidPointer(
                    container,
                    _relativeToNode,
                    _sceneRoot,
                    nodeVar,
                    Data.Operation,
                    Data.ValueType
                )
            );
            _nodeVarPointerSelect.VarNameChanged += OnContainerVarNameSelected;
            _nodeVarPointerSelect.NodePathChanged += OnNodePathChanged;

            _firstRowHBox.AddChild(_nameProperty);
            _firstRowHBox.AddChild(_valueTypeButton);
            _firstRowHBox.AddChild(_operationButton);

            _secondRowHBox.AddChild(_isPointerButton);
            _secondRowHBox.AddChild(_nodeVarPointerSelect);
            _secondRowHBox.AddChild(_valuePropertyContainer);
            _secondRowHBox.AddChild(_resetInitialValueButton);
            _secondRowHBox.AddChild(_deleteButton);
        }

        public override void _Ready()
        {
            base._Ready();

#if TOOLS
            if (NodeUtils.IsInEditorSceneTab(this))
                return;
#endif
            _valueTypeButton.AddColorOverride("icon_color_disabled", Colors.White);
            _valueTypeButton.AddColorOverride("font_color_disabled", _operationButton.GetColor("font_color"));
            _isPointerButton.Icon = GetIcon("GuiScrollArrowRightHl", "EditorIcons");

            InitValueTypes();
            UpdateDisabledAndFixedUI();
        }

        protected override void UpdateDisabledAndFixedUI()
        {
            base.UpdateDisabledAndFixedUI();

            _secondRowHBox.Visible = !NonSetDisabled;
            _valueTypeButton.Disabled = Disabled || IsFixed || PrivateDisabled || NonSetDisabled;
            if (_valueProperty != null)
                _valueProperty.Disabled = Disabled || PrivateDisabled || NonSetDisabled;
            _nodeVarPointerSelect.Disabled = Disabled || PrivateDisabled || NonSetDisabled;
            _isPointerButton.Disabled = Disabled || PrivateDisabled || NonSetDisabled;
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

        public override void SetData(DynamicNodeVarData value, DynamicNodeVarData defaultData = null)
        {
            var oldData = Data;
            base.SetData(value, defaultData);

            if ((oldData == null && Data != null) || (oldData != null && oldData.ValueType != Data.ValueType))
                UpdateValuePropertyType();

            SetValueTypeValueDisplay(Data.ValueType);
            _valueProperty.SetValue(Data.InitialValue, false);
            _isPointerButton.SetPressedNoSignal(Data.IsPointer);
            UpdatePointerSelectAndVisibility();

            UpdateDisabledAndFixedUI();
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
    }
}
#endif