using Godot;
using System;

#if TOOLS
namespace Fractural.NodeVars
{
    // TODO NOW: Finish porting UI code to use new NodeVarStrategy system
    [Tool]
    public abstract class NodeVarStrategyDisplay : HBoxContainer
    {
        public event Action DataChanged;

        public NodeVarData Data { get; protected set; }
        public NodeVarData DefaultData { get; protected set; }
        public NodeVarStrategy Strategy => Data?.Strategy;
        public NodeVarStrategy DefaultStrategy => DefaultData?.Strategy;

        public virtual void SetData(NodeVarData data, NodeVarData defaultData = null)
        {
            Data = data;
            DefaultData = defaultData;
        }

        public virtual void UpdateDisabledAndFixedUI(bool isFixed, bool disabled, bool privateDisabled, bool nonSetDisabled) { }
        protected void InvokeDataChanged() => DataChanged?.Invoke();
    }

    [Tool]
    public abstract class NodeVarStrategyDisplay<T> : NodeVarStrategyDisplay where T : NodeVarStrategy
    {
        public new T Strategy
        {
            get => (T)base.Strategy;
        }
        public new T DefaultStrategy
        {
            get => (T)base.DefaultStrategy;
        }
    }
}
#endif