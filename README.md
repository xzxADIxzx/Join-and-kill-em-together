[![Discord](https://img.shields.io/badge/discord-server-5865F2?style=for-the-badge&logo=discord&logoColor=white)](https://discord.gg/USpt3hCBgn)
[![Support](https://img.shields.io/badge/Buy%20Me%20a-Coffee-FFDD00?style=for-the-badge&logo=buymeacoffee)](https://www.buymeacoffee.com/adidev)
[![License](https://img.shields.io/github/license/xzxADIxzx/Join-and-kill-em-together?style=for-the-badge)](https://github.com/xzxADIxzx/Join-and-kill-em-together/blob/main/LICENSE)
[![Stars](https://img.shields.io/github/stars/xzxADIxzx/Join-and-kill-em-together?style=for-the-badge&logo=githubsponsors&color=EA4AAA)](https://github.com/xzxADIxzx/Join-and-kill-em-together)
[![Devlogs](https://img.shields.io/badge/dev-logs-FF0000?style=for-the-badge&logo=youtube)](https://www.youtube.com/playlist?list=PLcTAO30JMDuRpoBTAkvu2ELKDM74j43Tz)

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
   * Information about teammates: their health and rail charge
   * List of public lobbies so you never get bored
   * Chat, in case you have no other means of communication
   * Interactive guide to help you understand the basics
* Interaction between players
   * Up to 5 teams, making available both the passage of the campaign and PvP
   * Emotions wheel to tease your friends or bosses
   * Pointers to guide your friends in the right direction
   * SAM TTS Engine for speaking messages via /tts command
   * Sprays and moderation system for them
   * Extended V2 coins mechanic
* Synchronization of everything
   * Players, their weapons, weapons paint, fists, hook, animations, particles and even head rotation
   * All projectiles in the game and chargeback damage
   * All sorts of items such as torches, skulls and developer plushies
   * Synchronization of position and attacks of enemies
   * Synchronization of special bosses such as Leviathan, Minos' hand and Minotaur
   * Synchronization of different triggers at levels
   * Synchronization of the Cyber Grind
* Translation into many languages
   * Arabic
   * Portuguese
   * English
   * Filipino
   * French
   * Italian
   * Polish
   * Russian
   * Spanish
   * Ukrainian

## Installation
Before installing, it's important to know that the mod requires **BepInEx** to work.  
Without it, nothing will make a *beep-beep* sound.

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
2. Run `dotnet restore`
3. Create lib folder in root directory.
   1. Copy **Assembly-CSharp.dll**, **Facepunch.Steamworks.Win64.dll**, **plog.dll**, **Unity.Addressables.dll**, **Unity.ResourceManager.dll**, **Unity.TextMeshPro.dll**, **UnityEngine.UI.dll** and **UnityUIExtensions.dll** from `ULTRAKILL\ULTRAKILL_Data\Managed`
   2. As well as **BepInEx.dll** and **0Harmony.dll** from `ULTRAKILL\BepInEx\core`
4. Compile the mod with `dotnet build`
5. At the output you will get the **Jaket.dll** file, which will be located in the `bin\Debug\netstandard2.0` folder.
   1. Copy this file to the mods folder.
   2. Copy the **jaket-player-doll.bundle** file and bundles folder from the assets folder to the mods folder.
   3. Copy the **manifest.json** file from the root folder.

## Afterword
I fix bugs all the time, but some of them are hidden from me.  
Anyway feel free to ping me on Discord **xzxADIxzx** or join our [server](https://discord.gg/USpt3hCBgn).

I am very grateful to all those who supported me during development. Thank you!  
Cheers~ ♡
