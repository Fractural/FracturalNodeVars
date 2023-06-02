using Fractural.Utils;
using Godot;
using System;
using GDC = Godot.Collections;

namespace Fractural.NodeVars
{
    /// <summary>
    /// NodeVar that can change it's behaviour. It can change it's operation, and also change whether it's a pointer or not.
    /// </summary>
    public class DynamicNodeVarData : NodeVarData<DynamicNodeVarData>, IGetSetNodeVar, ITypedNodeVar
    {
        // Serialized
        public Type ValueType { get; set; }
        public NodeVarOperation Operation { get; set; }
        public string ContainerVarName { get; set; }
        public NodePath ContainerPath { get; set; }

        // Runtime
        public INodeVarContainer Container { get; set; }
        public object InitialValue { get; set; }
        private object _value;
        public object Value
        {
            get
            {
                if (Operation != NodeVarOperation.Get && Operation != NodeVarOperation.GetSet)
                    throw new Exception($"{nameof(DynamicNodeVarData)}: Attempted to get a non-getttable NodeVar \"{Name}\".");
                if (IsPointer)
                    return Container.GetDictNodeVar(ContainerVarName);
                return _value;
            }
            set
            {
                if (Operation != NodeVarOperation.Set && Operation != NodeVarOperation.GetSet)
                    throw new Exception($"{nameof(DynamicNodeVarData)}: Attempted to set a non-setttable NodeVar \"{Name}\".");
                if (IsPointer)
                    Container.SetDictNodeVar(ContainerVarName, value);
                else
                    _value = value;
            }
        }

        public override void Ready(Node node)
        {
            if (IsPointer)
                Container = node.GetNode<INodeVarContainer>(ContainerPath);
            else
                Reset();
        }

        public void Reset() => _value = InitialValue;

        /// <summary>
        /// Whether the DictNodeVar is a pointer to another DictNodeVar
        /// </summary>
        public bool IsPointer => ContainerPath != null;

        public override DynamicNodeVarData WithChanges(DynamicNodeVarData newData)
        {
            if (newData.Name == Name && newData.ValueType == ValueType)
            {
                var inheritedData = TypedClone();
                if (!Equals(newData.InitialValue, InitialValue))
                    // If the newData's value is different from our value, then prefer the new data's value
                    inheritedData.InitialValue = newData.InitialValue;
                if (!Equals(newData.ContainerPath, ContainerPath))
                    inheritedData.ContainerPath = newData.ContainerPath;
                if (!Equals(newData.ContainerVarName, ContainerVarName))
                    inheritedData.ContainerVarName = newData.ContainerVarName;
                return inheritedData;
            }
            return null;
        }

        public override DynamicNodeVarData TypedClone()
        {
            return new DynamicNodeVarData()
            {
                ValueType = ValueType,
                Operation = Operation,
                Name = Name,
                ContainerPath = ContainerPath,
                InitialValue = InitialValue,
                ContainerVarName = ContainerVarName
            };
        }

        public override bool Equals(DynamicNodeVarData otherData)
        {
            return otherData.ValueType == ValueType &&
                otherData.Operation == Operation &&
                otherData.Name == Name &&
                otherData.ContainerPath == ContainerPath &&
                Equals(otherData.InitialValue, InitialValue) &&
                otherData.ContainerVarName == ContainerVarName;
        }

        public override int GetHashCodeForData()
        {
            var code = ValueType.GetHashCode();
            code = GeneralUtils.CombineHashCodes(code, Operation.GetHashCode());
            code = GeneralUtils.CombineHashCodes(code, Name.GetHashCode());
            if (ContainerPath != null)
                code = GeneralUtils.CombineHashCodes(code, ContainerPath.GetHashCode());
            if (InitialValue != null)
                code = GeneralUtils.CombineHashCodes(code, InitialValue.GetHashCode());
            if (ContainerVarName != null)
                code = GeneralUtils.CombineHashCodes(code, ContainerVarName.GetHashCode());
            return code;
        }

        public override GDC.Dictionary ToGDDict()
        {
            var dict = new GDC.Dictionary()
            {
                { "Type", nameof(DynamicNodeVarData) },
                { nameof(ValueType), ValueType.FullName },
                { nameof(Operation), (int)Operation },
            };
            if (InitialValue != null)
                dict[nameof(InitialValue)] = InitialValue;
            if (IsPointer)
            {
                dict[nameof(ContainerPath)] = ContainerPath;
                dict[nameof(ContainerVarName)] = ContainerVarName;
            }
            return dict;
        }

        public override void FromGDDict(GDC.Dictionary dict, string name)
        {
            ValueType = ReflectionUtils.FindTypeFullName(dict.Get<string>(nameof(ValueType)));
            Operation = (NodeVarOperation)dict.Get<int>(nameof(Operation));
            ContainerPath = dict.Get<NodePath>(nameof(ContainerPath), null);
            ContainerVarName = dict.Get<string>(nameof(ContainerVarName), null);
            InitialValue = dict.Get<object>(nameof(InitialValue), null);
            Name = name;
        }
    }
}
