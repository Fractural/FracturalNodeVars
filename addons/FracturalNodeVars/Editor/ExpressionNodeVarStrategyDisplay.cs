using System.Collections.Generic;
using System.Linq;
using Fractural.Plugin;
using Fractural.Plugin.AssetsRegistry;
using Fractural.Utils;
using Godot;
using static Fractural.NodeVars.ExpressionNodeVarStrategy;

#if TOOLS
namespace Fractural.NodeVars
{
    // TODO: Add meta based collapsing
    [Tool]
    public class ExpressionNodeVarStrategyDisplay : NodeVarStrategyDisplay<ExpressionNodeVarStrategy>
    {
        private StringValueProperty _expressionProperty;
        private VBoxContainer _referenceEntriesVBox;
        private Button _addElementButton;
        private Button _resetExpressionButton;

        private INodeVarContainer _propagationSource;
        private IAssetsRegistry _assetsRegistry;
        private PackedSceneDefaultValuesRegistry _defaultValuesRegistry;
        private Node _sceneRoot;
        private Node _relativeToNode;

        private Control CurrentFocused
        {
            get
            {
                if (!IsInstanceValid(_currentFocused))
                    _currentFocused = null;
                return _currentFocused;
            }
            set => _currentFocused = value;
        }
        private Control _currentFocused;

        private bool IsExpressionSameAsDefault => Strategy.Expression == DefaultStrategy.Expression || Strategy.Expression == "";

        public ExpressionNodeVarStrategyDisplay() { }
        public ExpressionNodeVarStrategyDisplay(Control topRow, Control bottomRow, INodeVarContainer propagationSource, IAssetsRegistry assetsRegistry, PackedSceneDefaultValuesRegistry defaultValuesRegistry, Node sceneRoot, Node relativeToNode)
        {
            _propagationSource = propagationSource;
            _assetsRegistry = assetsRegistry;
            _defaultValuesRegistry = defaultValuesRegistry;
            _sceneRoot = sceneRoot;
            _relativeToNode = relativeToNode;

            _expressionProperty = new StringValueProperty();
            _expressionProperty.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            _expressionProperty.RectMinSize = Vector2.Zero;
            _expressionProperty.PlaceholderText = "Expression";
            _expressionProperty.RectClipContent = true;
            _expressionProperty.ValueChanged += OnExpressionChanged;

            _referenceEntriesVBox = new VBoxContainer();

            _resetExpressionButton = new Button();
            _resetExpressionButton.Connect("pressed", this, nameof(OnResetExpressionButtonPressed));

            _addElementButton = new Button();
            _addElementButton.Connect("pressed", this, nameof(OnAddElementPressed));

            topRow.AddChild(_expressionProperty);
            topRow.AddChild(_resetExpressionButton);
            topRow.AddChild(_addElementButton);
            bottomRow.AddChild(_referenceEntriesVBox);
        }

        public override void _Ready()
        {
            base._Ready();
#if TOOLS
            if (NodeUtils.IsInEditorSceneTab(this))
                return;
#endif
            _expressionProperty.Font = (Font)GetFont("source", "EditorFonts").Duplicate();
            var dynamicFont = _expressionProperty.Font as DynamicFont;
            dynamicFont.Size = (int)(16 * _assetsRegistry.Scale);
            _addElementButton.Icon = GetIcon("Add", "EditorIcons");
            _resetExpressionButton.Icon = GetIcon("Reload", "EditorIcons");
            GetViewport().Connect("gui_focus_changed", this, nameof(OnFocusChanged));
        }

        public override void SetData(NodeVarData value, NodeVarData defaultData = null)
        {
            base.SetData(value, defaultData);

            if (defaultData != null && Strategy.Expression == "")
                _expressionProperty.SetValue(DefaultStrategy.Expression, false);
            else
                _expressionProperty.SetValue(Strategy.Expression, false);
            UpdateReferencesUI();
            UpdateExpressionResetButton();
        }

