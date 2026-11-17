using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Godot;

namespace TianziGe.View.SteamTest;

/// <summary>
/// T-004 最小 Steam 探针：直接调用 Valve <c>steam_api</c> 的 flat C API
/// （与 Steamworks.NET 使用同一原生库；不经过任何托管封装）。
/// AppID 由运行环境的 SteamAppId 环境变量 / 本机 AppID 文件 提供，绝不写进仓库。
/// 用法：SteamAppId=&lt;test-appid&gt; godot --headless --path Client -- --steam-probe
/// </summary>
public static class SteamProbe
{
    private const string Library = "steam_api";
    private const int AchievementNameBuffer = 256;

    private static bool _resolverInstalled;

    public static int Run()
    {
        InstallResolver();

        try
        {
            bool steamRunning = SteamAPI_IsSteamRunning();
            GD.Print($"STEAM_PROBE steam_running={steamRunning}");
            if (!steamRunning)
            {
                GD.PushError("STEAM_PROBE FAILED: Steam 客户端未运行或未登录");
                return 2;
            }

            if (!SteamAPI_InitSafe())
            {
                GD.PushError("STEAM_PROBE FAILED: SteamAPI_InitSafe() 返回 false（检查 SteamAppId 环境变量 / 本机 AppID 文件 / 登录状态）");
                return 3;
            }

            try
            {
                IntPtr stats = SteamAPI_SteamUserStats_v013();
                IntPtr utils = SteamAPI_SteamUtils_v011();
                IntPtr user = SteamAPI_SteamUser_v023();

                GD.Print(
                    $"STEAM_PROBE app_id={SteamAPI_ISteamUtils_GetAppID(utils)} " +
                    $"steam_id={SteamAPI_ISteamUser_GetSteamID(user)} " +
                    $"user_stats_ptr={stats}");

                // steam_api 初始化后由后台线程拉取用户统计；泵回调直到拿到成就表。
                Pump(100);

                uint achievementCount = SteamAPI_ISteamUserStats_GetNumAchievements(stats);
                GD.Print($"STEAM_PROBE achievements={achievementCount}");
                for (uint index = 0; index < achievementCount; index++)
                {
                    GD.Print($"STEAM_PROBE achievement[{index}]={GetAchievementName(stats, index)}");
                }

                if (achievementCount == 0)
                {
                    GD.PushError("STEAM_PROBE FAILED: 该 AppID 没有配置成就，无法验证上报");
                    return 4;
                }

                // 挑一个尚未点亮的成就，确保日志能显示 false -> true。
                string achievement = GetAchievementName(stats, 0);
                for (uint index = 0; index < achievementCount; index++)
                {
                    string candidate = GetAchievementName(stats, index);
                    if (SteamAPI_ISteamUserStats_GetAchievement(stats, candidate, out bool candidateAchieved) && !candidateAchieved)
                    {
                        achievement = candidate;
                        break;
                    }
                }

                bool getBefore = SteamAPI_ISteamUserStats_GetAchievement(stats, achievement, out bool achievedBefore);
                bool setOk = SteamAPI_ISteamUserStats_SetAchievement(stats, achievement);
                bool storeOk = SteamAPI_ISteamUserStats_StoreStats(stats);
                Pump(100);
                bool getAfter = SteamAPI_ISteamUserStats_GetAchievement(stats, achievement, out bool achievedAfter);

                GD.Print(
                    $"STEAM_PROBE achievement={achievement} get_before_ok={getBefore} set_ok={setOk} " +
                    $"store_ok={storeOk} get_after_ok={getAfter} " +
                    $"achieved_before={achievedBefore} achieved_after={achievedAfter}");

                return setOk && storeOk && getAfter && achievedAfter ? 0 : 5;
            }
            finally
            {
                SteamAPI_Shutdown();
            }
        }
        catch (Exception exception)
        {
            GD.PushError($"STEAM_PROBE EXCEPTION {exception.GetType().Name}: {exception.Message}");
            return 6;
        }
    }

    /// <summary>
    /// 原生库不在可执行文件旁（编辑器运行）时，把 DllImport("steam_api") 指到仓库内路径。
    /// 导出构建则优先用可执行文件同级的 libsteam_api.dylib。
    /// </summary>
    private static void InstallResolver()
    {
        if (_resolverInstalled)
        {
            return;
        }

        _resolverInstalled = true;
        NativeLibrary.SetDllImportResolver(typeof(SteamProbe).Assembly, ResolveNativeLibrary);
    }

    private static IntPtr ResolveNativeLibrary(string name, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (name != Library)
        {
            return IntPtr.Zero;
        }

        foreach (string candidate in NativeCandidates())
        {
            if (File.Exists(candidate))
            {
                return NativeLibrary.Load(candidate);
            }
        }

        return IntPtr.Zero;
    }

    private static IEnumerable<string> NativeCandidates()
    {
        yield return ProjectSettings.GlobalizePath("res://native/macos/libsteam_api.dylib");

        string executableDirectory = Path.GetDirectoryName(OS.GetExecutablePath()) ?? string.Empty;
        yield return Path.Combine(executableDirectory, "libsteam_api.dylib");
        yield return Path.Combine(AppContext.BaseDirectory, "libsteam_api.dylib");
    }

    private static string GetAchievementName(IntPtr stats, uint index)
    {
        IntPtr pointer = SteamAPI_ISteamUserStats_GetAchievementName(stats, index);
        return pointer == IntPtr.Zero
            ? string.Empty
            : Marshal.PtrToStringUTF8(pointer) ?? string.Empty;
    }

    private static void Pump(int iterations)
    {
        for (int index = 0; index < iterations; index++)
        {
            SteamAPI_RunCallbacks();
            Thread.Sleep(20);
        }
    }

    // Valve SDK 1.65 flat API（steam_api.h 的 C 接口）。访问器版本号随后续 SDK 变化，
    // 升级原生库时必须同步核对 libsteam_api 的导出符号（见 T-004 记录）。
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SteamAPI_IsSteamRunning();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SteamAPI_InitSafe();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    private static extern void SteamAPI_Shutdown();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    private static extern void SteamAPI_RunCallbacks();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr SteamAPI_SteamUserStats_v013();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr SteamAPI_SteamUtils_v011();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr SteamAPI_SteamUser_v023();

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint SteamAPI_ISteamUtils_GetAppID(IntPtr self);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    private static extern ulong SteamAPI_ISteamUser_GetSteamID(IntPtr self);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint SteamAPI_ISteamUserStats_GetNumAchievements(IntPtr self);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr SteamAPI_ISteamUserStats_GetAchievementName(IntPtr self, uint index);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SteamAPI_ISteamUserStats_SetAchievement(IntPtr self, string name);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SteamAPI_ISteamUserStats_StoreStats(IntPtr self);

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SteamAPI_ISteamUserStats_GetAchievement(
        IntPtr self,
        string name,
        [MarshalAs(UnmanagedType.I1)] out bool achieved);
}
