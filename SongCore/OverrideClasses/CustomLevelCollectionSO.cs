using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SongCore.OverrideClasses
{
    public class CustomLevelCollectionSO : BeatmapLevelCollectionSO
    {
        public readonly List<BeatmapLevelSO> _levelList = new List<BeatmapLevelSO>();

        private static BeatmapCharacteristicSO _standardCharacteristic;
        private static BeatmapCharacteristicSO _oneSaberCharacteristic;
        private static BeatmapCharacteristicSO _noArrowsCharacteristic;
        private static BeatmapCharacteristicSO[] beatmapCharacteristicCollection = null;
        public static CustomLevelCollectionSO ReplaceOriginal(BeatmapLevelCollectionSO original)
        {
            var newCollection = CreateInstance<CustomLevelCollectionSO>();
            newCollection.UpdateArray();

            if (beatmapCharacteristicCollection == null) beatmapCharacteristicCollection = Resources.FindObjectsOfTypeAll<BeatmapCharacteristicCollectionSO>().FirstOrDefault().beatmapCharacteristics;

            if (_standardCharacteristic == null)
            {
                _standardCharacteristic = beatmapCharacteristicCollection[0];
            }

            if (_oneSaberCharacteristic == null)
            {
                _oneSaberCharacteristic = beatmapCharacteristicCollection[1];
            }

            if (_noArrowsCharacteristic == null)
            {
                _noArrowsCharacteristic = beatmapCharacteristicCollection[2];
            }

            return newCollection;
        }

        public void AddCustomLevels(IEnumerable<CustomLevel> customLevels)
        {
            foreach (var customLevel in customLevels)
            {
                var characteristics = new List<BeatmapCharacteristicSO>();
                foreach (var set in customLevel.difficultyBeatmapSets)
                    characteristics.Add(set.beatmapCharacteristic);

                customLevel.SetBeatmapCharacteristics(characteristics.ToArray());

                _levelList.Add(customLevel);
            }

            UpdateArray();
        }

        public void AddCustomLevel(CustomLevel customLevel)
        {
            var characteristics = new List<BeatmapCharacteristicSO>();

                foreach (var set in customLevel.difficultyBeatmapSets)
                    characteristics.Add(set.beatmapCharacteristic);

                customLevel.SetBeatmapCharacteristics(characteristics.ToArray());
            //     customLevel.SetDifficultyBeatmaps(_beatmapLevels, characteristics[0]);
            _levelList.Add(customLevel);

            UpdateArray();
        }

        public bool RemoveLevel(BeatmapLevelSO level)
        {
            var removed = _levelList.Remove(level);

            if (removed)
            {
                UpdateArray();
            }

            return removed;
        }

        private void UpdateArray()
        {
            _beatmapLevels = _levelList.ToArray();
        }
    }
}