# BeatSaberSongLoader
A plugin for adding custom songs into Beat Saber.

*This mod works on both the Steam and Oculus Store versions.*

## Installation Instructions
 1. Download the latest release
 2. Extract the .zip file into the `Oculus Apps\Software\hyperbolic-magnetism-beat-saber` for Oculus Home OR `steamapps\common\Beat Saber` for Steam. (The one with Beat Saber.exe)
  
    The Beat Saber folder should look something like this:
    * `Beat Saber_Data`
    * `CustomSongs`
    * `IPA`
    * `Plugins`
    * `WIP Songs`
    * `Beat Saber (Patch & Launch)`
    * `Beat Saber.exe`
    * `IPA.exe`
    * `Mono.Cecil.dll`
    * `UnityPlayer.dll`
 3. Done!

## Usage
 1. Launch Beat Saber through the platform you purchased it on.	
 2. Go to 'Solo' and scroll to the right on the song pack selection view to the `Custom Maps` Pack and select it to view your songs	


## Installing Custom Songs
- The following files must be placed within their own folder inside the "CustomSongs" folder.
- You Can place songs in the `WIP Songs` Folder instead to place them in the WIP Maps songpack and have them only be playable in practice mode, this is recommended if you are either making the map yourself or testing someone else's map
- If a duplicate song is in both the `CustomSongs` and `WIP Songs` folder it will only show in the Custom Maps pack
``` 
   Required files:
		1. cover.jpg (Size 256x256)
			-This is the picture shown next to song in the selection screen.
			-The name can be whatever you want, make sure its the same as the one found in info.json
			-Only supported image s are jpg and png
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
```
The following is a template for you to use:
```json
{
  "songName":"YourSongName",
  "songSubName":"ft. Name",
  "authorName":"AuthorName",
  "beatsPerMinute":179.0, 
  "previewStartTime":12.0,
  "previewDuration":10.0,
  "audioPath":"YourSong.ogg",
  "coverImagePath":"cover.jpg",
  "environmentName":"DefaultEnvironment",
  "customEnvironment": "Platform Name",
  "customEnvironmentHash": "<platform's ModelSaber md5sum hash>",
  "shuffle":1,
  "shufflePeriod":0.2,
  "difficultyLevels": [
	{ "difficulty":"Expert", "difficultyRank":4, "jsonPath":"expert.json" },
	{ "difficulty":"Easy", "difficultyRank":0, "jsonPath":"easy.json", "characteristic":"Standard", "difficultyLabel":"EX" }
  ],
  "contributors": [
	{	"role": "Kirb",	"name": "Kyle 1413", "iconPath": "test.png" },
	{	"role": "Lighter","name": "Kyle 1413 The Second", "iconPath": "test.png"}
  ]
  
}
```
___

