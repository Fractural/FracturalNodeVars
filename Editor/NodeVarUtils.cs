using System;
using System.Reflection;
using System.Collections.Generic;
using Fractural.Utils;
using Godot;
using GDC = Godot.Collections;
using System.Linq;

namespace Fractural.NodeVars
{
    public static class NodeVarOperations
    {
        public static readonly NodeVarOperation[] All = new[]
        {
            NodeVarOperation.GetSet,
            NodeVarOperation.Get,
            NodeVarOperation.Set,
            NodeVarOperation.GetPrivateSet,
            NodeVarOperation.SetPrivateGet,
            NodeVarOperation.PrivateGet,
            NodeVarOperation.PrivateSet,
            NodeVarOperation.PrivateGetSet,
        };
        public static readonly NodeVarOperation[] OnlyGet = new[] { NodeVarOperation.Get, NodeVarOperation.PrivateGet };
        public static readonly NodeVarOperation[] OnlySet = new[] { NodeVarOperation.Set, NodeVarOperation.PrivateSet };
        public static readonly NodeVarOperation[] GetSet = new[] { NodeVarOperation.GetSet, NodeVarOperation.GetPrivateSet, NodeVarOperation.SetPrivateGet, NodeVarOperation.PrivateGetSet };
        public static readonly NodeVarOperation[] Settable = new[] {
            NodeVarOperation.Set, NodeVarOperation.PrivateSet,
            NodeVarOperation.GetSet, NodeVarOperation.SetPrivateGet, NodeVarOperation.PrivateGetSet, NodeVarOperation.GetPrivateSet
        };
        public static readonly NodeVarOperation[] Gettable = new[] {
            NodeVarOperation.Get, NodeVarOperation.PrivateGet,
            NodeVarOperation.GetSet, NodeVarOperation.SetPrivateGet, NodeVarOperation.PrivateGetSet, NodeVarOperation.GetPrivateSet
        };
    }

    public static class NodeVarUtils
    {
        public static bool IsPrivateGet(this NodeVarOperation operation)
        {
            return operation == NodeVarOperation.SetPrivateGet || operation == NodeVarOperation.PrivateGet;
        }

        public static bool IsPrivateSet(this NodeVarOperation operation)
        {
            return operation == NodeVarOperation.GetPrivateSet || operation == NodeVarOperation.PrivateSet;
        }

        public static bool IsGet(this NodeVarOperation operation, bool includePrivate = false)
        {
            return operation == NodeVarOperation.Get || operation == NodeVarOperation.GetSet || operation == NodeVarOperation.GetPrivateSet || (includePrivate && IsPrivateGet(operation));
        }

        public static bool IsSet(this NodeVarOperation operation, bool includePrivate = false)
        {
            return operation == NodeVarOperation.Set || operation == NodeVarOperation.GetSet || operation == NodeVarOperation.SetPrivateGet || (includePrivate && IsPrivateSet(operation));
        }

        public static bool IsPrivate(this NodeVarOperation operation)
        {
            return operation == NodeVarOperation.PrivateGet || operation == NodeVarOperation.PrivateSet || operation == NodeVarOperation.PrivateGetSet;
        }

        public static bool HasPublic(this NodeVarOperation operation)
        {
            return !operation.IsPrivate();
        }

        public static bool IsNodeVarValidPointer(INodeVarContainer nodeVarContainer, Node sourceNode, Node sceneRoot, NodeVarData nodeVar, NodeVarOperation sourceOperation, Type sourceValueType = null)
        {
            if (sourceValueType != null && nodeVar.ValueType != sourceValueType) return false;
            NodeVarOperation nodeVarOperation = nodeVar.Operation;

            // Pointers can only be set on NodeVars that have a publically accessible setter.
            // Private Get NodeVars are only used when the sourceNode is a child of the nodeVarContainer.

            bool isSourceNodeInstanced = IsInstancedScene(sourceNode, sceneRoot);
            bool includePrivate = sourceNode == nodeVarContainer || sourceNode.HasParent(nodeVarContainer as Node);
            if (isSourceNodeInstanced)
                // If the source node is instanced, then we can only make pointers for settable NodeVars, and only 
                // attach gettable NodeVars as pointers.
                // Private NodeVars from the container can be used as a pointer if the container is a parent of the sourceNode.
                return sourceOperation.IsSet(true) && nodeVarOperation.IsGet(includePrivate);
            else
                // If the source node is not instanced, then we can make pointers for any sourceNode variable, which point to any
                // sort of gettable variable from nodeVarContainer.
                return nodeVarOperation.IsGet(includePrivate);
        }