        public override void UpdateDisabledAndFixedUI(bool isFixed, bool disabled, bool privateDisabled, bool nonSetDisabled)
        {
            _referenceEntriesVBox.Visible = !nonSetDisabled;
            _expressionProperty.Disabled = disabled || privateDisabled || nonSetDisabled;
            foreach (ExpressionNodeVarReferenceEntry entry in _referenceEntriesVBox.GetChildren())
            {
                entry.Disabled = disabled || privateDisabled || nonSetDisabled;
                entry.IsFixed = DefaultStrategy?.NodeVarReferences.ContainsKey(entry.Data.Name) ?? false;
            }
        }

        private void UpdateExpressionResetButton()
        {
            _resetExpressionButton.Visible = DefaultData != null && !IsExpressionSameAsDefault;
        }

        // TODO LATER: Delete if unecessary
        //protected override bool IsSameAsDefault()
        //{
        //    if (DefaultData == null)
        //        return false;
        //    return CheckReferencesSameAsDefault() &&
        //        Equals(Data.Name, DefaultData.Name) &&
        //        CheckExpressionSameAsDefault();
        //}

        private bool CheckReferencesSameAsDefault()
        {
            if (Strategy.NodeVarReferences.Count == 0)
                return true;
            if (DefaultStrategy == null)
                return false;
            if (Strategy.NodeVarReferences.Count > DefaultStrategy.NodeVarReferences.Count)
                return false;
            foreach (var reference in Strategy.NodeVarReferences.Values)
            {
                if (!DefaultStrategy.NodeVarReferences.TryGetValue(reference.Name, out NodeVarReference fixedReference))
                    return false;
                if (!reference.Equals(fixedReference))
                    return false;
            }
            return true;
        }

        private void UpdateReferencesUI()
        {
            if (Strategy.NodeVarReferences.Count > 0 && CheckReferencesSameAsDefault())
            {
                Strategy.NodeVarReferences.Clear();
            }

            var displayedReferences = new Dictionary<string, NodeVarReference>();
            foreach (var reference in Strategy.NodeVarReferences.Values)
            {
                displayedReferences.Add(reference.Name, reference);
            }

            if (DefaultData != null)
                foreach (var fixedReference in DefaultStrategy.NodeVarReferences.Values)
                {
                    var displayReference = fixedReference;
                    if (displayedReferences.TryGetValue(fixedReference.Name, out NodeVarReference existingNodeVar))
                    {
                        var referenceWithChanges = fixedReference.WithChanges(existingNodeVar);
                        if (referenceWithChanges != null)
                            displayReference = referenceWithChanges;
                        else
                            Strategy.NodeVarReferences.Remove(existingNodeVar.Name);
                    }
                    displayedReferences[fixedReference.Name] = displayReference;
                }

            var sortedReferences = new List<NodeVarReference>(displayedReferences.Values);
            sortedReferences.Sort((a, b) =>
            {
                if (DefaultStrategy != null)
                {
                    // Sort by whether it's fixed, and then by alphabetical order
                    int fixedOrdering = DefaultStrategy.NodeVarReferences.ContainsKey(b.Name).CompareTo(DefaultStrategy.NodeVarReferences.ContainsKey(a.Name));
                    if (fixedOrdering == 0)
                        return a.Name.CompareTo(b.Name);
                    return fixedOrdering;
                }
                return a.Name.CompareTo(b.Name);
            });

            var currFocusedEntry = CurrentFocused?.GetAncestor<ExpressionNodeVarReferenceEntry>();
            if (currFocusedEntry != null && currFocusedEntry.HasParent(this))
            {
                int keyIndex = sortedReferences.FindIndex(x => x.Name == currFocusedEntry.Data.Name);
                if (keyIndex < 0)
                    currFocusedEntry = null;
                else
                {
                    var targetEntry = _referenceEntriesVBox.GetChild(keyIndex);
                    _referenceEntriesVBox.SwapChildren(targetEntry, currFocusedEntry);
                }
            }

            int index = 0;
            int childCount = _referenceEntriesVBox.GetChildCount();
            foreach (NodeVarReference reference in sortedReferences)
            {
                ExpressionNodeVarReferenceEntry entry;
                if (index >= childCount)
                    entry = CreateNewEntry();
                else
                    entry = _referenceEntriesVBox.GetChild<ExpressionNodeVarReferenceEntry>(index);
                if (currFocusedEntry == null || entry != currFocusedEntry)
                    entry.SetData(reference, DefaultStrategy?.NodeVarReferences.GetValue(reference.Name, null));
                entry.IsFixed = DefaultStrategy?.NodeVarReferences.ContainsKey(reference.Name) ?? false;
                index++;
            }

            if (index < childCount)
            {
                for (int i = childCount - 1; i >= index; i--)
                {
                    var entry = _referenceEntriesVBox.GetChild<ExpressionNodeVarReferenceEntry>(i);
                    entry.NameChanged -= OnEntryNameChanged;
                    entry.DataChanged -= OnEntryDataChanged;
                    entry.Deleted -= OnEntryDeleted;
                    entry.QueueFree();
                }
            }

            _addElementButton.Disabled = CheckAllVarNamesTaken();
        }

