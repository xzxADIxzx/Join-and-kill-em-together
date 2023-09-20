[![Discord](https://img.shields.io/badge/discord-server-5865F2?style=for-the-badge&logo=discord&logoColor=white)](https://discord.gg/USpt3hCBgn)
[![License](https://img.shields.io/github/license/xzxADIxzx/Join-and-kill-em-together?style=for-the-badge)](https://github.com/xzxADIxzx/Join-and-kill-em-together/blob/main/LICENSE)
![Stars](https://img.shields.io/github/stars/xzxADIxzx/Join-and-kill-em-together?style=for-the-badge&logo=githubsponsors&color=EA4AAA)

# Join and kill 'em together
Multikill is still in development, so I created my own multiplayer mod for ultrakill.   
It's also in development, but with gnashing of teeth it's already playable.

## Features
* Steam integration for invitations.
* Chat, list of players and indicators to help you find each other on the map.
* Automatic check for an update, because stop playing on 0.3.0, please.
* Synchronization of player positions, their weapons and projectiles.
* Synchronization of the position and health of enemies.
* Synchronization of many triggers and doors.
* Up to 5 teams, making available both the passage of the campaign and pvp.
* Emotions wheel to tease your friends or bosses.
* SAM TTS Engine support via /tts command.

## Installation
Before installation, it's important to know that the mod needs **BepInEx** and **Ultra Mod Manager** to work.   
Without them nothing will make a *beep boop* sound.

### Mod manager
Mod manager will do everything itself, that's what the mod manager is for.

### Manual
1. Download the mod zip archive from Thunderstore.
2. Find the **UMM Mods** folder.
3. Extract the content of the archive into a subfolder.   
Example: UMM Mods/Jaket/Jaket.dll and etc.

## Bulding
To compile you need .NET SDK 6.0 and Git.

1. Clone the repository with `git clone https://github.com/xzxADIxzx/Join-and-kill-em-together.git`
   1. Run `cd <path-to-cloned-repository>`
   2. Delete the **BundleBuilder.cs** file from the assets folder, because it requires **UnityEditor.dll** but is not needed to run the mod.
2. Run `dotnet restore`
3. Create lib folder in root directory.
   1. Copy **Assembly-CSharp.dll**, **Facepunch.Steamworks.Win64.dll**, **UMM.dll**, **UnityEngine.UI.dll** and **UnityUIExtensions.dll** from `ULTRAKILL\ULTRAKILL_Data\Managed`
   2. As well as **BepInEx.dll** and **0Harmony.dll** from `ULTRAKILL\BepInEx\core`
4. Compile the mod with `dotnet build`.
5. At the output you will get the **Jaket.dll** file, which will be located in the `bin\Debug\netstandard2.0` folder.
   1. Copy this file to the mods folder.
   2. Copy the **jaket-player-doll.bundle** file from the assets folder to the mods folder.

## Afterword
The mod is still in development, so numerous bugs may occur.   
Anyway feel free to ping me on the discord **xzxADIxzx#7729** or join our [server](https://discord.gg/USpt3hCBgn).
