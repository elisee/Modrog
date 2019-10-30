using SwarmPlatform.UI;
using System;

namespace ModrogEditor.Interface.Editing
{
    abstract class BaseEditor : EditorElement
    {
        public readonly string FullAssetPath;

        public bool HasUnsavedChanges { get; private set; }
        readonly Action _onUnsavedStatusChanged;

        protected readonly Panel _mainLayer;
        readonly ErrorLayer _loadErrorLayer;

        public BaseEditor(EditorApp @interface, string fullAssetPath, Action onUnsavedStatusChanged)
            : base(@interface, null)
        {
            FullAssetPath = fullAssetPath;
            _onUnsavedStatusChanged = onUnsavedStatusChanged;

            _mainLayer = new Panel(this);
            _loadErrorLayer = new ErrorLayer(this);
        }

        internal void MarkUnsavedChanges()
        {
            HasUnsavedChanges = true;
            _onUnsavedStatusChanged();
        }

        protected void Load()
        {
            Unload();

            _loadErrorLayer.Visible = false;

            if (!TryLoad(out var error))
            {
                _loadErrorLayer.Open("Cannot open asset", error, onTryAgain: Load);
                Layout();
                return;
            }

            Layout();
        }

        public abstract void Unload();

        protected abstract bool TryLoad(out string error);
        protected abstract bool TrySave_Internal(out string error);

        public bool TrySave(out string error)
        {
            if (!TrySave_Internal(out error)) return false;

            HasUnsavedChanges = false;
            _onUnsavedStatusChanged();
            return true;
        }
    }
}
