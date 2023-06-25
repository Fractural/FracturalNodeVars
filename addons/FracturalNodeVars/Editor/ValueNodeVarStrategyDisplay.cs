using Fractural.Plugin;
using Godot;

#if TOOLS
namespace Fractural.NodeVars
{
    [Tool]
    public class ValueNodeVarStrategyDisplay : NodeVarStrategyDisplay<ValueNodeVarStrategy>
    {
        private MarginContainer _valuePropertyContainer;
        private ValueProperty _valueProperty;

        public ValueNodeVarStrategyDisplay() : base() { }
        public ValueNodeVarStrategyDisplay(Control topRow)
        {
            _valuePropertyContainer = new MarginContainer();
            _valuePropertyContainer.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;

            topRow.AddChild(_valuePropertyContainer);
        }

        public override void UpdateDisabledAndFixedUI(bool isFixed, bool disabled, bool privateDisabled, bool nonSetDisabled)
        {
            if (_valueProperty != null)
                _valueProperty.Disabled = disabled || privateDisabled || nonSetDisabled;
        }

        public override void SetData(NodeVarData value, NodeVarData defaultData = null)
        {
            var oldData = Data;
            base.SetData(value, defaultData);
            UpdateValuePropertyType(oldData?.ValueType != Data?.ValueType);
        }

        /// <summary>
        /// Recreates the ValueProperty based on the current Data.ValueType.
        /// </summary>
        private void UpdateValuePropertyType(bool rebuildProperty = false)
        {
            // Update the ValueProperty to the new data type if the data type changes.
            if (rebuildProperty)
            {
                _valueProperty?.QueueFree();
                _valueProperty = ValueProperty.CreateValueProperty(Data.ValueType);
                _valueProperty.ValueChanged += (newValue) =>
                {
                    Strategy.InitialValue = newValue;
                    InvokeDataChanged();
                };
                _valuePropertyContainer.AddChild(_valueProperty);
            }
            _valueProperty.SetValue(Strategy.InitialValue, false);
        }
    }
}
#endif