# DeepSwarm

## Building

You'll need .NET Core 3 and Visual Studio 2019. VS Code might work.

## Running

 * You can use `RunServer.bat` or `RunClient.bat` to start one and then you can debug the other from VS.
 * `RunServer.bat new` will overwrite the generated map.

## Things to do

    - Server: Send message when kicking player so reason can be displayed in the UI
    - Implement scrolling + ChildLayout.Bottom / .Right
    - UI: Possibly show focus outline only when using keyboard to avoid distracting players
    - InGameView: Fix not being able to select an entity when HoveredTileX/HoveredTileY is negative
    - UI: Copy / Paste in TextInput / TextEditor
    - UI: Keyboard fast movement (Ctrl) in TextInput / TextEditor
    - UI: Display script errors somewhere
    - UI: Increase script window size?
    - Server: Mark chunk as non-free if an entity enters it to prevent spawning people in it (or we need to track entities by chunk and check for that)
    - Server: Generate and control enemies
    - Implement Attack move
    - Rendering: Add light checkerboard pattern on the floor?
    - Design & implement more unit types: one that can fire from a distance but has less health, one that digs faster, etc.
    - Design & implement more building types: turrets, refinery, etc.
    - The Factory should be able to produce its own crystals very slowly so that you're never stuck (but it's so slow that it's not sustainable beyond building a couple workers)

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