        private void OnFocusChanged(Control control) => CurrentFocused = control;

        private void OnExpressionChanged(string newExpression)
        {
            if (DefaultStrategy != null && newExpression == DefaultStrategy.Expression)
                Strategy.Expression = "";
            else
                Strategy.Expression = newExpression;
            UpdateExpressionResetButton();
            InvokeDataChanged();
        }

        private void OnResetExpressionButtonPressed()
        {
            Strategy.Expression = "";
            _expressionProperty.SetValue(DefaultStrategy.Expression, false);
            InvokeDataChanged();
        }

        private bool CheckAllVarNamesTaken()
        {
            var nextKey = GetNextVarName();
            return Strategy.NodeVarReferences.ContainsKey(nextKey) || (DefaultStrategy?.NodeVarReferences.ContainsKey(nextKey) ?? false);
        }

        private string GetNextVarName()
        {
            IEnumerable<string> keys = Strategy.NodeVarReferences.Keys;
            if (DefaultData != null)
                keys = keys.Union(DefaultStrategy.NodeVarReferences.Keys);
            return NodeVarUtils.GetNextVarName(keys);
        }

        private ExpressionNodeVarReferenceEntry CreateNewEntry()
        {
            var entry = new ExpressionNodeVarReferenceEntry(
                _propagationSource,
                _assetsRegistry,
                _defaultValuesRegistry,
                _sceneRoot,
                _relativeToNode,
                (container, data) => NodeVarUtils.IsNodeVarValidPointer(container, _relativeToNode, _sceneRoot, data, NodeVarOperation.Set)
            );
            entry.NameChanged += OnEntryNameChanged;
            entry.DataChanged += OnEntryDataChanged;
            entry.Deleted += OnEntryDeleted;
            _referenceEntriesVBox.AddChild(entry);
            return entry;
        }

        private void OnAddElementPressed()
        {
            var nextKey = GetNextVarName();
            Strategy.NodeVarReferences[nextKey] = new NodeVarReference()
            {
                Name = nextKey,
            };
            InvokeDataChanged();
            UpdateReferencesUI();
        }

        private void OnEntryNameChanged(string oldKey, ExpressionNodeVarReferenceEntry entry)
        {
            var newKey = entry.Data.Name;
            if (Strategy.NodeVarReferences.ContainsKey(newKey))
            {
                // Reject change since the newKey already exists
                entry.ResetName(oldKey);
                return;
            }
            Strategy.NodeVarReferences.Remove(oldKey);
            Strategy.NodeVarReferences[newKey] = entry.Data.Clone();
            InvokeDataChanged();
            UpdateReferencesUI();
        }

        private void OnEntryDataChanged(string key, NodeVarReference newValue)
        {
            // Remove entry if it is the same as the fixed value (no point in storing redundant information)
            if (DefaultStrategy != null && DefaultStrategy.NodeVarReferences.TryGetValue(key, out NodeVarReference existingReference) && existingReference.Equals(newValue))
            {
                Strategy.NodeVarReferences.Remove(key);
            }
            else
            {
                Strategy.NodeVarReferences[key] = newValue;
            }
            InvokeDataChanged();
            UpdateReferencesUI();
        }

        private void OnEntryDeleted(string key)
        {
            Strategy.NodeVarReferences.Remove(key);
            InvokeDataChanged();
            UpdateReferencesUI();
        }
    }
}
#endif