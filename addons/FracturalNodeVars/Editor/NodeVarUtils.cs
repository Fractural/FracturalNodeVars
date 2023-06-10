using System;
using System.Reflection;
using System.Collections.Generic;
using Fractural.Utils;
using Godot;
using GDC = Godot.Collections;
using System.Linq;

namespace Fractural.NodeVars
{
    public static class NodeVarUtils
    {
        public static bool CheckNodeVarCompatible(NodeVarData nodeVar, NodeVarOperation operation, Type valueType = null)
        {
            if (valueType != null && nodeVar is ITypedNodeVar typedNodeVar && typedNodeVar.ValueType != valueType) return false;
            NodeVarOperation nodeVarOperation = NodeVarOperation.Get;
            if (nodeVar is ISetNodeVar)
                nodeVarOperation = NodeVarOperation.Set;
            if (nodeVar is IGetSetNodeVar)
                nodeVarOperation = NodeVarOperation.GetSet;
            if (nodeVar is DynamicNodeVarData dynamicData)
                nodeVarOperation = dynamicData.Operation;

            if (nodeVarOperation == operation
                || (operation == NodeVarOperation.Get && nodeVarOperation == NodeVarOperation.GetSet)
                || (operation == NodeVarOperation.Set && nodeVarOperation == NodeVarOperation.GetSet)
                ) return true;
            return false;
        }

        public static NodeVarData NodeVarDataFromGDDict(GDC.Dictionary dict, string key)
        {
            string type = dict.Get<string>("Type", nameof(DynamicNodeVarData));
            NodeVarData result;
            switch (type)
            {
                case nameof(DynamicNodeVarData):
                    result = new DynamicNodeVarData();
                    result.FromGDDict(dict, key);
                    break;
                case nameof(ExpressionNodeVarData):
                    result = new ExpressionNodeVarData();
                    result.FromGDDict(dict, key);
                    break;
                default:
                    throw new Exception($"{nameof(NodeVarData)}: Cannot convert type \"{type}\" to NodeVarData from GDDict.");
            }
            return result;
        }

        /// <summary>
        /// Returns all fixed node vars for a given type, with each node var
        /// given a default value.
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public static DynamicNodeVarData[] GetNodeVarsFromAttributes(Type objectType)
        {
            var fixedDictNodeVars = new List<DynamicNodeVarData>();
            foreach (var property in objectType.GetProperties(BindingFlags.Public).Union(objectType.GetProperties(BindingFlags.NonPublic)))
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
                        operation = NodeVarOperation.Set;
                    else
                        // If the property has a setter, then it means other users can get it's value
                        operation = NodeVarOperation.Get;
                }

                fixedDictNodeVars.Add(new DynamicNodeVarData()
                {
                    Name = property.Name,
                    ValueType = property.PropertyType,
                    Operation = operation,
                    InitialValue = DefaultValueUtils.GetDefault(property.PropertyType)
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
        public static DynamicNodeVarData[] GetNodeVarsFromAttributes<T>() => GetNodeVarsFromAttributes(typeof(T));

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
            if (nodeVarContainer is NodeVarContainer container)
            {
                var nodeVars = container.RawNodeVarsGDDict;
                var nodeVarsDict = new Dictionary<string, NodeVarData>();

                foreach (string key in nodeVars.Keys)
                {
                    var nodeVar = NodeVarDataFromGDDict(nodeVars.Get<GDC.Dictionary>(key), key);
                    nodeVarsDict.Add(key, nodeVar);
                }

                var defaultNodeVars = new Dictionary<string, NodeVarData>();
                if (container.Filename != "")
                {
                    var defaultInheritedNodeVars = packedSceneDefaultValuesRegistry.GetDefaultValue<GDC.Dictionary>(container.Filename, "_nodeVars");
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
                        if (nodeVarWithChanges == null)
                        {
                            GD.PushWarning($"{nameof(NodeVarContainer)}: NodeVar of name \"{defaultNodeVar.Name}\" could not be merged with its default value, therefore reverting back to default.");
                            nodeVarsDict[defaultNodeVar.Name] = defaultNodeVar;
                        }
                        else
                            nodeVarsDict[defaultNodeVar.Name] = nodeVarWithChanges;
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
                    Name = "Get",
                    Operation = NodeVarOperation.Get
                },
                new OperationTypeData() {
                    Name = "Set",
                    Operation = NodeVarOperation.Set
                },
            };
        }
    }
}
