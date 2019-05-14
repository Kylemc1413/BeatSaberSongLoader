using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SongCore.Data;
using Newtonsoft.Json;
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

        public static void Load()
        {
            customSongsData = JsonConvert.DeserializeObject<Dictionary<string, ExtraSongData>>(File.ReadAllText(dataPath));
            if (customSongsData == null)
                customSongsData = new Dictionary<string, ExtraSongData>();
        }
        public static void Save()
        {
            File.WriteAllText(dataPath, JsonConvert.SerializeObject(customSongsData, Formatting.Indented));
        }



    }
}
