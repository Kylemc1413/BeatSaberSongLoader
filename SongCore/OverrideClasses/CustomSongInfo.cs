using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
namespace SongCore.OverrideClasses
{
    [JsonObject(MemberSerialization.OptIn)]
    public class CustomSongInfo
    {
        public string songName
        {
            get
            {
                return this._songName;
            }
        }
        public string songSubName
        {
            get
            {
                return this._songSubName;
            }
        }
        public string songAuthorName
        {
            get
            {
                return this._songAuthorName;
            }
        }
        public string levelAuthorName
        {
            get
            {
                return this._levelAuthorName;
            }
        }
        public float beatsPerMinute
        {
            get
            {
                return this._beatsPerMinute;
            }
        }
        public float songTimeOffset
        {
            get
            {
                return this._songTimeOffset;
            }
        }
        public float shuffle
        {
            get
            {
                return this._shuffle;
            }
        }
        public float shufflePeriod
        {
            get
            {
                return this._shufflePeriod;
            }
        }
        public float previewStartTime
        {
            get
            {
                return this._previewStartTime;
            }
        }
        public float previewDuration
        {
            get
            {
                return this._previewDuration;
            }
        }
        public string songFilename
        {
            get
            {
                return this._songFilename;
            }
        }
        public string coverImageFilename
        {
            get
            {
                return this._coverImageFilename;
            }
        }
        public string environmentName
        {
            get
            {
                return this._environmentName;
            }
        }
        public DifficultyBeatmapSet[] difficultyBeatmapSets
        {
            get
            {
                return this._difficultyBeatmapSets;
            }
        }
        public string customLevelPath;
        public string levelId;
        [JsonProperty]
        protected string _songName;
        [JsonProperty]
        protected string _songSubName;
        [JsonProperty]
        protected string _songAuthorName;
        [JsonProperty]
        protected string _levelAuthorName;
        [JsonProperty]
        protected float _beatsPerMinute;
        [JsonProperty]
        protected float _songTimeOffset;
        [JsonProperty]
        protected float _shuffle;
        [JsonProperty]
        protected float _shufflePeriod;
        [JsonProperty]
        protected float _previewStartTime;
        [JsonProperty]
        protected float _previewDuration;
        [JsonProperty]
        protected string _songFilename;
        [JsonProperty]
        protected string _coverImageFilename;
        [JsonProperty]
        protected string _environmentName;
        [JsonProperty]
        protected DifficultyBeatmapSet[] _difficultyBeatmapSets;

        [JsonConstructor]
        public CustomSongInfo(string _songName, string _songSubName, string _songAuthorName, string _levelAuthorName, float _beatsPerMinute, float _songTimeOffset, float _shuffle, float _shufflePeriod, float _previewStartTime, float _previewDuration, string _songFilename, string _coverImageFilename, string _environmentName, DifficultyBeatmapSet[] _difficultyBeatmapSets)
        {
            this._songName = _songName;
            this._songSubName = _songSubName;
            this._songAuthorName = _songAuthorName;
            this._levelAuthorName = _levelAuthorName;
            this._beatsPerMinute = _beatsPerMinute;
            this._songTimeOffset = _songTimeOffset;
            this._shuffle = _shuffle;
            this._shufflePeriod = _shufflePeriod;
            this._previewStartTime = _previewStartTime;
            this._previewDuration = _previewDuration;
            this._songFilename = _songFilename;
            this._coverImageFilename = _coverImageFilename;
            this._environmentName = _environmentName;
            this._difficultyBeatmapSets = _difficultyBeatmapSets;
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class DifficultyBeatmapSet
        {
            public string beatmapCharacteristicName
            {
                get
                {
                    return _beatmapCharacteristicName;
                }
            }
            public DifficultyBeatmap[] difficultyBeatmaps
            {
                get
                {
                    return _difficultyBeatmaps;
                }
            }
            [JsonProperty]
            protected string _beatmapCharacteristicName;
            [JsonProperty]
            protected DifficultyBeatmap[] _difficultyBeatmaps;
            [JsonConstructor]
            public DifficultyBeatmapSet(string _beatmapCharacteristicName, DifficultyBeatmap[] _difficultyBeatmaps)
            {
                this._beatmapCharacteristicName = _beatmapCharacteristicName;
                this._difficultyBeatmaps = _difficultyBeatmaps;
            }

            [JsonObject(MemberSerialization.OptIn)]
            public class DifficultyBeatmap
            {
                public string difficulty
                {
                    get
                    {
                        return _difficulty;
                    }
                }
                public int difficultyRank
                {
                    get
                    {
                        return _difficultyRank;
                    }
                }
                public string beatmapFilename
                {
                    get
                    {
                        return _beatmapFilename;
                    }
                }
                public int noteJumpStartBeatOffset
                {
                    get
                    {
                        return _noteJumpStartBeatOffset;
                    }
                }
                public float noteJumpMovementSpeed
                {
                    get
                    {
                        return _noteJumpMovementSpeed;
                    }
                }
                [JsonProperty]
                protected string _difficulty;
                [JsonProperty]
                protected int _difficultyRank;
                [JsonProperty]
                protected string _beatmapFilename;
                [JsonProperty]
                protected float _noteJumpMovementSpeed;
                [JsonProperty]
                protected int _noteJumpStartBeatOffset;

                [JsonConstructor]
                public DifficultyBeatmap(string _difficulty, int _difficultyRank, string _beatmapFilename, float _noteJumpMovementSpeed, int _noteJumpStartBeatOffset)
                {
                    this._difficulty = _difficulty;
                    this._difficultyRank = _difficultyRank;
                    this._beatmapFilename = _beatmapFilename;
                    this._noteJumpMovementSpeed = _noteJumpMovementSpeed;
                    this._noteJumpStartBeatOffset = _noteJumpStartBeatOffset;
                }
            }
        }

        public string GetIdentifier()
        {
            string combinedData = "";
            var songfiles = Directory.GetFiles(customLevelPath, ".dat", SearchOption.TopDirectoryOnly);
            foreach (var file in songfiles)
            {
                //             Utilities.Logging.Log(file);
                string json = File.ReadAllText(customLevelPath + '/' + file);
                combinedData += json;
            }
            return SongCore.Utilities.Utils.CreateSha1FromString(combinedData + customLevelPath) + "∎" + string.Join("∎", songName, songSubName, levelAuthorName, beatsPerMinute.ToString()) + "∎";

        }


    }
}
