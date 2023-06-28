namespace Fractural.NodeVars
{
    public static class NodeVarsContainerExtensions
    {
        public static void SetNodeVar(this INodeVarContainer container, string key, object value, bool includePrivate)
        {
            if (includePrivate && container is IPrivateNodeVarContainer privateContainer)
                privateContainer.PrivateSetNodeVar(key, value);
            else
                container.SetNodeVar(key, value);
        }

        public static T GetNodeVar<T>(this INodeVarContainer container, string key, bool includePrivate = true)
        {
            if (includePrivate && container is IPrivateNodeVarContainer privateContainer)
                return (T)privateContainer.PrivateGetNodeVar(key);
            return (T)container.GetNodeVar(key);
        }


        public static T PrivateGetNodeVar<T>(this IPrivateNodeVarContainer container, string key) => (T)container.PrivateGetNodeVar(key);
    }
}
