using System;
using System.Collections.Generic;
using NetMsg;
using UnityEngine;
using UnityGameFramework.Runtime;

/// <summary>
/// 房间配置记忆管理器 - 用于保存和加载用户的开房配置偏好
/// </summary>
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public static class RoomConfigMemoryManager
{
    private const string ROOM_CONFIG_PREFIX = "RoomConfig_";
    private const string DEFAULT_CONFIG_KEY = "DefaultCreateDeskConfig";
    
    // 内存缓存，避免频繁访问PlayerPrefs
    private static Dictionary<MethodType, Msg_CreateDeskRq> s_MemoryCache = new Dictionary<MethodType, Msg_CreateDeskRq>();
    private static Msg_DefaultCreateDeskRs s_DefaultConfigCache = null;
    
    /// <summary>
    /// 保存用户的开房配置记忆
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="methodType">游戏类型</param>
    /// <param name="config">配置信息</param>
    public static void SaveRoomConfig(long playerId, MethodType methodType, Msg_CreateDeskRq config) 
    {
        try
        {
            if (config == null || playerId <= 0)
            {
                return;
            }
            
            string key = GetConfigKey(playerId, methodType);
            string jsonData = Google.Protobuf.JsonFormatter.Default.Format(config);
            PlayerPrefs.SetString(key, jsonData);
            PlayerPrefs.Save();
            
            // 更新内存缓存
            s_MemoryCache[methodType] = config;
            
            GF.LogInfo($"[RoomConfigMemory] 保存房间配置记忆 - 玩家:{playerId}, 游戏类型:{methodType}");
        }
        catch (Exception ex)
        {
            GF.LogError($"[RoomConfigMemory] 保存配置失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 加载用户的开房配置记忆
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="methodType">游戏类型</param>
    /// <returns>配置信息，如果没有记忆则返回null</returns>
    public static Msg_CreateDeskRq LoadRoomConfig(long playerId, MethodType methodType)
    {
        try
        {
            if (playerId <= 0)
            {
                return null;
            }
            
            // 先从内存缓存读取
            if (s_MemoryCache.ContainsKey(methodType))
            {
                GF.LogInfo($"[RoomConfigMemory] 从内存缓存加载配置 - 游戏类型:{methodType}");
                return s_MemoryCache[methodType];
            }
            
            string key = GetConfigKey(playerId, methodType);
            if (!PlayerPrefs.HasKey(key))
            {
                GF.LogInfo($"[RoomConfigMemory] 没有找到配置记忆 - 玩家:{playerId}, 游戏类型:{methodType}");
                return null;
            }
            
            string jsonData = PlayerPrefs.GetString(key);
            if (string.IsNullOrEmpty(jsonData))
            {
                return null;
            }
            
            Msg_CreateDeskRq config = Google.Protobuf.JsonParser.Default.Parse<Msg_CreateDeskRq>(jsonData);
            
            // 更新内存缓存
            s_MemoryCache[methodType] = config;
            
            GF.LogInfo($"[RoomConfigMemory] 加载房间配置记忆 - 玩家:{playerId}, 游戏类型:{methodType}");
            return config;
        }
        catch (Exception ex)
        {
            GF.LogError($"[RoomConfigMemory] 加载配置失败: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 保存服务器返回的默认创建房间配置
    /// </summary>
    /// <param name="defaultConfig">默认配置</param>
    public static void SaveDefaultConfig(Msg_DefaultCreateDeskRs defaultConfig)
    {
        try
        {
            if (defaultConfig == null)
            {
                return;
            }
            
            string jsonData = Google.Protobuf.JsonFormatter.Default.Format(defaultConfig);
            PlayerPrefs.SetString(DEFAULT_CONFIG_KEY, jsonData);
            PlayerPrefs.Save();
            
            // 更新内存缓存
            s_DefaultConfigCache = defaultConfig;
            
            GF.LogInfo($"[RoomConfigMemory] 保存默认房间配置");
        }
        catch (Exception ex)
        {
            GF.LogError($"[RoomConfigMemory] 保存默认配置失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 加载服务器返回的默认创建房间配置
    /// </summary>
    /// <returns>默认配置，如果没有则返回null</returns>
    public static Msg_DefaultCreateDeskRs LoadDefaultConfig()
    {
        try
        {
            // 先从内存缓存读取
            if (s_DefaultConfigCache != null)
            {
                GF.LogInfo($"[RoomConfigMemory] 从内存缓存加载默认配置");
                return s_DefaultConfigCache;
            }
            
            if (!PlayerPrefs.HasKey(DEFAULT_CONFIG_KEY))
            {
                GF.LogInfo($"[RoomConfigMemory] 没有找到默认配置");
                return null;
            }
            
            string jsonData = PlayerPrefs.GetString(DEFAULT_CONFIG_KEY);
            if (string.IsNullOrEmpty(jsonData))
            {
                return null;
            }
            
            Msg_DefaultCreateDeskRs config = Google.Protobuf.JsonParser.Default.Parse<Msg_DefaultCreateDeskRs>(jsonData);
            
            // 更新内存缓存
            s_DefaultConfigCache = config;
            
            GF.LogInfo($"[RoomConfigMemory] 加载默认房间配置");
            return config;
        }
        catch (Exception ex)
        {
            GF.LogError($"[RoomConfigMemory] 加载默认配置失败: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// 清除指定玩家的所有配置记忆
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    public static void ClearPlayerConfigs(long playerId)
    {
        try
        {
            if (playerId <= 0)
            {
                return;
            }
            
            // 清除所有游戏类型的配置
            PlayerPrefs.DeleteKey(GetConfigKey(playerId, MethodType.TexasPoker));
            PlayerPrefs.DeleteKey(GetConfigKey(playerId, MethodType.NiuNiu));
            PlayerPrefs.DeleteKey(GetConfigKey(playerId, MethodType.GoldenFlower));
            PlayerPrefs.DeleteKey(GetConfigKey(playerId, MethodType.CompareChicken));
            PlayerPrefs.Save();
            
            // 清除内存缓存
            s_MemoryCache.Clear();
            
            GF.LogInfo($"[RoomConfigMemory] 清除玩家配置记忆 - 玩家:{playerId}");
        }
        catch (Exception ex)
        {
            GF.LogError($"[RoomConfigMemory] 清除配置失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 清除默认配置缓存
    /// </summary>
    public static void ClearDefaultConfig()
    {
        try
        {
            PlayerPrefs.DeleteKey(DEFAULT_CONFIG_KEY);
            PlayerPrefs.Save();
            s_DefaultConfigCache = null;
            
            GF.LogInfo($"[RoomConfigMemory] 清除默认配置缓存");
        }
        catch (Exception ex)
        {
            GF.LogError($"[RoomConfigMemory] 清除默认配置失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 清除所有缓存(包括内存缓存)
    /// </summary>
    public static void ClearAllCache()
    {
        s_MemoryCache.Clear();
        s_DefaultConfigCache = null;
        GF.LogInfo($"[RoomConfigMemory] 清除所有内存缓存");
    }
    
    /// <summary>
    /// 生成配置存储的键
    /// </summary>
    private static string GetConfigKey(long playerId, MethodType methodType)
    {
        return $"{ROOM_CONFIG_PREFIX}{playerId}_{methodType}";
    }
}
