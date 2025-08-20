#pragma execution_character_set("utf-8")
//#pragma execution_character_set("Big-5")

#include <cstdint>
#include <iostream>
#include <string>
#include <unordered_map>
#include <vector>

#include "../include/banner.h"
#include "../include/pch.h"
#include "../include/battle_cat_data.h"

using std::string, std::vector, std::cout, std::endl;


// Banner Banner::operator=(const Banner &other) {
//     if (this == &other) {
//         return *this;
//     }

//     this->rateCumSum = other.rateCumSum;
//     this->pools.clear();
//     this->pools.reserve(other.pools.size());

//     for (const Pool& pool : other.pools) {
//         Pool p;
//         p.rate = pool.rate;
//         p.units = pool.units;
//         p.indexes = pool.indexes;
//         p.reroll = pool.reroll;
//         this->pools.emplace_back(std::move(p));
//     }

//     this->idxToName = other.idxToName;
//     this->guaranteed_rarity = other.guaranteed_rarity;

//     return *this;
// }


Banner initBanner(vector<SingleRarity> bannerInfo, const std::unordered_map<string, int32_t>& unitNameToId, uint32_t guaranteed_rarity)
{
    Banner banner;
    size_t len = bannerInfo.size();
    banner.rateCumSum = vector<int>(len);
    banner.pools = vector<Pool>(len);
    banner.idxToName = {};
    banner.idxToId = {}; // Initialize idxToId
    banner.rarityCumCount = {};
    uint32_t cumrate = 0, cumcount = 0;
    uint32_t idx = 0;
    //auto& unitNameToId = BattleCatData::getInstance().unitNameToId;
	std::cout << "Initializing banner with " << unitNameToId.size() << " units." << std::endl;
    for (size_t i = 0; i < len; ++i) {
        for (size_t j = 0; j < bannerInfo[i].units.size(); ++j) {
			bannerInfo[i].units[j] = utf8_to_big5(bannerInfo[i].units[j]);
		}
        cumrate += bannerInfo[i].rate;
        banner.rateCumSum[i] = cumrate;
        banner.pools[i].rate = bannerInfo[i].rate;
        banner.pools[i].units = bannerInfo[i].units;
		// use utf8_to_big5 make sure the unit names are in the correct encoding
        banner.pools[i].reroll = bannerInfo[i].reroll;
        for (auto& unit : bannerInfo[i].units) {
            banner.idxToName.emplace_back(unit);
			//banner.idxToId.emplace_back(unitNameToId[unit]); // Get ID from BattleCatData
			auto it = unitNameToId.find(unit);
			std::cout << "Unit: " << unit << ", ID: " << (it != unitNameToId.end() ? std::to_string(it->second) : "Unknown") << std::endl;
			banner.idxToId.emplace_back(it != unitNameToId.end() ? it->second : -1); // Use -1 for unknown units
            banner.pools[i].indexes.emplace_back(idx++);
        }
        banner.rarityCumCount.emplace_back(cumcount);
        cumcount += bannerInfo[i].units.size();
    }
    banner.guaranteed_rarity = guaranteed_rarity;

    // print idxToId
 //   for (size_t i = 0; i < banner.idxToId.size(); ++i) {
 //      cout << "Name: " << banner.idxToName[i] << ", ID: " << banner.idxToId[i] << endl;
 //   }

 //   for (auto& [name, id] : unitNameToId) {
	//	std::cout << "Unit: " << name << ", ID: " << id << std::endl;
	//}

    // for (auto& [name, id] : unitNameToId) {
    //    std::cout << "Key(hex): ";
    //    for (unsigned char c : name) {
    //        std::cout << std::hex << (int)c << " ";
    //    }
    //    std::cout << " | " << name << std::dec << " (" << id  << ")" << std::endl;
    // }

    // for (size_t i = 0; i < len; ++i) {
    //    for (auto& unit : bannerInfo[i].units) {
    //        std::cout << "Unit(hex): ";
    //        for (unsigned char c : unit) {
    //            std::cout << std::hex << (int)c << " ";
    //        }
    //        std::cout << " | " << unit << std::dec << std::endl;
    //    }
    // }

    return banner;
}
