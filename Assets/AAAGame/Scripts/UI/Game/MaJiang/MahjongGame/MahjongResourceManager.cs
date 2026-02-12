using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using static UtilityBuiltin;

/// <summary>
/// 麻将资源统一管理器
/// 负责预加载和缓存所有麻将相关的图片资源
/// 建议在大厅界面时后台加载，提升进入游戏的流畅度
/// </summary>
public static class MahjongResourceManager
{
    #region 缓存数据

    /// <summary>麻将点数图片缓存（1-34）</summary>
    private static Dictionary<int, Sprite> _pointSprites = new Dictionary<int, Sprite>();

    /// <summary>麻将背景图片缓存（颜色_方位_类型）</summary>
    private static Dictionary<string, Sprite> _backgroundSprites = new Dictionary<string, Sprite>();

    /// <summary>是否已初始化</summary>
    private static bool _isInitialized = false;

    /// <summary>是否正在加载</summary>
    private static bool _isLoading = false;

    /// <summary>加载进度（0-1）</summary>
    private static float _loadProgress = 0f;

    /// <summary>颜色数组常量</summary>
    private static readonly string[] Colors = { "lv", "huang", "lan" };

    /// <summary>方向数组常量</summary>
    private static readonly string[] Directions = { "south", "west", "north", "east" };

    /// <summary>类型数组常量</summary>
    private static readonly string[] Types = { "hand", "discard", "meld" };

    #endregion

    #region 公共接口

