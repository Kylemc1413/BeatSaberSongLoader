using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SongLoaderPlugin.OverrideClasses
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
                    foreach (CustomSongInfo.DifficultyLevel diffLevel in customLevel.customSongInfo.difficultyLevels)
                    {
                        switch (diffLevel.characteristic)
                        {
                            case "":
                                if (!characteristics.Contains(_standardCharacteristic))
                                    characteristics.Add(_standardCharacteristic);
                                break;

                            case "Standard":
                                if (!characteristics.Contains(_standardCharacteristic))
                                    characteristics.Add(_standardCharacteristic);
                                break;

                            case "One Saber":
                                if (!characteristics.Contains(_oneSaberCharacteristic))
                                    characteristics.Add(_oneSaberCharacteristic);
                                break;
                            case "No Arrows":
                                if (!characteristics.Contains(_noArrowsCharacteristic))
                                    characteristics.Add(_noArrowsCharacteristic);
                                break;

                            default:
                                BeatmapCharacteristicSO characteristic = SongLoader.customCharacteristics.FirstOrDefault(x => x.characteristicName == diffLevel.characteristic);
                                if (characteristic != null)
                                    characteristics.Add(characteristic);
                                break;
                        }

                    }
                
                customLevel.SetBeatmapCharacteristics(characteristics.ToArray());

                _levelList.Add(customLevel);
            }

            UpdateArray();
        }

        public void AddCustomLevel(CustomLevel customLevel)
        {
            var characteristics = new List<BeatmapCharacteristicSO>();
                foreach (CustomSongInfo.DifficultyLevel diffLevel in customLevel.customSongInfo.difficultyLevels)
                {
                    switch (diffLevel.characteristic)
                    {
                        case "":
                            if (!characteristics.Contains(_standardCharacteristic))
                                characteristics.Add(_standardCharacteristic);
                            break;

                        case "Standard":
                            if (!characteristics.Contains(_standardCharacteristic))
                                characteristics.Add(_standardCharacteristic);
                            break;

                        case "One Saber":
                            if (!characteristics.Contains(_oneSaberCharacteristic))
                                characteristics.Add(_oneSaberCharacteristic);
                            break;
                        case "No Arrows":
                            if (!characteristics.Contains(_noArrowsCharacteristic))
                                characteristics.Add(_noArrowsCharacteristic);
                            break;

                        default:
                            BeatmapCharacteristicSO characteristic = SongLoader.customCharacteristics.FirstOrDefault(x => x.characteristicName == diffLevel.characteristic);
                            if (characteristic != null)
                                characteristics.Add(characteristic);
                            else
                                if (!characteristics.Contains(SongLoader.customCharacteristics.First(x => x.characteristicName == "Missing Characteristic")))
                                characteristics.Add(SongLoader.customCharacteristics.First(x => x.characteristicName == "Missing Characteristic"));
                            break;
                    


                }
            }
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