using System;
using System.Runtime.InteropServices;
using System.Text;

public static class BattleCatLib
{
    private const string DLL_NAME = "BattleCatLib.dll";

    // Handle type
    public struct BattleCatRollHandle
    {
        public IntPtr Ptr;
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
    public static extern void RollMultiple(IntPtr handle, int rollNum, [Out] uint[] results);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Roll11Guaranteed(IntPtr handle, [Out] uint[] results);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint RollGuaranteed(IntPtr handle);

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
}
