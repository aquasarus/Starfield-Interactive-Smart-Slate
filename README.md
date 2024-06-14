**Important:** Development on this project is currently on pause. Other stuff in real life has taken up all of my time and I also no longer have access to a copy of Starfield (game pass expired). If the upcoming DLC is good and I end up playing this game again, I may return to improve the app further. Still, if you run into problems, feel free to open an issue and I'll see what I can do :).

 # Welcome to Starfield ISS (Interactive Smart Slate)
*An Explorer's Survey Compendium, featuring a vastly higher tech Data Slate than Starfield's in-game version.*

- [This project also lives on **Nexus Mods**](https://www.nexusmods.com/starfield/mods/7074)
- [Jump to **Installation**](#installation)
- [Report a **Bug**](https://github.com/aquasarus/Starfield-Interactive-Smart-Slate/issues)
- [Make a **Suggestion**](https://github.com/aquasarus/Starfield-Interactive-Smart-Slate/discussions/categories/ideas)

## Overview
Starfield has such great potential for exploration content, but Survey Data in its current state serves as little more than a paper punch card. This app will help you gather your very own survey data in a viewable and searchable manner. The primary directive of this app (and all subsequent updates) is to help make exploration in Starfield more fun and rewarding.

### Limitations
- *Read no further if the following limitations are a dealbreaker for you.*
- **This is not a mod.** This standalone app is not connected to your Starfield game instance in any way. All data entry must be done manually, though efforts will be made to make it as easy as possible. 
    - As of now, I have not found a way to connect the app to the game. The in-game console also does not appear to expose commands related to survey data. Once Creation Kit is released, I will investigate again and hopefully find a way to finally auto-sync survey data with actual game state.
- **Windows only.** This is my first time building a Windows app and I wanted to minimize overhead of learning too many things at once. So a cross-platform mobile app is off the table for now.

### Features
- **100% free. No ads. No monetization.** Won't ever be, though I suppose I won't turn down a thank-you gift.
- **Open-source. Fully offline functionality.**
    - All your data is yours to manipulate with, [if you so choose](https://github.com/aquasarus/Starfield-Interactive-Smart-Slate/wiki/User-Data).
    - The app only uses the internet to check GitHub for updates and (optionally) log analytics data.
- <details>
    <summary>(Spoiler)</summary>
    Preserve your survey data through any number of New Game+ runs!
</details>

#### Discover solar systems
- See surface overview of planets and moons, just like in-game.
- Track planetary lifeform survey progress based on the fauna and flora you've cataloged.
- Use filters to find all planets/moons with life, or your outposts.

![Gif demo-ing discovering solar systems](https://raw.githubusercontent.com/aquasarus/Starfield-Interactive-Smart-Slate/main/Gifs/v1.2_discover_system.gif)

#### Survey lifeforms, catalog their primary resource drops, and add any notes you desire
- Auto-complete helps make manual input easier.

![Gif demo-ing surveying lifeforms](https://raw.githubusercontent.com/aquasarus/Starfield-Interactive-Smart-Slate/main/Gifs/v1.2_add_lifeform.gif)

#### Search for inorganic resources in discovered solar systems

![Gif demo-ing searching for inorganic resources](https://raw.githubusercontent.com/aquasarus/Starfield-Interactive-Smart-Slate/main/Gifs/v1.2_inorganic_search.gif)

#### Search for organic resources in fauna/flora you've cataloged

![Gif demo-ing searching for organic resources](https://raw.githubusercontent.com/aquasarus/Starfield-Interactive-Smart-Slate/main/Gifs/v1.2_organic_search.gif)

#### Conveniently save and view pictures for your fauna/flora
- Use UI + button to import a picture, or
- Drag pictures into Lifeform Overview, or
- Ctrl + V to paste from clipboard (best when combined with Windows screen cap tool `WinKey + Shift + S`).

![Gif demo-ing searching for inorganic resources](https://raw.githubusercontent.com/aquasarus/Starfield-Interactive-Smart-Slate/main/Gifs/v1.2_browse_pictures.gif)

#### Keep track of what and where your outposts are

![Gif demo-ing outpost tracking](https://raw.githubusercontent.com/aquasarus/Starfield-Interactive-Smart-Slate/main/Gifs/v1.2_track_outposts.gif)

## Installation
- Find the [latest release](https://github.com/aquasarus/Starfield-Interactive-Smart-Slate/releases) and choose:
    - **Option 1**: Download `Starfield_ISS.zip`, extract all to a folder, then run `Starfield ISS.exe`.
	    - Disclaimer: you should only do this if you trust me. I can theoretically attempt to package the executable with malicious code.
		- This app is built on Windows Presentation Foundation and uses `.NET 7.0` as a dependency. When you run the app for the first time, Windows may direct you to install it automatically.
    - **Option 2**: Download and review the source code. Compile with Visual Studio and run the application.

#### Upgrading To A Newer Version
- Updates are manual at the moment, though the app will check GitHub to compare your version with the latest release.
- To install an update, simply download and run the latest version.
- Since your data is stored [elsewhere](https://github.com/aquasarus/Starfield-Interactive-Smart-Slate/wiki/User-Data), a new version will not wipe your data.

## Instructions / Troubleshooting
- Everything in the app should be self-explanatory. Visit the [Advanced Instructions Wiki](https://github.com/aquasarus/Starfield-Interactive-Smart-Slate/wiki) for more tips!
- Found a problem? [Submit your **bug report** here.](https://github.com/aquasarus/Starfield-Interactive-Smart-Slate/issues)

## Upcoming Features
Here are some potential features in the distant future:
- **Cataloging planet/moon traits.** I need to work on obtaining a comprehensive list of traits first.
- **Cataloging secondary (chance-based) resource drops for fauna.**
- **Prefilled 100% mode** for users who prefer to use this app as a wiki. I will add this when I am more confident with the accuracy of my default database.
- **More complex outpost planning functions** are already available on [Frederik's spreadsheet](https://www.reddit.com/r/Starfield/comments/16g54cy/starfield_complete_list_of_resources_for_every/), but if there is demand I could add them in this app too.
- **Immersive game-like UI**. This is a big one and I'll probably only do it if more than a handful of people are using the app. It'd be cool if the app looks like you're actually using a data slate.

Have an idea? [Start a discussion](https://github.com/aquasarus/Starfield-Interactive-Smart-Slate/discussions/categories/ideas) to let me know!

## Credits
- Frederik, whose [massive data spreadsheet](https://www.reddit.com/r/Starfield/comments/16g54cy/starfield_complete_list_of_resources_for_every/) helped populate this app's default database.
- This app uses the [SofiaSans](https://fonts.google.com/specimen/Sofia+Sans) font.
- This app is not officially affiliated with Starfield / Bethesda. All intellectual property rights belong to Bethesda.
