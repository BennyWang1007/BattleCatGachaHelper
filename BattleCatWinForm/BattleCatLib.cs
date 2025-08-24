using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using BattleCatWinForm;

public static class BattleCatLib
{
    private const string DLL_NAME = "BattleCatLib.dll";

    // Handle type
    public struct BattleCatRollHandle
    {
        public IntPtr Ptr;
    }

    // C-compatible structures for P/Invoke
    [StructLayout(LayoutKind.Sequential)]
    public struct CPool
    {
        public int rate;
        public IntPtr units;           // char**
        public IntPtr indexes;         // uint32_t*
        public uint unitCount;
        [MarshalAs(UnmanagedType.I1)]
        public bool reroll;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CBanner
    {
        public IntPtr rateCumSum;      // int*
        public IntPtr rarityCumCount;  // uint32_t*
        public IntPtr pools;           // CPool*
        public IntPtr idxToName;       // char**
        public IntPtr idxToId;         // int*
        public uint poolCount;
        public uint totalUnits;
        public uint guaranteed_rarity;
    }

    // BCR creation and destruction
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateBattleCatRoll(string bannerName, uint initialSeed);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void DestroyBattleCatRoll(IntPtr handle);

    // [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    // public static extern IntPtr CreateBCRTest();

    // Seed operations
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint AdvanceSeed(uint seed);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint RevertSeed(uint targetSeed);

    // Gets / Sets
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetBannerNames(out int count);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetSeed(IntPtr handle, uint seed);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetBanner(IntPtr handle, string bannerName);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint Roll(IntPtr handle);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint RollUncheck(IntPtr handle);
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint RollWithRarity(IntPtr handle, UInt32 seed, UInt32 rarity);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void RollMultiple(IntPtr handle, int rollNum, [Out] uint[] results);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void RollMultipleUncheck(IntPtr handle, int rollNum, [Out] uint[] results);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Roll11Guaranteed(IntPtr handle, [Out] uint[] results);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint RollGuaranteed(IntPtr handle, out uint switchCount);

    // Utility functions
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetUnitId(IntPtr handle, uint unitIndex);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    private static extern int GetUnitName(IntPtr handle, uint unitIndex, StringBuilder buffer, int bufferSize);

    public static string GetUnitName(IntPtr handle, uint unitIndex)
    {
        StringBuilder sb = new StringBuilder(256);
        int len = GetUnitName(handle, unitIndex, sb, sb.Capacity);
        return (len > 0) ? sb.ToString() : string.Empty;
    }

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetUnitRarity(IntPtr handle, uint unitIndex);

    // Seed finding
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int FindSeedsByNames(IntPtr handle, string[] targetNames, int nameCount,
                                              uint numThreads, [Out] uint[] results, int maxResults);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int FindSeedsByIndices(IntPtr handle, uint[] targetIndices, int indexCount,
                                                uint numThreads, [Out] uint[] results, int maxResults);

