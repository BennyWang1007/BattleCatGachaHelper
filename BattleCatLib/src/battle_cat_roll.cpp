#include <atomic>
#include <cstdint>
#include <iostream>
#include <mutex>
#include <string>
#include <thread>
#include <utility>
#include <vector>

#include "../include/pch.h"
#include "../include/battle_cat_roll.h"

using namespace std;

mutex seedsMutex;

seed_t advance_seed(seed_t seed)
{
    seed ^= seed << 13;
    seed ^= seed >> 17;
    seed ^= seed << 15;
    return seed;
}

seed_t revert_seed(seed_t target_seed)
{
    for (seed_t seed = 0; seed < 0xFFFFFFFF; seed++) {
        if (advance_seed(seed) == target_seed) {
            return seed;
        }
    }
    return 0;
}

// Find the rarity of the unit, given the seed and rateCumSum
uint32_t getRarity(seed_t seed, const vector<int>& rateCumSum)
{
    int rarity = seed % 10000;
    for (size_t i = 0; i < rateCumSum.size(); ++i) {
        if (rarity < rateCumSum[i]) {
            return (uint32_t)(i);
        }
    }
    throw runtime_error("Rarity not found");
}

pair<uint32_t, string> getUnit(seed_t seed,
    const vector<string>& units,
    const vector<uint32_t>& removedIndices)
{
    uint32_t numUnitsInPool = (uint32_t)(units.size() - removedIndices.size());
    uint32_t seedMod = seed % numUnitsInPool;
    for (uint32_t removedIndex : removedIndices) {
        if (seedMod >= removedIndex) {
            seedMod++;
        }
    }
    return { seedMod, units[seedMod] };
}

uint32_t static getUnitIdx(seed_t seed,
    const vector<uint32_t>& units,
    const vector<uint32_t>& removedIndices)
{
    uint32_t numUnitsInPool = (uint32_t)(units.size() - removedIndices.size());
    uint32_t seedMod = seed % numUnitsInPool;
    for (uint32_t removedIndex : removedIndices) {
        if (seedMod >= removedIndex) {
            seedMod++;
        }
    }
    return units[seedMod];
}

void BattleCatRoll::setBanner(const string& bannerName) {
    std::unordered_map<string, Banner> banners = BattleCatData::getInstance().getBanners();
    auto it = banners.find(bannerName);
    if (it != banners.end()) {
        this->banner = it->second;
    }
    else {
        std::cerr << "Banner not found: " << bannerName << std::endl;
    }
}

vector<RollUnit> generateRolls(seed_t seed, size_t numRolls, const Banner& banner)
{
    vector<RollUnit> rolls;
    rolls.reserve(numRolls);
    string lastRoll = "";
    for (size_t i = 0; i < numRolls; i++) {
        RollUnit roll;
        seed = advance_seed(seed);
        uint32_t raritySeed = seed;
        uint32_t rarity = getRarity(raritySeed, banner.rateCumSum);
        roll.raritySeed = raritySeed;
        roll.rarity = rarity;
        seed = advance_seed(seed);
        seed_t unitSeed = seed;
        auto [unitIndex, unitName] =
            getUnit(unitSeed, banner.pools[rarity].units, {});
        // cout << "Unit: " << unitName << endl;
        if (unitName == lastRoll && banner.pools[rarity].reroll) {
            seed_t rerollSeed = unitSeed;
            string rerollUnitName = unitName;
            vector<uint32_t> rerollRemovedIndices = { unitIndex };
            int rerollTimes = 0;
            while (rerollUnitName == unitName) {
                rerollTimes++;
                rerollSeed = advance_seed(rerollSeed);
                auto [rerollIndex, rerollUnitName] =
                    getUnit(rerollSeed, banner.pools[rarity].units,
                        rerollRemovedIndices);
                rerollRemovedIndices.push_back(rerollIndex);
            }
            roll.unit = rerollUnitName;
            roll.nextSeed = rerollSeed;
        }
        else {
            roll.unit = unitName;
            roll.nextSeed = unitSeed;
        }
        rolls.emplace_back(roll);
    }
    return rolls;
}

// bool checkSeed(seed_t seed,
//                const vector<string> &targetRolls,
//                const Banner &banner)
// {
//     string lastRoll = "";
//     for (const string &targetRoll : targetRolls) {
//         seed = advance_seed(seed);
//         uint32_t rarity = getRarity(seed, banner.rateCumSum);
//         seed = advance_seed(seed);
//         auto [unitIndex, unitName] =
//             getUnit(seed, banner.pools[rarity].units, {});
//         if (unitName == lastRoll && banner.pools[rarity].reroll) {
//             seed = advance_seed(seed);
//             auto [_, rerolledUnitName] =
//                 getUnit(seed, banner.pools[rarity].units, {unitIndex});
//             if (targetRoll != rerolledUnitName) {
//                 return false;
//             }
//             lastRoll = rerolledUnitName;
//         } else {
//             if (targetRoll != unitName) {
//                 return false;
//             }
//             lastRoll = unitName;
//         }
//     }
//     return true;
// }

