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

    void print() const {
        std::cout << "Pool - Rate: " << rate << ", Reroll: " << reroll << std::endl;
        std::cout << "Units: ";
        for (const auto& unit : units) {
            std::cout << unit << " ";
        }
        std::cout << std::endl;
        std::cout << "Indexes: ";
        for (const auto& index : indexes) {
            std::cout << index << " ";
        }
        std::cout << std::endl;
    }
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

    void print() const
    {
        std::cout << "Rate cumulative sum: ";
        for (auto& rateCumSum : rateCumSum) {
            std::cout << rateCumSum << " ";
        }
        std::cout << std::endl;

        std::cout << "Rarity cumulative count: ";
        for (auto& rarityCumCount : rarityCumCount) {
            std::cout << rarityCumCount << " ";
        }
        std::cout << std::endl;

        std::cout << "Pools: " << std::endl;
        for (const auto& pool : pools) {
            pool.print();
        }

        std::cout << "Index to Name: ";
        for (const auto& name : idxToName) {
            std::cout << name << " ";
        }
        std::cout << std::endl;

        std::cout << "Index to ID: ";
        for (const auto& id : idxToId) {
            std::cout << id << " ";
        }
        std::cout << std::endl;

        std::cout << "Guaranteed Rarity: " << guaranteed_rarity << std::endl;
    }

};


struct SingleRarity {
    uint32_t rate;
    std::vector<string> units;
    bool reroll;
};



std::unordered_map<string, Banner> generate_banners(const std::unordered_map<string, int32_t>& unitNameToId);
Banner initBanner(std::vector<SingleRarity> bannerInfo, const std::unordered_map<string, int32_t>& unitNameToId, uint32_t guaranteed_rarity = 2);
