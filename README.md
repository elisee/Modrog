# DeepSwarm

## Building

You'll need .NET Core 3 and Visual Studio 2019. VS Code might work.

## Running

 * You can use `RunServer.bat` or `RunClient.bat` to start one and then you can debug the other from VS.
 * `RunServer.bat new` will overwrite the generated map.

## Things to do

    - Need to figure out ASAP what the actual gameplay loop is. Factorio-style? Northgard? Something else?
    - UI: Selection / Copy / Paste in TextInput / TextEditor
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
    - Load the ARROW, HAND and IBEAM SDL system cursors (https://wiki.libsdl.org/SDL_CreateSystemCursor) in RendererHelper (probably) and use them in .OnHovered / .OnUnhovered for Button, TextInput, TextEditor. (They don't need to be freed ever, we just load them on startup)
