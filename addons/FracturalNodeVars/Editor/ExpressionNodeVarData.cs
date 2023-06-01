using Fractural.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using GDC = Godot.Collections;

namespace Fractural.NodeVars
{
    /// <summary>
    /// NodeVar that evaluates an expression to get a value.
    /// </summary>
    public class ExpressionNodeVarData : NodeVarData<ExpressionNodeVarData>, IGetNodeVar
    {
        public class NodeVarReference
        {
            // Serialized
            public NodePath ContainerPath { get; set; }
            public string ContainerVarName { get; set; }
            /// <summary>
            /// The key for this variable in this expression.
            /// Usually the same as the ContainerVarName, but must be different
            /// if two variables have the same ContainerVarName.
            /// </summary>
            public string Name { get; set; }

            // Runtime
            public INodeVarContainer Container { get; set; }
            public object Value => Container.GetDictNodeVar(ContainerVarName);

            public void Ready(Node node)
            {
                Container = node.GetNode<INodeVarContainer>(ContainerPath);
            }

            public GDC.Dictionary ToGDDict()
            {
                var dict = new GDC.Dictionary()
                {
                    { nameof(ContainerPath), ContainerPath },
                };
                if (ContainerVarName != Name)
                    dict[nameof(ContainerVarName)] = ContainerVarName;
                return dict;
            }

            public void FromGDDict(GDC.Dictionary dict, string key)
            {
                Name = key;
                ContainerPath = dict.Get<NodePath>(nameof(ContainerPath));
                ContainerVarName = dict.Get(nameof(ContainerVarName), Name);
            }

            public NodeVarReference Clone()
            {
                return new NodeVarReference()
                {
                    ContainerPath = ContainerPath,
                    ContainerVarName = ContainerVarName,
                    Name = Name
                };
            }
        }

        // Serialized
        public string Expression { get; set; }
        public IDictionary<string, NodeVarReference> NodeVarReferences { get; set; } = new Dictionary<string, NodeVarReference>();

        // Runtime
        public ExpressionParser.Expression AST { get; set; }
        public object Value => AST.Evaluate();

        public override void Ready(Node node)
        {
            AST = ExpressionUtils.ParseFromText(Expression, GetVariable);
        }

        public object GetVariable(string name) => NodeVarReferences[name].Value;

        /// <summary>
        /// Attempts to apply the values of newData ontop of this NodeVar.
        /// Some values will only be copied on certain conditions, such as 
        /// NodeValue being only copied over if the NodeType of the newData 
        /// is the same.
        /// </summary>
        /// <param name="newData"></param>
        /// <returns></returns>
        public override ExpressionNodeVarData WithChanges(ExpressionNodeVarData newData)
        {
            var inheritedData = TypedClone();
            if (newData.Name != Name)
                return inheritedData;
            // Make sure old NodeVarReferences are always there.
            // Inheriting a NodeVarExpression should never remove existing NodeVarReferences.
            foreach (var reference in newData.NodeVarReferences)
            {
                if (!inheritedData.NodeVarReferences.Any(x => x.Equals(reference)))
                    inheritedData.NodeVarReferences.Add(reference);
            }
            if (!Equals(newData.Expression, Expression))
                // If the newData's value is different from our value, then prefer the new data's value
                inheritedData.Expression = newData.Expression;
            return inheritedData;
        }

        public override ExpressionNodeVarData TypedClone()
        {
            var inst = new ExpressionNodeVarData()
            {
                Name = Name,
                Expression = Expression,
            };
            foreach (var pair in NodeVarReferences)
                inst.NodeVarReferences.Add(pair.Key, pair.Value.Clone());
            return inst;
        }

        public override bool Equals(ExpressionNodeVarData otherData)
        {
            return otherData.Name == Name &&
                otherData.Expression == Expression &&
                otherData.NodeVarReferences.SequenceEqual(NodeVarReferences);
        }

        public override int GetHashCodeForData()
        {
            var code = Name.GetHashCode();
            code = GeneralUtils.CombineHashCodes(code, Expression.GetHashCode());
            foreach (var reference in NodeVarReferences)
                code = GeneralUtils.CombineHashCodes(code, reference.GetHashCode());
            return code;
        }

        public override GDC.Dictionary ToGDDict()
        {
            var dict = new GDC.Dictionary()
            {
                { "Type", nameof(ExpressionNodeVarData) },
                { nameof(Expression), Expression },
            };
            var nodeVarReferencesDict = new GDC.Dictionary();
            foreach (var pair in NodeVarReferences)
                nodeVarReferencesDict[pair.Key] = pair.Value.ToGDDict();
            dict[nameof(NodeVarReference)] = nodeVarReferencesDict;
            return dict;
        }

        public override void FromGDDict(GDC.Dictionary dict, string name)
        {
            Name = name;
            Expression = dict.Get<string>(nameof(Expression), null);
            var nodeVarReferencesDict = dict.Get<GDC.Dictionary>(nameof(NodeVarReferences));
            foreach (string key in nodeVarReferencesDict.Keys)
            {
                var reference = new NodeVarReference();
                reference.FromGDDict(nodeVarReferencesDict.Get<GDC.Dictionary>(key), key);
                NodeVarReferences.Add(key, reference);
            }
        }
    }
}
