using SwarmPlatform.Graphics;
using SwarmPlatform.Interface;
using SwarmPlatform.UI;

namespace ModrogClient.Interface
{
    class LobbyView : InterfaceElement
    {
        readonly Panel _playerListPanel;
        readonly Panel _scenarioListPanel;
        readonly Panel _savedGamesListPanel;

        readonly Label _noGameSelectedLabel;
        readonly Panel _scenarioInfoContainer;
        readonly Label _scenarioTitleLabel;
        readonly Label _minMaxPlayersLabel;
        readonly Label _modesLabel;
        readonly Label _scenarioDescriptionLabel;

        readonly Panel _chatLog;
        readonly TextInput _chatInput;

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
                    var centerPanel = new Panel(mainPanel)
                    {
                        LayoutWeight = 1,
                        ChildLayout = ChildLayoutMode.Top,
                    };

                    {
                        var gameInfoPanel = new Panel(centerPanel, new TexturePatch(0x456721ff)) { LayoutWeight = 1, ChildLayout = ChildLayoutMode.Top };

                        _noGameSelectedLabel = new Label(gameInfoPanel) { Text = "No game selected.", Visible = true, Padding = 8 };

                        _scenarioInfoContainer = new Panel(gameInfoPanel) { ChildLayout = ChildLayoutMode.Top, Visible = false, Padding = 8 };
                        _scenarioTitleLabel = new Label(_scenarioInfoContainer) { Bottom = 16, FontStyle = new FontStyle(@interface.TitleFont) { LetterSpacing = 1 } };

                        var minMaxPlayersPanel = new Panel(_scenarioInfoContainer) { ChildLayout = ChildLayoutMode.Left, Bottom = 16 };
                        new Label(minMaxPlayersPanel) { Text = "Players: ", FontStyle = @interface.HeaderFontStyle };
                        _minMaxPlayersLabel = new Label(minMaxPlayersPanel) { VerticalFlow = Flow.Shrink, Bottom = 0 };

                        var modesPanel = new Panel(_scenarioInfoContainer) { ChildLayout = ChildLayoutMode.Left, Bottom = 16 };
                        new Label(modesPanel) { Text = "Supported modes: ", FontStyle = @interface.HeaderFontStyle };
                        _modesLabel = new Label(modesPanel) { Flow = Flow.Shrink, Bottom = 0 };

                        var descriptionPanel = new Panel(_scenarioInfoContainer) { ChildLayout = ChildLayoutMode.Top };
                        new Label(descriptionPanel) { Text = "Description:", FontStyle = @interface.HeaderFontStyle, Bottom = 8 };
                        _scenarioDescriptionLabel = new Label(descriptionPanel) { Wrap = true };
                    }

                    {
                        var chatPanel = new Panel(centerPanel, new TexturePatch(0x756124ff))
                        {
                            Height = 200,
                            ChildLayout = ChildLayoutMode.Top
                        };

                        _chatLog = new Panel(chatPanel)
                        {
                            LayoutWeight = 1,
                            ChildLayout = ChildLayoutMode.Bottom,
                            TopPadding = 8,
                            Flow = Flow.Scroll
                        };

                        _chatInput = new TextInput(chatPanel)
                        {
                            Padding = 8,
                            BackgroundPatch = new TexturePatch(0x123456ff)
                        };
                    }
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
                var actionsContainer = new Panel(panel) { Padding = 8, ChildLayout = ChildLayoutMode.Left };
                new StyledTextButton(actionsContainer) { Text = "Ready", Right = 8, OnActivate = () => Engine.State.ToggleReady() };
                new StyledTextButton(actionsContainer) { Text = "Start Game", Right = 8, OnActivate = () => Engine.State.StartGame() };
                new StyledTextButton(actionsContainer) { Text = "Leave", Right = 8, OnActivate = () => Engine.State.Disconnect() };
            }

            // TODO: Display saved games & scenarios to choose from
            // TODO: Add buttons for readying up and starting the game
        }

        public override void OnMounted()
        {
            OnPlayerListUpdated();

            _scenarioListPanel.Clear();
            foreach (var entry in Engine.State.ScenarioEntries)
            {
                new TextButton(_scenarioListPanel)
                {
                    Padding = 8,
                    Text = entry.Title,
                    OnActivate = () => { Engine.State.SetScenario(entry.Name); }
                }.Label.Ellipsize = true;
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

            Desktop.SetFocusedElement(_chatInput);
        }

        public override void OnUnmounted()
        {
            _chatLog.Clear();
            _chatInput.SetValue("");
        }

        public override void Validate()
        {
            if (Desktop.FocusedElement == _chatInput)
            {
                var text = _chatInput.Value.Trim();
                _chatInput.SetValue("");

                if (text.Length > 0) Engine.State.SendChatMessage(text);
            }
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

        public void OnChatMessageReceived(string author, string message)
        {
            if (author.Length > 0) AppendToChatLog($"{author}: {message}");
            else AppendToChatLog($"[Server] {message}");
        }

        void AppendToChatLog(string text)
        {
            new Label(_chatLog)
            {
                BottomPadding = 8,
                HorizontalPadding = 8,
                Wrap = true
            }.Text = text;

            _chatLog.Layout();
            _chatLog.ScrollToBottom();
        }

        public void OnActiveScenarioChanged()
        {
            var scenario = Engine.State.ActiveScenario;

            if (scenario != null)
            {
                _noGameSelectedLabel.Visible = false;
                _scenarioInfoContainer.Visible = true;

                _scenarioTitleLabel.Text = scenario.Title;
                _minMaxPlayersLabel.Text = $"{scenario.MinPlayers} to {scenario.MaxPlayers}";
                _modesLabel.Text = scenario.SupportsCoop ? (scenario.SupportsVersus ? "Coop & Versus" : "Coop") : "Versus";
                _scenarioDescriptionLabel.Text = scenario.Description;

                _scenarioInfoContainer.Parent.Layout();
            }
            else
            {
                _noGameSelectedLabel.Visible = true;
                _scenarioInfoContainer.Visible = false;

                _noGameSelectedLabel.Parent.Layout();
            }
        }

        public void OnIsCountingDownChanged()
        {
            var isCountingDown = Engine.State.IsCountingDown;

            // TODO: Disable / enable all buttons except chat

            if (isCountingDown) AppendToChatLog("Game starts in a few seconds...");
            else AppendToChatLog("Countdown cancelled.");
        }

    }
}
