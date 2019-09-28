# DeepSwarm

## Building

You'll need .NET Core 3 and Visual Studio 2019. VS Code might work.

## Running

 * You can use `RunServer.bat` or `RunClient.bat` to start one and then you can debug the other from VS.
 * `RunServer.bat new` will overwrite the generated map.

## Things to do

    - InGameView: Fix not being able to select an entity when HoveredTileX/HoveredTileY is negative
    - Need to figure out ASAP what the actual gameplay loop is. Factorio-style? Northgard? Something else?
    - UI: Copy / Paste in TextInput / TextEditor
    : UI: Keyboard fast movement (Ctrl) + selection (Shift, Ctrl+A)
    - UI: Display script errors somewhere
    - UI: Increase script window size?
    - UI: Support resizable window (requires better AnchorRectangle system)
    - UI: Replace ugly single font with some of Chevy Ray's pixel fonts probably
    - Scripting: Support renaming scripts
    - Server: Mark chunk as non-free if an entity enters it to prevent spawning people in it (or we need to track entities by chunk and check for that)
    - Server: Generate and control enemies
    - Implement Attack move
    - Design & implement more unit types: one that can fire from a distance but has less health, one that digs faster, etc.
    - Design & implement more building types: turrets, refinery, etc.
    - Server sends list of connected core networks by team (red vs blue)? If we go that route
    - Add UI for connecting to a server and report errors rather than just crashing
    - Rendering: Implement auto-tiling so we can build art like Rogventure / Worlds
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

