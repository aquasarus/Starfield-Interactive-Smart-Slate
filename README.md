# Welcome to Starfield ISS (Interactive Smart Slate)
*An Explorer's Survey Compendium, featuring a vastly higher tech Data Slate than Starfield's in-game version.*

## Overview
Starfield has such great potential for exploration content, but Survey Data in its current state serves as little more than a paper punch card. This app will help you gather your very own survey data in a viewable and searchable manner. The primary directive of this app (and all subsequent updates) is to help make exploration in Starfield more fun and rewarding.

### Limitations
- *Read no further if the following limitations are a dealbreaker for you.*
- **This is not a mod.** This standalone app is not connected to your Starfield game instance in any way. All data entry must be done manually, though efforts will be made to make it as easy as possible. 
- **Windows only.** This is my first time building a Windows app and I wanted to minimize overhead of learning too many things at once. So a cross-platform mobile app is off the table for now.

### Features
- **100% free. No ads. No monetization.** Won't ever be, though I suppose I won't turn down a thank-you gift.
- **Open source. Fully offline.** All your data is yours to manipulate with, if you so choose (see [instructions](#installation--instructions) below for where your data is).

#### Discover solar systems
- See surface overview of planets and moons, just like in-game.
- Track planetary lifeform survey progress based on the fauna and flora you've cataloged.  

![Gif demo-ing discovering solar systems](https://raw.githubusercontent.com/aquasarus/Starfield-Interactive-Smart-Slate/main/Gifs/discover-system.gif)

#### Survey lifeforms, catalog their primary resource drops, and add any notes you desire
- Auto-complete helps make manual input easier.

![Gif demo-ing surveying lifeforms](https://raw.githubusercontent.com/aquasarus/Starfield-Interactive-Smart-Slate/main/Gifs/add-lifeform-2.gif)

#### Search for inorganic resources in discovered solar systems

![Gif demo-ing searching for inorganic resources](https://raw.githubusercontent.com/aquasarus/Starfield-Interactive-Smart-Slate/main/Gifs/inorganic-resource-search.gif)

#### Search for organic resources in fauna/flora you've cataloged

![Gif demo-ing searching for organic resources](https://raw.githubusercontent.com/aquasarus/Starfield-Interactive-Smart-Slate/main/Gifs/organic-resource-search-2.gif)

## Installation / Instructions
- Find the [latest release](https://github.com/aquasarus/Starfield-Interactive-Smart-Slate/releases) and choose one of the following options:
    - Option 1: Download `Starfield_ISS.zip`, extract all to a folder, then run `Starfield ISS.exe`.
	    - Disclaimer: you should only do this if you trust me. I can theoretically attempt to package the executable with malicious code.
		- This app is built on Windows Presentation Foundation and uses `.NET 7.0` as a dependency. When you run the app for the first time, Windows may direct you to install it automatically.
    - Option 2: Download and review the source code. Compile with Visual Studio and run the application.
- Everything should be pretty self-explanatory. If something is unclear or broken, [open an issue](https://github.com/aquasarus/Starfield-Interactive-Smart-Slate/issues) to let me know!
- All your survey data will be stored in `/<username>/AppData/local/Starfield_ISS/DataSlate.db` (SQLite)
    - Some prefilled default data will inevitably contain errors. They will be fixed via backwards-compatible app updates.
    - You may, of course, edit the DB file directly. I only recommend this if you know how SQLite works and how it can affect app compatibility. You could permanently break your app state by doing this.

## Regarding App Updates
- Updates are manual at the moment. The app will not connect to the internet to check or install anything.
- To install an update, simply download the latest version.
- Since your survey data is stored in a user-specific folder (see above), the new version will not wipe your data.

## Upcoming Features
- **Storing screenshots/photos for your fauna/flora.**
- **Cataloging planet/moon traits.** I need to work on obtaining a comprehensive list of traits first.
- **Cataloging secondary (chance-based) resource drops for fauna.**
- **Prefilled 100% mode** for users who prefer to use this app as a wiki. I will add this when I am more confident with the accuracy of my default database.
- **More complex outpost planning functions** are already available on [Frederik's spreadsheet](https://www.reddit.com/r/Starfield/comments/16g54cy/starfield_complete_list_of_resources_for_every/), but if there is demand I could add them in this app too.
- **Immersive game-like UI**. This is a big one and I'll probably only do it if more than a handful of people are using the app. It'd be cool if the app looks like you're actually using a data slate.
- [Add your own **suggestions/bug reports** here.](https://github.com/aquasarus/Starfield-Interactive-Smart-Slate/issues)

## Credits
- Frederik, whose [massive data spreadsheet](https://www.reddit.com/r/Starfield/comments/16g54cy/starfield_complete_list_of_resources_for_every/) helped populate this app's default database.
- This app uses the [SofiaSans](https://fonts.google.com/specimen/Sofia+Sans) font.
- This app is not officially affiliated with Starfield / Bethesda. All intellectual property rights belong to Bethesda.