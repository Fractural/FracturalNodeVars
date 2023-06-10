using Fractural.NodeVars;
using Godot;
using GDC = Godot.Collections;

namespace Tests
{
    [Tool]
    public class InheritedNodeVarContainer : NodeVarContainer
    {
        [Export]
        public Vector2 SomeVector2 { get; set; }
        [Export]
        public GDC.Dictionary SomeDictionary { get; set; }
        [Export]
        public GDC.Array SomeArray { get; set; }

        [NodeVar]
        public float MyFloatVar
        {
            get => GetDictNodeVar<float>(nameof(MyFloatVar));
            set => SetNodeVar(nameof(MyFloatVar), value);
        }

        [NodeVar]
        public bool MyBoolVar
        {
            get => GetDictNodeVar<bool>(nameof(MyBoolVar));
            set => SetNodeVar(nameof(MyBoolVar), value);
        }

        [NodeVar]
        public Vector3 MySetVar
        {
            set => SetNodeVar(nameof(MySetVar), value);
        }

        [NodeVar]
        public string MyGetVar
        {
            get => GetDictNodeVar<string>(nameof(MyGetVar));
        }

        [NodeVar(NodeVarOperation.Set)]
        public bool MyAttributeSetVar
        {
            get => GetDictNodeVar<bool>(nameof(MyAttributeSetVar));
            set => SetNodeVar(nameof(MyAttributeSetVar), value);
        }

        [NodeVar(NodeVarOperation.Get)]
        public bool MyAttributeGetVar
        {
            get => GetDictNodeVar<bool>(nameof(MyAttributeGetVar));
            set => SetNodeVar(nameof(MyAttributeGetVar), value);
        }

        [NodeVarFunc]
        public int MyPow(int num, int amount)
        {
            return (int)Mathf.Pow(num, amount);
        }
    }
}