        public static bool IsInstancedScene(Node node, Node sceneRoot)
        {
            return node != sceneRoot && node.Filename != "";
        }

        public static NodeVarData NodeVarDataFromGDDict(GDC.Dictionary dict, string name)
        {
            var nodeVarData = new NodeVarData();
            nodeVarData.FromGDDict(dict, name);
            return nodeVarData;
        }

        public static NodeVarStrategy NodeVarStrategyFromGDDict(GDC.Dictionary dict)
        {
            string type = dict.Get<string>("Type", nameof(PointerNodeVarStrategy));
            NodeVarStrategy result;
            switch (type)
            {
                case nameof(ValueNodeVarStrategy):
                    result = new ValueNodeVarStrategy();
                    break;
                case nameof(PointerNodeVarStrategy):
                    result = new PointerNodeVarStrategy();
                    break;
                case nameof(ExpressionNodeVarStrategy):
                    result = new ExpressionNodeVarStrategy();
                    break;
                default:
                    throw new Exception($"{nameof(NodeVarUtils)}: Cannot convert type \"{type}\" to {nameof(NodeVarStrategy)} from GDDict.");
            }
            result.FromGDDict(dict);
            return result;
        }

        /// <summary>
        /// Returns all fixed node vars for a given type, with each node var
        /// given a default value.
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public static NodeVarData[] GetNodeVarsFromAttributes(Type objectType)
        {
            var fixedDictNodeVars = new List<NodeVarData>();
            foreach (var property in objectType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Concat(objectType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)))
            {
                var attribute = property.GetCustomAttribute<NodeVarAttribute>();
                if (attribute == null)
                    continue;

                NodeVarOperation operation;
                if (attribute.Operation.HasValue)
                    operation = attribute.Operation.Value;
                else
                {
                    // We scan the property for getters and setters to determien the operation
                    var getMethod = property.GetGetMethod();
                    var setMethod = property.GetSetMethod();

                    bool hasGetter = getMethod != null && getMethod.IsPublic;
                    bool hasSetter = setMethod != null && setMethod.IsPublic;

                    if (hasGetter && hasSetter)
                        operation = NodeVarOperation.GetSet;
                    else if (hasGetter)
                        // If the property has a getter, then it needs someone else to set it's value
                        operation = NodeVarOperation.SetPrivateGet;
                    else
                        // If the property has a setter, then it means other users can get it's value
                        operation = NodeVarOperation.Get;
                }

                fixedDictNodeVars.Add(new NodeVarData()
                {
                    Name = property.Name,
                    ValueType = property.PropertyType,
                    Operation = operation,
                    Strategy = new ValueNodeVarStrategy()
                    {
                        InitialValue = DefaultValueUtils.GetDefault(property.PropertyType)
                    }
                });
            }
            return fixedDictNodeVars.ToArray();
        }

        /// <summary>
        /// Returns all fixed node vars for a given type, with each node var
        /// given a default value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static NodeVarData[] GetNodeVarsFromAttributes<T>() => GetNodeVarsFromAttributes(typeof(T));

        public static string GetNextVarName(IEnumerable<string> previousValues)
        {
            uint highestNumber = 0;
            if (previousValues != null)
            {
                foreach (var value in previousValues)
                    if (uint.TryParse(value.TrimPrefix("Var"), out uint intValue) && intValue > highestNumber)
                        highestNumber = intValue;
                highestNumber++;
            }
            return "Var" + highestNumber.ToString();
        }

