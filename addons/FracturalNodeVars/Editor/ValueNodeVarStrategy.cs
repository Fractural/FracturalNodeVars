using Fractural.Utils;
using Godot;
using GDC = Godot.Collections;

namespace Fractural.NodeVars
{
    public interface IResetNodeVarStrategy
    {
        void Reset();
    }

    public interface ISerializableNodeVarStrategy
    {
        object Save();
        void Load(object data);
    }

    public class ValueNodeVarStrategy : NodeVarStrategy, IResetNodeVarStrategy, ISerializableNodeVarStrategy
    {
        public override NodeVarOperation[] ValidOperations => NodeVarOperations.All;

        public object InitialValue { get; set; }
        private object _value;
        public override object Value
        {
            get => _value;
            set => _value = value;
        }

        public override void Ready(Node node)
        {
            Reset();
        }
        public void Reset() => _value = InitialValue;
        public object Save()
        {
            return Value;
        }

        public void Load(object data)
        {
            Value = data;
        }

        public override NodeVarStrategy WithChanges(NodeVarStrategy newData, bool forEditorSerialization = false)
        {
            if (!(newData is ValueNodeVarStrategy strategy)) return null;
            var inheritedData = Clone() as ValueNodeVarStrategy;

            if (!Equals(strategy.InitialValue, InitialValue))
                // If the newData's value is different from our value, then prefer the new data's value
                inheritedData.InitialValue = strategy.InitialValue;
            return inheritedData;
        }

        public override NodeVarStrategy Clone()
        {
            return new ValueNodeVarStrategy()
            {
                InitialValue = InitialValue,
            };
        }

        public override bool Equals(object other)
        {
            return other is ValueNodeVarStrategy strategy &&
                Equals(strategy.InitialValue, InitialValue);
        }

        public override int GetHashCode()
        {
            return InitialValue.GetHashCode();
        }

        public override GDC.Dictionary ToGDDict()
        {
            var dict = base.ToGDDict();
            if (InitialValue != null)
                dict[nameof(InitialValue)] = InitialValue;
            return dict;
        }

        public override void FromGDDict(GDC.Dictionary dict)
        {
            InitialValue = dict.Get<object>(nameof(InitialValue), null);
        }
    }
}
