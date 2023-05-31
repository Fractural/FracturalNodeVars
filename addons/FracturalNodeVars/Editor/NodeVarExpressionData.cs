using Fractural.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using GDC = Godot.Collections;

namespace Fractural.NodeVars
{
    // TODO: Implement NodeVarExpressions
    public class NodeVarExpressionData : IGetNodeVar
    {
        public class NodeVarReference
        {
            public NodePath ContainerPath { get; set; }
            public string ContainerVarName { get; set; }
            public string VarNameAlias { get; set; }

            public GDC.Dictionary ToGDDict()
            {
                var dict = new GDC.Dictionary()
                {
                    { nameof(ContainerPath), ContainerPath },
                    { nameof(ContainerVarName), ContainerVarName }
                };
                if (VarNameAlias != null)
                    dict[nameof(VarNameAlias)] = VarNameAlias;
                return dict;
            }

            public static NodeVarReference FromGDDict(GDC.Dictionary dict)
            {
                return new NodeVarReference()
                {
                    ContainerPath = dict.Get<NodePath>(nameof(ContainerPath)),
                    ContainerVarName = dict.Get<string>(nameof(ContainerVarName)),
                    VarNameAlias = dict.Get<string>(nameof(VarNameAlias), null)
                };
            }

            public NodeVarReference Clone()
            {
                return new NodeVarReference()
                {
                    ContainerPath = ContainerPath,
                    ContainerVarName = ContainerVarName,
                    VarNameAlias = VarNameAlias
                };
            }
        }

        // Serialized
        public string Name { get; set; }
        public string Expression { get; set; }
        public ExpressionParser.Expression AST { get; set; }
        public List<NodeVarReference> NodeVarReferences { get; set; }

        // Runtime
        public object Value => AST.Evaluate();

        public GDC.Dictionary ToGDDict()
        {
            return new GDC.Dictionary()
            {
                { "Type", nameof(NodeVarExpressionData) },
                { nameof(Expression), Expression },
                { nameof(NodeVarReferences), NodeVarReferences.Select(x => x.ToGDDict()).ToGDArray()}
            };
        }

        /// <summary>
        /// Attempts to apply the values of newData ontop of this NodeVar.
        /// Some values will only be copied on certain conditions, such as 
        /// NodeValue being only copied over if the NodeType of the newData 
        /// is the same.
        /// </summary>
        /// <param name="newData"></param>
        /// <returns></returns>
        public NodeVarExpressionData WithChanges(NodeVarExpressionData newData)
        {
            var inheritedData = Clone();
            if (newData.Name != Name)
                return inheritedData;
            if (NodeVarReference)
                foreach (var reference in NodeVarReferences)
                {
                    reference.Equals()
                }
            if (!Equals(newData.Expression, Expression))
                // If the newData's value is different from our value, then prefer the new data's value
                inheritedData.Expression = newData.Expression;
            if (!Equals(newData.ContainerPath, ContainerPath))
                inheritedData.ContainerPath = newData.ContainerPath;
            if (!Equals(newData.ContainerVarName, ContainerVarName))
                inheritedData.ContainerVarName = newData.ContainerVarName;
            return inheritedData;
        }

        public NodeVarExpressionData Clone()
        {
            return new NodeVarExpressionData()
            {
                Name = Name,
                Expression = Expression,
                NodeVarReferences = new List<NodeVarReference>(NodeVarReferences.Select(x => x.Clone()))
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
