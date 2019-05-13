using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Globalization;
namespace SongCore.Data
{
    [Serializable]
    public class ExtraSongData
    {
        public string levelID;
        public Contributor[] contributors; //convert legacy mappers/lighters fields into contributors
        public string customEnvironmentName;
        public string customEnvironmentHash;
        public DifficultyData[] difficultes;




        public ExtraSongData(string levelID, string songPath)
        {
            try
            { 
            this.levelID = levelID;
            if (!File.Exists(songPath + "/info.json")) return;
            var infoText = File.ReadAllText(songPath + "/info.json");

            JObject info = JObject.Parse(infoText);
            //Check if song uses legacy value for full song One Saber mode
            bool legacyOneSaber = false;
            if (info.ContainsKey("oneSaber")) legacyOneSaber = (bool)info["oneSaber"];


            if (info.ContainsKey("contributors"))
            {
                contributors = info["contributors"].ToObject<Contributor[]>();
            }
            else
            {
                contributors = new Contributor[0];
            }
            if (info.ContainsKey("customEnvironment")) customEnvironmentName = (string)info["customEnvironment"];
            if (info.ContainsKey("customEnvironmentHash")) customEnvironmentHash = (string)info["customEnvironmentHash"];
            List<DifficultyData> diffData = new List<DifficultyData>();
            JArray diffLevels = (JArray)info["difficultyLevels"];
            foreach (JObject diff in diffLevels)
            {
                //       Utilities.Logging.Log((string)diff["difficulty"]);
                if (!File.Exists(songPath + "/" + diff["jsonPath"])) continue;
                JObject diffFile = JObject.Parse(File.ReadAllText(songPath + "/" + diff["jsonPath"]));


                string diffCharacteristic = legacyOneSaber ? "One Saber" : "Standard";
                if (diff.ContainsKey("characteristic")) diffCharacteristic = (string)diff["characteristic"];

                BeatmapDifficulty diffDifficulty = Utilities.Utils.ToEnum((string)diff["difficulty"], BeatmapDifficulty.Normal);

                string diffLabel = "";
                if (diff.ContainsKey("difficultyLabel")) diffLabel = (string)diff["difficultyLabel"];


                //Get difficulty json fields
                MapColor diffLeft = null;
                if (diffFile.ContainsKey("_colorLeft"))
                {
                    diffLeft = new MapColor(0, 0, 0);
                    diffLeft.r = (float)diffFile["_colorLeft"]["r"];
                    diffLeft.g = (float)diffFile["_colorLeft"]["g"];
                    diffLeft.b = (float)diffFile["_colorLeft"]["b"];
                }
                MapColor diffRight = null;
                if (diffFile.ContainsKey("_colorRight"))
                {
                    diffRight = new MapColor(0, 0, 0);
                    diffRight.r = (float)diffFile["_colorRight"]["r"];
                    diffRight.g = (float)diffFile["_colorRight"]["g"];
                    diffRight.b = (float)diffFile["_colorRight"]["b"];
                }

                string[] diffRequirements = new string[0];
                string[] diffSuggestions = new string[0];
                string[] diffWarnings = new string[0];
                string[] diffInfo = new string[0];
                if (diffFile.ContainsKey("_requirements"))
                    diffRequirements = ((JArray)diffFile["_requirements"]).Select(c => (string)c).ToArray();
                if (diffFile.ContainsKey("_suggestions"))
                    diffSuggestions = ((JArray)diffFile["_suggestions"]).Select(c => (string)c).ToArray();
                if (diffFile.ContainsKey("_warnings"))
                    diffWarnings = ((JArray)diffFile["_warnings"]).Select(c => (string)c).ToArray();
                if (diffFile.ContainsKey("_information"))
                    diffInfo = ((JArray)diffFile["_information"]).Select(c => (string)c).ToArray();
                RequirementData diffReqData = new RequirementData
                {
                    requirements = diffRequirements,
                    suggestions = diffSuggestions,
                    information = diffInfo,
                    warnings = diffWarnings
                };

                diffData.Add(new DifficultyData
                {
                    beatmapCharacteristicName = diffCharacteristic,
                    difficulty = diffDifficulty,
                    difficultyLabel = diffLabel,
                    additionalDifficultyData = diffReqData,
                    colorLeft = diffLeft,
                    colorRight = diffRight

                }
                );

            }
            difficultes = diffData.ToArray();

        }
        catch(Exception ex)
            {
                Utilities.Logging.Log($"Error in Level {levelID}: \n {ex}", IPA.Logging.Logger.Level.Error);
            }
        }

        public void UpdateData(string songPath)
        {
            if (!File.Exists(songPath + "/info.json")) return;
            var infoText = File.ReadAllText(songPath + "/info.json");
            try
            {
                JObject info = JObject.Parse(infoText);
            //Check if song uses legacy value for full song One Saber mode
            bool legacyOneSaber = false;
            if (info.ContainsKey("oneSaber")) legacyOneSaber = (bool)info["oneSaber"];


            if (info.ContainsKey("contributors"))
            {
                contributors = info["contributors"].ToObject<Contributor[]>();
            }
            else
            {
                contributors = new Contributor[0];
            }
            if (info.ContainsKey("customEnvironment")) customEnvironmentName = (string)info["customEnvironment"];
            if (info.ContainsKey("customEnvironmentHash")) customEnvironmentHash = (string)info["customEnvironmentHash"];
            List<DifficultyData> diffData = difficultes?.ToList();
                if (diffData == null) return;
            JArray diffLevels = (JArray)info["difficultyLevels"];
            for(int i = 0; i < diffData.Count; ++i)
            {
                var json = (JObject)diffLevels[i];

                diffData[i].difficulty = Utilities.Utils.ToEnum((string)json["difficulty"], BeatmapDifficulty.Normal);
                diffData[i].beatmapCharacteristicName = json.ContainsKey("characteristic") ? (string)json["characteristic"] : legacyOneSaber ? "One Saber" : "Standard";
                diffData[i].difficultyLabel = "";
                if (json.ContainsKey("difficultyLabel")) diffData[i].difficultyLabel = (string)json["difficultyLabel"];
            }

            difficultes = diffData.ToArray();
        }
        catch(Exception ex)
            {
                Utilities.Logging.Log($"Error in Level {levelID}: \n {ex}", IPA.Logging.Logger.Level.Error);
            }
}

        


        [Serializable]
        public class Contributor
        {
            public string role;
            public string name;
            public string iconPath;
            public Sprite icon = null;

        }
        [Serializable]
        public class DifficultyData
        {
            public string beatmapCharacteristicName;
            public BeatmapDifficulty difficulty;
            public string difficultyLabel;
            public RequirementData additionalDifficultyData;
            public MapColor colorLeft;
            public MapColor colorRight;
        }
        [Serializable]
        public class RequirementData
        {
            public string[] requirements;
            public string[] suggestions;
            public string[] warnings;
            public string[] information;
        }
        [Serializable]
        public class MapColor
        {
            public float r;
            public float g;
            public float b;


            public MapColor(float r, float g, float b)
            {
                this.r = r;
                this.g = g;
                this.b = b;
            }
        }
    }
}

 
 