    /// <summary>
    /// 预加载所有麻将资源（建议在大厅界面调用）
    /// </summary>
    /// <param name="onProgress">加载进度回调（0-1）</param>
    /// <param name="onComplete">加载完成回调</param>
    public static async UniTask PreloadAllAsync(Action<float> onProgress = null, Action onComplete = null)
    {
        if (_isLoading)
        {
            GF.LogWarning("[麻将资源] 正在加载中，跳过重复请求");
            return;
        }

        if (_isInitialized)
        {
            GF.LogInfo("[麻将资源] 已初始化，跳过重复加载");
            onComplete?.Invoke();
            return;
        }

        _isLoading = true;
        _loadProgress = 0f;
        GF.LogInfo("[麻将资源] 开始预加载所有资源...");

        try
        {
            // 计算总任务数：34个基础点数 + 3个中发白(41-43) + 36个正面背景 + 36个背面背景
            int totalTasks = 34 + 3 + 36 + 36;
            int completedTasks = 0;

            // 1. 加载所有点数图片（1-34）
            var pointTasks = new List<UniTask>();
            for (int i = 1; i <= 34; i++)
            {
                int value = i; // 闭包变量
                //没有10 20 30
                if (value % 10 == 0)
                {
                    completedTasks++;
                    _loadProgress = (float)completedTasks / totalTasks;
                    onProgress?.Invoke(_loadProgress);
                    continue;
                }
                var task = LoadPointSpriteAsync(value, () =>
                {
                    completedTasks++;
                    _loadProgress = (float)completedTasks / totalTasks;
                    onProgress?.Invoke(_loadProgress);
                });
                pointTasks.Add(task);
            }

            // 1.5 加载中发白点数图片（41-43）
            for (int i = 41; i <= 43; i++)
            {
                int value = i; // 闭包变量
                var task = LoadPointSpriteAsync(value, () =>
                {
                    completedTasks++;
                    _loadProgress = (float)completedTasks / totalTasks;
                    onProgress?.Invoke(_loadProgress);
                });
                pointTasks.Add(task);
            }

            // 2. 加载所有正面背景图片（3颜色 x 4方位 x 3类型）
            var bgTasks = new List<UniTask>();

            foreach (var color in Colors)
            {
                foreach (var direction in Directions)
                {
                    foreach (var type in Types)
                    {
                        string c = color;
                        string d = direction;
                        string t = type;
                        var task = LoadBackgroundSpriteAsync(c, d, t, false, () =>
                        {
                            completedTasks++;
                            _loadProgress = (float)completedTasks / totalTasks;
                            onProgress?.Invoke(_loadProgress);
                        });
                        bgTasks.Add(task);
                    }
                }
            }

            // 3. 加载所有背面背景图片（cover版本）
            // 注意：hand类型的背面只有south方向有资源（其他位置手牌没有背牌）
            foreach (var color in Colors)
            {
                foreach (var direction in Directions)
                {
                    foreach (var type in Types)
                    {
                        // hand类型的cover只有south方向存在
                        if (type == "hand" && direction != "south")
                        {
                            completedTasks++;
                            _loadProgress = (float)completedTasks / totalTasks;
                            onProgress?.Invoke(_loadProgress);
                            continue;
                        }
                        
                        string c = color;
                        string d = direction;
                        string t = type;
                        var task = LoadBackgroundSpriteAsync(c, d, t, true, () =>
                        {
                            completedTasks++;
                            _loadProgress = (float)completedTasks / totalTasks;
                            onProgress?.Invoke(_loadProgress);
                        });
                        bgTasks.Add(task);
                    }
                }
            }

            // 并行加载所有资源
            await UniTask.WhenAll(pointTasks);
            await UniTask.WhenAll(bgTasks);

            _isInitialized = true;
            _loadProgress = 1f;
            onProgress?.Invoke(1f);

            GF.LogInfo($"[麻将资源] 预加载完成！点数: {_pointSprites.Count}/37（含中发白）, 背景: {_backgroundSprites.Count}/72");
            onComplete?.Invoke();
        }
        catch (Exception ex)
        {
            GF.LogError($"[麻将资源] 预加载失败: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// 同步预加载（不阻塞，后台加载）
    /// </summary>
    public static void PreloadAllInBackground()
    {
        PreloadAllAsync(
            onProgress: (progress) => 
            {
                // 可以在这里更新UI进度条（如果有的话）
                // GF.LogInfo($"[麻将资源] 加载进度: {progress:P0}");
            },
            onComplete: () => 
            {
                GF.LogInfo("[麻将资源] 后台加载完成");
            }
        ).Forget();
    }

    /// <summary>
    /// 获取点数图片
    /// </summary>
    public static Sprite GetPointSprite(int value)
    {
        // 支持 1-34 和 41-43（中发白）
        if ((value < 1 || value > 34) && (value < 41 || value > 43))
        {
            GF.LogWarning($"[麻将资源] 无效的点数值: {value}");
            return null;
        }

        if (_pointSprites.TryGetValue(value, out var sprite))
        {
            return sprite;
        }

        // 缓存未命中时，动态加载（兜底方案）
        GF.LogWarning($"[麻将资源] 点数图片未缓存，动态加载: Point_{value}");
        LoadPointSpriteFallback(value);
        return null;
    }

    /// <summary>
    /// 获取背景图片
    /// </summary>
    /// <param name="color">麻将颜色样式</param>
    /// <param name="seatIndex">座位索引（0-3）</param>
    /// <param name="cardType">卡牌类型（hand/discard/meld）</param>
    /// <param name="isCover">是否为背面</param>
    public static Sprite GetBackgroundSprite(MjColor color, int seatIndex, string cardType, bool isCover = false)
    {
        string[] directions = { "south", "west", "north", "east" };
        if (seatIndex < 0 || seatIndex >= directions.Length)
        {
            GF.LogWarning($"[麻将资源] 无效的座位索引: {seatIndex}");
            return null;
        }

        // 根据 isCover 参数构建文件名
        string key;
        if (isCover)
        {
            // 背面：{color}_{direction}_{type}_cover_board
            key = $"{color}_{directions[seatIndex]}_{cardType}_cover_board";
        }
        else
        {
            // 正面：{color}_{direction}_{type}_board
            key = $"{color}_{directions[seatIndex]}_{cardType}_board";
        }
        
        if (_backgroundSprites.TryGetValue(key, out var sprite))
        {
            return sprite;
        }

        // 缓存未命中时，动态加载（兜底方案）
        GF.LogWarning($"[麻将资源] 背景图片未缓存，动态加载: {key}");
        LoadBackgroundSpriteFallback(color.ToString(), directions[seatIndex], cardType, isCover);
        return null;
    }

    /// <summary>
    /// 清除所有缓存
    /// </summary>
    public static void ClearCache()
    {
        _pointSprites.Clear();
        _backgroundSprites.Clear();
        _isInitialized = false;
        _loadProgress = 0f;
        GF.LogInfo("[麻将资源] 清除所有缓存");
    }

    /// <summary>
    /// 获取加载进度
    /// </summary>
    public static float GetLoadProgress()
    {
        return _loadProgress;
    }

    /// <summary>
    /// 是否已初始化完成
    /// </summary>
    public static bool IsInitialized()
    {
        return _isInitialized;
    }

    #endregion

    #region 内部加载方法

    /// <summary>
    /// 异步加载点数图片
    /// </summary>
    private static async UniTask LoadPointSpriteAsync(int value, Action onComplete = null)
    {
        if (_pointSprites.ContainsKey(value))
        {
            onComplete?.Invoke();
            return;
        }

        var tcs = new UniTaskCompletionSource<Sprite>();
        string path = AssetsPath.GetSpritesPath($"MJGame/MJCardAll/Point_{value}.png");

        GF.UI.LoadSprite(path, (sprite) =>
        {
            if (sprite != null)
            {
                _pointSprites[value] = sprite;
            }
            onComplete?.Invoke();
            tcs.TrySetResult(sprite);
        });

        await tcs.Task;
    }

    /// <summary>
    /// 异步加载背景图片
    /// </summary>
    private static async UniTask LoadBackgroundSpriteAsync(string color, string direction, string cardType, bool isCover, Action onComplete = null)
    {
        // 根据 isCover 参数构建文件名
        string key;
        if (isCover)
        {
            // 背面：{color}_{direction}_{type}_cover_board
            key = $"{color}_{direction}_{cardType}_cover_board";
        }
        else
        {
            // 正面：{color}_{direction}_{type}_board
            key = $"{color}_{direction}_{cardType}_board";
        }
        
        if (_backgroundSprites.ContainsKey(key))
        {
            onComplete?.Invoke();
            return;
        }

        var tcs = new UniTaskCompletionSource<Sprite>();
        string path = AssetsPath.GetSpritesPath($"MJGame/MJCardAll/{key}.png");

        GF.UI.LoadSprite(path, (sprite) =>
        {
            if (sprite != null)
            {
                _backgroundSprites[key] = sprite;
            }
            onComplete?.Invoke();
            tcs.TrySetResult(sprite);
        });

        await tcs.Task;
    }

    /// <summary>
    /// 兜底：动态加载点数图片
    /// </summary>
    private static void LoadPointSpriteFallback(int value)
    {
        string path = AssetsPath.GetSpritesPath($"MJGame/MJCardAll/Point_{value}.png");
        GF.UI.LoadSprite(path, (sprite) =>
        {
            if (sprite != null)
            {
                _pointSprites[value] = sprite;
            }
        });
    }

    /// <summary>
    /// 兜底：动态加载背景图片
    /// </summary>
    private static void LoadBackgroundSpriteFallback(string color, string direction, string cardType, bool isCover)
    {
        // 根据 isCover 参数构建文件名
        string key;
        if (isCover)
        {
            // 背面：{color}_{direction}_{type}_cover_board
            key = $"{color}_{direction}_{cardType}_cover_board";
        }
        else
        {
            // 正面：{color}_{direction}_{type}_board
            key = $"{color}_{direction}_{cardType}_board";
        }
        
        string path = AssetsPath.GetSpritesPath($"MJGame/MJCardAll/{key}.png");
        GF.UI.LoadSprite(path, (sprite) =>
        {
            if (sprite != null)
            {
                _backgroundSprites[key] = sprite;
            }
        });
    }

    #endregion
}
