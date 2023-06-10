namespace Fractural.NodeVars
{
    public static class NodeVarsContainerExtensions
    {
        public static T GetNodeVar<T>(this INodeVarContainer container, string key) => (T)container.GetNodeVar(key);
    }
}
