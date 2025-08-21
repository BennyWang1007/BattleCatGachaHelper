using System.Collections.Generic;
using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

using CatId = System.Int32;

namespace BattleCatWinForm
{
    // Represents a single event's data
    public class EventData
    {
        public string start_on { get; set; }
        public string end_on { get; set; }
        public string version { get; set; }
        public string name { get; set; }
        public int id { get; set; }
        public uint rare { get; set; }
        public uint supa { get; set; }
        public uint uber { get; set; }
        public uint legend { get; set; } // can be missing
        public string platinum { get; set; } // can be string or missing
        public bool guaranteed { get; set; } // optional
        public bool step_up { get; set; } // optional
    }
    // Represents the stats for a single cat evolution/form as a dynamic dictionary
    public class CatStat : Dictionary<string, object>
    {
    }

    // Represents a single cat's talent
    public class CatTalent : Dictionary<string, object>
    {
    }

    // Represents a single cat's data
    public class CatDataRaw
    {
        public List<string> Name { get; set; }
        public List<string> Desc { get; set; }
        public List<CatStat> Stat { get; set; }
        public int Rarity { get; set; }
        public int MaxLevel { get; set; }
        public List<int> Growth { get; set; }
        public List<string> TalentAgainst { get; set; }
        public Dictionary<string, object> Talent { get; set; } // flexible mapping for talents
    };

    // public static Dictionary<string, List<int>> RatenameToRates { get; private set; } = new Dictionary<string, List<int>>
    // {
    //     { "regular", new List<int> { 0, 0, 6970, 2500, 500, 30 } },
    //     { "no_legend", new List<int> { 0, 0, 7000, 2500, 500, 0 } },
    //     { "uberfest_legend", new List<int> { 0, 0, 6470, 2600, 900, 30 } }
    //     // Add more mappings as needed
    // };


    // Represents a gacha/banner
    public class BannerDataRaw
    {
        public List<int> Cats { get; set; }
        public int SeriesId { get; set; }
        public string Name { get; set; }
        public string Rate { get; set; } = "unknown";
        public int Similarity { get; set; } = -1;
    }

    public class BCDataConstant
    {
        public const int RarityCount = 6; // Exclude None
    }

    public enum RarityClass : uint
    {
        Normal,
        Ex,
        Rare,
        SuperRare,
        Uber,
        Legend,
        None // Used for no guarantee
    }

    public class Pool
    {
        public List<int> Cats { get; set; } = new List<int>();
        public void AddCat(CatId catId)
        {
            Cats.Add(catId);
        }
    }

    public class Banner
    {
        private readonly uint _unitCount;
        private uint[] _rateCumSum { get; set; } = new uint[BCDataConstant.RarityCount];
        private uint[] _rarityCumCount { get; set; } = new uint[BCDataConstant.RarityCount];
        public Pool[] Pools { get; set; } = new Pool[BCDataConstant.RarityCount];

        public List<string> IdxToName { get; set; } = new List<string>();
        public List<int> IdxToId { get; set; } = new List<int>();

        public RarityClass GuaranteedRarity { get; set; }

        public Banner(List<int> poolUnitIds,
              uint[] rarityCumCount,
              RarityClass guaranteedRarity,
              Dictionary<int, int> unitIdToRarity,
              Dictionary<int, string> unitIdToName)
        {
            for (int i = 0; i < BCDataConstant.RarityCount; i++)
            {
                Pools[i] = new Pool();
            }
            for (CatId i = 0; i < _unitCount; i++)
            {
                var rarity = unitIdToRarity[poolUnitIds[i]];
                System.Diagnostics.Debug.Assert(rarity >= 0 && rarity < BCDataConstant.RarityCount, $"Invalid rarity {rarity} for unit ID {poolUnitIds[i]}");
                Pools[rarity].AddCat(poolUnitIds[i]);
                IdxToName.Add(unitIdToName[poolUnitIds[i]]);
                IdxToId.Add(poolUnitIds[i]);
            }

            for (int i = 0; i < BCDataConstant.RarityCount; i++)
            {
                _rarityCumCount[i] = rarityCumCount[i];
            }

            _rateCumSum[0] = _rarityCumCount[0];
            for (int i = 1; i < _rateCumSum.Length; i++)
            {
                _rateCumSum[i] = _rateCumSum[i - 1] + _rarityCumCount[i];
            }

            GuaranteedRarity = guaranteedRarity;
        }
    }

    // Root structure for the YAML file
    public class BattleCatYaml
    {
        public Dictionary<CatId, CatDataRaw> cats { get; set; }
        public Dictionary<int, BannerDataRaw> gacha { get; set; }
        public Dictionary<string, EventData> events { get; set; }
    }

