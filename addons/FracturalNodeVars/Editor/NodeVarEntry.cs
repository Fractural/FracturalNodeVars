using Godot;
using System;
using GDC = Godot.Collections;

#if TOOLS
namespace Fractural.NodeVars
{
    [Tool]
    public abstract class NodeVarEntry : HBoxContainer
    {
        /// <summary>
        /// NameChanged(string oldName, Entry entry)
        /// </summary>
        public event Action<string, NodeVarEntry> NameChanged;
        /// <summary>
        /// DataChanged(string name, NodeVarData newValue)
        /// </summary>
        public event Action<string, NodeVarData> DataChanged;
        /// <summary>
        /// Deleted(string name)
        /// </summary>
        public event Action<string> Deleted;

        public NodeVarData Data { get; set; }
        public NodeVarData DefaultData { get; set; }

        public abstract void SetFixed(bool isFixed);
        public abstract void ResetName(string oldKey);
        protected virtual void InvokeDeleted() => Deleted?.Invoke(Data.Name);
        protected virtual void InvokeDataChanged() => DataChanged?.Invoke(Data.Name, Data);
        protected virtual void InvokeNameChanged(string oldName) => NameChanged?.Invoke(oldName, this);
        public abstract void SetData(NodeVarData value, NodeVarData defaultData = null);
    }

    public abstract class NodeVarEntry<T> : NodeVarEntry where T : NodeVarData
    {
        public new T Data
        {
            get => (T)base.Data;
            set => base.Data = value;
        }
        public new T DefaultData
        {
            get => (T)base.DefaultData;
            set => base.DefaultData = value;
        }

        public override void SetData(NodeVarData value, NodeVarData defaultData = null) => this.SetData(value, defaultData);
        public abstract void SetData(T value, T defaultData = null);
    }
}
#endif