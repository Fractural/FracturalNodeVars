using System;
using System.Collections.Generic;
using System.Linq;
using Fractural.Plugin;
using Fractural.Plugin.AssetsRegistry;
using Fractural.Utils;
using Godot;
using GDC = Godot.Collections;

#if TOOLS
namespace Fractural.NodeVars
{
    /// <summary>
    /// The operation that users of the DictNodeVar can perform on a given DictNodeVar.
    /// </summary>
    public enum NodeVarOperation
    {
        /// <summary>
        /// DictNodeVar can be fetched from the outside
        /// </summary>
        Get,
        /// <summary>
        /// DictNodeVar can be set from the outside
        /// </summary>
        Set,
        /// <summary>
        /// DictNodeVar can get fetched and set from the outside
        /// </summary>
        GetSet
    }

    [Tool]
    public class DictNodeVarsValueProperty : ValueProperty<GDC.Dictionary>
    {
        private class CSharpData
        {
            public DictNodeVarsValueProperty _source;
            public CSharpData(DictNodeVarsValueProperty source)
            {
                _source = source;
            }

            public INodeVarContainer _propagationSource;
            public Button _editButton;
            public Control _container;
            public Button _addElementButton;
            public OptionButton _addOptionButton;
            public VBoxContainer _nodeVarEntriesVBox;
            public Node _sceneRoot;
            public Node _relativeToNode;
            public Dictionary<string, NodeVarData> _fixedNodeVarsDict;
            public PackedSceneDefaultValuesRegistry _defaultValuesRegistry;

            public string EditButtonText => $"DictNodeVars [{_source.Value.Count}]";
            public bool HasFixedNodeVars => _fixedNodeVarsDict != null;
            public bool _canAddNewVars;
            public IAssetsRegistry _assetsRegistry;
            public Control _currentFocused;
        }

        private enum AddOptionIndex
        {
            Dynamic = 0,
            Expression = 1
        }

        private CSharpData _data;

        public DictNodeVarsValueProperty() { }
        public DictNodeVarsValueProperty(
            INodeVarContainer propagationSource,
            IAssetsRegistry assetsRegistry,
            PackedSceneDefaultValuesRegistry defaultValuesRegistry,
            Node sceneRoot,
            Node relativeToNode,
            NodeVarData[] fixedNodeVars = null,
            bool canAddNewVars = true
        ) : base()
        {
            _data = new CSharpData(this);
            _data._propagationSource = propagationSource;
            _data._assetsRegistry = assetsRegistry;
            _data._defaultValuesRegistry = defaultValuesRegistry;
            _data._sceneRoot = sceneRoot;
            _data._relativeToNode = relativeToNode;
            if (fixedNodeVars != null && fixedNodeVars.Length > 0)
            {
                // fixedNodeVars are NodeVars that should be shown, but not actually saved unless they've changed.
                // This can include
                // - Fixed node vars values inherited from the PackedScene that the node was an instance of
                // - NodeVar attributes on some of the Node's properties
                _data._fixedNodeVarsDict = new Dictionary<string, NodeVarData>();
                foreach (var nodeVar in fixedNodeVars)
                    _data._fixedNodeVarsDict[nodeVar.Name] = nodeVar;
            }
            _data._canAddNewVars = canAddNewVars;

            _data._editButton = new Button();
            _data._editButton.ToggleMode = true;
            _data._editButton.ClipText = true;
            _data._editButton.Connect("toggled", this, nameof(OnEditToggled));
            AddChild(_data._editButton);

            _data._addElementButton = new Button();
            _data._addElementButton.Text = "Add NodeVar";
            _data._addElementButton.Connect("pressed", this, nameof(OnAddElementPressed));
            _data._addElementButton.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            _data._addElementButton.RectMinSize = new Vector2(24 * 4, 0);
            _data._addElementButton.Visible = _data._canAddNewVars;
            _data._addElementButton.ClipText = true;

            _data._addOptionButton = new OptionButton();
            _data._addOptionButton.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            _data._addOptionButton.AddItem("Dynamic", (int)AddOptionIndex.Dynamic);
            _data._addOptionButton.AddItem("Expression", (int)AddOptionIndex.Expression);
            _data._addOptionButton.Select((int)AddOptionIndex.Dynamic);
            _data._addOptionButton.ClipText = true;
            _data._addOptionButton.Visible = _data._canAddNewVars;

            var hbox = new HBoxContainer();
            hbox.AddChild(_data._addElementButton);
            hbox.AddChild(_data._addOptionButton);

            _data._nodeVarEntriesVBox = new VBoxContainer();

            var vbox = new VBoxContainer();
            vbox.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            vbox.AddChild(hbox);
            vbox.AddChild(_data._nodeVarEntriesVBox);

            _data._container = vbox;
            AddChild(_data._container);
        }

