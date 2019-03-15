using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace SongLoaderPlugin.OverrideClasses {
    public class CustomBeatmapLevelPackCollectionSO : BeatmapLevelPackCollectionSO {

        internal List<BeatmapLevelPackSO> _customBeatmapLevelPacks = new List<BeatmapLevelPackSO>();

        public static CustomBeatmapLevelPackCollectionSO ReplaceOriginal(BeatmapLevelPackCollectionSO original) {
            var newCollection = CreateInstance<CustomBeatmapLevelPackCollectionSO>();
          //  newCollection._allBeatmapLevelPacks.AddRange((BeatmapLevelPackSO[])original.GetField("_beatmapLevelPacks"));
            //Figure out how to properly add the preview song packs
            List<IBeatmapLevelPack> levelPacks = new List<IBeatmapLevelPack>();
            levelPacks.AddRange((BeatmapLevelPackSO[])original.GetField("_beatmapLevelPacks"));
            levelPacks.AddRange((PreviewBeatmapLevelPackSO[])original.GetField("_previewBeatmapLevelPack"));
            newCollection._allBeatmapLevelPacks = levelPacks.ToArray();
            
            
            newCollection.UpdateArray();
            newCollection.ReplaceReferences();
            return newCollection;
        }

        public void ReplaceReferences() {

            var soloFreePlay = Resources.FindObjectsOfTypeAll<SoloFreePlayFlowCoordinator>().FirstOrDefault();
            if (soloFreePlay != null) {
                soloFreePlay.SetPrivateField("_levelPackCollection", this);
            }

            var partyFreePlay = Resources.FindObjectsOfTypeAll<PartyFreePlayFlowCoordinator>().FirstOrDefault();
            if (partyFreePlay != null) {
                partyFreePlay.SetPrivateField("_levelPackCollection", this);
            }
            
        }

        public void AddLevelPack(BeatmapLevelPackSO pack) {
            _customBeatmapLevelPacks.Add(pack);
            UpdateArray();
            ReplaceReferences();
        }

        private void UpdateArray() {

            _beatmapLevelPacks = _customBeatmapLevelPacks.ToArray();
        }
    }
}
