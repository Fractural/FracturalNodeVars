using Fractural.Utils;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GDC = Godot.Collections;

namespace Fractural.NodeVars
{
    public class ExpressionNodeVarStrategy : NodeVarStrategy
    {
        #region Static
        public struct TypeAndMethod
        {
            public TypeAndMethod(Type type, string method)
            {
                Type = type;
                Method = method;
            }

            public Type Type { get; set; }
            public string Method { get; set; }

            public override bool Equals(object obj)
            {
                return obj is TypeAndMethod data &&
                data.Type == Type &&
                data.Method == Method;
            }

            public override int GetHashCode() => GeneralUtils.CombineHashCodes(Type.GetHashCode(), Method.GetHashCode());
        }

        public static IDictionary<TypeAndMethod, MethodInfo> TypeToNodeVarFuncDict = new Dictionary<TypeAndMethod, MethodInfo>();
        private static bool _initialized = false;

        public static void InitializeStaticData()
        {
            if (_initialized)
                return;
            _initialized = true;
            var types =
                from type in Assembly.GetAssembly(typeof(ExpressionNodeVarStrategy)).GetTypes()
                select type;

            foreach (var type in types)
                foreach (var method in type.GetMethods().Where(x => x.GetCustomAttributes(typeof(NodeVarFuncAttribute), false).Length > 0))
                    TypeToNodeVarFuncDict.Add(new TypeAndMethod(type, method.Name), method);
        }
        #endregion

        public class NodeVarReference
        {
            // Serialized
            public NodePath ContainerPath { get; set; } = new NodePath();
            public string ContainerVarName { get; set; }
            /// <summary>
            /// The key for this variable in this expression.
            /// Usually the same as the ContainerVarName, but must be different
            /// if two variables have the same ContainerVarName.
            /// </summary>
            public string Name { get; set; }

            // Runtime
            public INodeVarContainer Container { get; set; }
            public object Value => Container.GetNodeVar(ContainerVarName);

            public void Ready(Node node)
            {
                Container = node.GetNode<INodeVarContainer>(ContainerPath);
            }

            public NodeVarReference WithChanges(NodeVarReference other)
            {
                if (other.Name == Name)
                    return other.Clone();
                return null;
            }

            public override bool Equals(object obj)
            {
                return obj is NodeVarReference reference &&
                    Equals(reference.ContainerPath, ContainerPath) &&
                    Equals(reference.ContainerVarName, ContainerVarName) &&
                    Equals(reference.Name, Name);
            }

            public override int GetHashCode()
            {
                int code = ContainerPath?.GetHashCode() ?? 0;
                code = GeneralUtils.CombineHashCodes(code, ContainerVarName?.GetHashCode() ?? 0);
                code = GeneralUtils.CombineHashCodes(code, Name.GetHashCode());
                return code;
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

            public override string ToString() => $"{Name}: {JSON.Print(ToGDDict())}";
        }

        #region Main
        public override NodeVarOperation[] ValidOperations => NodeVarOperations.Gettable;

        // Serialized
        public string Expression { get; set; } = "";
        public IDictionary<string, NodeVarReference> NodeVarReferences { get; set; } = new Dictionary<string, NodeVarReference>();

        // Runtime
        public ExpressionParser.Expression AST { get; set; }
        public override object Value
        {
            get => AST.Evaluate();
        }
        public object PrivateValue => AST.Evaluate();

        private Node _node;
        private Type _nodeType;

        public override void Ready(Node node)
        {
            InitializeStaticData();

            _node = node;
            _nodeType = node.GetType();
            var methods = node.GetType().GetMethods().Where(x => x.GetCustomAttributes(typeof(NodeVarFuncAttribute), false).Length > 0).ToArray();

            AST = ExpressionUtils.ParseFromText(Expression, GetVariable, CallFunction);
            foreach (var reference in NodeVarReferences.Values)
                reference.Ready(node);
        }

        public object GetVariable(string name) => NodeVarReferences[name].Value;
        public object CallFunction(string name, object[] args)
        {
            if (TypeToNodeVarFuncDict.TryGetValue(new TypeAndMethod(_nodeType, name), out MethodInfo method))
                return method.Invoke(_node, args);
            return null;
        }

        public override NodeVarStrategy WithChanges(NodeVarStrategy newData, bool forEditorSerialization = false)
        {
            if (!(newData is ExpressionNodeVarStrategy strategy)) return null;

            // NOTE: We dont' care if the newData.ValueType == ValueType, since types for
            // expression node vars are user set anyways.
            var inheritedData = Clone() as ExpressionNodeVarStrategy;
            if (forEditorSerialization)
                // We don't save default NodeVarReference for editor.
                inheritedData.NodeVarReferences.Clear();
            // Make sure old NodeVarReferences are always there.
            // Inheriting a NodeVarExpression should never remove existing NodeVarReferences.
            foreach (var reference in strategy.NodeVarReferences.Values)
                inheritedData.NodeVarReferences[reference.Name] = reference.Clone();
            if (!Equals(strategy.Expression, Expression))
                // If the newData's value is different from our value, then prefer the new data's value
                inheritedData.Expression = strategy.Expression;
            return inheritedData;
        }

        public override NodeVarStrategy Clone()
        {
            var inst = new ExpressionNodeVarStrategy()
            {
                Expression = Expression,
            };
            foreach (var pair in NodeVarReferences)
                inst.NodeVarReferences.Add(pair.Key, pair.Value.Clone());
            return inst;
        }

        public override bool Equals(object other)
        {
            return other is ExpressionNodeVarStrategy strategy &&
                Equals(strategy.Expression, Expression) &&
                strategy.NodeVarReferences.SequenceEqual(NodeVarReferences);
        }

        public override int GetHashCode() => GeneralUtils.CombineHashCodes(Expression.GetHashCode(), NodeVarReferences);

        public override GDC.Dictionary ToGDDict()
        {
            var dict = base.ToGDDict();
            if (Expression != "")
                dict[nameof(Expression)] = Expression;
            if (NodeVarReferences.Count > 0)
            {
                var nodeVarReferencesDict = new GDC.Dictionary();
                foreach (var pair in NodeVarReferences)
                    nodeVarReferencesDict[pair.Key] = pair.Value.ToGDDict();
                dict[nameof(NodeVarReferences)] = nodeVarReferencesDict;
            }
            return dict;
        }

        public override void FromGDDict(GDC.Dictionary dict)
        {
            Expression = dict.Get<string>(nameof(Expression), "");
            var nodeVarReferencesDict = dict.Get(nameof(NodeVarReferences), new GDC.Dictionary());
            foreach (string key in nodeVarReferencesDict.Keys)
            {
                var reference = new NodeVarReference();
                reference.FromGDDict(nodeVarReferencesDict.Get<GDC.Dictionary>(key), key);
                NodeVarReferences.Add(key, reference);
            }
        }
        #endregion
    }
}
