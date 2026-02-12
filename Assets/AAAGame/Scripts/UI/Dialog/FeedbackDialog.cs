using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class FeedbackDialog : UIFormBase
{
    private UserDataModel userDataModel;
    public Text text_TotalInput;
    public InputField input_Feedback;
    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        GF.LogInfo("FeedbackDialog");
        userDataModel = Util.GetMyselfInfo();
    }

    public void onInputValueChange()
    {
        if (input_Feedback.text.Length > 100)
        {
            text_TotalInput.text = $"<color=red>{input_Feedback.text.Length}</color>/100";
        }
        else
        {
            text_TotalInput.text = input_Feedback.text.Length.ToString() + "/100";
        }
    }

    protected override void OnButtonClick(object sender, string btId)
    {
        base.OnButtonClick(sender, btId);
        switch (btId)
        {
            case "Commit":
                OnFeedbackCommit();
                break;
            case "UploadLog":
                OnUploadLog();
                break;
        }
    }

    public void OnFeedbackCommit()
    {
        if (input_Feedback.text.Length >= 100)
        {
            GF.UI.ShowToast("文本太长");
            return;
        }
        if (input_Feedback.text.Length < 10)
        {
            GF.UI.ShowToast("文本太短");
            return;
        }
        Util.GetInstance().ShowWaiting("正在提交反馈...", "postFeedback");
        StartCoroutine(postFeedback());
    }

    IEnumerator postFeedback()
    {
        WWWForm form = new WWWForm();
        form.AddField("userId", userDataModel.PlayerId.ToString());
        form.AddField("bug", input_Feedback.text);

        UnityWebRequest request = UnityWebRequest.Post($"{Const.HttpStr}{GlobalManager.ServerInfo_HouTaiIP}/api/bugList", form);
        GF.LogInfo("提交反馈", request.ToString());
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest. Result. ConnectionError || request.result == UnityWebRequest. Result. ProtocolError)
        {
            GF.UI.ShowToast(request.error);
        }
        else
        {
            GF.UI.ShowToast("提交成功!");
        }
        input_Feedback.text = "";
        Util.GetInstance().CloseWaiting("postFeedback");
        GF.UI.Close(this.UIForm);
    }

    /// <summary>
    /// 上传日志按钮点击
    /// </summary>
    public void OnUploadLog()
    {
        if (LogManager.Instance == null)
        {
            return;
        }
        Util.GetInstance().ShowWaiting("正在上传日志...", "UploadLogFiles");
        StartCoroutine(UploadLogFiles());
    }

    /// <summary>
    /// 上传日志文件到服务器（带安全防护+ZIP压缩+重试）
    /// </summary>
    IEnumerator UploadLogFiles()
    {
        // 1. 强制刷新日志队列到文件
        LogManager.Instance.FlushLogs();
        yield return null;

        // 2. 获取最近24小时的日志文件
        List<string> allLogFiles = LogManager.Instance.GetAllLogFiles();
        if (allLogFiles == null || allLogFiles.Count == 0)
        {
            Util.GetInstance().CloseWaiting("UploadLogFiles");
            yield break;
        }

        // 只上传最近3个文件或24小时内的
        List<string> logFiles = new List<string>();
        System.DateTime yesterday = System.DateTime.Now.AddHours(-24);
        foreach (var file in allLogFiles)
        {
            if (logFiles.Count >= 3) break;
            var fileInfo = new FileInfo(file);
            if (fileInfo.CreationTime > yesterday)
            {
                logFiles.Add(file);
            }
        }

        if (logFiles.Count == 0)
        {
            Util.GetInstance().CloseWaiting("UploadLogFiles");
            yield break;
        }

        GF.LogInfo($"开始上传 {logFiles.Count} 个日志文件");

        // 3. 压缩日志文件为ZIP（使用安全读取方式）
        string zipPath = null;
        try
        {
            // 获取跨平台安全的临时目录
            string tempDir = GetSafeTempDirectory();
            GF.LogInfo($"使用临时目录: {tempDir}");
            
            zipPath = Path.Combine(tempDir, $"logs_{System.DateTime.Now:yyyyMMddHHmmss}.zip");
            GF.LogInfo($"准备创建ZIP文件: {zipPath}");
            
            using (var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var logFile in logFiles)
                {
                    if (!File.Exists(logFile)) continue;

                    try
                    {
                        // 使用FileShare.ReadWrite允许并发读取正在写入的文件
                        using (var fileStream = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            var zipEntry = zipArchive.CreateEntry(Path.GetFileName(logFile), System.IO.Compression.CompressionLevel.Optimal);
                            using (var zipEntryStream = zipEntry.Open())
                            {
                                fileStream.CopyTo(zipEntryStream);
                            }
                        }
                        GF.LogInfo($"已添加到ZIP: {Path.GetFileName(logFile)}");
                    }
                    catch (System.Exception fileEx)
                    {
                        GF.LogWarning($"跳过文件 {Path.GetFileName(logFile)}: {fileEx.Message}");
                    }
                }
            }

            var zipFileInfo = new FileInfo(zipPath);
            if (zipFileInfo.Length == 0)
            {
                GF.LogError("ZIP文件为空，所有日志文件都无法读取");
                Util.GetInstance().CloseWaiting("UploadLogFiles");
                yield break;
            }

            GF.LogInfo($"日志压缩完成: {zipPath}, 大小: {zipFileInfo.Length / 1024}KB");
        }
        catch (System.Exception ex)
        {
            GF.LogError($"压缩日志失败: {ex.Message}\n路径: {zipPath}\n堆栈: {ex.StackTrace}");
            
            // 尝试清理部分创建的文件
            if (!string.IsNullOrEmpty(zipPath) && File.Exists(zipPath))
            {
                try { File.Delete(zipPath); } catch { }
            }
            Util.GetInstance().CloseWaiting("UploadLogFiles");
            yield break;
        }

        // 3.5 加密ZIP文件
        string encryptedZipPath = null;
        try
        {
            encryptedZipPath = zipPath + ".encrypted";
            byte[] zipBytes = File.ReadAllBytes(zipPath);
            byte[] encryptedBytes = EncryptBytes(zipBytes, GetEncryptionKey());
            File.WriteAllBytes(encryptedZipPath, encryptedBytes);
            
            GF.LogInfo($"日志加密完成: {encryptedZipPath}, 加密后大小: {encryptedBytes.Length / 1024}KB");
            
            // 删除原始ZIP
            File.Delete(zipPath);
            zipPath = encryptedZipPath;
        }
        catch (System.Exception ex)
        {
            GF.LogError($"加密日志失败: {ex.Message}\n路径: {encryptedZipPath}\n堆栈: {ex.StackTrace}");
            
            // 清理失败的文件
            if (!string.IsNullOrEmpty(zipPath) && File.Exists(zipPath))
            {
                try { File.Delete(zipPath); } catch { }
            }
            if (!string.IsNullOrEmpty(encryptedZipPath) && File.Exists(encryptedZipPath))
            {
                try { File.Delete(encryptedZipPath); } catch { }
            }
            Util.GetInstance().CloseWaiting("UploadLogFiles");
            yield break;
        }

        // 4. 上传ZIP文件（带重试）
        int maxRetries = 3;
        
        for (int retry = 0; retry < maxRetries; retry++)
        {
            if (retry > 0)
            {
                float delay = Mathf.Pow(2, retry); // 指数退避: 2s, 4s, 8s
                GF.LogInfo($"等待 {delay}秒 后重试...");
                yield return new WaitForSeconds(delay);
            }

            // 读取ZIP文件
            byte[] zipData = File.ReadAllBytes(zipPath);
            float zipSizeKB = zipData.Length / 1024f;
            
            // 生成唯一文件名: 用户ID_时间戳.zip
            string fileName = $"user_{userDataModel.PlayerId}_{System.DateTime.Now:yyyyMMdd_HHmmss}.zip";
            string uploadUrl = GetUploadUrl(fileName);

            GF.LogInfo($"准备上传 ({retry + 1}/{maxRetries}): {uploadUrl}, 大小: {zipSizeKB:F2}KB ({zipData.Length} bytes)");

            // 使用PUT方法直接上传文件到静态服务器
            UnityWebRequest request = UnityWebRequest.Put(uploadUrl, zipData);
            request.SetRequestHeader("Content-Type", "application/zip");
            request.SetRequestHeader("Content-Length", zipData.Length.ToString());
            request.timeout = 30;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // 打印响应信息
                string responseText = request.downloadHandler?.text ?? "(无响应内容)";
                long uploadedBytes = (long)request.uploadedBytes;
                
                GF.LogInfo($"✅ 上传成功: {fileName}\n" +
                          $"  - 本地大小: {zipSizeKB:F2}KB ({zipData.Length} bytes)\n" +
                          $"  - 已上传: {uploadedBytes} bytes\n" +
                          $"  - HTTP状态: {request.responseCode}\n" +
                          $"  - 服务器响应: {responseText}");
                
                GF.UI.ShowToast($"上传成功!");
                break;
            }
            else
            {
                GF.LogError($"上传失败 (尝试 {retry + 1}/{maxRetries}): {request.error}");
                
                if (retry == maxRetries - 1)
                {
                    GF.UI.ShowToast($"上传失败");
                }
            }
        }

        // 5. 清理临时ZIP文件
        if (!string.IsNullOrEmpty(zipPath) && File.Exists(zipPath))
        {
            try { File.Delete(zipPath); } catch { }
        }
        Util.GetInstance().CloseWaiting("UploadLogFiles");
    }

    /// <summary>
    /// 计算字节数组的MD5哈希
    /// </summary>
    private string ComputeMD5(byte[] data)
    {
        using (var md5 = MD5.Create())
        {
            byte[] hash = md5.ComputeHash(data);
            var sb = new StringBuilder();
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// 获取上传URL（使用热更新服务器的resource目录）
    /// </summary>
    private string GetUploadUrl(string fileName)
    {
        // 使用https避免重定向
        string baseUrl = "https://xingyunxing999.xin/resource/logs";
        return $"{baseUrl}/{fileName}";
    }

    /// <summary>
    /// 获取加密密钥
    /// </summary>
    private string GetEncryptionKey()
    {
        // 32字节密钥(AES-256) - 随机生成,请妥善保管
        return "IobAU9&5OiSaheR@xcjJDkp!TME3dwG*";
    }

    /// <summary>
    /// AES加密
    /// </summary>
    private byte[] EncryptBytes(byte[] data, string password)
    {
        using (Aes aes = Aes.Create())
        {
            // 从密码派生密钥
            byte[] key = new byte[32];
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            Array.Copy(passwordBytes, key, System.Math.Min(passwordBytes.Length, key.Length));
            
            aes.Key = key;
            aes.GenerateIV();
            
            using (var ms = new MemoryStream())
            {
                // 写入IV(解密时需要)
                ms.Write(aes.IV, 0, aes.IV.Length);
                
                // 加密数据
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.FlushFinalBlock();
                }
                
                return ms.ToArray();
            }
        }
    }

    /// <summary>
    /// 获取跨平台安全的临时目录
    /// </summary>
    private string GetSafeTempDirectory()
    {
        string tempDir;
        
#if UNITY_EDITOR || UNITY_STANDALONE
        // PC编辑器和独立平台使用系统临时目录
        tempDir = Path.GetTempPath();
#elif UNITY_ANDROID || UNITY_IOS || UNITY_WEBGL
        // 移动平台和WebGL使用持久化数据目录的temp子目录
        tempDir = Path.Combine(Application.persistentDataPath, "temp");
        
        // 确保目录存在
        if (!Directory.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
            GF.LogInfo($"创建临时目录: {tempDir}");
        }
        
        // 清理7天前的旧压缩文件，避免占用空间
        try
        {
            var oldFiles = Directory.GetFiles(tempDir, "logs_*.zip*");
            var sevenDaysAgo = DateTime.Now.AddDays(-7);
            foreach (var oldFile in oldFiles)
            {
                var fileInfo = new FileInfo(oldFile);
                if (fileInfo.CreationTime < sevenDaysAgo)
                {
                    File.Delete(oldFile);
                    GF.LogInfo($"清理旧压缩文件: {Path.GetFileName(oldFile)}");
                }
            }
        }
        catch (Exception ex)
        {
            GF.LogWarning($"清理旧文件失败: {ex.Message}");
        }
#else
        // 其他平台兜底
        tempDir = Application.persistentDataPath;
#endif
        
        return tempDir;
    }

    
}
