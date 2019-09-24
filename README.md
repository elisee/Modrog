# DeepSwarm

## Building

You'll need .NET Core 3 and Visual Studio 2019. VS Code might work.

## Running

 * You can use `RunServer.bat` or `RunClient.bat` to start one and then you can debug the other from VS.
 * `RunServer.bat new` will overwrite the generated map.

## TODO

* (x) Text editor
  * (x) Scrolling
  * ( ) Allow moving text cursor with mouse
  * ( ) Selection / Copy / Paste
* ( ) Fix crash with invalid coordinates / fog of war
* ( ) Display script errors somewhere
* ( ) Increase script window size?
* ( ) Replace ugly single font with some of Chevy Ray's pixel fonts probably
* ( ) Support renaming scripts
* ( ) Make the map loop around on the client
* ( ) If a move enters a chunk that was marked as free, mark it as used to prevent spawning people in it (or we need to try entities by chunk and check for that)
* ( ) Make server generate and control enemies, support combat
* ( ) The factory has a limited amount of crystals and dies if it's not filled up
* ( ) Goal : Find people in your team and link your cores with wires or something
* ( ) Server sends list of connected core networks by team (red vs blue)
* ( ) Generate Rocks!

## DONE

* (x) Créer & configurer les projets .NET Core 3
* (x) Serveur écoute
* (x) Client se connecte
* (x) SDL / SDL-CS 
* (x) bitmap font
* (x) UI simple
* (x) Client envoie nom
* (x) Serveur envoie liste de joueurs
* (x) Client affiche la liste des joueurs
* (x) La liste des joueurs se met à jour quand quelqu'un rejoint ou quitte
* (x) Identité unique pour un joueur
* (x) Serveur choisit chunk pour joueur dans le monde principal et creuse
* (x) Installer l'usine & le coeur du joueur
* (x) Serveur tick
* (x) Serveur génère grottes & cristaux
* (x) Serveur envoie données appropriées au client après chaque tour
* (x) Joueur peut sélectionner entité
* (x) Afficher du noir là où on ne voit pas + extension line of sight de 1
* (x) Afficher des graphismes au lieu de juste des couleurs plates
* (x) Ajouter en-tête pour pouvoir reconstruire les packets logiques à partir des segments TCP
* (x) Factory peut créer robot
* (x) Déplacer un robot (tourner / avancer)
* (x) Valider l'id du tick quand on reçoit un paquet Tick
* (x) Afficher les parties inconnues de la map en noir
* (x) Centrer le popup de sélection de nom et permettre de le valider avec Entrée
* (x) Sauvegarder nom du joueur histoire de pas avoir à le taper
* (x) Collisions
* (x) Creuser avec un robot
* (x) Brouillard de guerre
* (x) Support opening an existing script
* (x) Mount / Unmount support in UI
* (x) Scriptable entities with Lua (fully client-side)
* (x) Pouvoir laisser les boutons enfoncés
* (x) bouton STOP qui marche
* (x) ne pas crasher si script invalide
* (x) display version number on startup
