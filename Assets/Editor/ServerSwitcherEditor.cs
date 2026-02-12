﻿using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class ServerSwitcherEditor : EditorWindow
{
    private Const.ServerType selectedServer = Const.ServerType.本地服; // 默认值

    private const string constCs = "Assets/AAAGame/Scripts/Common/Const.cs"; // 脚本路径

    [MenuItem("Tools/切换服务器")]
    public static void ShowWindow()
    {
        var window = GetWindow<ServerSwitcherEditor>("切换服务器");
        window.minSize = new Vector2(300, 200);
        window.LoadCurrentServerConfig(); // 初始化时加载当前配置
    }

    private void LoadCurrentServerConfig()
    {
        if (File.Exists(constCs))
        {
            string content = File.ReadAllText(constCs);
            
            // 提取当前服务器配置
            var ipMatch = Regex.Match(content, @"ServerInfo_ServerIP = ""(.*?)""");
            var portMatch = Regex.Match(content, @"ServerInfo_ServerPort = (\d+)");
            
            if (ipMatch.Success && portMatch.Success)
            {
                string currentIP = ipMatch.Groups[1].Value;
                int currentPort = int.Parse(portMatch.Groups[1].Value);

                // 根据IP和端口匹配服务器类型
                if (currentIP == "192.168.31.82" && currentPort == 7001)
                {
                    selectedServer = Const.ServerType.本地服;
                }
                else if (currentIP == "211.149.244.181" && currentPort == 7001)
                {
                    selectedServer = Const.ServerType.测试服;
                }
                else if (currentIP == "xyxhoutai.xin" && currentPort == 7001)
                {
                    selectedServer = Const.ServerType.外网服;
                }
            }
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("选择服务器配置", EditorStyles.boldLabel);

        selectedServer = (Const.ServerType)EditorGUILayout.EnumPopup("服务器类型", selectedServer);

        GUILayout.Space(10);

        if (selectedServer == Const.ServerType.本地服)
        {
            GUILayout.Label("本地服务器配置:", EditorStyles.boldLabel);
            GUILayout.Label("IP: 192.168.31.82");
            GUILayout.Label("Port: 7001");
        }
        else if (selectedServer == Const.ServerType.测试服)
        {
            GUILayout.Label("测试服务器配置:", EditorStyles.boldLabel);
            GUILayout.Label("IP: 211.149.244.181");
            GUILayout.Label("Port: 7001");
        }
        else if (selectedServer == Const.ServerType.外网服)
        {
            GUILayout.Label("外网服务器配置:", EditorStyles.boldLabel);
            GUILayout.Label("IP: xyxhoutai.xin");
            GUILayout.Label("Port: 7001");
        }

        GUILayout.Space(20);

        if (GUILayout.Button("应用并保存"))
        {
            ApplyServerSettings();
        }
    }

    private void ApplyServerSettings()
    {
        // 根据选择生成对应的服务器配置
        string serverIP, backEndIP, checkVersionUrl;
        int serverPort, backEndPort;
        serverIP = "192.168.31.82";
        serverPort = 7001;
        backEndIP = "192.168.31.12";
        backEndPort = 80;
        checkVersionUrl = "http://192.168.31.12:80/resource";

        if (selectedServer == Const.ServerType.本地服)
        {
            serverIP = "192.168.31.82";
            serverPort = 7001;
            backEndIP = "192.168.31.12";
            backEndPort = 80;
            checkVersionUrl = "http://192.168.31.12:80/resource";
        }
        else if (selectedServer == Const.ServerType.测试服)
        {
            serverIP = "211.149.244.181";
            serverPort = 7001;
            backEndIP = "211.149.244.181";
            backEndPort = 8000;
            checkVersionUrl = "http://211.149.244.181:8000/resource";
        }
        else if (selectedServer == Const.ServerType.外网服)
        {
            serverIP = "xyxhoutai.xin";
            serverPort = 7001;
            backEndIP = "xyxhoutai.xin";
            backEndPort = 80;
            checkVersionUrl = "http://xingyunxing999.xin/resource";
        }

        // 更新 ConstBuiltin 脚本内容
        if (File.Exists(constCs))
        {
            string content = File.ReadAllText(constCs);

            // 使用正则表达式替换相应的字段值
            content = Regex.Replace(content, @"public static string ServerInfo_ServerIP = "".*?"";", $"public static string ServerInfo_ServerIP = \"{serverIP}\";");
            content = Regex.Replace(content, @"public static int ServerInfo_ServerPort = \d+;", $"public static int ServerInfo_ServerPort = {serverPort};");
            content = Regex.Replace(content, @"public static string ServerInfo_BackEndIP = "".*?"";", $"public static string ServerInfo_BackEndIP = \"{backEndIP}\";");
            content = Regex.Replace(content, @"public static int ServerInfo_BackEndPort = \d+;", $"public static int ServerInfo_BackEndPort = {backEndPort};");
            // 更新当前服务器类型
            content = Regex.Replace(content, @"public static ServerType CurrentServerType { get; set; } = ServerType\..*?;", $"public static ServerType CurrentServerType {{ get; set; }} = ServerType.{selectedServer};");
            
            // 保存修改后的脚本内容
            File.WriteAllText(constCs, content);

            Debug.Log("服务器配置已保存到 const.cs 脚本！");
        }

        // 更新 AppSettings.asset
        var appSettingsAsset = AssetDatabase.LoadAssetAtPath<AppSettings>("Assets/Resources/AppSettings.asset");
        if (appSettingsAsset != null)
        {
            appSettingsAsset.CheckVersionUrl = checkVersionUrl;
            EditorUtility.SetDirty(appSettingsAsset);
            AssetDatabase.SaveAssets();
            Debug.Log("服务器配置已保存到 AppSettings.asset！");
        }
        else
        {
            Debug.LogError("无法找到 AppSettings.asset 文件！");
        }

        AssetDatabase.Refresh();
    }
}