    public static class BattleCatDataLoader
    {
        public static BattleCatYaml LoadYaml(string filePath)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            using (var reader = new StreamReader(filePath))
            {
                return deserializer.Deserialize<BattleCatYaml>(reader);
            }
        }

        public static Dictionary<CatId, string> GetUnitIdToName(BattleCatYaml data)
        {
            var unitIdToName = new Dictionary<CatId, string>();
            if (data?.cats != null)
            {
                foreach (var kvp in data.cats)
                {
                    var id = kvp.Key;
                    var cat = kvp.Value;
                    if (cat.Name != null && cat.Name.Count > 0)
                        unitIdToName[id] = cat.Name[0];
                    else
                        unitIdToName[id] = "unknown";
                }
            }
            return unitIdToName;
        }

        public static Dictionary<string, CatId> GetUnitNameToId(BattleCatYaml data)
        {
            var unitNameToId = new Dictionary<string, CatId>();
            if (data?.cats != null)
            {
                foreach (var kvp in data.cats)
                {
                    var id = kvp.Key;
                    var cat = kvp.Value;
                    if (cat.Name != null && cat.Name.Count > 0)
                        unitNameToId[cat.Name[0]] = id;
                    else
                        unitNameToId["unknown"] = id; // fallback for unknown names
                }
            }
            return unitNameToId;
        }

        public static Dictionary<int, int> GetUnitIdToRarity(BattleCatYaml data)
        {
            var unitIdToRarity = new Dictionary<int, int>();
            if (data?.cats != null)
            {
                foreach (var kvp in data.cats)
                {
                    var id = kvp.Key;
                    var cat = kvp.Value;
                    unitIdToRarity[id] = cat.Rarity;
                }
            }
            return unitIdToRarity;
        }

        public static Dictionary<string, EventData> GetEvents(BattleCatYaml data)
        {
            return data?.events ?? new Dictionary<string, EventData>();
        }

        public static Dictionary<int, BannerDataRaw> GetGachaBanners(BattleCatYaml data)
        {
            return data?.gacha ?? new Dictionary<int, BannerDataRaw>();
        }
    }

    public static class BattleCatData
    {
        public static Dictionary<CatId, string> UnitIdToName { get; private set; } = new Dictionary<CatId, string>();
        public static Dictionary<string, CatId> UnitNameToId { get; private set; } = new Dictionary<string, CatId>();
        public static Dictionary<int, int> UnitIdToRarity { get; private set; } = new Dictionary<int, int>();
        public static Dictionary<string, Banner> Banners { get; private set; } = new Dictionary<string, Banner>();
        public static List<string> BannerNames { get; private set; } = new List<string>();
        private static Dictionary<int, List<CatId>> _idToCatIds { get; set; } = new Dictionary<int, List<CatId>>();

        public static Dictionary<int, BannerDataRaw> _gachaBanners { get; private set; } = new Dictionary<int, BannerDataRaw>();
        private static BattleCatYaml _yamlData { get; set; }
        private static Dictionary<string, EventData> _events;
        static BattleCatData()
        {
            // Initialize all dictionaries
            _yamlData = BattleCatDataLoader.LoadYaml("bc-tw.yaml");
            _events = BattleCatDataLoader.GetEvents(_yamlData);
            _gachaBanners = BattleCatDataLoader.GetGachaBanners(_yamlData);

            foreach (var kvp in _yamlData.cats)
            {
                var id = kvp.Key;
                var cat = kvp.Value;
                UnitIdToName[id] = cat.Name[0];
                UnitNameToId[cat.Name[0]] = id;
                UnitIdToRarity[id] = cat.Rarity;
            }

            foreach (var kvp in _gachaBanners)
            {
                int i = kvp.Key;
                BannerDataRaw banner = kvp.Value;
                if (!_idToCatIds.ContainsKey(i))
                {
                    _idToCatIds[i] = new List<CatId>();
                }
                if (banner.Cats != null)
                {
                    foreach (var catId in banner.Cats)
                    {
                        _idToCatIds[i].Add(catId);
                    }
                }
                else
                {
                    throw new InvalidDataException($"Banner id {i} has null cats list.");
                }
            }

            foreach (var kvp in _events)
            {
                var eventData = kvp.Value;
                string bannerName = eventData.start_on + " ~ " + eventData.end_on + ": " + eventData.name;
                uint[] cumRate = new uint[] { 0, 0, eventData.rare, eventData.supa, eventData.uber, eventData.legend };
                RarityClass guaranteed = eventData.guaranteed ? RarityClass.Uber : RarityClass.None;
                Banners[bannerName] = new Banner(_idToCatIds[eventData.id], cumRate, guaranteed, UnitIdToRarity, UnitIdToName);
                BannerNames.Add(bannerName);
                // TODO: (platinum: legend/platinum)
            }
        }
    }
}
