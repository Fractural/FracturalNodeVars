using Fractural.Plugin;
using Fractural.Utils;
using Godot;
using System;
using GDC = Godot.Collections;

#if TOOLS
namespace Fractural.NodeVars
{
    [Tool]
    public abstract class NodeVarEntry : HBoxContainer
    {
        /// <summary>
        /// NameChanged(string oldName, Entry entry)
        /// </summary>
        public event Action<string, NodeVarEntry> NameChanged;
        /// <summary>
        /// DataChanged(string name, NodeVarData newValue)
        /// </summary>
        public event Action<string, NodeVarData> DataChanged;
        /// <summary>
        /// Deleted(string name)
        /// </summary>
        public event Action<string> Deleted;

        public NodeVarData Data { get; set; }
        public NodeVarData DefaultData { get; set; }

        private bool _disabled = false;
        public bool Disabled
        {
            get => _disabled;
            set
            {
                _disabled = value;
                if (IsInsideTree())
                    UpdateDisabledAndFixedUI();
            }
        }

        private bool _isFixed;
        public bool IsFixed
        {
            get => _isFixed;
            set
            {
                _isFixed = value;
                if (IsInsideTree())
                    UpdateDisabledAndFixedUI();
            }
        }

        protected Button _resetInitialValueButton;
        protected Button _deleteButton;
        protected StringValueProperty _nameProperty;
        protected VBoxContainer _contentVBox;

        public NodeVarEntry()
        {
            _resetInitialValueButton = new Button();
            _resetInitialValueButton.Connect("pressed", this, nameof(OnResetButtonPressed));

            _deleteButton = new Button();
            _deleteButton.Connect("pressed", this, nameof(InvokeDeleted));

            _nameProperty = new StringValueProperty();
            _nameProperty.ValueChanged += OnNameChanged;
            _nameProperty.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;

            var control = new Control();
            control.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            control.SizeFlagsStretchRatio = 0.1f;
            control.RectSize = new Vector2(24, 0);
            AddChild(control);

            _contentVBox = new VBoxContainer();
            _contentVBox.SizeFlagsHorizontal = (int)SizeFlags.ExpandFill;
            _contentVBox.SizeFlagsStretchRatio = 0.9f;
            AddChild(_contentVBox);
        }

        public override void _Ready()
        {
#if TOOLS
            if (NodeUtils.IsInEditorSceneTab(this))
                return;
#endif
            _deleteButton.Icon = GetIcon("Remove", "EditorIcons");
            _resetInitialValueButton.Icon = GetIcon("Reload", "EditorIcons");
        }

        public virtual void ResetName(string oldName)
        {
            Data.Name = oldName;
            _nameProperty.SetValue(oldName, false);
        }
        protected virtual void UpdateDisabledAndFixedUI() { }
        protected virtual void InvokeDeleted() => Deleted?.Invoke(Data.Name);
        protected virtual void InvokeDataChanged() => DataChanged?.Invoke(Data.Name, Data);
        protected virtual void InvokeNameChanged(string oldName) => NameChanged?.Invoke(oldName, this);
        public virtual void SetData(NodeVarData data, NodeVarData defaultData = null)
        {
            Data = data.Clone();
            DefaultData = defaultData;
            _nameProperty.SetValue(data.Name, false);
            UpdateResetButton();
        }

        protected void OnNameChanged(string newName)
        {
            var oldName = Data.Name;
            Data.Name = newName;
            InvokeNameChanged(oldName);
        }

        protected void OnResetButtonPressed()
        {
            SetData(DefaultData.Clone(), DefaultData);
            InvokeDataChanged();
        }

        protected void UpdateResetButton()
        {
            _resetInitialValueButton.Visible = DefaultData != null && !Data.Equals(DefaultData);
        }
    }

    public abstract class NodeVarEntry<T> : NodeVarEntry where T : NodeVarData
    {
        public new T Data
        {
            get => (T)base.Data;
            set => base.Data = value;
        }
        public new T DefaultData
        {
            get => (T)base.DefaultData;
            set => base.DefaultData = value;
        }

        public override void SetData(NodeVarData data, NodeVarData defaultData = null) => SetData((T)data, (T)defaultData);
        public virtual void SetData(T data, T defaultData = null) => base.SetData(data, defaultData);
    }
}
#endif