    // Progress callback
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ProgressCallback(float percentage);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetProgressCallback(ProgressCallback callback);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool SetBanner(IntPtr handle, string bannerName, ref CBanner banner);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    public static extern bool AddBannerSimple(
        IntPtr handle,
        string bannerName,
        int[] rateCumSum,
        uint[] rarityCumCount,
        int[] poolRates,
        IntPtr poolUnits,        // char***
        IntPtr poolIndexes,      // uint32_t**
        uint[] poolUnitCounts,
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I1)] bool[] poolRerolls,
        uint poolCount,
        // string[] idxToName,
        IntPtr[] idxToName,
        int[] idxToId,
        uint totalUnits,
        uint guaranteed_rarity
    );
    
    // High-level C# method to set banner using Banner object
    public static bool SetBanner(IntPtr handle, string bannerName, Banner banner)
    {
        try
        {
            var poolCount = (uint)banner.Pools.Length;
            var totalUnits = (uint)banner.IdxToName.Count;
            
            // Prepare arrays
            var rateCumSum = new int[poolCount];
            var rarityCumCount = new uint[poolCount];
            var poolRates = new int[poolCount];
            var poolUnitCounts = new uint[poolCount];
            var poolRerolls = new bool[poolCount];
            
            // Calculate cumulative sums and prepare pool data
            uint cumCount = 0;
            int cumRate = 0;
            for (int i = 0; i < poolCount; i++)
            {
                poolRates[i] = (int)banner.RarityRates[i];
                poolUnitCounts[i] = (uint)banner.Pools[i].Cats.Count;
                poolRerolls[i] = banner.Pools[i].Reroll;

                cumRate += poolRates[i];
                rateCumSum[i] = cumRate;
                rarityCumCount[i] = cumCount;
                cumCount += poolUnitCounts[i];
            }
            
            // Prepare pool units (char***)
            var poolUnitsHandles = new IntPtr[poolCount];
            var poolIndexesHandles = new IntPtr[poolCount];
            var allStringHandles = new List<IntPtr>();

            var idxToNamePtrs = new IntPtr[banner.IdxToName.Count];
            var idxNameHandles = new List<IntPtr>();

            try
            {
                uint idxCounter = 0;
                for (int i = 0; i < poolCount; i++)
                {
                    var pool = banner.Pools[i];

                    // Allocate array for unit names in this pool
                    var unitPtrs = new IntPtr[pool.Cats.Count];
                    for (int j = 0; j < pool.Cats.Count; j++)
                    {
                        // var stringPtr = Marshal.StringToHGlobalAnsi(pool.Cats[j]);
                        // TODO
                        var stringPtr = Marshal.StringToHGlobalAnsi("Unit " + j);
                        unitPtrs[j] = stringPtr;
                        allStringHandles.Add(stringPtr);
                    }

                    // Allocate and copy unit name pointers
                    var unitsPtrArray = Marshal.AllocHGlobal(IntPtr.Size * pool.Cats.Count);
                    Marshal.Copy(unitPtrs, 0, unitsPtrArray, pool.Cats.Count);
                    poolUnitsHandles[i] = unitsPtrArray;

                    // Allocate and copy indexes
                    // var indexes = pool.Indexes.ToArray();
                    // var indexesPtr = Marshal.AllocHGlobal(sizeof(uint) * indexes.Length);
                    // Marshal.Copy(indexes.Select(x => (int)x).ToArray(), 0, indexesPtr, indexes.Length);
                    // poolIndexesHandles[i] = indexesPtr;

                    // Communicate IdxToName in Big5
                    Encoding big5 = Encoding.GetEncoding(950); // 950 = Big5 codepage

                    for (int j = 0; j < banner.IdxToName.Count; j++)
                    {
                        byte[] big5Bytes = big5.GetBytes(banner.IdxToName[j] + "\0");
                        IntPtr strPtr = Marshal.AllocHGlobal(big5Bytes.Length);
                        Marshal.Copy(big5Bytes, 0, strPtr, big5Bytes.Length);
                        idxToNamePtrs[j] = strPtr;
                        idxNameHandles.Add(strPtr);
                    }

                    var indexes = new int[pool.Cats.Count];
                    for (int j = 0; j < pool.Cats.Count; j++)
                    {
                        indexes[j] = (int)idxCounter++;
                    }
                    var indexesPtr = Marshal.AllocHGlobal(sizeof(int) * indexes.Length);
                    Marshal.Copy(indexes, 0, indexesPtr, indexes.Length);
                    poolIndexesHandles[i] = indexesPtr;
                }

                // Allocate top-level arrays
                var poolUnitsPtr = Marshal.AllocHGlobal(IntPtr.Size * (int)poolCount);
                Marshal.Copy(poolUnitsHandles, 0, poolUnitsPtr, (int)poolCount);

                var poolIndexesPtr = Marshal.AllocHGlobal(IntPtr.Size * (int)poolCount);
                Marshal.Copy(poolIndexesHandles, 0, poolIndexesPtr, (int)poolCount);

                for (int i = 0; i < poolCount; i++)
                {
                    System.Diagnostics.Debug.WriteLine($"PoolRerolls[{i}] = {poolRerolls[i]}");
                }

                try
                {
                    return AddBannerSimple(
                        handle,
                        bannerName,
                        rateCumSum,
                        rarityCumCount,
                        poolRates,
                        poolUnitsPtr,
                        poolIndexesPtr,
                        poolUnitCounts,
                        poolRerolls,
                        poolCount,
                        idxToNamePtrs,
                        banner.IdxToId.ToArray(),
                        totalUnits,
                        (uint)banner.GuaranteedRarity
                    );
                }
                finally
                {
                    Marshal.FreeHGlobal(poolUnitsPtr);
                    Marshal.FreeHGlobal(poolIndexesPtr);
                }
            }
            finally
            {
                // Clean up allocated memory
                foreach (var handle2 in poolUnitsHandles)
                {
                    if (handle2 != IntPtr.Zero)
                        Marshal.FreeHGlobal(handle2);
                }
                foreach (var handle2 in poolIndexesHandles)
                {
                    if (handle2 != IntPtr.Zero)
                        Marshal.FreeHGlobal(handle2);
                }
                foreach (var idxNameHandle in idxNameHandles)
                {
                    Marshal.FreeHGlobal(idxNameHandle);
                }
                foreach (var stringHandle in allStringHandles)
                {
                    Marshal.FreeHGlobal(stringHandle);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting banner: {ex.Message}");
            return false;
        }
    }
}
