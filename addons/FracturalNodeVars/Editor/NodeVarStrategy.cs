using Godot;
using System;
using GDC = Godot.Collections;

namespace Fractural.NodeVars
{
    public abstract class NodeVarStrategy
    {
        public virtual object Value
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public virtual void Ready(Node node) { }
        public abstract NodeVarOperation[] ValidOperations { get; }
        public abstract NodeVarStrategy WithChanges(NodeVarStrategy other, bool forEditorSerialization = false);
        public abstract NodeVarStrategy Clone();
        public virtual GDC.Dictionary ToGDDict()
        {
            return new GDC.Dictionary()
            {
                { "Type", GetType().Name }
            };
        }
        public abstract void FromGDDict(GDC.Dictionary dictionary);
    }
}
