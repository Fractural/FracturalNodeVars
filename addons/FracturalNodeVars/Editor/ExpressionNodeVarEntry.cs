using Fractural.Plugin;
using Fractural.Plugin.AssetsRegistry;
using Godot;
using System.Collections.Generic;
using static Fractural.NodeVars.ExpressionNodeVarData;

#if TOOLS
namespace Fractural.NodeVars
{
    // TODO: Add meta based collapsing
    [Tool]
    public class ExpressionNodeVarEntry : NodeVarEntry<ExpressionNodeVarData>
    {
        private StringValueProperty _expressionProperty;
        private StringValueProperty _nameProperty;
        private VBoxContainer _referenceEntriesVBox;
        private Button _addElementButton;

        private IDictionary<string, NodeVarReference> _fixedNodeVarsDict => DefaultData?.NodeVarReferences;
        private bool HasFixedNodeVars => DefaultData != null;

        public ExpressionNodeVarEntry() { }
        public ExpressionNodeVarEntry(IAssetsRegistry assetsRegistry, Node sceneRoot, Node relativeToNode) : base()
        {
            _nameProperty = new StringValueProperty();
            _expressionProperty = new StringValueProperty();
            _referenceEntriesVBox = new VBoxContainer();

            _addElementButton = new Button();
            _addElementButton.Text = "Add NodeVar Reference";
            _addElementButton.Connect("pressed", this, nameof(OnAddElementPressed));
            _addElementButton.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            _addElementButton.RectMinSize = new Vector2(24 * 4, 0);

            var hbox = new HBoxContainer();
            hbox.AddChild(_nameProperty);
            hbox.AddChild(_deleteButton);

            var vbox = new VBoxContainer();
            vbox.AddChild(hbox);
            vbox.AddChild(_addElementButton);
            vbox.AddChild(_expressionProperty);
            vbox.AddChild(_referenceEntriesVBox);

            AddChild(vbox);
        }

        public override void ResetName(string oldName)
        {
            Data.Name = oldName;
            _nameProperty.SetValue(oldName, false);
        }

        public override void SetData(ExpressionNodeVarData value, ExpressionNodeVarData defaultData = null)
        {
            base.SetData(value, defaultData);

            // TODO: FInish this
            var displayedNodeVars = new Dictionary<string, NodeVarReference>();
            foreach (string key in Value.Keys)
            {

                displayedNodeVars.Add(key, NodeVarUtils.NodeVarDataFromGDDict(Value.Get<GDC.Dictionary>(key), key));
            }

            if (HasFixedNodeVars)
            {
                // Popupulate Value with any _fixedNodeVars that it is missing
                foreach (var fixedNodeVar in _fixedNodeVarsDict.Values)
                {
                    var displayNodeVar = fixedNodeVar;
                    if (displayedNodeVars.TryGetValue(fixedNodeVar.Name, out NodeVarData existingNodeVar))
                    {
                        if (existingNodeVar.ValueType == fixedNodeVar.ValueType)
                            displayNodeVar = fixedNodeVar.WithChanges(existingNodeVar);
                        else
                            // If the exiting entry's type is different from the fixed entry, then we must purge
                            // the existing entry to ensure the saved entries are always consistent with the fixed entries
                            Value.Remove(existingNodeVar.Name);
                    }
                    displayedNodeVars[fixedNodeVar.Name] = displayNodeVar;
                }
            }

            var sortedDisplayNodeVars = new List<NodeVarData>(displayedNodeVars.Values);
            sortedDisplayNodeVars.Sort((a, b) =>
            {
                if (_fixedNodeVarsDict != null)
                {
                    // Sort by whether it's fixed, and then by alphabetical order
                    int fixedOrdering = _fixedNodeVarsDict.ContainsKey(b.Name).CompareTo(_fixedNodeVarsDict.ContainsKey(a.Name));
                    if (fixedOrdering == 0)
                        return a.Name.CompareTo(b.Name);
                    return fixedOrdering;
                }
                return a.Name.CompareTo(b.Name);
            });

            // Move the current focused entry into it's Value dict index inside the entries vBox.
            // We don't want to just overwrite the current focused entry since that would
            // cause the user to retain gui focus on the wrong entry.
            var currFocusedEntry = _currentFocused?.GetAncestor<NodeVarEntry>();
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
                    var targetEntry = _nodeVarEntriesVBox.GetChild<NodeVarEntry>(keyIndex);
                    _nodeVarEntriesVBox.SwapChildren(targetEntry, currFocusedEntry);
                }
            }

            // Set the data of each entry with the corresponding values from the Value dictionary
            int index = 0;
            int childCount = _nodeVarEntriesVBox.GetChildCount();
            foreach (NodeVarData nodeVar in sortedDisplayNodeVars)
            {
                NodeVarEntry entry;
                if (index >= childCount)
                    entry = CreateDefaultEntry(nodeVar);
                else
                    entry = _nodeVarEntriesVBox.GetChild<NodeVarEntry>(index);
                if (!CanEntryHandleNodeVar(entry, nodeVar))
                {
                    // If the Entry can't handle the NodeVar (because they are different types)
                    // then we free the entry and replace it with the correct one.
                    entry.QueueFree();
                    entry = CreateDefaultEntry(nodeVar);
                    _nodeVarEntriesVBox.MoveChild(entry, index);
                }
                if (currFocusedEntry == null || entry != currFocusedEntry)
                    entry.SetData(nodeVar, _fixedNodeVarsDict?.GetValue(nodeVar.Name, null));
                if (HasFixedNodeVars)
                {
                    var isFixed = _fixedNodeVarsDict.ContainsKey(nodeVar.Name);
                    entry.SetFixed(isFixed);
                }
                index++;
            }
            // Free extra entries
            if (index < childCount)
            {
                for (int i = childCount - 1; i >= index; i--)
                {
                    var entry = _nodeVarEntriesVBox.GetChild<NodeVarEntry>(i);
                    entry.NameChanged -= OnEntryNameChanged;
                    entry.DataChanged -= OnEntryDataChanged;
                    entry.QueueFree();
                }
            }
            GD.Print("3");

            if (!IsInstanceValid(currFocusedEntry))
                currFocusedEntry = null;

            var nextKey = GetNextVarName();
            _addElementButton.Disabled = Value?.Contains(nextKey) ?? false;
        }

        protected override void UpdateDisabledAndFixedUI()
        {
            _expressionProperty.Disabled = Disabled;
            _nameProperty.Disabled = Disabled;
            foreach (NodeVarReferenceEntry entry in _referenceEntriesVBox.GetChildren())
            {
                entry.Disabled = Disabled;
                entry.IsFixed = DefaultData.NodeVarReferences.ContainsKey(entry.Name);
            }
        }

        private string GetNextVarName() => NodeVarUtils.GetNextVarName();

        private void OnAddElementPressed()
        {
            // The adding is done in UpdateProperty
            // Note the edited a field in Value doesn't invoke ValueChanged, so we must do it manually
            //
            // Use default types for the newly added element
            var nextKey = GetNextVarName();
            switch ((AddOptionIndex)_addOptionButton.Selected)
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
                        ValueType = typeof(int)
                    }.ToGDDict();
                    break;
                default:
                    throw new Exception($"{nameof(DictNodeVarsValueProperty)}: Could not handle adding NodeVar with option \"{_addOptionButton.Selected}\".");
            }
            InvokeValueChanged(Value);
        }
    }
}
#endif