/*
void seedFinderThread(seed_t start,
                      seed_t end,
                      const vector<string> &targetRolls,
                      vector<uint32_t> &seeds,
                      atomic<size_t> &progressCounter,
                      const Banner &banner)
{
    int localProgressCounter = 0;
    Roll roll;

    for (seed_t seed = start; seed < end; seed++) {
        if (checkSeed(seed, targetRolls, banner)) {
            lock_guard<mutex> guard(seedsMutex);
            seeds.emplace_back(seed);
        }

        localProgressCounter++;
        if (localProgressCounter > 0x00100000) {
            progressCounter += localProgressCounter;
            localProgressCounter = 0;
        }
    }
    progressCounter += localProgressCounter;
}
*/

void static seedFinderThread2(seed_t start,
    seed_t end,
    const vector<uint32_t>& targetRolls,
    vector<seed_t>& seeds,
    atomic<size_t>& progressCounter,
    const Banner& banner)
{
    int localProgressCounter = 0;
    RollUnit roll;

    BattleCatRoll bcr = BattleCatRoll(banner);

    for (seed_t seed = start; seed < end; seed++) {
        bcr.setSeed(seed);
        if (bcr.checkSeed(targetRolls)) {
            lock_guard<mutex> guard(seedsMutex);
            seeds.emplace_back(seed);
        }

        localProgressCounter++;
        if (localProgressCounter > 0x00100000) {
            progressCounter += localProgressCounter;
            localProgressCounter = 0;
        }
    }
    progressCounter += localProgressCounter;
}

// vector<seed_t> seedFinder(const vector<string> &targetRolls,
//                           const Banner &banner,
//                           size_t numThreads)
// {
//     vector<seed_t> seeds;
//     if (numThreads == 0) {
//         numThreads = thread::hardware_concurrency();
//     }
//     vector<thread> threads;
//     atomic<size_t> progressCounter(0);

//     unordered_map<string, uint32_t> nameToIndex;
//     const auto &name_vec = banner.idxToName;
//     for (uint32_t i = 0; i < name_vec.size(); ++i) {
//         nameToIndex[name_vec[i]] = i;
//     }

//     vector<uint32_t> target_idxs;
//     for (const auto &targetName : targetRolls) {
//         auto it = nameToIndex.find(targetName);
//         if (it != nameToIndex.end()) {
//             target_idxs.emplace_back(it->second);
//         } else {
//             throw runtime_error("Target name not found: " + targetName);
//         }
//     }

//     seed_t range = 0xFFFFFFFF / numThreads;
//     for (unsigned int i = 0; i < numThreads; ++i) {
//         seed_t start = i * range;
//         seed_t end = (i + 1 == numThreads) ? 0xFFFFFFFF : start + range;
//         threads.emplace_back(seedFinderThread2, start, end,
//         cref(target_idxs),
//                              ref(seeds), ref(progressCounter), cref(banner));
//     }

//     // Progress monitoring
//     size_t lastProgress = 0;
//     while (lastProgress < 0xFFFFFFFF) {
//         this_thread::sleep_for(chrono::seconds(1));  // Update every second
//         size_t progress = progressCounter.load();
//         cout << "Progress: " << (float) progress / 0xFFFFFFFF * 100 << "%\n";
//         if (progress == lastProgress) {
//             break;  // No progress, all threads must have finished
//         }
//         lastProgress = progress;
//     }

//     for (auto &t : threads) {
//         t.join();
//     }

//     return seeds;
// }

BattleCatRoll::BattleCatRoll(const Banner& banner, seed_t seed)
    : banner(banner), seed(seed), lastRoll(UINT32_MAX), switchCount(0) {
}

seed_t BattleCatRoll::advanceSeed()
{
    seed ^= seed << 13;
    seed ^= seed >> 17;
    seed ^= seed << 15;
    return seed;
}

void BattleCatRoll::setSeed(seed_t seed)
{
    this->seed = seed;
}

uint32_t BattleCatRoll::roll()
{
    seed_t raritySeed = advanceSeed();
    seed_t unitSeed = advanceSeed();

    uint32_t rarity = getRarity(raritySeed, banner.rateCumSum);
    uint32_t offset = banner.rarityCumCount[rarity];
    Pool* pool = &banner.pools[rarity];
    uint32_t unitIndex = getUnitIdx(unitSeed, pool->indexes, {});

    if (unitIndex == lastRoll && pool->reroll) {
        uint32_t rerollSeed;
        vector<uint32_t> removedIdxs = { unitIndex - offset };
        do {
            switchCount++;
            rerollSeed = advanceSeed();
            unitIndex = getUnitIdx(rerollSeed, pool->indexes, removedIdxs);
            removedIdxs.emplace_back(unitIndex - offset);
        } while (unitIndex == lastRoll);
    }
    lastRoll = unitIndex;
    return unitIndex;
}

uint32_t BattleCatRoll::rollUncheck()
{
    seed_t raritySeed = advanceSeed();
    seed_t unitSeed = advanceSeed();

    uint32_t rarity = getRarity(raritySeed, banner.rateCumSum);
    uint32_t offset = banner.rarityCumCount[rarity];
    Pool* pool = &banner.pools[rarity];
    uint32_t unitIndex = getUnitIdx(unitSeed, pool->indexes, {});
    return unitIndex;
}

