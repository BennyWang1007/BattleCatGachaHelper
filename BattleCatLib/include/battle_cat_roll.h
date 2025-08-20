#pragma once

#include <atomic>
#include <cstdint>
#include <iostream>
#include <mutex>
#include <string>
#include <thread>
#include <utility>
#include <vector>
#include <unordered_map>

#include "banner.h"
#include "battle_cat_data.h"

#ifdef _WIN32
#include <windows.h>
#define ENABLE_UTF8_CONSOLE() SetConsoleOutputCP(CP_UTF8)
#else
// On Linux/Unix/macOS, UTF-8 is typically the default
#define ENABLE_UTF8_CONSOLE() ((void) 0)  // No-op
#endif

using std::string, std::vector, std::pair, std::mutex, std::thread, std::atomic;

typedef uint32_t seed_t;

// return the next seed
seed_t advance_seed(seed_t seed);

// return the seed that generates the target_seed
seed_t revert_seed(seed_t target_seed);

/**
 * @brief Get the Rarity index
 *
 * @param seed
 * @param rateCumSum
 * @return find the rarity of the unit, given the seed and rateCumSum
 */
uint32_t getRarity(seed_t seed, const vector<int>& rateCumSum);

/**
 * @brief Get the Unit object
 *
 * @param seed
 * @param units list of units in the pool of the current rarity
 * @param removedIndices list of indices that should be removed from the pool
 * @return pair<int, string> of the unit index and unit name
 */
pair<uint32_t, string> getUnit(seed_t seed,
    const vector<string>& units,
    const vector<uint32_t>& removedIndices);

// generate rolls based on the seed
vector<RollUnit> generateRolls(seed_t seed, size_t numRolls, const Banner& banner);

// check if the seed generates the target rolls
// bool checkSeed(seed_t seed,
//                const vector<string> &targetRolls,
//                const Banner &banner);

// function for each thread to find seeds
// void seedFinderThread(seed_t start,
//                       seed_t end,
//                       const vector<string> &targetRolls,
//                       vector<seed_t> &seeds,
//                       atomic<size_t> &progressCounter,
//                       const Banner &banner);

// find seeds that generate the target rolls
// vector<seed_t> seedFinder(const vector<string> &targetRolls,
//                           const Banner &banner,
//                           size_t numThreads = 0);

class BattleCatRoll
{
public:
    BattleCatRoll(const Banner& banner, seed_t seed = 0);
	~BattleCatRoll() = default;
    seed_t advanceSeed();
	seed_t getSeed() const { return seed; }
    void setSeed(seed_t seed);

    // roll one time, return the unit index
    uint32_t roll();
    uint32_t rollUncheck();

    // roll rollNum times, return vector of the unit indexes
    vector<uint32_t> rolls(int rollNum);
    // roll 11 times with last poll of gaurantee
    vector<uint32_t> rolls_11guaranteed();

    bool checkSeed(const vector<seed_t>& targets);
    vector<seed_t> findSeed(const vector<uint32_t>& targets, uint32_t numThreads = 0);
    vector<seed_t> findSeed(const vector<string>& targets, uint32_t numThreads = 0);

    // convert from index to Roll object(s)
    RollUnit idxToRoll(uint32_t rolledIdxs);
    vector<RollUnit> idxToRolls(vector<uint32_t> rolledIdxs);

    vector<string> getBannerNames() const {
        return BattleCatData::getInstance().getBannerNames();
	}
    void setBanner(const Banner& banner) { this->banner = banner; }
    void setBanner(const string& bannerName);

private:
    Banner banner;
    seed_t seed;
    uint32_t lastRoll;
};
