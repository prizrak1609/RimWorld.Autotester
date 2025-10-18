# [Autotester](https://steamcommunity.com/sharedfiles/filedetails/?id=3497629017)

![Image](https://i.postimg.cc/PJc4kLbg/Self-Info.png)

I made this to ease my testing of mods before publishing. A few people asked about it so I decided to publish it. 
If you are not a modder this is probably not of any use to you and for most modders the [Spawn Mod Content](https://steamcommunity.com/workshop/filedetails/?id=2922590693)-mod should be enough for testing.
However if you have a lot of mods, it might help with automatic testing of these mods.

### How to use



- Populate the mod-list outside of the game, with the mod you want to test at the last place in the mod-list
- Start RimWorld with the -quicktest parameter causing it to go directly to a default test map
- With this mod loaded the test-map will be smaller (100x100) to increase map-load speed
- The mod will open and close any mod-config of the test-mod
- It will will use the Spawn Mod Content-mod to spawn everything
- It will then in turn select each item spawned
- When there are no more items it will close the game



If there are any warnings or errors it will close the game instantly.
All warnings and errors will have a [Warning] or [Error] prefix in the log to find them easy.
It will also create a TranslationTemplate folder in the Source-folder of the mod.
This will contain English translation-data for all Defs as a template for other languages.

![Image](https://i.postimg.cc/cC6mj7Y4/Self-Reporting-Issues.png)



-  See if the the error persists if you just have this mod and its requirements active.
-  If not, try adding your other mods until it happens again.
-  Always post your log using the [Log Uploader](https://steamcommunity.com/sharedfiles/filedetails/?id=2873415404)
-  For best support, please use the Discord-channel for error-reporting.
-  Do not report errors by making a discussion-thread, I get no notification of that.
-  If you have the solution for a problem, please post it to the GitHub repository.
-  Use [RimSort](https://github.com/RimSort/RimSort/releases/latest) to sort your mods

 

[![Image](https://img.shields.io/github/v/release/emipa606/Autotester?label=latest%20version&style=plastic&labelColor=0070cd&color=white)](https://steamcommunity.com/sharedfiles/filedetails/changelog/3497629017) | tags:  automatic testing