        public override void _Ready()
        {
#if TOOLS
            if (NodeUtils.IsInEditorSceneTab(this))
                return;
#endif
            _data._addElementButton.Icon = GetIcon("Add", "EditorIcons");
            GetViewport().Connect("gui_focus_changed", this, nameof(OnFocusChanged));
        }

        public override void _Process(float delta)
        {
            // We need SetBottomEditor to run here because it won't work in _data._Ready due to
            // the tree being busy setting up nodes.
            SetBottomEditor(_data._container);
            SetProcess(false);
        }

        protected override void OnDisabled(bool disabled)
        {
            _data._addElementButton.Disabled = disabled;
            _data._addOptionButton.Disabled = disabled;
            foreach (NodeVarEntry entry in _data._nodeVarEntriesVBox.GetChildren())
                entry.Disabled = disabled;
        }

        private void OnFocusChanged(Control control) => _data._currentFocused = control;

        private bool CheckValueSameAsFixed()
        {
            if (Value.Count == 0)
                return true;
            if (_data._fixedNodeVarsDict == null)
                return false;
            if (Value.Count > _data._fixedNodeVarsDict.Count)
                return false;
            foreach (string key in Value.Keys)
            {
                var itemNodeVar = NodeVarUtils.NodeVarDataFromGDDict(Value.Get<GDC.Dictionary>(key), key);
                if (!_data._fixedNodeVarsDict.TryGetValue(key, out NodeVarData fixedNodeVar))
                    return false;
                if (!itemNodeVar.Equals(fixedNodeVar))
                    return false;
            }
            return true;
        }

