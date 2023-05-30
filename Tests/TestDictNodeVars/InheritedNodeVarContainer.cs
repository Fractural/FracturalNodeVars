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
            set => SetDictNodeVar(nameof(MyFloatVar), value);
        }

        [NodeVar]
        public bool MyBoolVar
        {
            get => GetDictNodeVar<bool>(nameof(MyBoolVar));
            set => SetDictNodeVar(nameof(MyBoolVar), value);
        }

        [NodeVar]
        public Vector3 MySetVar
        {
            set => SetDictNodeVar(nameof(MySetVar), value);
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
            set => SetDictNodeVar(nameof(MyAttributeSetVar), value);
        }

        [NodeVar(NodeVarOperation.Get)]
        public bool MyAttributeGetVar
        {
            get => GetDictNodeVar<bool>(nameof(MyAttributeGetVar));
            set => SetDictNodeVar(nameof(MyAttributeGetVar), value);
        }
    }
}