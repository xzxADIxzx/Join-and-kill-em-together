# How To Build Assets
Small tips and instructions on how to correctly import assets into a Unity project for subsequent assembly into an asset bundle are listed here.
*Please note that all textures use sRGB color space.*

## Preparations
1. Create a new project on Unity version 2022.3.29, all the work is to be done there from now.
2. Create a subfolder called `Editor` inside the assets folder.
3. Copy the **BundleBuilder.cs** into the new folder.

## Common
1. Copy the font file.
2. Copy the shop and bestiary entries.
3. Add to the bundle via the bottom window.

## Chan
1. Copy the corresponding folder.
2. Configure all of the textures:
   * Set the texture type to `Sprite`
   * Set the maximum size to `2048`
   * Set the filter mode to `Bilinear`
   * Set the compression to `High Quality`
   * Add to the bundle via the bottom window.

## Icons
1. Copy the corresponding folder.
2. Configure all of the textures:
   * Set the texture type to `Sprite`
   * Set the maximum size to `1024`
   * Set the filter mode to `Bilinear`
   * Set the compression to `High Quality`
   * Add to the bundle via the bottom window.
3. Configure all of the shop icons:
   * Set the filter mode to `Point`
   * Set the compression to `None`

## Plushies
1. Export the corresponding models as fbx.
2. Configure all of the textures:
   * Set the texture type to `Default`
   * Set the maximum size to `1024`
   * Set the filter mode to `Point`
   * Set the compression to `High Quality`

## Variants
1. Copy the corresponding folder.
2. Configure all of the textures:
   * Set the texture type to `Default`
   * Set the maximum size to `1024`
   * Set the filter mode to `Point`
   * Set the compression to `High Quality`
   * Add to the bundle via the bottom window.
