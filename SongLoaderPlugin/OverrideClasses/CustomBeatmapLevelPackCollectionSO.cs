using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace SongLoaderPlugin.OverrideClasses {
    public class CustomBeatmapLevelPackCollectionSO : BeatmapLevelPackCollectionSO {

        internal List<BeatmapLevelPackSO> _customBeatmapLevelPacks = new List<BeatmapLevelPackSO>();

        public static CustomBeatmapLevelPackCollectionSO ReplaceOriginal(BeatmapLevelPackCollectionSO original) {
            var newCollection = CreateInstance<CustomBeatmapLevelPackCollectionSO>();
            newCollection._customBeatmapLevelPacks.AddRange((BeatmapLevelPackSO[])original.GetField("_beatmapLevelPacks"));
            //Figure out how to properly add the preview song packs
            
            var previewpacks = (PreviewBeatmapLevelPackSO[])original.GetField("_previewBeatmapLevelPack");
            IBeatmapLevelPack pack = null;
            foreach (PreviewBeatmapLevelPackSO previewpack in previewpacks)
            {
                pack = previewpack;
                BeatmapLevelPackSO newpack = pack as BeatmapLevelPackSO;
                if (newpack != null)
                {
                    Console.WriteLine("Adding Preview Pack: " + newpack.packName);
                    newCollection._customBeatmapLevelPacks.Add(pack as BeatmapLevelPackSO);
                }
                else
                    Console.WriteLine("Preview Pack Null, not adding.");
            }
            
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
