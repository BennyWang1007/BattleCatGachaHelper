#pragma once

#include <string>
#include <unordered_map>
#include <vector>

//#include "../pch.h"

using std::string;

struct Pool {
    int rate = 0;
    std::vector<string> units;
    std::vector<uint32_t> indexes;
    bool reroll = false;
};

struct RollUnit {
    uint32_t seed = 0;
    uint32_t raritySeed = 0;
    uint32_t rarity = 0;
    string unit = "";
    uint32_t nextSeed = 0;
};

struct SimpleRollUnit {
    uint32_t raritySeed = 0;
    uint32_t rarity = 0;
    uint32_t index = 0;
};

struct Banner {
    std::vector<int> rateCumSum;
    std::vector<uint32_t> rarityCumCount;
    std::vector<Pool> pools;
    std::vector<string> idxToName;
    std::vector<int> idxToId;
    uint32_t guaranteed_rarity = 2;

    Banner& operator=(const Banner&) = default;
};


struct SingleRarity {
    uint32_t rate;
    std::vector<string> units;
    bool reroll;
};



std::unordered_map<string, Banner> generate_banners(const std::unordered_map<string, int32_t>& unitNameToId);
Banner initBanner(std::vector<SingleRarity> bannerInfo, const std::unordered_map<string, int32_t>& unitNameToId, uint32_t guaranteed_rarity = 2);