        public override void UpdateProperty()
        {
            if (Value == null || (Value.Count > 0 && CheckValueSameAsFixed()))
                Value = new GDC.Dictionary();

            _data._container.Visible = this.GetMeta<bool>("visible", true);   // Fixed to being visible if the meta tag doesn't exist.
            _data._editButton.Pressed = _data._container.Visible;
            _data._editButton.Text = _data.EditButtonText;

            var displayedNodeVars = new Dictionary<string, NodeVarData>();
            foreach (string key in Value.Keys)
                displayedNodeVars.Add(key, NodeVarUtils.NodeVarDataFromGDDict(Value.Get<GDC.Dictionary>(key), key));

            if (_data.HasFixedNodeVars)
            {
                // Add any fixed vars that are missing
                foreach (var fixedNodeVar in _data._fixedNodeVarsDict.Values)
                {
                    var displayNodeVar = fixedNodeVar;
                    if (displayedNodeVars.TryGetValue(fixedNodeVar.Name, out NodeVarData existingNodeVar))
                    {
                        var nodeVarWithChanges = fixedNodeVar.WithChanges(existingNodeVar, true);
                        if (nodeVarWithChanges != null)
                            displayNodeVar = nodeVarWithChanges; // Changes were compatible
                        else
                            Value.Remove(existingNodeVar.Name); // Changes were not compatible, so NodeVarWithChanges was null
                    }
                    displayedNodeVars[fixedNodeVar.Name] = displayNodeVar;
                }
                if (!_data._canAddNewVars)
                {
                    // If we cannot add new vars, then we can only show fixed vars, so we delete all other vars.
                    foreach (string key in Value.Keys)
                    {
                        if (_data._fixedNodeVarsDict.ContainsKey(key))
                            continue;
                        var entry = NodeVarUtils.NodeVarDataFromGDDict(Value.Get<GDC.Dictionary>(key), key);
                        // _data._fixedDictNodeVars doesn't contain an entry in Value dict, so we remove it from Value dict
                        Value.Remove(key);
                    }
                }
            }

            var sortedDisplayNodeVars = new List<NodeVarData>(displayedNodeVars.Values);
            sortedDisplayNodeVars.Sort((a, b) =>
            {
                if (_data._fixedNodeVarsDict != null)
                {
                    // Sort by whether it's fixed, and then by alphabetical order
                    int fixedOrdering = _data._fixedNodeVarsDict.ContainsKey(b.Name).CompareTo(_data._fixedNodeVarsDict.ContainsKey(a.Name));
                    if (fixedOrdering == 0)
                        return a.Name.CompareTo(b.Name);
                    return fixedOrdering;
                }
                return a.Name.CompareTo(b.Name);
            });

            // Move the current focused entry into it's Value dict index inside the entries vBox.
            // We don't want to just overwrite the current focused entry since that would
            // cause the user to retain gui focus on the wrong entry.
            var currFocusedEntry = _data._currentFocused?.GetAncestor<NodeVarEntry>();
            if (currFocusedEntry != null)
            {
                // Find the new index of the current focused entry within the Value dictionary.
                int keyIndex = sortedDisplayNodeVars.FindIndex(x => x.Name == currFocusedEntry.Data.Name);
                if (keyIndex < 0)
                {
                    // Set current focused entry back to null. We couldn't
                    // find the current focused entry in the new dictionary, meaning
                    // this entry must have been deleted, therefore we don't care about it
                    // anymore.
                    currFocusedEntry = null;
                }
                else
                {
                    // Swap the entry that's currently in the focused entry's place with the focused entry.
                    var targetEntry = _data._nodeVarEntriesVBox.GetChild<NodeVarEntry>(keyIndex);
                    _data._nodeVarEntriesVBox.SwapChildren(targetEntry, currFocusedEntry);
                }
            }

            // Set the data of each entry with the corresponding values from the Value dictionary
            int index = 0;
            int childCount = _data._nodeVarEntriesVBox.GetChildCount();
            foreach (NodeVarData nodeVar in sortedDisplayNodeVars)
            {
                NodeVarEntry entry;
                if (index >= childCount)
                    entry = CreateNewEntry(nodeVar);
                else
                    entry = _data._nodeVarEntriesVBox.GetChild<NodeVarEntry>(index);
                if (!CanEntryHandleNodeVar(entry, nodeVar))
                {
                    // If the Entry can't handle the NodeVar (because they are different types)
                    // then we free the entry and replace it with the correct one.
                    _data._nodeVarEntriesVBox.RemoveChild(entry);
                    entry.QueueFree();
                    entry = CreateNewEntry(nodeVar);
                    _data._nodeVarEntriesVBox.MoveChild(entry, index);
                }
                if (currFocusedEntry == null || entry != currFocusedEntry)
                {
                    entry.SetData(nodeVar, _data._fixedNodeVarsDict?.GetValue(nodeVar.Name, null));
                }
                entry.IsFixed = _data.HasFixedNodeVars && _data._fixedNodeVarsDict.ContainsKey(nodeVar.Name);
                index++;
            }

            // Free extra entries
            if (index < childCount)
            {
                for (int i = childCount - 1; i >= index; i--)
                {
                    var entry = _data._nodeVarEntriesVBox.GetChild<NodeVarEntry>(i);
                    entry.NameChanged -= OnEntryNameChanged;
                    entry.DataChanged -= OnEntryDataChanged;
                    entry.Deleted -= OnEntryDeleted;
                    entry.QueueFree();
                }
            }

            _data._addElementButton.Disabled = !_data._canAddNewVars || CheckAllVarNamesTaken();
        }

        private bool CheckAllVarNamesTaken()
        {
            var nextKey = GetNextVarName();
            return Value.Contains(nextKey) || (_data._fixedNodeVarsDict?.ContainsKey(nextKey) ?? false);
        }

