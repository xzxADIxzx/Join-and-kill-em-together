# The mod is currently being worked on to make it compatible with the ULTRA_REVAMP update, do not make issues regarding this. If you are still wishing to play, read the [installation](#installation) section below for instructions on how to downgrade ultrakill to patch 15d.

[![Discord](https://img.shields.io/badge/discord-server-5865F2?style=for-the-badge&logoColor=white&logo=discord)](https://discord.gg/USpt3hCBgn)
[![PayPal ](https://img.shields.io/badge/support%20on-paypal-003087?style=for-the-badge&logoColor=white&logo=paypal)](https://www.paypal.com/donate/?hosted_button_id=U5T68JC5LWEMU)
[![Coffee ](https://img.shields.io/badge/buy%20me%20a-coffee-FFDD00?style=for-the-badge&logoColor=white&logo=buymeacoffee)](https://www.buymeacoffee.com/adithedev)
[![License](https://img.shields.io/github/license/xzxADIxzx/Join-and-kill-em-together?style=for-the-badge)](https://github.com/xzxADIxzx/Join-and-kill-em-together/blob/main/LICENSE)
[![Stars  ](https://img.shields.io/github/stars/xzxADIxzx/Join-and-kill-em-together?style=for-the-badge&color=EA4AAA)](https://github.com/xzxADIxzx/Join-and-kill-em-together)
[![DevLogs](https://img.shields.io/badge/dev-logs-FF0033?style=for-the-badge&logoColor=white&logo=youtube)](https://www.youtube.com/playlist?list=PLcTAO30JMDuRpoBTAkvu2ELKDM74j43Tz)

# Join and kill 'em together
This modification made by [me](https://github.com/xzxADIxzx) and my team adds support for multiplayer via Steamworks to ULTRAKILL. The idea to create this project came to me immediately after completing the game in a week, and since MULTIKILL is still in development, nothing stopped me from speedrunning programming.

## Features
* Integration with Steam
   * Public, friends only and private lobbies
   * Invitations via Steam or lobby code
   * Rich Presence
   * Lobby settings
* Automatic check for updates
* User interface
   * Lobby menu, player list and settings
   * Player indicators to help you find each other on the map
   * Information about teammates: their health and railgun charge
   * List of public lobbies so you never get bored
   * Chat, in case you have no other means of communication
   * Interactive guide to help you understand the basics
* Interaction between players
   * Up to 5 teams, making available both the passage of the campaign and PvP
   * Emote wheel to tease your friends or bosses
   * Pointers to guide your friends in the right direction
   * SAM TTS Engine for speaking messages via /tts command
   * Sprays and moderation system for them
   * Extended V2 coins mechanic
   * Voting system to skip cutscenes or choose dialogs
* Synchronization of everything
   * Players, their weapons, weapons paint, fist, hook, animations, particles and even head rotation
   * All projectiles in the game and chargeback damage
   * All sorts of items such as torches, skulls and developer plushies
   * Synchronization of position and attacks of enemies
   * Synchronization of special bosses such as Leviathan, Minos' hand and Minotaur
   * Synchronization of different triggers at levels
   * Synchronization of the Cyber Grind
   * Synchronization of 2-S and 5-S
* New plushies of V2, V3 and Jaket developers
* New arts for the ~~ALL IMPERFECT LOVE SONG~~ level
* Translation into many languages
   * Arabic        by Iyad
   * Portuguese    by Poyozit
   * English
   * Filipino      by Fraku
   * French        by Theoyeah
   * Italian       by sSAR, Fenicemaster
   * Polish        by Sowler
   * Russian
   * Spanish       by NotPhobos
   * Ukrainian     by Sowler

## Installation
Before installing, it's important to know that the mod requires **BepInEx** to work.  
Without it, nothing will make a *beep-beep* sound.

### Install patch 15d
This only works if you own the game on Steam.   
If you are using Linux, you must be clever enough to do it yourself.

1. Press `Win+R` and type `steam://open/console` - this will open Steam with a new console tab that only has a bar to write commands.
2. Input `download_depot 1229490 1229491 6666720363269022893` to the bar; once it prints `Depot download complete` along with the path of the downgraded game, you may proceed further.
3. Navigate to the folder with the downgraded game (use the path from the console). If you haven't changed the Steam directory, it should be `C:\Program Files (x86)\Steam\steamapps\content\app_1229490\depot_1229491`
4. Now you can install Jaket directly to that folder or copy its content to downpatch your ULTRAKILL installation.

### Mod manager
Your mod manager will do everything itself, that's what mod managers are for.  
Personally, I recommend [r2modman](https://github.com/ebkr/r2modmanPlus).

### Manual
1. Download the mod zip archive from [Thunderstore](https://thunderstore.io/c/ultrakill/p/xzxADIxzx/Jaket).
2. Find your plugins folder.
3. Extract the content of the archive into a subfolder.  
   Example: `BepInEx/plugins/Jaket/Jaket.dll`

## Building
To compile you will need .NET SDK 6.0 and Git.  
**Important**: You don't need this if you just want to play with the mod.

1. Clone the repository with `git clone https://github.com/xzxADIxzx/Join-and-kill-em-together.git`
   1. Run `cd <path-to-cloned-repository>`
2. Make sure to set the correct game path in the `Path.props` file.
3. Compile the mod with `dotnet build`
4. At the output you will get the **Jaket.dll** file, which will be located in the `bin\Debug\netstandard2.0` folder.
   1. Copy this file to the mods folder.
   2. Copy the **jaket-assets.bundle** file and bundles folder from the assets folder to the mods folder.
   3. Copy the **manifest.json** file from the root folder.

## Afterword
I fix bugs all the time, but some of them are hidden from me.  
Anyway feel free to ping me on Discord **xzxADIxzx** or join our [server](https://discord.gg/USpt3hCBgn).

I am very grateful to all those who supported me during development. Thank you!  
Cheers~ ♡