### info.json Explanation
```json
"songName" - Name of your song
"songSubName" - Text rendered in smaller letters next to song name. (ft. Artist)
# Mappers/lighters fields being renovated, do not use
"beatsPerMinute" - BPM of the song you are using
"previewStartTime" - How many seconds into the song the preview should start
"previewDuration" - Time in seconds the song will be previewed in selection screen
"coverImagePath" - Cover image name
"environmentName" - Game environment to be used
"customEnvironment" - Custom Platform override, will use "environmentName" if CustomPlatforms isn't installed or disabled
"customEnvironmentHash" - The hash found on ModelSaber, used to download missing platforms
"shuffle" - Time in number of beats how much a note should shift
"shufflePeriod" - Time in number of beats how often a note should shift. Don't ask me why this is a feature, I don't know
# Instead of the legacy one saber setting just use characteristics for each difficulty

All possible environmentNames:
-DefaultEnvironment
-BigMirrorEnvironment
-TriangleEnvironment
-NiceEnvironment
-KDAEnvironment
-MonstercatEnvironment
```
```json
"difficultyLevels"
		"difficulty" - This can only be set to Easy, Normal, Hard, Expert or ExpertPlus,
		"difficultyRank" - Currently unused whole number for ranking difficulty,
		"jsonPath" - The name of the json file for this specific difficulty,
		"characteristic" - What section you want the difficulty to be listed under, refer to bottom of readme for usage
		"difficultyLabel" - The name to display for the difficulty in game
		 Note: Difficulty labels affect Every difficulty of the same type(Easy, Normal, etc) 
		 for that song, if you have two difficulty labels
		 for a single type, only one will take effect
		 This will also not affect how the difficulty displays on scoresaber or elsewhere
	
"contributors"
		"role" - The role the person had, i.e. Mapper, Lighter, Inspiration
		"name" - The name of the person
		"iconPath" - path to the icon to use for the person within the song folder i.e. icon.png
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
 * Extra data contained in songs that are not part of the base game, i.e. requirements, colors, custom platforms, etc etc can be accessed using SongCore.
 ```csharp
 //To retrieve the data for a song
 SongCore.Collections.RetrieveExtraSongData(string levelID, string loadIfNullPath = "");
 //This will return the ExtraSongData for the given levelID if it finds it, and if it does not find it, it will attempt
 // To load it using the loadIfNullPath if one is provided, otherwise it will return null

 //You can also use the below function to specifically get the DifficultyData for a song, which contains information such as requirements that difficulty has
   SongCore.Collections.RetrieveDifficultyData(IDifficultyBeatmap beatmap)
  // This will attempt to retrieve the DifficultyData, and return null if it does not find one
 
 //You can also use this function to manually load and add the ExtraSongData for a song by giving it the levelID and path which can be found in the CustomSongInfo for a song
 SongCore.Collections.AddSong(string levelID, string path, bool replace = false)
 // If replace is true it will update an existing ExtraSongData for that song if one is present, otherwise if one already exists it will do nothing
 ```
 * You can add/remove capabilities to your mods for maps to be able to use by doing the following
 * You can register a beatmap characteristic OnApplicationStart by doing the following **Make sure to do this before songloader loads songs**
 ```csharp
 // To register
 SongCore.Collections.RegisterCapability("Capability name");
 // To remove
  SongCore.Collections.DeregisterizeCapability("Capability name");
 
 //If you make a mod that registers a capability feel free to message me on Discord ( Kyle1413#1413 ) and I will add it to the list below
 ```
 
 ```csharp
 SongCore.Collections.RegisterCustomCharacteristic(Sprite Icon, "Characteristic Name", "Hint Text", "SerializedName", "CompoundIdPartName");
//For the SerializedName and CompoundIdPartName, as a basic rule can just put the characteristic name without spaces or special characters
//The Characteristic Name will be what mappers put as the characteristic when labelling their difficulties
//If you make a mod that registers a characteristic feel free to message me on Discord ( Kyle1413#1413 ) and I will add it to the list below

 ```
#### Capabilities
- Note: Make sure to add a requirement if your map uses Capabilities from a mod that are not compatible with the base game! Example being anything from Mapping Extensions

| Capability | Mod |
| - | - |
| "Mapping Extensions"| Mapping Extensions |
| "Mapping Extensions-Precision Placement"| Mapping Extensions |
| "Mapping Extensions-Extra Note Angles"| Mapping Extensions |
| "Mapping Extensions-More Lanes"| Mapping Extensions |
| "Chroma"| Chroma |
| "Chroma Lighting Events"| Chroma |
| "Chroma Special Events"| Chroma |

#### Beatmap Characteristics
- These control what difficulty set the difficulty is placed under, so if you wanted to include a set of 5 difficulties that were all one saber in addition to a normal set of 5 difficulties, similar to what the OST does, you would give all of the one saber difficultyLevels a characteristic of one saber

| Characteristic | Source |
| - | - |
| "Standard"| Base Game |
| "No Arrows"| Base Game |
| "One Saber"| Base Game |
| "Lawless"| SongLoader |
| "Lightshow"| SongLoader |

# Keyboard Shortcuts
*(Make sure Beat Saber's window is in focus when using these shortcuts)*
---
 * Press <kbd>Ctrl+R</kbd> when in the main menu to do a full refresh. (This means removing deleted songs and updating existing songs)
 * Press <kbd>R</kbd> when in main menu to do a quick refresh (This will only add new songs in the CustomSongs folder)
