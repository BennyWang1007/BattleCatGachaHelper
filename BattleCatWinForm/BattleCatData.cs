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
        public string StartOn { get; set; }
        public string EndOn { get; set; }
        public string Version { get; set; }
        public string Name { get; set; }
        public int Id { get; set; }
        public uint Rare { get; set; }
        public uint Supa { get; set; }
        public uint Uber { get; set; }
        public uint Legend { get; set; } // can be missing
        public string Platinum { get; set; } // can be string or missing
        public bool Guaranteed { get; set; } // optional
        public bool StepUp { get; set; } // optional
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
        Common,
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
        public bool Reroll { get; set; } = false;
        public void AddCat(CatId catId)
        {
            Cats.Add(catId);
        }

        public void SetReroll(bool reroll)
        {
            Reroll = reroll;
        }
    }

    public class Banner
    {
        private uint[] _rateCumSum { get; set; } = new uint[BCDataConstant.RarityCount];
        public uint[] RarityRates { get; set; } = new uint[BCDataConstant.RarityCount];
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
            // if rarity is uber(4) then move unit to the back
            // for (int i = 0; i < poolUnitIds.Count; i++)
            // {
            //     var rarity = unitIdToRarity[poolUnitIds[i]];
            //     if (rarity == (int)RarityClass.Uber)
            //     {
            //         poolUnitIds.Add(poolUnitIds[i]);
            //         poolUnitIds.RemoveAt(i);
            //         i--;
            //     }
            // }   
            // for (int i = 0; i < BCDataConstant.RarityCount; i++)
            // {
            //     Pools[i] = new Pool();
            // }
            for (int r = 0; r < BCDataConstant.RarityCount; r++)
            {
                Pools[r] = new Pool();
                for (CatId i = 0; i < poolUnitIds.Count; i++)
                {
                    var rarity = unitIdToRarity[poolUnitIds[i]];
                    if (rarity != r) continue;
                    System.Diagnostics.Debug.Assert(rarity >= 0 && rarity < BCDataConstant.RarityCount, $"Invalid rarity {rarity} for unit ID {poolUnitIds[i]}");
                    Pools[rarity].AddCat(poolUnitIds[i]);
                    IdxToName.Add(unitIdToName[poolUnitIds[i]]);
                    IdxToId.Add(poolUnitIds[i]);
                }
            }

            // TODO: check if there are exceptions
            Pools[(int)RarityClass.Rare].SetReroll(true);

            for (int i = 0; i < BCDataConstant.RarityCount; i++)
            {
                RarityRates[i] = rarityCumCount[i];
            }

            _rateCumSum[0] = RarityRates[0];
            for (int i = 1; i < _rateCumSum.Length; i++)
            {
                _rateCumSum[i] = _rateCumSum[i - 1] + RarityRates[i];
            }

            GuaranteedRarity = guaranteedRarity;
        }
    }

    // Root structure for the YAML file
    public class BattleCatYaml
    {
        public Dictionary<CatId, CatDataRaw> Cats { get; set; }
        public Dictionary<int, BannerDataRaw> Gacha { get; set; }
        public Dictionary<string, EventData> Events { get; set; }
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
            if (data?.Cats != null)
            {
                foreach (var kvp in data.Cats)
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
            if (data?.Cats != null)
            {
                foreach (var kvp in data.Cats)
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
            if (data?.Cats != null)
            {
                foreach (var kvp in data.Cats)
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
            return data?.Events ?? new Dictionary<string, EventData>();
        }

        public static Dictionary<int, BannerDataRaw> GetGachaBanners(BattleCatYaml data)
        {
            return data?.Gacha ?? new Dictionary<int, BannerDataRaw>();
        }
    }

    public static class BattleCatData
    {
        public static Dictionary<CatId, string> UnitIdToName { get; private set; } = new Dictionary<CatId, string>();
        public static Dictionary<string, CatId> UnitNameToId { get; private set; } = new Dictionary<string, CatId>();
        public static Dictionary<int, int> UnitIdToRarity { get; private set; } = new Dictionary<int, int>();
        public static Dictionary<string, Banner> Banners { get; private set; } = new Dictionary<string, Banner>();
        public static List<string> BannerNames { get; private set; } = new List<string>();
        // The mapping of banner ID to list of CatIds in that banner
        private static Dictionary<int, List<CatId>> _bannerIdToCatIds { get; set; } = new Dictionary<int, List<CatId>>();

        private static Dictionary<int, BannerDataRaw> _gachaBanners { get; set; } = new Dictionary<int, BannerDataRaw>();
        //private static BattleCatYaml _yamlData { get; set; }
        //private static Dictionary<string, EventData> _events;
        static BattleCatData()
        {
            // Initialize all dictionaries
            BattleCatYaml _yamlData = BattleCatDataLoader.LoadYaml("bc-tw.yaml");
            Dictionary<string, EventData> _events = BattleCatDataLoader.GetEvents(_yamlData);
            _gachaBanners = BattleCatDataLoader.GetGachaBanners(_yamlData);

            foreach (var kvp in _yamlData.Cats)
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
                if (!_bannerIdToCatIds.ContainsKey(i))
                {
                    _bannerIdToCatIds[i] = new List<CatId>();
                }
                if (banner.Cats != null)
                {
                    foreach (var catId in banner.Cats)
                    {
                        _bannerIdToCatIds[i].Add(catId);
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
                string bannerName = eventData.StartOn + " ~ " + eventData.EndOn + ": " + eventData.Name;
                uint[] cumRate = new uint[] { 0, 0, eventData.Rare, eventData.Supa, eventData.Uber, eventData.Legend };
                var poolUnitIds = _bannerIdToCatIds.ContainsKey(eventData.Id) ? _bannerIdToCatIds[eventData.Id] : new List<CatId>();
                RarityClass guaranteed = eventData.Guaranteed ? RarityClass.Uber : RarityClass.None;

                var banner = new Banner(poolUnitIds, cumRate, guaranteed, UnitIdToRarity, UnitIdToName);
                Banners[bannerName] = banner;
                if (bannerName == "2025-08-13 ~ 2025-08-16: 傳說中的龍族們霸氣降臨！★點圖確認詳細吧!!")
                {
                    for (int i = 0; i < BCDataConstant.RarityCount; i++)
                    {
                        System.Diagnostics.Debug.WriteLine($"Pool[{i}].Reroll = {banner.Pools[i].Reroll}");
                    }
                }
                // Banners[bannerName] = new Banner(poolUnitIds, cumRate, guaranteed, UnitIdToRarity, UnitIdToName);
                BannerNames.Add(bannerName);
                // TODO: (platinum: legend/platinum)
            }
        }
    }
}
