using Fractural.NodeVars;
using Godot;

namespace Tests
{
    [Tool]
    public class FunctionCallNodeVarContainer : NodeVarContainer
    {
        [NodeVarFunc]
        public int MyPow(int num, int amount)
        {
            return (int)Mathf.Pow(num, amount);
        }
    }
}