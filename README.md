# DeepSwarm

Top-down adventures to play with friends and build yourself

## Building

You'll need .NET Core 3 and Visual Studio 2019. VS Code might work.

## Running

 * You can use `RunServer.bat` or `RunClient.bat` to start one and then you can debug the other from VS.
 * `RunClient.bat localhost Tutorial` will autoconnect to the local server and start the `Tutorial` scenario.

## Tasks

    - (Editor) Build a simple map editor
    - (Team) Find a pixel artist
    - (Project) Find a new name that fits better
    - (Client) Fix confusion between Engine and ClientState? Have a ClientApp (renamed from ClientState) which has an Engine instead
    - (Gameplay) Add back client-side script editing and display script errors somewhere
    - (Gameplay) Add light checkerboard pattern on the floor
    - (UI) Desktop: Show focus outline only after navigating with keyboard once to avoid distracting players and hide after clicking with mouse
    - (UI) Element: Implement scrollbars
    - (UI) Element: Implement ChildLayout.Bottom, .Right
    - (UI) TextEditor / TextInput: Undo/redo support
    - (UI) TextEditor / TextInput: Quick navigation with Ctrl
    - (UI) TextEditor: Auto-indent when inserting lines
    - (UI) TextEditor: Language-specific syntax highlighting & auto-completion support (using callbacks or specialization, not built into the UI framework I think)
