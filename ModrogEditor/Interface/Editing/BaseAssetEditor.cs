using SwarmPlatform.Graphics;
using SwarmPlatform.Interface;
using SwarmPlatform.UI;
using System;

namespace ModrogEditor.Interface.Editing
{
    abstract class BaseAssetEditor : EditorElement
    {
        public readonly string FullAssetPath;

        bool _hasUnsavedChanges;

        protected readonly Panel _mainLayer;
        readonly ErrorLayer _loadAndSaveErrorLayer;

        readonly Panel _saveBeforeClosingLayer;
        readonly Label _saveBeforeClosingErrorLabel;
        readonly Button _saveBeforeClosingButton;
        readonly Button _discardBeforeClosingButton;

        public BaseAssetEditor(EditorApp @interface, string fullAssetPath)
            : base(@interface, null)
        {
            FullAssetPath = fullAssetPath;

            _mainLayer = new Panel(this);
            _loadAndSaveErrorLayer = new ErrorLayer(this) { Visible = false };

            _saveBeforeClosingLayer = new Panel(this)
            {
                BackgroundPatch = new TexturePatch(0x00000088),
                Visible = false,
            };

            {
                var windowPanel = new Panel(_saveBeforeClosingLayer)
                {
                    BackgroundPatch = new TexturePatch(0x228800ff),
                    Width = 480,
                    Flow = Flow.Shrink,
                    ChildLayout = ChildLayoutMode.Top,
                };

                var titlePanel = new Panel(windowPanel, new TexturePatch(0x88aa88ff));
                new Label(titlePanel) { Flow = Flow.Shrink, Padding = 8, Text = "Save before closing" };

                new Label(windowPanel)
                {
                    Text = "Would you like to save your changes before closing?",
                    Wrap = true,
                    Padding = 8,
                };

                _saveBeforeClosingErrorLabel = new Label(windowPanel) { Text = "ERROR", Padding = 8, BackgroundPatch = new TexturePatch(0xff0000ff), Visible = false };

                var buttonsContainer = new Element(windowPanel)
                {
                    Top = 8,
                    ChildLayout = ChildLayoutMode.Left,
                    Padding = 8
                };

                _saveBeforeClosingButton = new StyledTextButton(buttonsContainer) { Right = 8, Text = "Save" };
                _discardBeforeClosingButton = new StyledTextButton(buttonsContainer) { Right = 8, Text = "Discard changes" };

                new StyledTextButton(buttonsContainer)
                {
                    Text = "Don't close",
                    OnActivate = () =>
                    {
                        _saveBeforeClosingLayer.Visible = false;
                        Desktop.SetFocusedElement(this);
                    }
                };
            }
        }

        public void MaybeUnload(Action onUnloaded)
        {
            if (_loadAndSaveErrorLayer.Visible) return;

            if (!_hasUnsavedChanges)
            {
                Unload();
                onUnloaded();
                return;
            }

            _saveBeforeClosingButton.OnActivate = () =>
            {
                if (!TrySave(out var error))
                {
                    _saveBeforeClosingErrorLabel.Text = error;
                    _saveBeforeClosingErrorLabel.Visible = true;
                    _saveBeforeClosingLayer.Layout();
                    return;
                }

                ContinueWithUnload();
            };

            _discardBeforeClosingButton.OnActivate = ContinueWithUnload;

            _saveBeforeClosingLayer.Visible = true;
            _saveBeforeClosingErrorLabel.Visible = false;
            Desktop.SetFocusedElement(_saveBeforeClosingLayer);
            Layout();

            void ContinueWithUnload()
            {
                _saveBeforeClosingButton.OnActivate = null;
                _discardBeforeClosingButton.OnActivate = null;
                _saveBeforeClosingLayer.Visible = false;
                Desktop.SetFocusedElement(this);

                Unload();
                onUnloaded();
            };
        }

        void CancelUnload()
        {
            _saveBeforeClosingLayer.Visible = false;
            Desktop.SetFocusedElement(this);
        }

        internal void MarkUnsavedChanges()
        {
            _hasUnsavedChanges = true;
            // TODO: Update tab title with a star or something
        }

        protected void Load()
        {
            Unload();

            _loadAndSaveErrorLayer.Visible = false;

            if (!TryLoad(out var error))
            {
                _loadAndSaveErrorLayer.Show("Cannot open asset", error, onTryAgain: Load);
                Layout();
                return;
            }

            Layout();
        }

        protected abstract void Unload();

        void Save()
        {
            if (!_hasUnsavedChanges) return;

            _loadAndSaveErrorLayer.Visible = false;

            if (!TrySave(out var error))
            {
                _loadAndSaveErrorLayer.Show("Cannot save asset", error, onTryAgain: Save);
                Layout();
                return;
            }
        }

        protected abstract bool TryLoad(out string error);
        protected abstract bool TrySave(out string error);
    }
}
