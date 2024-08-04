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
   2. Copy the **jaket-assets.bundle** file and bundles folder from the assets folder to the mods folder.
   3. Copy the **manifest.json** file from the root folder.

## Installation
Replace your jaket.dll with the one in bin/Debug/netstandard2.0
- if using r2ModMan do the following to find your Jaket installation
   1. Go to your profile that has your Jaket installation
   2. Go to Settings > Locations > Browse Profile Folder
      - this should open it in file explorer
   3. Go to BepInEx/Plugins/xzxADIxzx-Jaket

## Troubleshooting
If your copy of modded Jaket causes all or some text to become "OH NO" do the following:
   - replace all files in (jaket installation path)/assets/bundles with the ones from this repo
