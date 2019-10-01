using DeepSwarmClient.UI;
using System;

namespace DeepSwarmClient.Interface
{
    class LobbyView : InterfaceElement
    {
        readonly Panel _verticalSplitter;

        public LobbyView(Interface @interface)
            : base(@interface, null)
        {
            var panel = new Panel(Desktop, this, new TexturePatch(0x88aa88ff))
            {
                Flow = Flow.Shrink,
                ChildLayout = ChildLayoutMode.Top,
            };

            new Label(Desktop, panel) { Text = "Lobby" };
            var horizontalSplitter = new Element(Desktop, panel)
            {
                ChildLayout = ChildLayoutMode.Left
            };

            {
                // TODO: Display player list
                var playerListPanel = new Panel(Desktop, horizontalSplitter, new TexturePatch(0xaa0000ff))
                {
                    Width = 200
                };

                new Label(Desktop, playerListPanel) { Text = "Player list" };
            }

            {
                _verticalSplitter = new Panel(Desktop, horizontalSplitter, new TexturePatch(0x228800ff))
                {
                    LayoutWeight = 1,
                    Width = 500,
                    ChildLayout = ChildLayoutMode.Top
                };

                new Label(Desktop, _verticalSplitter)
                {
                    LayoutWeight = 1,
                    Padding = 8,
                    Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Praesent vehicula velit libero, ac eleifend sapien malesuada ultricies. Sed metus orci, ultrices at mauris ut, pretium tempor arcu. Nunc eu quam sit amet nunc lobortis laoreet sit amet et risus. Nam orci ex, pretium quis commodo eu, imperdiet quis mi. Sed tristique mattis purus, fringilla elementum leo volutpat eget. Donec sollicitudin nisi libero, a pretium sapien facilisis vitae. Donec massa nisl, fermentum a feugiat non, tristique sit amet turpis. Etiam ornare pellentesque molestie. Praesent molestie ultrices nunc, nec mattis urna finibus nec.",
                    Wrap = true,
                    Ellipsize = true
                };

                new Label(Desktop, _verticalSplitter)
                {
                    BackgroundPatch = new TexturePatch(0x123456ff),
                    Text = "Text",
                };
            }

            // TODO: Display saved games & scenarios to choose from
            // TODO: Add buttons for readying up and starting the game
        }

        public override void OnMounted()
        {
            Desktop.RegisterAnimation(Animate);
        }

        public override void OnUnmounted()
        {
            Desktop.UnregisterAnimation(Animate);
        }

        float _timer;

        void Animate(float deltaTime)
        {
            _timer += deltaTime;

            _verticalSplitter.Width = 400 + (int)(MathF.Cos(_timer) * 100);
            _verticalSplitter.Layout();
        }
    }
}
