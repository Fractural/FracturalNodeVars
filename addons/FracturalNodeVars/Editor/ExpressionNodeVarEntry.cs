using System.Collections.Generic;
using System.Linq;
using Fractural.Plugin;
using Fractural.Plugin.AssetsRegistry;
using Fractural.Utils;
using Godot;
using static Fractural.NodeVars.ExpressionNodeVarData;

#if TOOLS
namespace Fractural.NodeVars
{
    // TODO: Add meta based collapsing
    [Tool]
    public class ExpressionNodeVarEntry : NodeVarEntry<ExpressionNodeVarData>
    {
        private StringValueProperty _expressionProperty;
        private VBoxContainer _referenceEntriesVBox;
        private Button _addElementButton;
        private Button _resetExpressionButton;

        private IAssetsRegistry _assetsRegistry;
        private Node _sceneRoot;
        private Node _relativeToNode;
        private PackedSceneDefaultValuesRegistry _defaultValuesRegistry;

        public ExpressionNodeVarEntry() { }
        public ExpressionNodeVarEntry(IAssetsRegistry assetsRegistry, PackedSceneDefaultValuesRegistry defaultValuesRegistry, Node sceneRoot, Node relativeToNode) : base()
        {
            _defaultValuesRegistry = defaultValuesRegistry;
            _assetsRegistry = assetsRegistry;
            _sceneRoot = sceneRoot;
            _relativeToNode = relativeToNode;

            _expressionProperty = new StringValueProperty();
            _expressionProperty.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            _expressionProperty.RectMinSize = Vector2.Zero;
            _expressionProperty.PlaceholderText = "Expression";
            _expressionProperty.ValueChanged += OnExpressionChanged;

            _referenceEntriesVBox = new VBoxContainer();

            _resetExpressionButton = new Button();
            _resetExpressionButton.Connect("pressed", this, nameof(OnResetExpressionButtonPressed));

            _addElementButton = new Button();
            _addElementButton.Connect("pressed", this, nameof(OnAddElementPressed));

            var topHBox = new HBoxContainer();
            topHBox.AddChild(_nameProperty);
            topHBox.AddChild(_resetInitialValueButton);
            topHBox.AddChild(_deleteButton);

            var midHBox = new HBoxContainer();
            midHBox.AddChild(_expressionProperty);
            midHBox.AddChild(_resetExpressionButton);
            midHBox.AddChild(_addElementButton);

            _contentVBox.AddChild(topHBox);
            _contentVBox.AddChild(midHBox);
            _contentVBox.AddChild(_referenceEntriesVBox);
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

        public override void SetData(ExpressionNodeVarData value, ExpressionNodeVarData defaultData = null)
        {
            base.SetData(value, defaultData);

            if (defaultData != null && value.Expression == "")
                _expressionProperty.SetValue(defaultData.Expression, false);
            else
                _expressionProperty.SetValue(value.Expression, false);
            UpdateReferencesUI();
        }

        protected override void UpdateDisabledAndFixedUI()
        {
            base.UpdateDisabledAndFixedUI();
            _expressionProperty.Disabled = Disabled;
            foreach (ExpressionNodeVarReferenceEntry entry in _referenceEntriesVBox.GetChildren())
            {
                entry.Disabled = Disabled;
                entry.IsFixed = DefaultData?.NodeVarReferences.ContainsKey(entry.Data.Name) ?? false;
            }
        }

        protected override void UpdateResetButton()
        {
            _resetInitialValueButton.Visible = DefaultData != null && !CheckIsSameAsDefault();
            _resetExpressionButton.Visible = DefaultData != null && !CheckExpressionSameAsDefault();
        }

        protected override bool CheckIsSameAsDefault()
        {
            if (DefaultData == null)
                return false;
            return CheckReferencesSameAsDefault() &&
                Equals(Data.Name, DefaultData.Name) &&
                CheckExpressionSameAsDefault();
        }

        private bool CheckExpressionSameAsDefault()
        {
            return Data.Expression == DefaultData.Expression || Data.Expression == "";
        }

        private bool CheckReferencesSameAsDefault()
        {
            if (Data.NodeVarReferences.Count == 0)
                return true;
            if (DefaultData == null)
                return false;
            if (Data.NodeVarReferences.Count > DefaultData.NodeVarReferences.Count)
                return false;
            foreach (var reference in Data.NodeVarReferences.Values)
            {
                if (!DefaultData.NodeVarReferences.TryGetValue(reference.Name, out NodeVarReference fixedReference))
                    return false;
                if (!reference.Equals(fixedReference))
                    return false;
            }
            return true;
        }

        private void UpdateReferencesUI()
        {
            if (Data.NodeVarReferences.Count > 0 && CheckReferencesSameAsDefault())
            {
                Data.NodeVarReferences.Clear();
            }

            var displayedReferences = new Dictionary<string, NodeVarReference>();
            foreach (var reference in Data.NodeVarReferences.Values)
            {
                displayedReferences.Add(reference.Name, reference);
            }

            if (DefaultData != null)
                foreach (var fixedReference in DefaultData.NodeVarReferences.Values)
                {
                    var displayReference = fixedReference;
                    if (displayedReferences.TryGetValue(fixedReference.Name, out NodeVarReference existingNodeVar))
                    {
                        var referenceWithChanges = fixedReference.WithChanges(existingNodeVar);
                        if (referenceWithChanges != null)
                            displayReference = referenceWithChanges;
                        else
                            Data.NodeVarReferences.Remove(existingNodeVar.Name);
                    }
                    displayedReferences[fixedReference.Name] = displayReference;
                }

            var sortedReferences = new List<NodeVarReference>(displayedReferences.Values);
            sortedReferences.Sort((a, b) =>
            {
                if (DefaultData != null)
                {
                    // Sort by whether it's fixed, and then by alphabetical order
                    int fixedOrdering = DefaultData.NodeVarReferences.ContainsKey(b.Name).CompareTo(DefaultData.NodeVarReferences.ContainsKey(a.Name));
                    if (fixedOrdering == 0)
                        return a.Name.CompareTo(b.Name);
                    return fixedOrdering;
                }
                return a.Name.CompareTo(b.Name);
            });

            var currFocusedEntry = _currentFocused?.GetAncestor<ExpressionNodeVarReferenceEntry>();
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
                    entry.SetData(reference, DefaultData?.NodeVarReferences.GetValue(reference.Name, null));
                entry.IsFixed = DefaultData?.NodeVarReferences.ContainsKey(reference.Name) ?? false;
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

        private Control _currentFocused;
        private void OnFocusChanged(Control control) => _currentFocused = control;

        private void OnExpressionChanged(string newExpression)
        {
            if (DefaultData != null && newExpression == DefaultData.Expression)
                Data.Expression = "";
            else
                Data.Expression = newExpression;
            InvokeDataChanged();
        }

        private void OnResetExpressionButtonPressed()
        {
            Data.Expression = "";
            _expressionProperty.SetValue(DefaultData.Expression, false);
            InvokeDataChanged();
        }

        private bool CheckAllVarNamesTaken()
        {
            var nextKey = GetNextVarName();
            return Data.NodeVarReferences.ContainsKey(nextKey) || (DefaultData?.NodeVarReferences.ContainsKey(nextKey) ?? false);
        }

        private string GetNextVarName()
        {
            IEnumerable<string> keys = Data.NodeVarReferences.Keys;
            if (DefaultData != null)
                keys = keys.Union(DefaultData.NodeVarReferences.Keys);
            return NodeVarUtils.GetNextVarName(keys);
        }

        private void OnAddElementPressed()
        {
            var nextKey = GetNextVarName();
            Data.NodeVarReferences[nextKey] = new NodeVarReference()
            {
                Name = nextKey,
            };
            InvokeDataChanged();
            UpdateReferencesUI();
        }

        private ExpressionNodeVarReferenceEntry CreateNewEntry()
        {
            var entry = new ExpressionNodeVarReferenceEntry(
                _assetsRegistry,
                _defaultValuesRegistry,
                _sceneRoot,
                _relativeToNode,
                (data) => NodeVarUtils.CheckNodeVarCompatible(data, NodeVarOperation.Get)
            );
            entry.NameChanged += OnEntryNameChanged;
            entry.DataChanged += OnEntryDataChanged;
            entry.Deleted += OnEntryDeleted;
            _referenceEntriesVBox.AddChild(entry);
            return entry;
        }

        private void OnEntryNameChanged(string oldKey, ExpressionNodeVarReferenceEntry entry)
        {
            var newKey = entry.Data.Name;
            if (Data.NodeVarReferences.ContainsKey(newKey))
            {
                // Reject change since the newKey already exists
                entry.ResetName(oldKey);
                return;
            }
            Data.NodeVarReferences.Remove(oldKey);
            Data.NodeVarReferences[newKey] = entry.Data.Clone();
            InvokeDataChanged();
            UpdateReferencesUI();
        }

        private void OnEntryDataChanged(string key, NodeVarReference newValue)
        {
            // Remove entry if it is the same as the fixed value (no point in storing redundant information)
            if (DefaultData != null && DefaultData.NodeVarReferences.TryGetValue(key, out NodeVarReference existingReference) && existingReference.Equals(newValue))
            {
                Data.NodeVarReferences.Remove(key);
            }
            else
            {
                Data.NodeVarReferences[key] = newValue;
            }
            InvokeDataChanged();
            UpdateReferencesUI();
        }

        private void OnEntryDeleted(string key)
        {
            Data.NodeVarReferences.Remove(key);
            InvokeDataChanged();
            UpdateReferencesUI();
        }

        public void OnBeforeSerialize()
        {
            _assetsRegistry = null;
            Data = null;
            DefaultData = null;
        }

        public void OnAfterDeserialize() { }
    }
}
#endif