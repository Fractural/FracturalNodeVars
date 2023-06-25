using Fractural.Commons;
using Fractural.DependencyInjection;
using Fractural.Utils;
using Godot;
using System;
using System.Collections.Generic;
using GDC = Godot.Collections;

namespace Fractural.NodeVars
{
    [RegisteredType(nameof(NodeVarContainer3D), "res://addons/FracturalNodeVars/Assets/dependency-container-3d.svg", nameof(Spatial))]
    [Tool]
    public class NodeVarContainer3D : Spatial, IDictNodeVarContainer, IInjectDIContainer, ISerializationListener, IPrivateNodeVarContainer
    {
        // Native C# Dictionary is around x9 faster than Godot Dictionary
        public IDictionary<string, NodeVarData> NodeVars { get; private set; }

        protected GDC.Dictionary _nodeVars;
        private HintString.DictNodeVarsMode _mode = HintString.DictNodeVarsMode.LocalAttributes;
        [Export]
        public virtual HintString.DictNodeVarsMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                PropertyListChangedNotify();
            }
        }
        public GDC.Dictionary RawNodeVarsGDDict => _nodeVars;
        protected PackedSceneDefaultValuesRegistry _packedSceneDefaultValuesRegistry;

        public void Construct(DIContainer container)
        {
            _packedSceneDefaultValuesRegistry = container.Resolve<PackedSceneDefaultValuesRegistry>();
        }

        public override void _Ready()
        {
#if TOOLS
            if (NodeUtils.IsInEditorSceneTab(this))
                return;
#endif
            NodeVars = new Dictionary<string, NodeVarData>();
            foreach (var nodeVar in GetNodeVarsList())
                AddNodeVar(nodeVar);
        }

        /// <summary>
        /// Adds a new NodeVar to the container. This is used at runtime.
        /// </summary>
        /// <param name="nodeVar"></param>
        public void AddNodeVar(NodeVarData nodeVar)
        {
            nodeVar.Ready(this);
            NodeVars.Add(nodeVar.Name, nodeVar);
        }

        /// <summary>
        /// Gets a NodeVar value at runtime. Does nothing when called from the editor.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T GetDictNodeVar<T>(string key) => (T)GetNodeVar(key);

        /// <summary>
        /// Gets a NodeVar value at runtime. Only works if the NodeVar has a public get accesor. 
        /// Does nothing when called from the editor.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object GetNodeVar(string key)
        {
            var data = NodeVars[key];
            if (data.Operation.IsGet())
                return data.GetValue();
            throw new Exception($"{nameof(NodeVarContainer)}: Could not get NodeVar of \"{key}\".");
        }

        /// <summary>
        /// Sets a NodeVar value at runtime. Only works if the NodeVar has a public set accesor. 
        /// Does nothing when called from the editor.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetNodeVar(string key, object value)
        {
            var data = NodeVars[key];
            if (data.Operation.IsSet())
            {
                data.SetValue(value);
                return;
            }
            throw new Exception($"{nameof(NodeVarContainer)}: Could not set NodeVar of \"{key}\".");
        }

        /// <summary>
        /// Gets a NodeVar value at runtime.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object PrivateGetNodeVar(string key)
        {
            var data = NodeVars[key];
            if (data.Operation.IsGet(true))
                return data.GetValue(true);
            throw new Exception($"{nameof(NodeVarContainer)}: Could not private get NodeVar of \"{key}\".");
        }

        /// <summary>
        /// Sets a NodeVar value at runtime.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public void PrivateSetNodeVar(string key, object value)
        {
            var data = NodeVars[key];
            if (data.Operation.IsSet(true))
            {
                data.SetValue(value, true);
                return;
            }
            throw new Exception($"{nameof(NodeVarContainer)}: Could not private get NodeVar of \"{key}\".");
        }

        /// <summary>
        /// Gets a list of all DictNodeVars for this <see cref="INodeVarContainer"/>
        /// </summary>
        /// <returns></returns>
        public NodeVarData[] GetNodeVarsList() => NodeVarUtils.GetNodeVarsList(this, _packedSceneDefaultValuesRegistry);

        public override GDC.Array _GetPropertyList()
        {
            var builder = new PropertyListBuilder();
            builder.AddDictNodeVarsProp(
                name: nameof(_nodeVars),
                mode: Mode
            );
            return builder.Build();
        }

        public void OnBeforeSerialize()
        {
            NodeVars = null;
        }

        public void OnAfterDeserialize() { }
    }
}
