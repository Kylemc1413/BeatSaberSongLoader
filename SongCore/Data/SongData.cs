using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json.Linq;
namespace SongCore.Data
{
    public class ExtraSongData
    {
        public string levelID;
        public Contributor[] contributors; //convert legacy mappers/lighters fields into contributors
        public string customEnvironmentName;
        public string customEnvironmentHash;
        public DifficultyData[] difficultes;




        public ExtraSongData(string levelID, string songPath)
        {
            this.levelID = levelID;
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
            foreach(JObject diff in diffLevels)
            {
                   Utilities.Logging.Log((string)diff["difficulty"]);

                string diffCharacteristic = legacyOneSaber? "One Saber" : "Standard";
                if (diff.ContainsKey("characteristic")) diffCharacteristic = (string)diff["characteristic"];

                BeatmapDifficulty diffDifficulty = Utilities.Utils.ToEnum((string)diff["difficulty"], BeatmapDifficulty.Normal);

                string diffLabel = "";
                if (diff.ContainsKey("difficultyLabel")) diffLabel = (string)diff["difficultyLabel"];

                JObject diffFile = JObject.Parse(File.ReadAllText(songPath + "/" + diff["jsonPath"]));
                //Get difficulty json fields
                Color diffLeft = Color.clear;
                if(diffFile.ContainsKey("_colorLeft"))
                {
                    diffLeft = new Color(
                        (float)diffFile["_colorLeft"]["r"],
                        (float)diffFile["_colorLeft"]["g"],
                        (float)diffFile["_colorLeft"]["b"]
                        );
                }
                Color diffRight = Color.clear;
                if (diffFile.ContainsKey("_colorRight"))
                {
                    diffRight = new Color(
                        (float)diffFile["_colorRight"]["r"],
                        (float)diffFile["_colorRight"]["g"],
                        (float)diffFile["_colorRight"]["b"]
                        );
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
                    requirements = diffInfo,
                    suggestions = diffSuggestions,
                    information = diffInfo,
                    warnings = diffWarnings
                };

                diffData.Add(new DifficultyData
                {
                    beatmapCharacteristic = diffCharacteristic,
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
    }

    [Serializable]
    public class Contributor
    {
        public string role;
        public string name;
        public string iconPath;
        public Sprite icon = null;

    }
    public class DifficultyData
    {
        public string beatmapCharacteristic;
        public BeatmapDifficulty difficulty;
        public string difficultyLabel;
        public RequirementData additionalDifficultyData;
        public Color colorLeft = Color.clear;
        public Color colorRight = Color.clear;
    }
    public class RequirementData
    {
        public string[] requirements;
        public string[] suggestions;
        public string[] warnings;
        public string[] information;
    }
}
