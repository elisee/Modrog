using DeepSwarmClient.UI;

namespace DeepSwarmClient.Interface
{
    class LobbyView : InterfaceElement
    {
        readonly Panel _playerListPanel;
        readonly Panel _scenarioListPanel;
        readonly Panel _savedGamesListPanel;

        readonly Label _noGameSelectedLabel;
        readonly Panel _gameInfoContainer;
        readonly Label _gameTitleLabel;
        readonly Label _gameMinMaxPlayersLabel;
        readonly Label _gameDescriptionLabel;

        public LobbyView(Interface @interface)
            : base(@interface, null)
        {
            var panel = new Panel(this, new TexturePatch(0x88aa88ff))
            {
                ChildLayout = ChildLayoutMode.Top,
            };

            new Label(panel) { Text = "Lobby", Padding = 8 };
            var playerListAndMainPanelContainer = new Panel(panel)
            {
                LayoutWeight = 1,
                ChildLayout = ChildLayoutMode.Left
            };

            {
                var mainPanel = new Panel(playerListAndMainPanelContainer, new TexturePatch(0x228800ff))
                {
                    LayoutWeight = 1,
                    ChildLayout = ChildLayoutMode.Left
                };

                {
                    var gameSelectionPanel = new Panel(mainPanel)
                    {
                        Width = 260,
                        ChildLayout = ChildLayoutMode.Top,
                    };

                    new Label(gameSelectionPanel)
                    {
                        Padding = 8,
                        Text = "Scenarios",
                        BackgroundPatch = new TexturePatch(0x112345ff),
                    };

                    _scenarioListPanel = new Panel(gameSelectionPanel)
                    {
                        LayoutWeight = 1,
                        ChildLayout = ChildLayoutMode.Top,
                        VerticalFlow = Flow.Scroll
                    };

                    new Label(gameSelectionPanel)
                    {
                        Padding = 8,
                        Text = "Saved Games",
                        BackgroundPatch = new TexturePatch(0x112345ff),
                    };

                    _savedGamesListPanel = new Panel(gameSelectionPanel)
                    {
                        LayoutWeight = 1,
                        ChildLayout = ChildLayoutMode.Top,
                        VerticalFlow = Flow.Scroll
                    };
                }

                {
                    var gameInfoPanel = new Panel(mainPanel, new TexturePatch(0x456721ff)) { LayoutWeight = 1, ChildLayout = ChildLayoutMode.Top };

                    _noGameSelectedLabel = new Label(gameInfoPanel) { Text = "No game selected.", IsVisible = true, Padding = 8 };

                    _gameInfoContainer = new Panel(gameInfoPanel) { ChildLayout = ChildLayoutMode.Top, IsVisible = false, Padding = 8 };
                    _gameTitleLabel = new Label(_gameInfoContainer) { Bottom = 16, FontStyle = new FontStyle(@interface.TitleFont) { LetterSpacing = 1 } };

                    var minMaxPlayersPanel = new Panel(_gameInfoContainer) { ChildLayout = ChildLayoutMode.Left, Bottom = 16 };
                    new Label(minMaxPlayersPanel) { Text = "Min / Max players: " };
                    _gameMinMaxPlayersLabel = new Label(minMaxPlayersPanel);

                    var descriptionPanel = new Panel(_gameInfoContainer) { ChildLayout = ChildLayoutMode.Top };
                    new Label(descriptionPanel) { Text = "Description:", Bottom = 8 };
                    _gameDescriptionLabel = new Label(descriptionPanel) { Wrap = true };

                    new TextEditor(_gameInfoContainer)
                    {
                        Top = 16,
                        Height = 300,
                        Padding = 8,
                        BackgroundPatch = new TexturePatch(0x123789ff)
                    }.SetText("Bonjour! This is a test.");

                    // TODO: the list of existing players
                }
            }

            {
                // TODO: Move this in the lower part of the game info panel and separate players between known identities & unknown if loading a saved game
                // TODO: Add chat box
                var playerListArea = new Panel(playerListAndMainPanelContainer, new TexturePatch(0xaa0000ff))
                {
                    Width = 200,
                    ChildLayout = ChildLayoutMode.Top,
                };

                new Label(playerListArea) { Text = "Player list", Padding = 8, BackgroundPatch = new TexturePatch(0x112345ff) };

                _playerListPanel = new Panel(playerListArea)
                {
                    LayoutWeight = 1,
                    ChildLayout = ChildLayoutMode.Top
                };
            }

            {
                var actionsContainer = new Panel(panel)
                {
                    Padding = 8,
                    ChildLayout = ChildLayoutMode.Left
                };

                new TextButton(actionsContainer)
                {
                    Text = "Ready",
                    Padding = 8,
                    Right = 8,
                    Flow = Flow.Shrink,
                    BackgroundPatch = new TexturePatch(0x4444aaff),
                    OnActivate = () => Engine.State.ToggleReady()
                };

                new TextButton(actionsContainer)
                {
                    Text = "Start Game",
                    Padding = 8,
                    Right = 8,
                    Flow = Flow.Shrink,
                    BackgroundPatch = new TexturePatch(0x4444aaff),
                    OnActivate = () => Engine.State.StartGame()
                };

                new TextButton(actionsContainer)
                {
                    Text = "Leave",
                    Padding = 8,
                    Right = 8,
                    Flow = Flow.Shrink,
                    BackgroundPatch = new TexturePatch(0x4444aaff),
                    OnActivate = () => Engine.State.Disconnect()
                };
            }

            // TODO: Display saved games & scenarios to choose from
            // TODO: Add buttons for readying up and starting the game
        }

        public override void OnMounted()
        {
            Desktop.SetFocusedElement(this);

            OnPlayerListUpdated();

            _scenarioListPanel.Clear();
            foreach (var entry in Engine.State.ScenarioEntries)
            {
                new TextButton(_scenarioListPanel)
                {
                    Padding = 8,
                    Text = entry.Name,
                    OnActivate = () => { Engine.State.ChooseScenario(entry.Name); }
                };
            }
            _scenarioListPanel.Layout();

            /*
            _savedGamesListPanel.Clear();
            foreach (var entry in Engine.State.SavedGameEntries)
            {
                // TODO: Add date last played and stuff like that
                new TextButton(_scenarioListPanel)
                {
                    Padding = 8,
                    Text = entry.ScenarioName,
                    OnActivate = () => { TODO }
                };
            }
            */
        }

        public void OnPlayerListUpdated()
        {
            _playerListPanel.Clear();

            foreach (var playerEntry in Engine.State.PlayerList)
            {
                var playerPanel = new Panel(_playerListPanel) { Padding = 8 };
                var label = new Label(playerPanel) { Text = $"[{(playerEntry.IsReady ? "x" : " ")}] {playerEntry.Name}" };
            }

            _playerListPanel.Layout();
        }

        public void OnActiveScenarioChanged()
        {
            if (Engine.State.ActiveScenario != null)
            {
                _noGameSelectedLabel.IsVisible = false;
                _gameInfoContainer.IsVisible = true;

                _gameTitleLabel.Text = Engine.State.ActiveScenario.Name; // TODO: Title
                _gameDescriptionLabel.Text = Engine.State.ActiveScenario.Description;

                _gameInfoContainer.Parent.Layout();
            }
            else
            {
                _noGameSelectedLabel.IsVisible = true;
                _gameInfoContainer.IsVisible = false;

                _noGameSelectedLabel.Parent.Layout();
            }
        }
    }
}
