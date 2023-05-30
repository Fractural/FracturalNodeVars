﻿using Fractural.Utils;
using Godot;
using System;
using GDC = Godot.Collections;

namespace Fractural.NodeVars
{
    public class NodeVarData
    {
        // Serialized
        public Type ValueType { get; set; }
        public NodeVarOperation Operation { get; set; }
        public string Name { get; set; }
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
                if (IsPointer)
                    return Container.GetDictNodeVar(ContainerVarName);
                return _value;
            }
            set
            {
                if (IsPointer)
                    Container.SetDictNodeVar(ContainerVarName, value);
                else
                    _value = value;
            }
        }

        public void Reset() => _value = InitialValue;

        /// <summary>
        /// Whether the DictNodeVar is a pointer to another DictNodeVar
        /// </summary>
        public bool IsPointer => ContainerPath != null;

        public GDC.Dictionary ToGDDict()
        {
            var dict = new GDC.Dictionary()
            {
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

        /// <summary>
        /// Attempts to apply the values of newData ontop of this NodeVar.
        /// Some values will only be copied on certain conditions, such as 
        /// NodeValue being only copied over if the NodeType of the newData 
        /// is the same.
        /// </summary>
        /// <param name="newData"></param>
        /// <returns></returns>
        public NodeVarData WithChanges(NodeVarData newData)
        {
            var inheritedData = Clone();
            if (newData.Name != Name)
                return inheritedData;
            if (newData.ValueType == ValueType)
            {
                if (!Equals(newData.InitialValue, InitialValue))
                    // If the newData's value is different from our value, then prefer the new data's value
                    inheritedData.InitialValue = newData.InitialValue;
                if (!Equals(newData.ContainerPath, ContainerPath))
                    inheritedData.ContainerPath = newData.ContainerPath;
                if (!Equals(newData.ContainerVarName, ContainerVarName))
                    inheritedData.ContainerVarName = newData.ContainerVarName;
            }
            return inheritedData;
        }

        public NodeVarData Clone()
        {
            return new NodeVarData()
            {
                ValueType = ValueType,
                Operation = Operation,
                Name = Name,
                ContainerPath = ContainerPath,
                InitialValue = InitialValue,
                ContainerVarName = ContainerVarName
            };
        }

        public override bool Equals(object obj)
        {
            return obj is NodeVarData otherData &&
                otherData.ValueType == ValueType &&
                otherData.Operation == Operation &&
                otherData.Name == Name &&
                otherData.ContainerPath == ContainerPath &&
                Equals(otherData.InitialValue, InitialValue) &&
                otherData.ContainerVarName == ContainerVarName;
        }

        public override int GetHashCode()
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

        public static NodeVarData FromGDDict(GDC.Dictionary dict, string name)
        {
            return new NodeVarData()
            {
                ValueType = ReflectionUtils.FindTypeFullName(dict.Get<string>(nameof(ValueType))),
                Operation = (NodeVarOperation)dict.Get<int>(nameof(Operation)),
                ContainerPath = dict.Get<NodePath>(nameof(ContainerPath), null),
                ContainerVarName = dict.Get<string>(nameof(ContainerVarName), null),
                InitialValue = dict.Get<object>(nameof(InitialValue), null),
                Name = name,
            };
        }
    }
}
