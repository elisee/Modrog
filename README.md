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

    - (Gameplay) Implement drag'n'drop inventory management with list of items on the ground
    - (Network) Track player view for each player, including fog-of-war chunks so we can resend on reconnection
    - (Api) Have a setup phase and then prevent making further changes to entity kinds, tile kinds, etc.
    - (Gameplay) Implement Rogventure-style combat with hits & projectiles
    - (Gameplay) When the selected player moves around, make sure to clamp the camera to it so player doesn't have to manage it manually
    - (Editor) Allow renaming assets
    - (Editor) Allow reparenting assets with drag'n'drop (need to add drag'n'drop in UI framework)
    - (Editor) Fix focus in asset tree view
    - (Map Editor) Allow switching layers with 1, 2, 3
    - (Map Editor) Display all tileset layers in the sidebar at once
    - (Map Editor) Trace line segments when using the brush rather than just placing tiles at hovered position. (Can reuse line drawing logic from line of sight)
    - (Map Editor) Bucket fill
    - (Map Editor) Skip empty chunks when saving
    - (Tile Set Editor) Support marker tile kinds
    - (Map Editor) Support placing markers on their own special sparse layer? Maybe have two modes for chunk storage?
    - (Tile Set Editor) Replace JSON editor with visual editor, and maybe switch to binary format
    - (Team) Find a pixel artist! Waiting on mockups from Timothée
    - (Tutorial) Build it!
    - (Gameplay) Add back client-side script editing and display script errors somewhere
      -> Move towards some visual scripting that is executed server-side and with instruction costs
    - (Gameplay) Add light checkerboard pattern on the floor?
    - (UI Desktop) Show focus outline only after navigating with keyboard once to avoid distracting players and hide after clicking with mouse
    - (UI Element) Allow dragging scrollbars
    - (UI Element) Implement ChildLayout.Bottom (use for chat box), .Right
    - (UI TextEditor)/ TextInput: Undo/redo support
    - (UI TextEditor) Auto-indent when inserting lines
    - (UI TextEditor) Language-specific syntax highlighting & auto-completion support (using callbacks or specialization, not built into the UI framework I think)
    - (UI TextEditor) Add option for line count gutter on the left and use it in script editor
    - (UI TextEditor) Add option to wrap text and use it for manifest description
    - Support saving & loading games

## Visual inspirations

  * Zelda: A Link to the Past https://static.hitek.fr/img/up_m/1373484298/Link_to_the_past_poules.jpg
  * Arclands https://pbs.twimg.com/media/EENUKr0W4AEu9cW?format=png&name=medium
  * Worlds MMO https://twitter.com/2Pblog1/status/546067188558221313, https://pbs.twimg.com/media/CoakSQlWIAAfUhJ?format=jpg&name=large
  * Spelunky Classic https://lh3.googleusercontent.com/SxQKcijZk2wVyhDThNeXj8vfzCDHqegyp4Ar23bS8fAVO--XzLrBm25qfKQGbKhGkhrLMsqhqL0=w640-h400-e365
  * Yokai Dungeon https://twitter.com/Neutronized/status/1187390284117823488
  * Fidel Dungeon Rescue https://img.itch.zone/aW1nLzE2MjEyNjYucG5n/original/KfVMMM.png
