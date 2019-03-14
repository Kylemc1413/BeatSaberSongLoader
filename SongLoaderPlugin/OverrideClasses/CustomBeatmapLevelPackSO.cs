using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongLoaderPlugin.OverrideClasses {
    public class CustomBeatmapLevelPackSO : BeatmapLevelPackSO {

        public static CustomBeatmapLevelPackSO GetPack(CustomLevelCollectionSO beatmapLevelCollectionSO) {

            var newPack = CreateInstance<CustomBeatmapLevelPackSO>();
            newPack.Init(beatmapLevelCollectionSO);
            return newPack;
        }

        private void Init(CustomLevelCollectionSO beatmapLevelCollectionSO) {
            _isPackAlwaysOwned = true;
            _packID = "Custom";
            _packName = "Custom Songs";
           //_coverImage = CustomUI.Utilities.UIUtilities.LoadSpriteFromResources("SongLoaderPlugin.Icons.CustomSongs.png"); ;
            _beatmapLevelCollection = beatmapLevelCollectionSO;
        }
    }
}
