# BeatSaberSongLoader
A plugin for adding custom songs into Beat Saber.

*This mod works on both the Steam and Oculus Store versions.*

## Installation Instructions
 1. Download the latest release from the modsaber page [Here](https://www.modsaber.org/mod/song-loader/)
 2. Extract the .zip file into the `Oculus Apps\Software\hyperbolic-magnetism-beat-saber` for Oculus Home OR `steamapps\common\Beat Saber` for Steam. (The one with Beat Saber.exe)
  
    The Beat Saber folder should look something like this:
    * `Beat Saber_Data`
    * `CustomSongs`
    * `IPA`
    * `Plugins`
    * `Beat Saber (Patch & Launch)`
    * `Beat Saber.exe`
    * `IPA.exe`
    * `Mono.Cecil.dll`
    * `UnityPlayer.dll`
 3. Done!

## Usage
 1. Launch Beat Saber through the platform you purchased it on.	
 2. Go to 'Solo' -> 'Standard' and your custom song will be available to play at the bottom of the list.	


## Installing Custom Songs
The following files must be placed within their own folder inside the "CustomSongs" folder.

    Required files:
		1. cover.jpg (Size 256x256)
			-This is the picture shown next to song in the selection screen.
			-The name can be whatever you want, make sure its the same as the one found in info.json
			-Only supported image types are jpg and png
		2. song.wav / song.ogg
			-This is your song you would like to load
			-Name must be the same as in info.json
			-Only supported audio types are wav and ogg
		3. easy.json / normal.json / hard.json / expert.json
			-This is the note chart for each difficulty
			-Names must match the "jsonPath" in info.json
			-Use a Beat Saber editor to make your own note chart for the song
		4. info.json
			-Contains the info for the song

The following is a template for you to use:
```json
{
  "songName":"YourSongName",
  "songSubName":"ft. Name",
  "songAuthorName":"AuthorName",
  "mappers":["Name One", "Name Two"],
  "lighters":["Name One", "Name Two"],
  "beatsPerMinute":179.0, 
  "previewStartTime":12.0,
  "previewDuration":10.0,
  "audioPath":"YourSong.ogg",
  "coverImagePath":"cover.jpg",
  "environmentName":"DefaultEnvironment",
  "customEnvironment": "Platform Name",
  "customEnvironmentHash": "<platform's ModelSaber md5sum hash>",
  "songTimeOffset":-2,
  "shuffle":1,
  "shufflePeriod":0.2,
  "oneSaber":true,
  "difficultyLevels": [
	{ "difficulty":"Expert", "difficultyRank":4, "jsonPath":"expert.json" },
	{ "difficulty":"Easy", "difficultyRank":0, "jsonPath":"easy.json", "characteristic":0 }
  ]
}
```
___

### info.json Explanation
```
"songName" - Name of your song
"songSubName" - Text rendered in smaller letters next to song name. (ft. Artist)
"mappers" - People who contributed to mapping for the various difficulties of the map to be listed in the additional information display
"lighters" - People who contributed to lighting for the various difficulties of the map to be listed in the additional information display
"beatsPerMinute" - BPM of the song you are using
"previewStartTime" - How many seconds into the song the preview should start
"previewDuration" - Time in seconds the song will be previewed in selection screen
"coverImagePath" - Cover image name
"environmentName" - Game environment to be used
"customEnvironment" - Custom Platform override, will use "environmentName" if CustomPlatforms isn't installed or disabled
"customEnvironmentHash" - The hash found on ModelSaber, used to download missing platforms
"songTimeOffset" - Seems to be obsolete. Do not use.
"shuffle" - Time in number of beats how much a note should shift
"shufflePeriod" - Time in number of beats how often a note should shift. Don't ask me why this is a feature, I don't know
"oneSaber" - true or false if it should appear in the one saber list 
(If the WHOLE map is only one saber! For one saber difficulties use the characteristic in the difficultylevels)

All possible environmentNames:
-DefaultEnvironment
-BigMirrorEnvironment
-TriangleEnvironment
-NiceEnvironment
-KDAEnvironment

"difficultyLevels": [
	{
		"difficulty": This can only be set to Easy, Normal, Hard, Expert or ExpertPlus,
		"difficultyRank": Currently unused whole number for ranking difficulty,
		"jsonPath": The name of the json file for this specific difficulty,
		"characteristic": The mode that you want the specific difficulty to be under
		 0 for standard, 1 for one saber, 2 for no arrows
	}
  ]
```

### Difficulty json additional fields, (i.e. expert.json,  etc.)
- Refer to Modders section for a list of common capabilites that mods register
```json
"_warnings":["Warning1","Warning2","WarningX"],   - Any warnings you would like the player to be aware of before playing the song

"_information":["Thing1","Thing2","ThingX"],   - Any general information you would like the player to be aware of before playing the song

"_suggestions":["Mod1","Mod2","ModX"], - Any Mods to suggest the player uses for playing the song, must be supported by the mod in question otherwise the player will constantly be informed they are missing suggested mod(s)

"_requirements":["Mod1","Mod2","ModX"], - Any Mods to require the player has before being able to play the song, must be supported by mod in question otherwise song will simply not be playable

"_colorLeft":{"r":1, "g":1, "b":1}, - A color to override the left color to if the player has custom song colors enabled, color range for r,g, and b is a 0-1 scale, not 0-255 scale 

"_colorRight":{"r":1, "g":1, "b":1}, - A color to override the left color to if the player has custom song colors enabled, color range for r,g, and b is a 0-1 scale, not 0-255 scale

If using either color override you must add a color for both sides
Default colors are approximately
Left: 1,0,0
Right, 0, 0.706, 1


"_noteJumpStartBeatOffset":1, - Set the noteJumpStartBeatOffset for the song, default value is 0 if not implemented
```
### For modders
 * You can add/remove capabilities to your mods for maps to be able to use by doing the following
 ```csharp
 // To register
 SongLoaderPlugin.SongLoader.RegisterCapability("Capability name");
 // To remove
 SongLoaderPlugin.SongLoader.DeregisterizeCapability("Capability name");
 ```
#### Capabilities
- Note: Songloader currently auto detects Precision Placement, Extra Note Angles, and More Lanes. Other features of mapping extensions require you to add the "Mapping Extensions" Capability as a requirement for your song, and it is advised if you use any of the capabilities of a mpod, you assume it will not be auto added and add the capability to the JSON.

| Capability | Mod |
| - | - |
| "Mapping Extensions"| Mapping Extensions |
| "Mapping Extensions-Precision Placement"| Mapping Extensions |
| "Mapping Extensions-Extra Note Angles"| Mapping Extensions |
| "Mapping Extensions-More Lanes"| Mapping Extensions |
| "Chroma"| Chroma |
| "Chroma Lighting Events"| Chroma |
| "Chroma Special Events"| Chroma |
# Keyboard Shortcuts
*(Make sure Beat Saber's window is in focus when using these shortcuts)*
---
 * Press <kbd>Ctrl+R</kbd> when in the main menu to do a full refresh. (This means removing deleted songs and updating existing songs)
 * Press <kbd>R</kbd> when in main menu to do a quick refresh (This will only add new songs in the CustomSongs folder)
