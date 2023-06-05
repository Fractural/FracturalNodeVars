using System;

namespace Fractural.NodeVars
{
    [AttributeUsage(AttributeTargets.Method)]
    public class NodeVarFuncAttribute : Attribute
    {
        public NodeVarFuncAttribute() { }
    }
}