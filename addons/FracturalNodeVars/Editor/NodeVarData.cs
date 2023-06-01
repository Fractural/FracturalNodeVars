using Fractural.Utils;
using Godot;
using System;
using GDC = Godot.Collections;

namespace Fractural.NodeVars
{
    public interface INodeVar
    {
        string Name { get; set; }
    }

    public interface ISetNodeVar : INodeVar
    {
        object Value { set; }
    }

    public interface IGetNodeVar : INodeVar
    {
        object Value { get; }
    }

    public interface IGetSetNodeVar : ISetNodeVar, IGetNodeVar { }

    /// <summary>
    /// Base class for NodeVars. Is used to serialize editor data, as well as hold runtime data.
    /// </summary>
    public abstract class NodeVarData : INodeVar
    {
        public string Name { get; set; }
        public Type ValueType { get; set; }

        public virtual void Ready(Node node) { }
        public override bool Equals(object obj)
        {
            if (obj is NodeVarData data)
                return Equals(data);
            return false;
        }
        public override int GetHashCode() => GetHashCodeForData();

        public abstract GDC.Dictionary ToGDDict();
        public abstract void FromGDDict(GDC.Dictionary dict, string key);
        public abstract NodeVarData WithChanges(NodeVarData newData);
        public abstract NodeVarData Clone();
        public abstract bool Equals(NodeVarData data);
        public abstract int GetHashCodeForData();
    }

    public abstract class NodeVarData<T> : NodeVarData, INodeVar where T : NodeVarData
    {
        public override NodeVarData WithChanges(NodeVarData newData) => WithChanges((T)newData);
        public override NodeVarData Clone() => TypedClone();
        public override bool Equals(NodeVarData data)
        {
            if (data is T newData)
                return Equals(newData);
            return false;
        }

        public abstract T WithChanges(T newData);
        public abstract T TypedClone();
        public abstract bool Equals(T data);
    }
}
