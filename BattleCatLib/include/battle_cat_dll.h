#pragma once

#include "pch.h"

#ifdef _WIN32
#ifdef BATTLE_CAT_EXPORTS
#define BATTLE_CAT_API __declspec(dllexport)
#else
#define BATTLE_CAT_API __declspec(dllimport)
#endif
#else
#define BATTLE_CAT_API
#endif

#ifdef __cplusplus
extern "C" {
#endif

    // C-style interface for C# interop
    typedef unsigned int uint32_t;
    typedef uint32_t seed_t;

    // Handle for BattleCatRoll instance
    typedef void* BattleCatRollHandle;

    // Banner creation and management
    BATTLE_CAT_API BattleCatRollHandle CreateBattleCatRoll(const char* bannerName, seed_t initialSeed);
    BATTLE_CAT_API void DestroyBattleCatRoll(BattleCatRollHandle handle);

    // Seed operations
    BATTLE_CAT_API seed_t AdvanceSeed(seed_t seed);
    BATTLE_CAT_API seed_t RevertSeed(seed_t targetSeed);
    BATTLE_CAT_API void SetSeed(BattleCatRollHandle handle, seed_t seed);

    // Roll operations, return unit **index**
    BATTLE_CAT_API uint32_t Roll(BattleCatRollHandle handle);
    BATTLE_CAT_API uint32_t RollUncheck(BattleCatRollHandle handle);
    BATTLE_CAT_API void RollMultiple(BattleCatRollHandle handle, int rollNum, uint32_t* results);
    BATTLE_CAT_API void Roll11Guaranteed(BattleCatRollHandle handle, uint32_t* results);
    BATTLE_CAT_API uint32_t RollGuaranteed(BattleCatRollHandle handle);

    // Utility functions
    BATTLE_CAT_API int GetUnitId(BattleCatRollHandle handle, uint32_t unitIndex);
    BATTLE_CAT_API int GetUnitName(BattleCatRollHandle handle, uint32_t unitIndex, char* buffer, int bufferSize);
    BATTLE_CAT_API int GetUnitRarity(BattleCatRollHandle handle, uint32_t unitIndex);

    // Seed finding functions
    BATTLE_CAT_API int FindSeedsByNames(BattleCatRollHandle handle, const char** targetNames, int nameCount,
        uint32_t numThreads, seed_t* results, int maxResults);
    BATTLE_CAT_API int FindSeedsByIndices(BattleCatRollHandle handle, const uint32_t* targetIndices, int indexCount,
        uint32_t numThreads, seed_t* results, int maxResults);

    // Progress callback for seed finding
    typedef void (*ProgressCallback)(float percentage);
    BATTLE_CAT_API void SetProgressCallback(ProgressCallback callback);

#ifdef __cplusplus
}
#endif
