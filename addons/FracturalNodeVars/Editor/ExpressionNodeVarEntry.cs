using Godot;
using System;

#if TOOLS
namespace Fractural.NodeVars
{
    // TODO: Finish this
    [Tool]
    public class ExpressionNodeVarEntry : NodeVarEntry<ExpressionNodeVarData>
    {
        public override void ResetName(string oldKey)
        {
            throw new NotImplementedException();
        }

        public override void SetData(ExpressionNodeVarData value, ExpressionNodeVarData defaultData = null)
        {
            throw new NotImplementedException();
        }

        public override void SetFixed(bool isFixed)
        {
            throw new NotImplementedException();
        }
    }
}
#endif