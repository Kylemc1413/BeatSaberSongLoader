using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SongCore.Data;
namespace SongCore
{
    public static class Collections
    {
        internal static readonly Dictionary<string, ExtraSongData> customSongsData = new Dictionary<string, ExtraSongData>();


        public static void AddSong(string levelID, string path, bool replace = false)
        {
            if (!customSongsData.ContainsKey(levelID))
            customSongsData.Add(levelID, new ExtraSongData(levelID, path));
            else
            {
                if (replace)
                {
                    customSongsData.Remove(levelID);
                    customSongsData.Add(levelID, new ExtraSongData(levelID, path));
                }
            }
            Utilities.Logging.Log("Entry: :"  + levelID + "    " + customSongsData.Count);
        }

        public static ExtraSongData RetrieveExtraSongData(string levelID)
        {
            if (customSongsData.ContainsKey(levelID))
                return customSongsData[levelID];
            else
                return null;
        }




    }
}