        private string GetNextVarName()
        {
            IEnumerable<string> keys = Value.Keys.Cast<string>();
            if (_data.HasFixedNodeVars)
                keys = keys.Union(_data._fixedNodeVarsDict.Keys);
            return NodeVarUtils.GetNextVarName(keys);
        }

        private new ValueProperty CreateValueProperty(Type type)
        {
            var property = ValueProperty.CreateValueProperty(type);
            if (type == typeof(NodePath) && property is NodePathValueProperty valueProperty)
            {
                valueProperty.SelectRootNode = _data._sceneRoot;
                valueProperty.RelativeToNode = _data._relativeToNode;
            }
            return property;
        }

        private bool CanEntryHandleNodeVar(NodeVarEntry entry, NodeVarData nodeVarData)
        {
            if (nodeVarData is DynamicNodeVarData && entry is DynamicNodeVarEntry)
                return true;
            if (nodeVarData is ExpressionNodeVarData && entry is ExpressionNodeVarEntry)
                return true;
            return false;
        }

        private NodeVarEntry CreateNewEntry(NodeVarData nodeVar)
        {
            NodeVarEntry entry;
            if (nodeVar is DynamicNodeVarData)
                entry = new DynamicNodeVarEntry(_data._propagationSource, _data._assetsRegistry, _data._defaultValuesRegistry, _data._sceneRoot, _data._relativeToNode);
            else if (nodeVar is ExpressionNodeVarData)
                entry = new ExpressionNodeVarEntry(_data._propagationSource, _data._assetsRegistry, _data._defaultValuesRegistry, _data._sceneRoot, _data._relativeToNode);
            else
                throw new Exception($"{nameof(DictNodeVarsValueProperty)}: No suitable entry type foudn for {nodeVar.GetType()}.");

            entry.NameChanged += OnEntryNameChanged;
            entry.DataChanged += OnEntryDataChanged;
            entry.Deleted += OnEntryDeleted;
            _data._nodeVarEntriesVBox.AddChild(entry);
            return entry;
        }

        private void OnEntryNameChanged(string oldKey, NodeVarEntry entry)
        {
            var newKey = entry.Data.Name;
            if (Value.Contains(newKey))
            {
                // Reject change since the newKey already exists
                entry.ResetName(oldKey);
                return;
            }
            var currValue = Value[oldKey];
            Value.Remove(oldKey);
            Value[newKey] = currValue;
            InvokeValueChanged(Value);
        }

        private void OnEntryDataChanged(string key, NodeVarData newValue, bool isDefault)
        {
            // Remove entry if it is the same as the default value (no point in storing redundant information)
            if (isDefault)
                Value.Remove(key);
            else
                Value[key] = newValue.ToGDDict();
            InvokeValueChanged(Value);
        }

        private void OnEntryDeleted(string key)
        {
            Value.Remove(key);
            InvokeValueChanged(Value);
        }

        private void OnAddElementPressed()
        {
            // The adding is done in UpdateProperty
            // Note the edited a field in Value doesn't invoke ValueChanged, so we must do it manually
            //
            // Use fixed types for the newly added element
            var nextKey = GetNextVarName();
            switch ((AddOptionIndex)_data._addOptionButton.Selected)
            {
                case AddOptionIndex.Dynamic:
                    Value[nextKey] = new DynamicNodeVarData()
                    {
                        Name = nextKey,
                        ValueType = typeof(int),
                        InitialValue = DefaultValueUtils.GetDefault<int>()
                    }.ToGDDict();
                    break;
                case AddOptionIndex.Expression:
                    Value[nextKey] = new ExpressionNodeVarData()
                    {
                        Name = nextKey,
                    }.ToGDDict();
                    break;
                default:
                    throw new Exception($"{nameof(DictNodeVarsValueProperty)}: Could not handle adding NodeVar with option \"{_data._addOptionButton.Selected}\".");
            }
            InvokeValueChanged(Value);
        }

        private void OnEditToggled(bool toggled)
        {
            SetMeta("visible", toggled);
            _data._container.Visible = toggled;
        }
    }
}
#endif