﻿using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PhotoManager : MonoBehaviour
{
    private static PhotoManager instance;

    public static PhotoManager GetInstance()
    {
        if (instance == null)
        {
            GameObject obj = new GameObject("PhotoManager");
            instance = obj.AddComponent<PhotoManager>();
            DontDestroyOnLoad(obj);
        }
        return instance;
    }
    
    public void TakePhoto(RawImage head)
    {
        GF.LogInfo("TakePhoto");
        GF.LogInfo("当前权限状态: " + NativeGallery.CheckPermission(NativeGallery.PermissionType.Read, NativeGallery.MediaType.Image));
        if (NativeGallery.CheckPermission(NativeGallery.PermissionType.Read, NativeGallery.MediaType.Image) == NativeGallery.Permission.Denied)
        {
            NativeGallery.OpenSettings(); 
        }

        int maxSize = 512; // 根据屏幕宽度调整
        NativeGallery.Permission permission = NativeGallery.GetImageFromGallery((path) =>
        {
            // GF.ClearCameraResiduals();
            if (string.IsNullOrEmpty(path))
            {
                GF.LogInfo("No image selected");
                return;
            }

            GF.LogInfo("Image path: " , path);
            Texture2D texture = NativeGallery.LoadImageAtPath(path, maxSize);
            if (texture == null)
            {
                GF.LogError("Couldn't load texture from " , path);
                return;
            }

            // 强制用户选择正方形区域
            if (!ImageCropper.Instance.IsOpen)
            {
                ImageCropper.Instance.Show(texture, (bool result, Texture originalImage, Texture2D croppedImage) =>
                {
                    if (result && croppedImage != null)
                    {
                        try
                        {
                            // 压缩裁剪后的图片到 120x120
                            Texture2D compressedTexture = CompressTexture(croppedImage, 120, 120);
                            Destroy(croppedImage); // 释放裁剪后的纹理

                            // 显示头像到 UI
                            if (head != null)
                            {
                                head.texture = compressedTexture;
                                // head.SetNativeSize();
                            }
                        }
                        catch (System.Exception ex)
                        {
                            GF.LogError("Error while processing photo: " + ex.Message);
                        }
                    }
                    else
                    {
                        GF.LogWarning("User canceled cropping or cropping failed.");
                    }
                },
                settings: new ImageCropper.Settings
                {
                    guidelinesVisibility = ImageCropper.Visibility.Hidden, //    隐藏辅助线
                    selectionMinAspectRatio = 1f, // 固定为正方形
                    selectionMaxAspectRatio = 1f,
                    autoZoomEnabled = true,
                    ovalSelection = false, // 使用矩形选取框
                    visibleButtons = ImageCropper.Button.None,
                });
            }
        }, "选择一张图片作为头像", "image/*");
    }

    /// <summary>
    /// 压缩纹理到指定大小
    /// </summary>
    private Texture2D CompressTexture(Texture2D original, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        RenderTexture.active = rt;

        Graphics.Blit(original, rt);

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    public byte[] Texture2DTobytes(Texture2D texture, bool usePng = true, int jpegQuality = 75)
    {
        if (texture == null)
        {
            throw new ArgumentNullException(nameof(texture), "Texture cannot be null.");
        }

        // 确保质量参数在合法范围内
        jpegQuality = Mathf.Clamp(jpegQuality, 0, 100);

        // 确保 Texture2D 可读
        Texture2D temp = CopyT2DToWrite(texture);

        // 根据格式选择编码方式
        if (usePng)
        {
            return temp.EncodeToPNG();
        }
        else
        {
            return temp.EncodeToJPG(jpegQuality);
        }
    }

    // 复制出可读的Texture2D
    private Texture2D CopyT2DToWrite(Texture2D source)
    {
        // 先把Texture2D转成临时的RenderTexture
        RenderTexture renderTex = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);
        Graphics.Blit(source, renderTex);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTex;
        // 复制进新的Texture2D
        Texture2D readableText = new Texture2D(source.width, source.height);
        readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        readableText.Apply();
        // 恢复_释放 RenderTexture
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTex);
        return readableText;
    }


    public IEnumerator DownloadAvatar(string avatarId, int headType, System.Action<Texture2D> onAvatarDownloaded)
    {
        string downloadUrl = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}:{Const.ServerInfo_BackEndPort}/";
        if (Const.CurrentServerType == Const.ServerType.外网服)
        {
            downloadUrl = $"{Const.HttpStr}{Const.ServerInfo_BackEndIP}/";
        }
        string typeStr = headType == -1 ? "role" : "head";
        string avatarUrl = $"{downloadUrl}{typeStr}/{avatarId}";
        GF.LogInfo("下载头像url:" , avatarUrl);
        using UnityWebRequest www = UnityWebRequestTexture.GetTexture(avatarUrl);
        www.certificateHandler = new BypassCertificate();
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(www);
            // Sprite avatar = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            onAvatarDownloaded?.Invoke(texture);
        }
        else
        {
            GF.LogWarning("头像下载失败: " , www.error);
            onAvatarDownloaded?.Invoke(null);
        }
    }

    private string GetCachePath(string avatarId)
    {
        string directory = Path.Combine(Application.persistentDataPath, "avatars");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        return Path.Combine(directory, avatarId);
    }


    public IEnumerator LoadAvatar(string avatarId, int headType, System.Action<Texture2D> onAvatarLoaded)
    {
        yield return null;
        string cachePath = GetCachePath(avatarId);

        // 检查本地缓存
        if (System.IO.File.Exists(cachePath))
        {
            GF.LogInfo("加载本地头像:" , cachePath);
            try
            {
                byte[] imageData = System.IO.File.ReadAllBytes(cachePath);
                Texture2D texture = new Texture2D(2, 2);
                if (texture.LoadImage(imageData))
                {
                    onAvatarLoaded?.Invoke(texture);
                    yield break; // 成功返回，结束协程
                }
                else
                {
                    GF.LogError("加载本地头像失败，图像数据无效");
                    File.Delete(cachePath);
                }
            }
            catch (Exception ex)
            {
                GF.LogError("读取本地头像失败: " + ex.Message);
            }
        }

        // 缓存不存在，下载头像
        yield return DownloadAvatar(avatarId, headType, avatar =>
        {
            if (avatar != null)
            {
                // 保存到本地缓存
                try
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(cachePath));
                    System.IO.File.WriteAllBytes(cachePath, avatar.EncodeToPNG());
                }
                catch (Exception ex)
                {
                    GF.LogError("保存头像到本地失败: " + ex.Message);
                }
            }
            onAvatarLoaded?.Invoke(avatar);
        });
    }

    public bool CompareHeadData(byte[] _data1, byte[] _data2)
    {
        if (_data1 == null || _data2 == null)
            return false;
        
        if (_data1.Length != _data2.Length)
            return false;
        
        for (int i = 0; i < _data1.Length; i++)
        {
            if (_data1[i] != _data2[i])
                return false;
        }
        
        return true;
    }
}