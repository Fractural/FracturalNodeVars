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
        public static bool CheckNodeVarCompatible(NodeVarData nodeVar, NodeVarOperation operation, Type valueType)
        {
            if (nodeVar.ValueType != valueType) return false;
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

        public static NodeVarData[] GetCompatibleVariables(this INodeVarContainer container, NodeVarOperation operation, Type valueType)
        {
            return container.GetNodeVarsList().Where(nodeVar => CheckNodeVarCompatible(nodeVar, operation, valueType)).ToArray();
        }

        public static NodeVarData NodeVarDataFromGDDict(GDC.Dictionary dict, string key)
        {
            string type = dict.Get<string>("Type");
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
            foreach (var property in objectType.GetProperties())
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
                    bool hasGetter = property.GetGetMethod() != null;
                    bool hasSetter = property.GetSetMethod() != null;

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
    }
}
