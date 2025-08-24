#pragma once

#include <fstream>
#include <iostream>
#include <regex>
#include <string>
#include <unordered_map>
#include <vector>

#include "banner.h"
#include "pch.h"

using std::string, std::vector, std::unordered_map;

enum class RarityClass
{
    Common = 0,
    Ex = 1,
    Rare = 2,
    SuperRare = 3,
    Uber = 4,
    Legend = 5,
    None = 6
};

string utf8_to_big5(const string& utf8);

class BattleCatData
{
public:
    BattleCatData();
    ~BattleCatData() = default;
    unordered_map<string, int32_t> unitNameToId;

    static BattleCatData& getInstance();

    string getUnitName(uint32_t unitId);
    vector<string> getBannerNames() const {
        return bannerNames;
    }

    unordered_map<string, Banner> getBanners() const {
        return banners;
    }

    int32_t getUnitId(const string &unitName) const;

private:
    vector<string> bannerNames;
    unordered_map<string, Banner> banners;
    unordered_map<int32_t, string> idToUnitName;
    //unordered_map<string, int32_t> unitNameToId;
    // Load unit names and banners from a data source (e.g., file, database)
    void loadUnits();
    void loadBanners();

    // Prevent copying and assignment
    BattleCatData(const BattleCatData&) = delete;
    BattleCatData& operator=(const BattleCatData&) = delete;
};
