using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
        public int rare { get; set; }
        public int supa { get; set; }
        public int uber { get; set; }
        public int legend { get; set; } // can be missing
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
    public class CatData
    {
    public List<string> name { get; set; }
    public List<string> desc { get; set; }
    public List<CatStat> stat { get; set; }
    public int rarity { get; set; }
    public int max_level { get; set; }
    public List<int> growth { get; set; }
    public List<string> talent_against { get; set; }
    public Dictionary<string, object> talent { get; set; } // flexible mapping for talents
        // public List<CatTalent> talent { get; set; }
    }

    // Represents a gacha/banner
    public class GachaBanner
    {
        public List<int> cats { get; set; }
        public int series_id { get; set; }
        public string name { get; set; }
        public string rate { get; set; } = "unknown";
        public int similarity { get; set; } = -1;
    }

    // Root structure for the YAML file
    public class BattleCatYaml
    {
        public Dictionary<int, CatData> cats { get; set; }
        public Dictionary<int, GachaBanner> gacha { get; set; }
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

        public static Dictionary<int, string> GetUnitIdToName(BattleCatYaml data)
        {
            var unitIdToName = new Dictionary<int, string>();
            if (data?.cats != null)
            {
                foreach (var kvp in data.cats)
                {
                    var id = kvp.Key;
                    var cat = kvp.Value;
                    if (cat.name != null && cat.name.Count > 0)
                        unitIdToName[id] = cat.name[0];
                    else
                        unitIdToName[id] = "unknown";
                }
            }
            return unitIdToName;
        }

        public static Dictionary<string, int> GetUnitNameToId(BattleCatYaml data)
        {
            var unitNameToId = new Dictionary<string, int>();
            if (data?.cats != null)
            {
                foreach (var kvp in data.cats)
                {
                    var id = kvp.Key;
                    var cat = kvp.Value;
                    if (cat.name != null && cat.name.Count > 0)
                        unitNameToId[cat.name[0]] = id;
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
                    unitIdToRarity[id] = cat.rarity;
                }
            }
            return unitIdToRarity;
        }

        public static Dictionary<string, EventData> GetEvents(BattleCatYaml data)
        {
            return data?.events ?? new Dictionary<string, EventData>();
        }

        public static Dictionary<int, GachaBanner> GetGachaBanners(BattleCatYaml data)
        {
            return data?.gacha ?? new Dictionary<int, GachaBanner>();
        }
    }
}
