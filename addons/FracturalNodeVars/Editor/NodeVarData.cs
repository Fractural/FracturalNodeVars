using Fractural.Utils;
using Godot;
using System;
using GDC = Godot.Collections;

namespace Fractural.NodeVars
{
    public abstract class NodeVarStrategy
    {
        public abstract NodeVarOperation[] ValidOperations { get; }
        public abstract NodeVarStrategy WithChanges(NodeVarStrategy other, bool forEditorSerialization = false);

    }

    public class NodeVarData
    {
        public string Name { get; set; }
        public NodeVarOperation Operation { get; set; }
        public NodeVarStrategy Strategy { get; set; }

        public virtual void Ready(Node node) { }
        public override bool Equals(object obj)
        {
            if (obj is NodeVarData data)
                return Equals(data);
            return false;
        }
        public override int GetHashCode() => GetHashCodeForData();

        /// <summary>
        /// Attempts to use another NodeVar's data to make changes to this NodeVar.
        /// </summary>
        /// <param name="other">Data to use as changed</param>
        /// <param name="forEditorSerialization">Is the returned data for editor use?</param>
        /// <returns>Returns the resulting NodeVar with the changes on success. Returns null if the two NodeVars are incompatible.</returns>
        public NodeVarData WithChanges(NodeVarData other, bool forEditorSerialization = false);
        public virtual GDC.Dictionary ToGDDict()
        {
            var dict = new GDC.Dictionary()
            {
                { "Type", GetType().Name },
                { nameof(Operation), (int)Operation },
            };
            return dict;
        }
        public virtual void FromGDDict(GDC.Dictionary dict, string name)
        {
            Operation = (NodeVarOperation)dict.Get<int>(nameof(Operation));
            Name = name;
        }
        public abstract NodeVarData Clone();
        public abstract bool Equals(NodeVarData data);
        public abstract int GetHashCodeForData();
    }

    public abstract class NodeVarData<T> : NodeVarData, INodeVar where T : NodeVarData
    {
        public override NodeVarData Clone() => TypedClone();
        public override bool Equals(NodeVarData data)
        {
            if (data is T newData)
                return Equals(newData);
            return false;
        }
        public override NodeVarData WithChanges(NodeVarData other, bool forEditorSerialization = false) => WithChanges((T)other, forEditorSerialization);
        public abstract T WithChanges(T other, bool forEditorSerialization = false);
        public abstract T TypedClone();
        public abstract bool Equals(T data);
    }
}
