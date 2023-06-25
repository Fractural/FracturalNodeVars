using Fractural.Utils;
using Godot;
using System;
using GDC = Godot.Collections;

namespace Fractural.NodeVars
{
    public class PointerNodeVarStrategy : NodeVarStrategy
    {
        public override NodeVarOperation[] ValidOperations => NodeVarOperations.Gettable;

        // Serialized
        public string ContainerVarName { get; set; }
        public NodePath ContainerPath { get; set; }

        // Runtime
        public INodeVarContainer Container { get; set; }

        public override object Value
        {
            get
            {
                if (Container is IPrivateNodeVarContainer privateContainer)
                    return privateContainer.PrivateGetNodeVar(ContainerVarName);
                return Container.GetNodeVar(ContainerVarName);
            }
        }

        public override void Ready(Node node)
        {
            Container = node.GetNode<INodeVarContainer>(ContainerPath);
        }

        public override NodeVarStrategy WithChanges(NodeVarStrategy newData, bool forEditorSerialization = false)
        {
            if (!(newData is PointerNodeVarStrategy strategy)) return null;
            var inheritedData = Clone() as PointerNodeVarStrategy;

            if (!Equals(strategy.ContainerPath, ContainerPath))
                inheritedData.ContainerPath = strategy.ContainerPath;
            if (!Equals(strategy.ContainerVarName, ContainerVarName))
                inheritedData.ContainerVarName = strategy.ContainerVarName;
            return inheritedData;
        }

        public override NodeVarStrategy Clone()
        {
            return new PointerNodeVarStrategy()
            {
                ContainerPath = ContainerPath,
                ContainerVarName = ContainerVarName
            };
        }

        public override bool Equals(object other)
        {
            return other is PointerNodeVarStrategy strategy &&
                strategy.ContainerPath == ContainerPath &&
                strategy.ContainerVarName == ContainerVarName;
        }

        public override int GetHashCode() => GeneralUtils.CombineHashCodes(ContainerPath, ContainerVarName);

        public override GDC.Dictionary ToGDDict()
        {
            var dict = base.ToGDDict();
            dict[nameof(ContainerPath)] = ContainerPath;
            dict[nameof(ContainerVarName)] = ContainerVarName;
            return dict;
        }

        public override void FromGDDict(GDC.Dictionary dict)
        {
            ContainerPath = dict.Get<NodePath>(nameof(ContainerPath), null);
            ContainerVarName = dict.Get<string>(nameof(ContainerVarName), null);
        }
    }
}
