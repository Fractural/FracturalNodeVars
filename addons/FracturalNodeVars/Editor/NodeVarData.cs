using Fractural.Utils;
using Godot;
using System;
using GDC = Godot.Collections;

namespace Fractural.NodeVars
{
    public class NodeVarData
    {
        public string Name { get; set; }
        public NodeVarOperation Operation { get; set; }
        public NodeVarStrategy Strategy { get; set; }
        public Type ValueType { get; set; }
        public object Value
        {
            get => GetValue();
            set => SetValue(value);
        }
        public object GetValue(bool includePrivate = false)
        {
            if (Operation.IsGet(includePrivate))
            {
                var result = Strategy.Value;
                if (result.GetType() != ValueType)
                    throw new Exception($"{nameof(NodeVarData)}: Get value is not of type \"{ValueType.Name}\".");
                return result;
            }
            else
                throw new Exception($"{nameof(NodeVarData)}: Could not get on NodeVar of operation \"{Operation}\".");
        }
        public void SetValue(object value, bool includePrivate = false)
        {
            if (Operation.IsSet(includePrivate))
            {
                if (value.GetType() != ValueType)
                    throw new Exception($"{nameof(NodeVarData)}: Attempted to set value that's not of type \"{ValueType.Name}\".");
                Strategy.Value = value;
            }
            else
                throw new Exception($"{nameof(NodeVarData)}: Could not set on NodeVar of operation \"{Operation}\".");
        }

        public void Ready(Node node) => Strategy.Ready(node);
        public override bool Equals(object obj)
        {
            if (obj is NodeVarData data)
                return Equals(data);
            return false;
        }
        public override int GetHashCode() => GeneralUtils.CombineHashCodes(Name.GetHashCode(), Operation.GetHashCode(), Strategy.GetHashCode());

        /// <summary>
        /// Attempts to use another NodeVar's data to make changes to this NodeVar.
        /// </summary>
        /// <param name="other">Data to use as changed</param>
        /// <param name="forEditorSerialization">Is the returned data for editor use?</param>
        /// <returns>Returns the resulting NodeVar with the changes on success. Returns null if the two NodeVars are incompatible.</returns>
        public NodeVarData WithChanges(NodeVarData other, bool forEditorSerialization = false)
        {
            if (other.Name == Name)
            {
                var inheritedData = Clone();
                if (Strategy.GetType() == other.Strategy.GetType())
                    inheritedData.Strategy = Strategy.WithChanges(other.Strategy, forEditorSerialization);
                else if (other.Strategy.ValidOperations.Contains(Operation))
                    inheritedData.Strategy = other.Strategy;
                return inheritedData;
            }
            return null;
        }
        public NodeVarData Clone()
        {
            return new NodeVarData()
            {
                Name = Name,
                Operation = Operation,
                Strategy = Strategy.Clone()
            };
        }
        public virtual GDC.Dictionary ToGDDict()
        {
            var dict = new GDC.Dictionary()
            {
                { nameof(Operation), (int)Operation },
                { nameof(Strategy), Strategy.ToGDDict() }
            };
            return dict;
        }
        public virtual void FromGDDict(GDC.Dictionary dict, string name)
        {
            Name = name;
            Operation = (NodeVarOperation)dict.Get<int>(nameof(Operation));
            Strategy = NodeVarUtils.NodeVarStrategyFromGDDict(dict.Get<GDC.Dictionary>(nameof(Strategy)));
        }
    }
}
