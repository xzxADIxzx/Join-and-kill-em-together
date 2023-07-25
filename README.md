# Join and kill 'em together
Multikill is still in development, so I created my own multiplayer mod for ultrakill.   
It's also in development, but with gnashing of teeth it's already playable.

## Features
* Steam integration for invitations.
* Chat, list of players and indicators to help you find each other on the map.
* Synchronization of player positions, their weapons and projectiles.
* Synchronization of the position and health of enemies.
* Up to 5 teams, making available both the passage of the campaign and pvp.

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
2. Run `dotnet restore`
3. Create a folder `lib` and put the `Assembly-CSharp.dll`, `Facepunch.Steamworks.Win64.dll`, `UMM.dll` and `UnityEngine.UI.dll` from `ULTRAKILL\ULTRAKILL_Data\Managed` folder.
4. Compile the mod with `dotnet build`.
5. At the output you will get the **Jaket.dll** file, which must be placed in the mods folder.

## Afterword
The mod is still in development, so numerous bugs may occur.   
Anyway feel free to ping me on the discord **xzxADIxzx#7729** or join our [server](https://discord.gg/USpt3hCBgn).