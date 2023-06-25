using Fractural.NodeVars;
using Godot;
using GDC = Godot.Collections;

namespace Tests
{
    [Tool]
    public class CustomNodeVarContainer : NodeVarContainer
    {
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
        public Vector3 MyGetVar
        {
            set => SetNodeVar(nameof(MyGetVar), value);
        }

        [NodeVar]
        public string MySetVar
        {
            get => GetDictNodeVar<string>(nameof(MySetVar));
        }

        [NodeVar(NodeVarOperation.SetPrivateGet)]
        public bool MyAttributeSetVar
        {
            get => GetDictNodeVar<bool>(nameof(MyAttributeSetVar));
            set => SetNodeVar(nameof(MyAttributeSetVar), value);
        }

        [NodeVar(NodeVarOperation.GetPrivateSet)]
        public bool MyAttributeGetVar
        {
            get => GetDictNodeVar<bool>(nameof(MyAttributeGetVar));
            set => SetNodeVar(nameof(MyAttributeGetVar), value);
        }
    }
}