uint32_t BattleCatRoll::rollWithRarity(uint32_t rarity)
{
    // FIXME: the result is incorrect
    // if (banner.pools.size() < 4)
    //     return 0;
    // std::cout << "Rolling with rarity: " << rarity << ", seed: " << seed << std::endl;
    // seed = advanceSeed();
    // seed = advanceSeed();
    Pool* pool = &banner.pools[rarity];
    //pool->print();
    uint32_t unitIndex = getUnitIdx(seed, pool->indexes, {});
    return unitIndex;
}

RollUnit BattleCatRoll::idxToRoll(uint32_t rolledIdxs)
{
    RollUnit r = { 0, 0, 0, banner.idxToName[rolledIdxs], 0 };
    for (auto& cumNum : banner.rarityCumCount) {
        if (rolledIdxs >= cumNum) {
            r.rarity++;
        }
    }
    r.rarity--;
    return r;
}

vector<RollUnit> BattleCatRoll::idxToRolls(vector<uint32_t> rolledIdxs)
{
    vector<RollUnit> rs;
    rs.reserve(rolledIdxs.size());
    for (auto& rolledIdx : rolledIdxs) {
        rs.emplace_back(idxToRoll(rolledIdx));
    }
    return rs;
}

vector<seed_t> BattleCatRoll::rolls(int rollNum)
{
    vector<seed_t> rolled;
    rolled.reserve(rollNum);
    for (int i = 0; i < rollNum; ++i) {
        rolled.emplace_back(roll());
    }
    return rolled;
}

vector<seed_t> BattleCatRoll::rolls_11guaranteed()
{
    if (banner.guaranteed_rarity == (int)RarityClass::None) {
        throw std::runtime_error("Error: rolls_11guaranteed should not be called without guaranteed rarity.");
    }
    switchCount = 0;
    vector<seed_t> rolled;
    rolled.reserve(11);
    for (int i = 0; i < 10; ++i) {
        rolled.emplace_back(roll());
    }
    seed_t unitSeed = advanceSeed();
    uint32_t rarity = banner.guaranteed_rarity;
    uint32_t offset = banner.rarityCumCount[rarity];
    Pool* pool = &banner.pools[rarity];
    uint32_t unitIndex = getUnitIdx(unitSeed, pool->indexes, {});
    if (unitIndex == lastRoll && pool->reroll) {
        seed_t rerollSeed;
        vector<uint32_t> removedIdxs = { unitIndex - offset };
        do {
            rerollSeed = advanceSeed();
            unitIndex = getUnitIdx(rerollSeed, pool->indexes, removedIdxs);
            removedIdxs.emplace_back(unitIndex - offset);
        } while (unitIndex == lastRoll);
    }
    lastRoll = unitIndex;
    rolled.emplace_back(unitIndex);
    return rolled;
}

bool BattleCatRoll::checkSeed(const vector<uint32_t>& targets)
{
    for (uint32_t target : targets) {
        uint32_t rolled = roll();
        if (rolled != target)
            return false;
    }
    return true;
}

vector<seed_t> BattleCatRoll::findSeed(const vector<string>& targetNames,
    uint32_t numThreads)
{
    unordered_map<string, uint32_t> nameToIndex;
    const auto& name_vec = banner.idxToName;
    for (uint32_t i = 0; i < name_vec.size(); ++i) {
        nameToIndex[name_vec[i]] = i;
    }

    vector<uint32_t> target_idxs;
    for (const auto& targetName : targetNames) {
        auto it = nameToIndex.find(targetName);
        if (it != nameToIndex.end()) {
            target_idxs.emplace_back(it->second);
        }
        else {
            throw runtime_error("Target name not found: " + targetName);
        }
    }

    return findSeed(target_idxs, numThreads);
}


vector<seed_t> BattleCatRoll::findSeed(const vector<uint32_t>& targetIdxs,
    uint32_t numThreads)
{
    vector<seed_t> seeds;
    if (numThreads == 0) {
        numThreads = thread::hardware_concurrency();
    }
    vector<thread> threads;
    atomic<size_t> progressCounter(0);

    seed_t range = 0xFFFFFFFF / numThreads;
    for (unsigned int i = 0; i < numThreads; ++i) {
        seed_t start = i * range;
        seed_t end = (i + 1 == numThreads) ? 0xFFFFFFFF : start + range;
        threads.emplace_back(seedFinderThread2, start, end, cref(targetIdxs),
            ref(seeds), ref(progressCounter), cref(banner));
    }

    // Progress monitoring
    size_t lastProgress = 0;
    while (lastProgress < 0xFFFFFFFF) {
        this_thread::sleep_for(chrono::seconds(1));  // Update every second
        size_t progress = progressCounter.load();
        cout << "Progress: " << (float)progress / 0xFFFFFFFF * 100 << "%\n";
        if (progress == lastProgress) {
            break;  // No progress, all threads must have finished
        }
        lastProgress = progress;
    }

    for (auto& t : threads) {
        t.join();
    }

    return seeds;
}
