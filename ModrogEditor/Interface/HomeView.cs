﻿using SwarmPlatform.Graphics;
using SwarmPlatform.UI;

namespace ModrogEditor.Interface
{
    class HomeView : EditorElement
    {
        readonly Panel _scenarioListPanel;

        public HomeView(EditorApp @interface)
            : base(@interface, null)
        {
            var windowPanel = new Panel(this, new TexturePatch(0x228800ff))
            {
                Width = 480,
                Flow = Flow.Shrink,
                ChildLayout = ChildLayoutMode.Top,
            };

            var titlePanel = new Panel(windowPanel, new TexturePatch(0x88aa88ff));
            new Label(titlePanel) { Text = "- Modrog Editor -", Flow = Flow.Shrink, Padding = 8 };

            var mainPanel = new Panel(windowPanel, new TexturePatch(0x228800ff))
            {
                Padding = 8,
                ChildLayout = ChildLayoutMode.Top,
            };

            new Label(mainPanel) { Text = "Choose a scenario:", Bottom = 8 };

            _scenarioListPanel = new Panel(mainPanel)
            {
                BackgroundPatch = new TexturePatch(0x001234ff),
                LayoutWeight = 1,
                ChildLayout = ChildLayoutMode.Top,
                VerticalFlow = Flow.Scroll,
                Height = 300
            };
        }

        public override void OnMounted()
        {
            _scenarioListPanel.Clear();

            foreach (var entry in App.State.ScenarioEntries)
            {
                new TextButton(_scenarioListPanel)
                {
                    Padding = 8,
                    Text = entry.Title,
                    OnActivate = () => { App.State.OpenScenario(entry); }
                }.Label.Ellipsize = true;
            }

            Desktop.SetFocusedElement(this);
        }
    }
}
