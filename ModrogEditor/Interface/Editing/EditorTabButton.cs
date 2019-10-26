using ModrogEditor.Scenario;
using SDL2;
using SwarmPlatform.Graphics;
using SwarmPlatform.UI;
using System;

namespace ModrogEditor.Interface.Editing
{
    class EditorTabButton : Button
    {
        public Action OnClose { set => _closeButton.OnActivate = value; }

        readonly TextButton _closeButton;

        public EditorTabButton(Element parent, AssetEntry entry)
            : base(parent)
        {
            HorizontalFlow = Flow.Shrink;
            ChildLayout = ChildLayoutMode.Left;
            BackgroundPatch = new TexturePatch(0x44aa44ff);
            Padding = 8;
            Right = 1;

            new Label(this) { Flow = Flow.Shrink, Text = entry.Path };
            _closeButton = new TextButton(this) { Left = 8, Text = "(x)" };
        }

        public void SetActive(bool active)
        {
            BackgroundPatch = new TexturePatch((uint)(active ? 0x44aa44ff : 0x226622ff));
        }

        public void SetUnsavedChanges(bool hasUnsavedChanges)
        {
            _closeButton.Text = hasUnsavedChanges ? "(*)" : "(x)";
            _closeButton.Layout();
        }

        public override void OnMouseDown(int button, int clicks)
        {
            if (button == SDL.SDL_BUTTON_MIDDLE)
            {
                Desktop.SetHoveredElementPressed(true);
                return;
            }

            base.OnMouseDown(button, clicks);
        }

        public override void OnMouseUp(int button)
        {
            if (button == SDL.SDL_BUTTON_MIDDLE && IsPressed)
            {
                _closeButton.OnActivate?.Invoke();
                return;
            }

            base.OnMouseUp(button);
        }
    }
}
