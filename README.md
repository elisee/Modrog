# DeepSwarm

Public link: https://sparklinlabs.itch.io/deepswarm

## Building

You'll need .NET Core 3 and Visual Studio 2019. VS Code might work.

## Running

 * You can use `RunServer.bat` or `RunClient.bat` to start one and then you can debug the other from VS.
 * `RunServer.bat new` will overwrite the generated map.

## TODO

    (x) Text editor
      (x) Scrolling
      ( ) Allow moving text cursor with mouse
      ( ) Selection / Copy / Paste
    ( ) Display script errors somewhere
    ( ) Increase script window size?
    ( ) Replace ugly single font with some of Chevy Ray's pixel fonts probably
    ( ) Support renaming scripts
    ( ) If a move enters a chunk that was marked as free, mark it as used to prevent spawning people in it (or we need to try entities by chunk and check for that)
    ( ) Make server generate and control enemies, support combat
    ( ) The factory has a limited amount of crystals and dies if it's not filled up
    ( ) Goal : Find people in your team and link your cores with wires or something
    ( ) Server sends list of connected core networks by team (red vs blue)
    ( ) Generate Rocks!
    ( ) Add UI for connecting to a server and report errors rather than just crashing

## DONE

    (x) Setup .NET Core 3 projects
    (x) Make server listen for conenctions
    (x) Make client connect
    (x) Setup SDL / SDL-CS 
    (x) Support bitmap font
    (x) Simple UI
    (x) Client sends name
    (x) Server sends player list
    (x) Client displays player list
    (x) Player list updates when someone joins or leaves
    (x) Unique identity for each player (Identity.dat)
    (x) Server chooses chunk for player and digs up a base
    (x) Setup factory and core for the player
    (x) Server ticks
    (x) Server generates caves & crystals
    (x) Server sends appropriate data to client after each tick
    (x) Player can select entity
    (x) Display black where player can't see + extend light of sight by 1
    (x) Display proper gfx instead of flat colors
    (x) Add header so we can rebuild logical packets from TCP segments
    (x) Factory can make robots
    (x) Move robots (rotate / forward)
    (x) Validate tick index when receiving moves from player
    (x) Center name input popup and allow validating with Return
    (x) Save player name so I don't have to type it again and again
    (x) Collisions
    (x) Dig with a robot
    (x) Fog of war
    (x) Support opening an existing script
    (x) Mount / Unmount support in UI
    (x) Scriptable entities with Lua (fully client-side)
    (x) Pouvoir laisser les boutons enfoncés
    (x) Make STOP button work
    (x) Don't crash on invalid script
    (x) display version number on startup
    (x) Fix crash with invalid coordinates / fog of war
    (x) Make the map loop around on the client
