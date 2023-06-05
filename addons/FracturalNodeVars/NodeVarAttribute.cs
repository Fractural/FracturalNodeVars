using System;

namespace Fractural.NodeVars
{
    /// <summary>
    /// Attribute to mark a property as a DictNodeVar that's settable from the inspector. Used within State nodes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class NodeVarAttribute : Attribute
    {
        public NodeVarOperation? Operation { get; set; }
        public NodeVarAttribute() { }
        public NodeVarAttribute(NodeVarOperation operation)
        {
            Operation = operation;
        }
    }
}