#if TOOLS
namespace Fractural.NodeVars
{
    /// <summary>
    /// The operation that users of the DictNodeVar can perform on a given DictNodeVar.
    /// </summary>
    public enum NodeVarOperation
    {
        /// <summary>
        /// public get; public set;
        /// </summary>
        GetSet,
        /// <summary>
        /// public get;
        /// </summary>
        Get,
        /// <summary>
        /// public set;
        /// </summary>
        Set,
        /// <summary>
        /// public get; private set;
        /// </summary>
        GetPrivateSet,
        /// <summary>
        /// private get; public set;
        /// </summary>
        SetPrivateGet,
        /// <summary>
        /// private get;
        /// </summary>
        PrivateGet,
        /// <summary>
        /// private set;
        /// </summary>
        PrivateSet,
        /// <summary>
        /// private get; set;
        /// </summary>
        PrivateGetSet,
    }
}
#endif