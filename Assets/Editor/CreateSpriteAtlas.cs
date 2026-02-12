/*
 * @Author: mikey.zhaopeng 
 * @Date: 2025-07-02 15:10:52 
 * @Last Modified by:   mikey.zhaopeng 
 * @Last Modified time: 2025-07-02 15:10:52 
 */
/*
 * @Author: mikey.zhaopeng 
 * @Date: 2025-07-02 15:10:51 
 * @Last Modified by:   mikey.zhaopeng 
 * @Last Modified time: 2025-07-02 15:10:51 
 */
/*
 * @Author: mikey.zhaopeng 
 * @Date: 2025-07-02 15:10:50 
 * @Last Modified by:   mikey.zhaopeng 
 * @Last Modified time: 2025-07-02 15:10:50 
 */
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public static class CreateSpriteAtlas
{
    //private static string _atlasPath = "New Sprite Atlas.spriteatlas";
    //private static string _texturePath = "Assets/Texture";
    static string[] ignoreFile = { "效果图" };

    //图集图片最大宽高
    private static int maxH_W = 2048;
    //图集图片同时满足宽高(高宽)
    private static int maxLength = 1024;
    private static int minLength = 512;

    static string atlasPath = "Assets/AAAGame/Atlas";

    [MenuItem("Assets/构建AllAtlas_Android")]
    public static void BuildSpriteAtlas()
    {
        SpriteAtlasUtility.PackAllAtlases(BuildTarget.Android);
    }
	
	[MenuItem("Assets/构建AllAtlas_IOS")]
    public static void BuildSpriteAtlas_Ios()
    {
        SpriteAtlasUtility.PackAllAtlases(BuildTarget.iOS);
    }
        

    [MenuItem("Assets/创建Atlas(移动大图)")]
    public static void CreateSpriteAtlasMain()
    {
        string[] selectPaths = Selection.assetGUIDs;
        if (selectPaths.Length == 0)
        {
            UnityEngine.Debug.LogError("请先选择任意一个文件，再点击此菜单");
            return;
        }

        foreach (string str in selectPaths)
        {
            string p = AssetDatabase.GUIDToAssetPath(str);
            string path = Path.Combine(Application.dataPath, p.Substring(7));
            //Debug.Log(path);
            if (path.EndsWith("/Res"))
            {
                //先删除输出缓存
                Debug.Log("清空图集缓存");
                if (Directory.Exists(atlasPath)) Directory.Delete(atlasPath, true);
            }
            if (Directory.Exists(path))
            {
                //先创建本身图集
                CreateSpriteAtlasFunc(path, atlasPath);
                //遍历生成子文件夹图鉴
                BuildAltas(path);
            }
        }
        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();
        Debug.Log("创建Atlas！ Success");
    }

    static void BuildAltas(string path)
    {
        DirectoryInfo direction = new DirectoryInfo(path);
        DirectoryInfo[] folders = direction.GetDirectories("*", SearchOption.TopDirectoryOnly);
        //Debug.Log("foldersLen" + folders.Length);
        for (int i = 0; i < folders.Length; i++)
        {
            int folderAssetsIndex = folders[i].FullName.IndexOf("Assets");
            string folderPath = folders[i].FullName.Substring(folderAssetsIndex);
            CreateSpriteAtlasFunc(folderPath, atlasPath);
            EditorUtility.DisplayProgressBar("打包图集" + folderPath.Replace("\\", "/").Split('/').Last(), i + "/" + folders.Length, i / (float)folders.Length);
            BuildAltas(folderPath);//递归遍历所有子文件夹
        }
    }

    public static void CreateSpriteAtlasFunc(string path, string savePath)
    {
        //Object obj = Selection.activeObject;
        //string path = AssetDatabase.GetAssetPath(obj);
        //string rootPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(obj));

        //空文件不需要保存
        bool IsSave = false;
        SpriteAtlas atlas = new SpriteAtlas();
        // 设置参数 可根据项目具体情况进行设置
        SpriteAtlasPackingSettings packSetting = new SpriteAtlasPackingSettings()
        {
            blockOffset = 1,
            enableRotation = false,
            enableTightPacking = false,
            padding = 4,
        };
        atlas.SetPackingSettings(packSetting);

        SpriteAtlasTextureSettings textureSetting = new SpriteAtlasTextureSettings()
        {
            readable = false,
            generateMipMaps = false,
            sRGB = true,
            filterMode = FilterMode.Bilinear,
        };
        atlas.SetTextureSettings(textureSetting);

        TextureImporterPlatformSettings platformSetting = new TextureImporterPlatformSettings()
        {
            maxTextureSize = 2048,
            format = TextureImporterFormat.Automatic,
            crunchedCompression = true,
            textureCompression = TextureImporterCompression.Compressed,
            compressionQuality = 100,
        };
        atlas.SetPlatformSettings(platformSetting);

        // 1、添加文件
        DirectoryInfo dir = new DirectoryInfo(path);
        // 这里我使用的是png图片，已经生成Sprite精灵了
        FileInfo[] files = dir.GetFiles("*.*");
        //Debug.Log("filesLength" + files.Length);
        if (files.Length == 0)
        {
            return;
        }

        string pathTemp = path.Replace("\\", "/");
        string name = pathTemp.Split('/').Last();
        
        if (ignoreFile.Contains(name))//忽略
        {
            return;
        }

        foreach (FileInfo file in files)
        {
            if (file.Name.EndsWith(".png") || file.Name.EndsWith(".jpg"))
            {
                IsSave = true;
                break;
            }
        }

        if (!IsSave)
        {
            //Debug.LogError("空文件夹没有图片:" + name);
            return;
        }

        // 2、添加文件夹
        int len1 = pathTemp.IndexOf("Assets");
        string sPath = pathTemp.Substring(len1);
        Object obj2 = AssetDatabase.LoadAssetAtPath(sPath, typeof(Object));
        atlas.Add(new[] { obj2 });

        savePath += "/" + name;
        if (Directory.Exists(savePath)) Directory.Delete(savePath, true);
        Directory.CreateDirectory(savePath);
        AssetDatabase.CreateAsset(atlas, Path.Combine(savePath, name + ".spriteatlas"));
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="texture">Texture</param>
    /// <returns>true:需要移动</returns>
    public static bool IsBigPic(Texture texture){
        // Debug.Log(texture.height + "    " + texture.width);
        //都小于最大值不移动
        if (texture.height < maxLength && texture.width < maxLength)
        {
            return false;
        }
        // 宽大于最大值 高小于最小值（高大于最大值 宽小于最小值）不移动
        else if ((texture.height < minLength && texture.width > maxLength) || (texture.width < minLength && texture.height > maxLength))
        {
            //都要小于图集最大值才不移动
            if (texture.height <= maxH_W && texture.width <= maxH_W)
            {
                return false;
            }
        }
        return true;
    }

    public static bool SafeMoveFile(string srcFileName, string dstFileName)
    {
        //Debug.Log(string.Format("SafeMoveFile failed! srcfile = {0} dstfile = {1}", srcFileName, dstFileName));
        try
        {
            if (string.IsNullOrEmpty(srcFileName))
            {
                return true;
            }

            if (!File.Exists(srcFileName))
            {
                return true;
            }
            if (File.Exists(dstFileName))//如果BigPicture中已经存在,就把dstFileName旧图删除，复制新的图片过去
            {
                // Debug.Log("BigPicture中已经存在:  "+dstFileName);
                File.Delete(dstFileName);
            }
            File.SetAttributes(srcFileName, FileAttributes.Normal);
            File.Move(srcFileName, dstFileName);

            string srcMetaFileName = srcFileName + ".meta";
            string dstMetaFileName = dstFileName + ".meta";
            if (File.Exists(dstMetaFileName))  //如果BigPicture中已经存在,就把srcFileName新生成的meta删除，保留旧的meta
            {
                if (File.Exists(srcFileName))
                    File.Delete(srcFileName);
                return true;
            }
            File.SetAttributes(srcMetaFileName, FileAttributes.Normal);
            File.Move(srcMetaFileName, dstMetaFileName);
            //FileInfo fileInfo = new FileInfo("");
            //fileInfo.Exists
            //fileInfo.MoveTo("");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format("SafeMoveFile failed! srcfile = {0} dstfile = {1} with err: {2}", srcFileName, dstFileName, ex.Message));
            return false;
        }
    }

    
}