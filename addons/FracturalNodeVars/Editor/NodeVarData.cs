using Godot;
using System;
using GDC = Godot.Collections;

namespace Fractural.NodeVars
{
    public interface ISerializableNodeVar
    {
        object Save();
        void Load(object any);
    }

    public interface IResetNodeVar
    {
        void Reset();
    }

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

    public interface ITypedNodeVar
    {
        Type ValueType { get; set; }
    }

    /// <summary>
    /// Base class for NodeVars. Is used to serialize editor data, as well as hold runtime data.
    /// </summary>
    public abstract class NodeVarData : INodeVar
    {
        public string Name { get; set; }

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
        public abstract NodeVarData WithChanges(NodeVarData other, bool forEditorSerialization = false);
        public abstract GDC.Dictionary ToGDDict();
        public abstract void FromGDDict(GDC.Dictionary dict, string key);
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
