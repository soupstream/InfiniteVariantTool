# Infinite Variant Tool

This is a tool for working with Halo Infinite's game variants and map variants.

## Features

* Extract variants from Halo Infinite (Steam version) and add variants to the game.
* Enable and disable variants in the Custom Games menu.
* Convert binary variant files to and from XML.
* Pack and unpack bundled Lua scripts.
* Includes a graphical user interface and a command line interface.

## Installation

Download and unzip one of the files from the [latest release](https://github.com/soupstream/InfiniteVariantTool/releases/latest). There are two options:

* InfiniteVariantTool_vX.Y.Z_selfcontained.zip: Contains everything it needs to run, but is larger in size.
* InfiniteVariantTool_vX.Y.Z.zip: Smaller executable which requires the [.NET 6.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0/runtime).
  If you don't have it installed, the program will prompt you to visit the download page, where you should download and install the x64 desktop runtime.

You can put the extracted files anywhere you want and run InfiniteVariantTool.exe.

## How to install and play variants

* To install a variant, click *File > Install variant...* and select the zip file containing the variant.
  * Note: variants will be overwritten each game update. To add them back, select them in *My Variants*, check the *Enabled* box, and click *Apply changes*.
* Launch the game and go to Custom Games. Set the server to *Local Offline* or *Local Area Network* and select your variant.
* Note that while online, the asset/version ID of the variant is checked server-side so the lobby won't load it if it doesn't exist on 343's servers.
  * However, you can force the lobby offline by switching the server to *Local Area Network*, joining the newly created LAN server, then switching back to *Local Offline*.
  * Alternatively, disconnect your PC from the internet before launching the game and play offline.
* Other players on your local network can join your modded LAN games (no need for them to install the modded variants).
  You might be able to join LAN games remotely using virtual LAN software like Hamachi, but I haven't tried this myself.

## How to modify variants

* Switch to the *All Variants* tab and select a variant.
* Basic edits (name, description, etc.) can be made in the GUI.
* Extract a variant to edit XML and Lua files.
* Use *Tools > Pack bond files...* and *Tools > Pack Lua bundle...* to repack your changes.
* Zip a variant folder to share it. You can include multiple variants in the zip file, and it doesn't matter which subfolder they're in.
* To save your changes into the game after editing variant files, select it in *My Variants* and click *Reinstall*.

## Building from source

Open the solution in Visual Studio 2022, and it should build without any extra steps.

## Q&A

### Can I play modded variants online?

Modded variants only work in offline and LAN matches. Online matches run serverside variants which can't be modified.

### Will this get me banned?

I haven't had any issues playing modded variants in Custom Games, but I can't guarantee that you won't get banned. Use at your own risk.

### Does this work with the PC Game Pass version of Halo Infinite?

Only the Steam version is supported, but the next answer also applies to PC Game Pass.

### Does this work on Xbox?

Sort of. You can't install modded variants directly on Xbox, but you can join a modded LAN game hosted on PC.

### My Halo Infinite installation got messed up. How do I fix it?

Delete the *disk_cache* and *server_disk_cache* folders from your Halo Infinite game folder.
They are recreated automatically when you play the game online.
If you're still having issues, run *Verify integrity of game files...* on Halo Infinite in Steam.