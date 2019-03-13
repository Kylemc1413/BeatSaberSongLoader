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

		public static CustomLevelCollectionSO ReplaceOriginal(BeatmapLevelCollectionSO original)
		{
			var newCollection = CreateInstance<CustomLevelCollectionSO>();
			newCollection.UpdateArray();

			foreach (var originalLevel in original.beatmapLevels)
			{
				if (_standardCharacteristic == null)
				{
					_standardCharacteristic = originalLevel.beatmapCharacteristics.FirstOrDefault(x => x.characteristicName == "Standard");
				}
				
				if (_oneSaberCharacteristic == null)
				{
					_oneSaberCharacteristic = originalLevel.beatmapCharacteristics.FirstOrDefault(x => x.characteristicName == "One Saber");
				}
				
				if (_noArrowsCharacteristic == null)
				{
					_noArrowsCharacteristic = originalLevel.beatmapCharacteristics.FirstOrDefault(x => x.characteristicName == "No Arrows");
				}
			}

			return newCollection;
		}
        
		public void AddCustomLevels(IEnumerable<CustomLevel> customLevels)
		{
			foreach (var customLevel in customLevels)
			{
				var characteristics = new List<BeatmapCharacteristicSO> {_standardCharacteristic, _noArrowsCharacteristic};

				if (customLevel.customSongInfo.oneSaber)
				{
					characteristics.Add(_oneSaberCharacteristic);
				}
				customLevel.SetBeatmapCharacteristics(characteristics.ToArray());
				
				_levelList.Add(customLevel);
			}
			
			UpdateArray();
		}
		
		public void AddCustomLevel(CustomLevel customLevel)
		{
			var characteristics = new List<BeatmapCharacteristicSO> {_standardCharacteristic, _noArrowsCharacteristic};

			if (customLevel.customSongInfo.oneSaber)
			{
				characteristics.Add(_oneSaberCharacteristic);
			}
			
			customLevel.SetBeatmapCharacteristics(characteristics.ToArray());
			
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