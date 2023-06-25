using Fractural.Plugin;
using Fractural.Utils;
using Godot;
using System;

#if TOOLS
namespace Fractural.NodeVars
{
    [Tool]
    public class ValueNodeVarStrategyDisplay : NodeVarStrategyDisplay<ValueNodeVarStrategy>
    {
        private MarginContainer _valuePropertyContainer;
        private ValueProperty _valueProperty;
        private Type _prevValueType;

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
            base.SetData(value, defaultData);

            bool rebuildProperty = false;
            bool isInitialSetup = _prevValueType == null;
            if (_prevValueType != Data.ValueType)
            {
                rebuildProperty = true;
                _prevValueType = Data.ValueType;
                if (!isInitialSetup)
                    Strategy.InitialValue = DefaultValueUtils.GetDefault(Data.ValueType);
                InvokeDataChanged();
            }
            UpdateValuePropertyType(rebuildProperty);
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