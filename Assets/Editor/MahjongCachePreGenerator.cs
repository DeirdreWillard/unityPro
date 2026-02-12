using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 麻将胡听检查数据预生成工具
/// 在Unity编辑器中预生成胡听检查数据,输出到热更新配置文件夹
/// 支持Android、iOS、WebGL等所有平台,通过GF.Config加载
/// </summary>
public class MahjongCachePreGenerator : EditorWindow
{
    private const string OUTPUT_FOLDER = "Assets/AAAGame/Config";
    private const string CACHE_FILE_NORMAL = "MahjongHuPaiData.bytes";
    private const string CACHE_FILE_FENGZI = "MahjongHuPaiDataFengZi.bytes";
    
    private int maxHuPaiAmount = 14;
    private int recordLaziDataLimitCount = 1;  // 默认值改为1
    private bool isCreateLaiziDetailData = true;
    
    [MenuItem("Tools/麻将/预生成胡听检查数据")]
    public static void ShowWindow()
    {
        GetWindow<MahjongCachePreGenerator>("麻将缓存预生成器");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("麻将胡听检查数据预生成", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "此工具用于预生成麻将胡听检查数据,生成的文件将保存到热更新配置文件夹。\n" +
            "输出路径: Assets/AAAGame/Config/\n" +
            "生成后的数据通过 GF.Resource 系统加载,支持 Android、iOS、WebGL 等所有平台。\n" +
            "文件可通过热更新系统更新。", 
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("生成参数", EditorStyles.boldLabel);
        maxHuPaiAmount = EditorGUILayout.IntField("最大胡牌数量", maxHuPaiAmount);
        recordLaziDataLimitCount = EditorGUILayout.IntField("记录赖子数据限制", recordLaziDataLimitCount);
        isCreateLaiziDetailData = EditorGUILayout.Toggle("生成赖子详细数据", isCreateLaiziDetailData);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("生成缓存数据", GUILayout.Height(30)))
        {
            GenerateCacheData();
        }
        
        EditorGUILayout.Space();
        
        string normalPath = Path.Combine(OUTPUT_FOLDER, CACHE_FILE_NORMAL);
        string fengziPath = Path.Combine(OUTPUT_FOLDER, CACHE_FILE_FENGZI);
        
        if (File.Exists(normalPath) && File.Exists(fengziPath))
        {
            EditorGUILayout.HelpBox(
                "✓ 缓存文件已生成:\n" +
                $"  - {CACHE_FILE_NORMAL}\n" +
                $"  - {CACHE_FILE_FENGZI}\n\n" +
                "文件位置: " + OUTPUT_FOLDER,
                MessageType.None);
            
            if (GUILayout.Button("在文件夹中显示"))
            {
                EditorUtility.RevealInFinder(normalPath);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("缓存文件尚未生成", MessageType.Warning);
        }
    }
    
    private void GenerateCacheData()
    {
        // 确保输出文件夹存在
        if (!Directory.Exists(OUTPUT_FOLDER))
        {
            Directory.CreateDirectory(OUTPUT_FOLDER);
            AssetDatabase.Refresh();
        }
        
        EditorUtility.DisplayProgressBar("生成缓存数据", "初始化麻将数据...", 0f);
        
        try
        {
            // 生成普通牌型数据 (10个索引位,包含万、条、筒)
            EditorUtility.DisplayProgressBar("生成缓存数据", "生成普通牌型数据...", 0.3f);
            MahjongHuPaiData mjHuPaiData = new MahjongHuPaiData(10, maxHuPaiAmount);
            mjHuPaiData.IsCreateLaiziDetailData = isCreateLaiziDetailData;
            mjHuPaiData.RecordLaziDataLimitCount = recordLaziDataLimitCount;
            mjHuPaiData.Train();
            
            string normalPath = Path.Combine(OUTPUT_FOLDER, CACHE_FILE_NORMAL);
            SaveCacheDataToBytes(mjHuPaiData, normalPath);
            Debug.Log($"普通牌型数据已生成: {normalPath}");
            
            // 生成风字牌型数据 (8个索引位,仅风字牌,不能顺子)
            EditorUtility.DisplayProgressBar("生成缓存数据", "生成风字牌型数据...", 0.7f);
            MahjongHuPaiData mjHuPaiDataFengZi = new MahjongHuPaiData(8, maxHuPaiAmount);
            mjHuPaiDataFengZi.IsCreateSingleShunZiVaildPaiType = false;
            mjHuPaiDataFengZi.IsCreateLaiziDetailData = isCreateLaiziDetailData;
            mjHuPaiDataFengZi.RecordLaziDataLimitCount = recordLaziDataLimitCount;
            mjHuPaiDataFengZi.Train();
            
            string fengziPath = Path.Combine(OUTPUT_FOLDER, CACHE_FILE_FENGZI);
            SaveCacheDataToBytes(mjHuPaiDataFengZi, fengziPath);
            Debug.Log($"风字牌型数据已生成: {fengziPath}");
            
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("成功", 
                $"麻将胡听检查数据已成功生成!\n\n" +
                $"文件位置: {OUTPUT_FOLDER}\n" +
                $"  - {CACHE_FILE_NORMAL}\n" +
                $"  - {CACHE_FILE_FENGZI}\n\n" +
                "这些文件位于热更新配置目录,可通过 GF.Resource 系统加载。", 
                "确定");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"生成缓存数据失败: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("错误", $"生成缓存数据失败:\n{e.Message}", "确定");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }
    
    /// <summary>
    /// 将麻将胡牌数据保存为二进制文件
    /// 与 MahjongHuPaiData.CreateKeyDataToFile 兼容
    /// </summary>
    private void SaveCacheDataToBytes(MahjongHuPaiData data, string filePath)
    {
        using (FileStream fs = File.Open(filePath, FileMode.Create, FileAccess.Write))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            // 通过反射获取私有字段
            var dictHuPaiKeysField = typeof(MahjongHuPaiData).GetField("dictHuPaiKeys", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxHuPaiNumField = typeof(MahjongHuPaiData).GetField("MAX_HUPAI_NUM", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (dictHuPaiKeysField == null || maxHuPaiNumField == null)
            {
                throw new System.Exception("无法访问 MahjongHuPaiData 的必要字段");
            }
            
            var dictHuPaiKeys = dictHuPaiKeysField.GetValue(data) as System.Collections.Generic.Dictionary<int, byte>[];
            int MAX_HUPAI_NUM = (int)maxHuPaiNumField.GetValue(data);
            
            // 写入格式与 CreateKeyDataToFile 一致
            bw.Write((byte)MAX_HUPAI_NUM);
            
            for (int i = 0; i < dictHuPaiKeys.Length; ++i)
            {
                foreach (var item in dictHuPaiKeys[i])
                {
                    byte val = (byte)(((i & 0x1f) << 3) | (item.Value & 0x7));
                    bw.Write(val);
                    bw.Write(item.Key);
                }
            }
            
            bw.Flush();
        }
    }
}
