# DeepSwarm

Multiplayer real-time chess adventure & strategy game

## Building

You'll need .NET Core 3 and Visual Studio 2019. VS Code might work.

## Running

 * You can use `RunServer.bat` or `RunClient.bat` to start one and then you can debug the other from VS.
 * `RunClient.bat localhost Tutorial` will autoconnect to the local server and start the `Tutorial` scenario.

## Tasks

### Interface

    - Build a simple scenario editor so we can make maps
    - Allow launching client from editor. Should pass a command-line argument that makes the client start its own server with the currently edited scenario
    - Add back client-side script editing and display script errors somewhere

### Gameplay

### Rendering

    - Add light checkerboard pattern on the floor

### UI Framework

    - Element: Implement scrollbars
    - TextEditor: Fix scrolling, building on Element scrolling
    - TextInput / TextEditor: Support Cut, Copy, Paste
    - Element: Implement ChildLayout.Bottom, .Right
    - TextInput / TextEditor: Keyboard fast movement (Ctrl)
    - Desktop: Show focus outline only after navigating with keyboard once to avoid distracting players and hide after clicking with mouse

## Design

A top-down 2D tile-based multiplayer strategy game set in a fantasy world mixing medieval and science-fiction.

There are various scenarios that can be played cooperatively or competitively. The players can make teams if they want (before the game starts). Some maps make more sense as cooperative vs competitive.

Examples of scenarios:

  * THE PLAGUE: Evil has risen, go kill it in its lair before the world is taken over. (cooperative)
  * THE RACE TO POWER: Build all the pieces of the ultimate weapon to win. The pieces require resources to be found in various parts of the world. (competitive)
  * EXTINCTION: The world is ending. You need to escape by building a time travelling spaceship. The various pieces are to be found in varius dungeons. (cooperative or competitive)
  * TREASURE HUNT: RPG-style adventure where each player controls one unit, no building.

The scenarios are built with maps & scripts, so they might have some (or a lot of) randomization.
Each scenario can have its own tech tree and even graphics, but we'll focus on one for now.