        public static NodeVarData[] GetNodeVarsList(this INodeVarContainer nodeVarContainer, PackedSceneDefaultValuesRegistry packedSceneDefaultValuesRegistry)
        {
            if (nodeVarContainer is IDictNodeVarContainer container && nodeVarContainer is Node containerNode)
            {
                var nodeVars = container.RawNodeVarsGDDict;
                var nodeVarsDict = new Dictionary<string, NodeVarData>();

                foreach (string key in nodeVars.Keys)
                {
                    var nodeVar = NodeVarDataFromGDDict(nodeVars.Get<GDC.Dictionary>(key), key);
                    nodeVarsDict.Add(key, nodeVar);
                }

                var defaultNodeVars = new Dictionary<string, NodeVarData>();
                if (containerNode.Filename != "")
                {
                    var defaultInheritedNodeVars = packedSceneDefaultValuesRegistry.GetDefaultValue<GDC.Dictionary>(containerNode.Filename, "_NodeVars");
                    foreach (string key in defaultInheritedNodeVars.Keys)
                        defaultNodeVars.Add(key, NodeVarDataFromGDDict(defaultInheritedNodeVars.Get<GDC.Dictionary>(key), key));
                }
                var defaultAttributes = GetNodeVarsFromAttributes(container.GetType());
                foreach (var nodeVar in defaultAttributes)
                    if (!defaultNodeVars.ContainsKey(nodeVar.Name))
                        defaultNodeVars.Add(nodeVar.Name, nodeVar);

                foreach (var defaultNodeVar in defaultNodeVars.Values)
                {
                    if (nodeVarsDict.TryGetValue(defaultNodeVar.Name, out NodeVarData localVar))
                    {
                        // Update the NodeVar based off of the existing NodeVar
                        var nodeVarWithChanges = defaultNodeVar.WithChanges(localVar);
                        if (nodeVarWithChanges != null)
                        {
                            nodeVarsDict[defaultNodeVar.Name] = nodeVarWithChanges;
                        }
                        else
                        {
                            GD.PushWarning($"{nameof(NodeVarContainer)}: NodeVar of name \"{defaultNodeVar.Name}\" could not be merged with its default value, therefore reverting back to default.");
                            nodeVarsDict[defaultNodeVar.Name] = defaultNodeVar;
                        }
                    }
                    else
                        nodeVarsDict[defaultNodeVar.Name] = defaultNodeVar;
                }

                return nodeVarsDict.Values.ToArray();
            }
            else
                return nodeVarContainer.GetNodeVarsList();
        }

        public static ValueTypeData[] GetValueTypes(Control node)
        {
            return new[] {
                new ValueTypeData() {
                    Name = "int",
                    Type = typeof(int),
                    Icon = node.GetIcon("int", "EditorIcons"),
                    UseIconOnly = true
                },
                new ValueTypeData() {
                    Name = "float",
                    Type = typeof(float),
                    Icon = node.GetIcon("float", "EditorIcons"),
                    UseIconOnly = true
                },
                new ValueTypeData() {
                    Name = "bool",
                    Type = typeof(bool),
                    Icon = node.GetIcon("bool", "EditorIcons"),
                    UseIconOnly = true
                },
                new ValueTypeData() {
                    Name = "string",
                    Type = typeof(string),
                    Icon = node.GetIcon("String", "EditorIcons"),
                    UseIconOnly = true
                },
                new ValueTypeData() {
                    Name = "Vector2",
                    Type = typeof(Vector2),
                    Icon = node.GetIcon("Vector2", "EditorIcons"),
                    UseIconOnly = true
                },
                new ValueTypeData() {
                    Name = "Vector3",
                    Type = typeof(Vector3),
                    Icon = node.GetIcon("Vector3", "EditorIcons"),
                    UseIconOnly = true
                }
            };
        }

        public static OperationTypeData[] GetOperationTypes()
        {
            return new[] {
                new OperationTypeData()
                {
                    Name = "Get/Set",
                    Operation = NodeVarOperation.GetSet
                },
                new OperationTypeData() {
                    Name = "Get/_Set",
                    Operation = NodeVarOperation.GetPrivateSet
                },
                new OperationTypeData() {
                    Name = "Set/_Get",
                    Operation = NodeVarOperation.SetPrivateGet
                },
                new OperationTypeData() {
                    Name = "Get",
                    Operation = NodeVarOperation.Get
                },
                new OperationTypeData() {
                    Name = "Set",
                    Operation = NodeVarOperation.Set
                },
                new OperationTypeData() {
                    Name = "_Get",
                    Operation = NodeVarOperation.PrivateGet
                },
                new OperationTypeData() {
                    Name = "_Set",
                    Operation = NodeVarOperation.PrivateSet
                },
                new OperationTypeData() {
                    Name = "_Get/_Set",
                    Operation = NodeVarOperation.PrivateGetSet
                },
            };
        }
    }
}
