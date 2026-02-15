[![Discord](https://img.shields.io/badge/discord-server-5865F2?style=for-the-badge&logoColor=white&logo=discord     )](https://discord.gg/USpt3hCBgn                                           )
[![Stars  ](https://img.shields.io/github/stars/xzxADIxzx/Join-and-kill-em-together?style=for-the-badge&color=FF77CC)](https://github.com/xzxADIxzx/Join-and-kill-em-together                  )
[![License](https://img.shields.io/github/license/xzxADIxzx/Join-and-kill-em-together?style=for-the-badge           )](https://github.com/xzxADIxzx/Join-and-kill-em-together/blob/main/LICENSE)
[![Devlogs](https://img.shields.io/badge/dev-logs-FF0033?style=for-the-badge&logoColor=white&logo=youtube           )](https://www.youtube.com/playlist?list=PLcTAO30JMDuRpoBTAkvu2ELKDM74j43Tz)

# Join and kill 'em together
The project features multiplayer support for ULTRAKILL, which includes multiple gamemodes, from cooperative campgaing to arena-like versus. It has been developing for years and thus has the most precise synchronization and plenty of content to offer you as a player.

## Features
* Integration with Steam
   * Create public, friends-only or private lobbies
   * Invite your friends using Steam overlay or a code
   * See what your homies play with rich presence
   * Set your own rules using lobby settings
* User interface
   * Lobby config, player list and various settings
   * Indicators that help you find each other on the map
   * Browser of public lobbies
   * Chat, in case you have no other means of communication
* Interaction between players
   * Up to five teams, making both the campaign passage and versus matches possible
   * Variety of gamemodes that won't let you get bored
   * Emote wheel to tease your friends or bosses
   * Pointers to guide others in the right direction
   * Sprays, letting you upload some funny memes
   * Sam TTS Engine available through the /tts command
   * Extended coins mechanics
   * Voting system to skip cutscenes or choose dialogs
* Synchronization of everything
   * Players: their weapons & colors, arms and hook, animations, particles, explosions and even their head rotation
   * All hitscans from players and enemies
   * All projectiles: nails, sawblades, rockets and etc.
   * All sorts of items such as torches, skulls, plushies and so on
   * All sorts of actions happening during missions
   * Synchronization of enemies and their various behavior
   * Synchronization of bosses and their unique attacks
   * Synchronization of the Cyber Grind
   * Synchronization of the fishing level
* New plushies of the V2, V3 and Jaket developers
* New arts for the ~~ALL IMPERFECT LOVE SONG~~ level
* Translation into many languages
   * Portuguese by Poyozit
   * English    by xzxADIxzx
   * Filipino   by Fraku
   * French     by Theoyeah
   * German     by Doomguy
   * Italian    by Fenicemaster & sSAR
   * Polish     by Becon
   * Russian    by xzxADIxzx
   * Spanish    by NotPhobos
   * Ukrainian  by Sowler

## Installation
Before installing, it's important to know that the mod requires BepInEx to work.   
Without it, nothing will make a *beep-beep* sound.

### Mod manager
The manager of your choice will do everything for you, that's what it is for.   
The recommended one is [R2modman](https://github.com/ebkr/r2modmanPlus).

### Manual
1. Download the latest version from the [Thunderstore](https://thunderstore.io/c/ultrakill/p/xzxADIxzx/Jaket/).
2. Locate the plugins folder.
3. Extract the content of the downloaded archive into a subfolder.   
   Example: `ULTRAKILL/BepInEx/plugins/Jaket/Jaket.dll`

## Building
All you need to compile the project is .NET SDK 10.0 and Git.   
**YOU DO NOT NEED THIS IF YOU JUST WANT TO PLAY THE MOD**

1. Clone the repository with `git clone https://github.com/xzxADIxzx/Join-and-kill-em-together.git`
2. Set the correct path in the `Path.props` file.
3. Compile the project with either `dotnet build` or `./build.sh -ri`
4. Locate the files required for the mod to work. 
   1. `Jaket.dll` in the `bin/Debug/netstandard2.1/...`
   2. `assets.bundle` and `icon.png` in the `assets/...`
   3. `*.properties` in the `assets/bundles/...`
   4. `manifest.json` in the root folder.
5. Either copy them to the plugins folder or archive them for publishing.   
   The `build.sh` script does it automatically when you run `./build.sh -rid <deploy-path>`

## Afterword
If you have any questions, feel free to ping [me](https://github.com/xzxADIxzx) in our [Discord](https://discord.gg/USpt3hCBgn) server. I am very grateful to everyone who supports the project, reports bugs or suggests new ideas. Thank you!
