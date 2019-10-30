using SwarmPlatform.Graphics;
using SwarmPlatform.Interface;
using SwarmPlatform.UI;
using System;

namespace ModrogEditor.Interface.Editing
{
    class ErrorLayer : Panel
    {
        readonly Label _titleLabel;
        readonly Label _detailsLabel;

        readonly Element _buttonsContainer;
        readonly Button _tryAgainButton;

        public ErrorLayer(Element parent) : base(parent)
        {
            BackgroundPatch = new TexturePatch(0x00000088);
            Visible = false;

            var windowPanel = new Panel(this)
            {
                BackgroundPatch = new TexturePatch(0x228800ff),
                Width = 480,
                Flow = Flow.Shrink,
                ChildLayout = ChildLayoutMode.Top,
            };

            var titlePanel = new Panel(windowPanel, new TexturePatch(0x88aa88ff));
            _titleLabel = new Label(titlePanel) { Flow = Flow.Shrink, Padding = 8 };

            _detailsLabel = new Label(windowPanel)
            {
                Wrap = true,
                Padding = 8,
            };

            _buttonsContainer = new Element(windowPanel)
            {
                Top = 8,
                ChildLayout = ChildLayoutMode.Left,
                Padding = 8
            };

            _tryAgainButton = new StyledTextButton(_buttonsContainer) { Right = 8, Text = "Try again" };
            new StyledTextButton(_buttonsContainer) { Text = "Close", OnActivate = Close };
        }

        public void Open(string title, string details, Action onTryAgain = null)
        {
            Visible = true;

            _titleLabel.Text = title;
            _detailsLabel.Text = details;

            _buttonsContainer.Visible = onTryAgain != null;
            _tryAgainButton.OnActivate = onTryAgain;
        }

        void Close()
        {
            Visible = false;
            Desktop.SetFocusedElement(Parent);
        }
    }
}
