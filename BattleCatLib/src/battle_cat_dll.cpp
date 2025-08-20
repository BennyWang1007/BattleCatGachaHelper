// #define BATTLE_CAT_EXPORTS
#include <iostream>
#include <memory>
#include <string>
#include <vector>
#include <unordered_map>
#include <cstring>
#include <algorithm>

#include "../include/pch.h"
#include "../include/banner.h"
#include "../include/battle_cat_dll.h"
#include "../include/battle_cat_roll.h"

// Global storage for banners
static std::unordered_map<std::string, Banner> g_banners;
static bool g_bannersInitialized = false;
static ProgressCallback g_progressCallback = nullptr;

// Initialize banners on first use
void InitializeBannersIfNeeded() {
    if (!g_bannersInitialized) {
		g_banners = BattleCatData::getInstance().getBanners();
        g_bannersInitialized = true;
    }
}


// Wrapper class to handle C++ exceptions
class BattleCatRollWrapper {
public:
    std::unique_ptr<BattleCatRoll> roll;
    Banner banner;

    BattleCatRollWrapper(const Banner& b, seed_t seed) : banner(b) {
        roll = std::make_unique<BattleCatRoll>(banner, seed);
    }
};


extern "C" {
    __declspec(dllexport) BattleCatRollHandle __cdecl CreateBCRTest() {
        try {
            InitializeBannersIfNeeded();
            auto it = g_banners.find("test");
            if (it == g_banners.end()) {
                return nullptr;
            }
            BattleCatRollWrapper* wrapper = new BattleCatRollWrapper(it->second, 42);
            return static_cast<BattleCatRollHandle>(wrapper);
        }
        catch (...) {
            return nullptr;
		}
    }

    __declspec(dllexport) BattleCatRollHandle __cdecl CreateBattleCatRoll(const char* bannerName, seed_t initialSeed) {
        try {
            InitializeBannersIfNeeded();

            std::string name(bannerName);
            auto it = g_banners.find(name);
            if (it == g_banners.end()) {
                return nullptr;
            }

            BattleCatRollWrapper* wrapper = new BattleCatRollWrapper(it->second, initialSeed);
            return static_cast<BattleCatRollHandle>(wrapper);
        }
        catch (...) {
            return nullptr;
        }
    }

    void DestroyBattleCatRoll(BattleCatRollHandle handle) {
        if (handle) {
            BattleCatRollWrapper* wrapper = static_cast<BattleCatRollWrapper*>(handle);
            delete wrapper;
        }
    }

    seed_t AdvanceSeed(seed_t seed) {
        return advance_seed(seed);
    }

    seed_t RevertSeed(seed_t targetSeed) {
        return revert_seed(targetSeed);
    }

    seed_t GetSeed(BattleCatRollHandle handle) {
        if (!handle) return 0;
        try {
            BattleCatRollWrapper* wrapper = static_cast<BattleCatRollWrapper*>(handle);
            return wrapper->roll->getSeed();
        }
        catch (...) {
            return 0;
        }
	}

    void SetSeed(BattleCatRollHandle handle, seed_t seed) {
        if (!handle) return;

        try {
            BattleCatRollWrapper* wrapper = static_cast<BattleCatRollWrapper*>(handle);
            wrapper->roll->setSeed(seed);
        }
        catch (...) {
            // Silently handle exceptions
        }
    }

    __declspec(dllexport) const char** __cdecl GetBannerNames(int* count) {
        try {
            auto& catData = BattleCatData::getInstance();
            static std::vector<std::string> names = catData.getBannerNames();

            // Store C-string pointers
            static std::vector<const char*> cstrs;
            cstrs.clear();
            for (auto& n : names) {
                cstrs.push_back(n.c_str());
            }

            if (count) {
                *count = static_cast<int>(cstrs.size());
            }

            return cstrs.data();
        }
        catch (...) {
            if (count) *count = 0;
            return nullptr;
        }
    }

    __declspec(dllexport) void __cdecl SetBanner(BattleCatRollHandle handle, const char* bannerName) {
        if (!handle || !bannerName) return;
        try {
            BattleCatRollWrapper* wrapper = static_cast<BattleCatRollWrapper*>(handle);
            std::string name(bannerName);
            auto it = g_banners.find(name);
            if (it != g_banners.end()) {
                wrapper->banner = it->second;
                wrapper->roll->setBanner(wrapper->banner);
				//std::cout << "Banner set to: " << name << std::endl;
            }
        }
        catch (...) {
            // Silently handle exceptions
        }
	}


    uint32_t Roll(BattleCatRollHandle handle) {
        if (!handle) return 0;

        try {
            BattleCatRollWrapper* wrapper = static_cast<BattleCatRollWrapper*>(handle);
            return wrapper->roll->roll();
        }
        catch (...) {
            return 0;
        }
    }

    uint32_t RollUncheck(BattleCatRollHandle handle) {
        if (!handle) return 0;

        try {
            BattleCatRollWrapper* wrapper = static_cast<BattleCatRollWrapper*>(handle);
            return wrapper->roll->rollUncheck();
        }
        catch (...) {
            return 0;
        }
    }

    void RollMultiple(BattleCatRollHandle handle, int rollNum, uint32_t* results) {
        if (!handle || !results || rollNum <= 0) return;

        try {
            BattleCatRollWrapper* wrapper = static_cast<BattleCatRollWrapper*>(handle);
            std::vector<uint32_t> rolls = wrapper->roll->rolls(rollNum);

            for (int i = 0; i < rollNum && i < static_cast<int>(rolls.size()); i++) {
                results[i] = rolls[i];
            }
        }
        catch (...) {
            // Fill with zeros on error
            for (int i = 0; i < rollNum; i++) {
                results[i] = 0;
            }
        }
    }

    void Roll11Guaranteed(BattleCatRollHandle handle, uint32_t* results) {
        if (!handle || !results) return;

        try {
            BattleCatRollWrapper* wrapper = static_cast<BattleCatRollWrapper*>(handle);
            std::vector<uint32_t> rolls = wrapper->roll->rolls_11guaranteed();

            for (int i = 0; i < 11 && i < static_cast<int>(rolls.size()); i++) {
                results[i] = rolls[i];
            }
        }
        catch (...) {
            // Fill with zeros on error
            for (int i = 0; i < 11; i++) {
                results[i] = 0;
            }
        }
    }

    __declspec(dllexport) uint32_t __cdecl RollGuaranteed(BattleCatRollHandle handle) {
		if (!handle) return 0;

        try {
            BattleCatRollWrapper* wrapper = static_cast<BattleCatRollWrapper*>(handle);
			seed_t seedBak = wrapper->roll->getSeed(); // Save current seed
            std::vector<uint32_t> rolls = wrapper->roll->rolls_11guaranteed();
            wrapper->roll->setSeed(seedBak);
            wrapper->roll->roll();
            return rolls[rolls.size() - 1]; // Return the last roll as the guaranteed unit
        }
        catch (...) {
			return 0; // Return 0 on error
        }
    }

    int GetUnitId(BattleCatRollHandle handle, uint32_t unitIndex) {
        if (!handle) return -1;

        try {
            BattleCatRollWrapper* wrapper = static_cast<BattleCatRollWrapper*>(handle);
			return wrapper->banner.idxToId[unitIndex];
        }
        catch (...) {
            return -1;
        }
    }

    int GetUnitName(BattleCatRollHandle handle, uint32_t unitIndex, char* buffer, int bufferSize) {
        if (!handle || !buffer || bufferSize <= 0) return 0;

        try {
            BattleCatRollWrapper* wrapper = static_cast<BattleCatRollWrapper*>(handle);

            if (unitIndex >= wrapper->banner.idxToName.size()) {
                return 0;
            }

            const std::string& name = wrapper->banner.idxToName[unitIndex];
            int nameLength = static_cast<int>(name.length());

            if (nameLength >= bufferSize) {
                // Truncate to fit in buffer (leaving space for null terminator)
                // std::strncpy(buffer, name.c_str(), bufferSize - 1);
                // buffer[bufferSize - 1] = '\0';

                for (int i = 0; i < bufferSize - 1 && i < nameLength; i++) {
                    buffer[i] = name[i];
				}
				buffer[bufferSize - 1] = '\0';

                return bufferSize - 1;
            }
            else {
                // std::strcpy(buffer, name.c_str());
				// std::strncpy(buffer, name.c_str(), nameLength);
                for (int i = 0; i < nameLength; i++) {
					buffer[i] = name[i];
				}
				buffer[nameLength] = '\0';
                return nameLength;
            }
        }
        catch (...) {
            buffer[0] = '\0';
            return 0;
        }
    }

    int GetUnitRarity(BattleCatRollHandle handle, uint32_t unitIndex) {
        if (!handle) return -1;

        try {
            BattleCatRollWrapper* wrapper = static_cast<BattleCatRollWrapper*>(handle);

            if (unitIndex >= wrapper->banner.idxToName.size()) {
                return -1;
            }

            // Find rarity based on rarityCumCount
            for (int rarity = 0; rarity < static_cast<int>(wrapper->banner.rarityCumCount.size()); rarity++) {
                if (unitIndex < wrapper->banner.rarityCumCount[rarity]) {
                    return rarity;
                }
            }

            return static_cast<int>(wrapper->banner.rarityCumCount.size()) - 1;
        }
        catch (...) {
            return -1;
        }
    }

    int FindSeedsByNames(BattleCatRollHandle handle, const char** targetNames, int nameCount,
        uint32_t numThreads, seed_t* results, int maxResults) {
        if (!handle || !targetNames || nameCount <= 0 || !results || maxResults <= 0) return 0;

        try {
            BattleCatRollWrapper* wrapper = static_cast<BattleCatRollWrapper*>(handle);

            std::vector<std::string> names;
            names.reserve(nameCount);
            for (int i = 0; i < nameCount; i++) {
                if (targetNames[i]) {
                    names.emplace_back(targetNames[i]);
                }
            }

            std::vector<seed_t> foundSeeds = wrapper->roll->findSeed(names, numThreads);

            // int resultCount = std::min(static_cast<int>(foundSeeds.size()), maxResults);
			int foundCount = static_cast<int>(foundSeeds.size());
			int resultCount = foundCount < maxResults ? foundCount : maxResults;
            for (int i = 0; i < resultCount; i++) {
                results[i] = foundSeeds[i];
            }

            return resultCount;
        }
        catch (...) {
            return 0;
        }
    }

    int FindSeedsByIndices(BattleCatRollHandle handle, const uint32_t* targetIndices, int indexCount,
        uint32_t numThreads, seed_t* results, int maxResults) {
        if (!handle || !targetIndices || indexCount <= 0 || !results || maxResults <= 0) return 0;

        try {
            BattleCatRollWrapper* wrapper = static_cast<BattleCatRollWrapper*>(handle);

            std::vector<uint32_t> indices(targetIndices, targetIndices + indexCount);
            std::vector<seed_t> foundSeeds = wrapper->roll->findSeed(indices, numThreads);

            // int resultCount = std::min(static_cast<int>(foundSeeds.size()), maxResults);
            int foundCount = static_cast<int>(foundSeeds.size());
			int resultCount = foundCount < maxResults ? foundCount : maxResults;
            for (int i = 0; i < resultCount; i++) {
                results[i] = foundSeeds[i];
            }

            return resultCount;
        }
        catch (...) {
            return 0;
        }
    }

    void SetProgressCallback(ProgressCallback callback) {
        g_progressCallback = callback;
    }

} // extern "C"
