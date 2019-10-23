# Modrog

Top-down adventures to play with friends and build yourself

## Building

You'll need .NET Core 3 and Visual Studio 2019. VS Code might work.

## Structure

    * ModrogClient, ModrogServer and ModrogEditor are the apps
    * ModrogCommon contains shared stuff for the apps
    * ModrogApi contains the scripting APIs
    * Swarm is the potentially reusable generic framework below it all
        * SwarmBasics contains shared inert features like Math, can be exposed to scripts safely
        * SwarmCore contains shared features that do I/O
        * SwarmPlatform contains shared features that are not suitable for command-line apps

## Running

 * You can use `RunServer.bat` or `RunClient.bat` to start one and then you can debug the other from VS.
 * `RunClient.bat localhost Tutorial` will autoconnect to the local server and start the `Tutorial` scenario.

## Tasks

    - (Map Editor) Bucket fill
    - (Editor) Allow creating or deleting assets
    - (Map Editor) Skip empty chunks when saving
    - (Map Editor) Support entities
    - (Tile Set Editor) Replace JSON editor with visual editor, and maybe switch to binary format
    - (Team) Find a pixel artist
    - (Gameplay) Add back client-side script editing and display script errors somewhere
    - (Gameplay) Add light checkerboard pattern on the floor
    - (UI) Desktop: Show focus outline only after navigating with keyboard once to avoid distracting players and hide after clicking with mouse
    - (UI) Element: Implement scrollbars
    - (UI) Element: Implement ChildLayout.Bottom, .Right
    - (UI) TextEditor / TextInput: Undo/redo support
    - (UI) TextEditor / TextInput: Quick navigation with Ctrl
    - (UI) TextEditor: Auto-indent when inserting lines
    - (UI) TextEditor: Language-specific syntax highlighting & auto-completion support (using callbacks or specialization, not built into the UI framework I think)
    - (UI) TextEditor: Add option for line count gutter on the left

## Visual inspirations

  * Zelda: A Link to the Past https://static.hitek.fr/img/up_m/1373484298/Link_to_the_past_poules.jpg
  * Towerfall http://www.towerfall-game.com/newsite/images/screenshots/Backfire.png
  * Crypt of the NecroDancer https://cdn.radiofrance.fr/s3/cruiser-production/2019/06/1334e540-005c-4ae4-97a2-0a633f5dfb12/640_2019061413364100-5c78065cd98929d80fe9662ac4a6dda2.jpg
  * Arclands https://twitter.com/jonkellerdev/status/1187017848004206592/photo/1
  * Worlds MMO https://twitter.com/2Pblog1/status/546067188558221313, https://pbs.twimg.com/media/CoakSQlWIAAfUhJ?format=jpg&name=large
  * Spelunky Classic https://blog.en.uptodown.com/files/2017/07/spelunky-classic-android-header.jpg
