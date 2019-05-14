﻿using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SongCore.Data;
using Newtonsoft.Json;
using UnityEngine;
using SongCore.Utilities;

namespace SongCore
{
    public static class Collections
    {
        internal static string dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"..\LocalLow\Hyperbolic Magnetism\Beat Saber\SongCoreExtraData.dat");
        internal static Dictionary<string, ExtraSongData> customSongsData = new Dictionary<string, ExtraSongData>();

        private static List<string> _capabilities = new List<string>();
        public static System.Collections.ObjectModel.ReadOnlyCollection<string> capabilities
        {
            get { return _capabilities.AsReadOnly(); }
        }

        private static List<BeatmapCharacteristicSO> _customCharacteristics = new List<BeatmapCharacteristicSO>();
        public static System.Collections.ObjectModel.ReadOnlyCollection<BeatmapCharacteristicSO> customCharacteristics
        {
            get { return _customCharacteristics.AsReadOnly(); }
        }


        public static void AddSong(string levelID, string path, bool replace = false)
        {
            
            if (!customSongsData.ContainsKey(levelID))
                customSongsData.Add(levelID, new ExtraSongData(levelID, path));
            else
            {
                if (replace)
                {
                    customSongsData[levelID].UpdateData(path);
               //     customSongsData.Add(levelID, new ExtraSongData(levelID, path));
                }
            }
   //         Utilities.Logging.Log("Entry: :"  + levelID + "    " + customSongsData.Count);
        }

        public static ExtraSongData RetrieveExtraSongData(string levelID)
        {
            if (customSongsData.ContainsKey(levelID))
                return customSongsData[levelID];
            else
                return null;
        }

        public static ExtraSongData.DifficultyData RetrieveDifficultyData(IDifficultyBeatmap beatmap)
        {
            ExtraSongData data = RetrieveExtraSongData(beatmap.level.levelID);
            if (data == null) return null;
            ExtraSongData.DifficultyData diffData = data.difficulties.FirstOrDefault(x => x.difficulty == beatmap.difficulty && x.beatmapCharacteristicName == beatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.characteristicName);
            return diffData;
        }
        public static void LoadExtraSongData()
        {
                customSongsData = Newtonsoft.Json.JsonConvert.DeserializeObject < Dictionary<string, ExtraSongData>>(File.ReadAllText(dataPath));
            if (customSongsData == null)
                customSongsData = new Dictionary<string, ExtraSongData>();
        }

        public static void SaveExtraSongData()
        {
            File.WriteAllText(dataPath, Newtonsoft.Json.JsonConvert.SerializeObject(customSongsData));
        }


        
        public static void RegisterCapability(string capability)
        {
            if (!_capabilities.Contains(capability))
                _capabilities.Add(capability);
        }

        public static BeatmapCharacteristicSO RegisterCustomCharacteristic(Sprite Icon, string CharacteristicName, string HintText, string SerializedName, string CompoundIdPartName)
        {
            BeatmapCharacteristicSO newChar = ScriptableObject.CreateInstance<BeatmapCharacteristicSO>();

            newChar.SetField("_icon", Icon);
            newChar.SetField("_hintText", HintText);
            newChar.SetField("_serializedName", SerializedName);
            newChar.SetField("_characteristicName", CharacteristicName);
            newChar.SetField("_compoundIdPartName", CompoundIdPartName);

            if (!_customCharacteristics.Any(x => x.serializedName == newChar.serializedName))
            {
                _customCharacteristics.Add(newChar);
                return newChar;
            }

            return null;
        }



        public static void DeregisterizeCapability(string capability)
        {
            _capabilities.Remove(capability);
        }


    }
}
