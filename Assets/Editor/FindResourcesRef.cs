using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

//查找预制体使用的所有资源
//查找文件夹下所有预制体所引用的资源
public static class FindResourcesRef
{
    
    [MenuItem("Assets/查找该资源引用了谁", false, 50)]
    static void FindResourcesRefFunction()
    {
        FindResources fr = new FindResources();
    }

}
public class FindResources
{
    private List<string> m_SelectList;
    private Dictionary<string, Dictionary<string, int>> m_ResourcesList = new Dictionary<string, Dictionary<string, int>>();
    private StreamWriter sw;
    public FindResources()
    {
        if (null == m_SelectList)
        {
            m_SelectList = new List<string>();
        }

        m_SelectList.Clear();

        if (null == m_ResourcesList)
        {
            m_ResourcesList = new Dictionary<string, Dictionary<string, int>>();
        }
        m_ResourcesList.Clear();
        //sw = new StreamWriter(@"findResourcesFile.txt", false, Encoding.UTF8);

        GetSelectItem();
        //sw.Close();
        //sw.Flush();
    }
    private void GetSelectItem()
    {
        Debug.Log("find resources ....................................................");
        //sw.WriteLineAsync("find resources ......................................................");
        Dictionary<string, int> abList = new Dictionary<string, int>();
        foreach (Object o in Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets))
        {
            var path = AssetDatabase.GetAssetPath(o);

            // 过滤掉meta文件和文件夹
            if (path.Contains(".meta") || path.Contains(".") == false)
            {
                continue;
            }
            if (path.Contains(".shader"))
            {
                path = path + ".meta";
            }
            string filename = GetShortPath(path);//System.IO.Path.GetFileName(path);
            m_ResourcesList.Clear();
            m_SelectList.Add(path);
            Debug.Log(string.Format("<color=#FF1493>[{0}]{1}</color>", GetTime(), " res=" + path));
            //sw.WriteLineAsync("resources=" + path);
            string text = System.IO.File.ReadAllText(path);

            //string[] arr = text.Split('\n');
            string pattern = @"guid: (.+),";
            foreach (Match match in Regex.Matches(text, pattern))
            {
                string guid = match.Value.Substring(6, match.Value.Length - 7);
                string res = GetShortPath1(AssetDatabase.GUIDToAssetPath(guid));
                string[] arr = res.Split('.');
                int len = arr.Length;
                if (len > 1 && arr[len - 1] != "dll" && arr[len - 1] != "cs")
                {

                    if (m_ResourcesList.ContainsKey(res))
                    {
                        if (m_ResourcesList[res].ContainsKey(filename))
                        {
                            m_ResourcesList[res][filename] = m_ResourcesList[res][filename] + 1;
                        }
                        else
                        {
                            m_ResourcesList[res].Add(filename, 1);
                        }
                    } else {
                        Dictionary<string, int> temp = new Dictionary<string, int>();
                        temp.Add(filename, 1);
                        m_ResourcesList.Add(res, temp);
                    }   
                }
            }
            



            foreach (KeyValuePair<string, Dictionary<string, int>> rres in m_ResourcesList)
            {
                string temp = rres.Key;
                int a = temp.IndexOf("/");
                int b = temp.LastIndexOf("/");
                temp = temp.Substring(a + 1, b - a - 1);
                if (abList.ContainsKey(temp))
                {
                    abList[temp] = abList[temp] + 1;
                }
                else
                {
                    abList.Add(temp, 1);
                }
                Debug.Log(string.Format("<color=yellow>[{0}]{1} {2}</color>", GetTime(), JsonConvert.SerializeObject(rres.Value), " reference=" + rres.Key));
                //sw.WriteLineAsync("  reference=" + rres.Key);
                /*foreach (KeyValuePair<string, int> rrres in rres.Value)
                {
                    Debug.Log(string.Format("<color=#3CB371>{0}</color>", "res=" + rrres.Key + " count =" + rrres.Value));//绿色
                    //sw.WriteLineAsync("    resources=" + rrres.Key + " count =" + rrres.Value);
                }*/
            }
        }
        Debug.Log("reference ab:\n");
        //sw.WriteLineAsync("reference ab:");
        foreach (KeyValuePair<string, int> rrres in abList)
        {
            Debug.Log(string.Format("<color=#FF00FF>[{0}]  {1}</color>", rrres.Value, rrres.Key));
            //sw.WriteLineAsync("    "+rrres.Value+rrres.Key);
        }

        Debug.Log("find reference end\n");
        //sw.WriteLineAsync("find reference end\n");
    }

    public string GetTime()
    {
        string millisecond = DateTime.Now.Millisecond < 10 ? "0" + DateTime.Now.Millisecond.ToString() : DateTime.Now.Millisecond.ToString();
        string second = DateTime.Now.Second < 10 ? "0" + DateTime.Now.Second.ToString() : DateTime.Now.Second.ToString();

        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(second);
        stringBuilder.Append(".");
        stringBuilder.Append(millisecond);

        //Debug.Log($"当前时间 = {}");
        return stringBuilder.ToString();
    }

    public string GetShortPath(String strPath)
    {
        return GetStringOne(strPath, "FishBattle/");
    }
    public string GetShortPath1(String strPath)
    {
        return GetStringOne(strPath, "AllResources/");
    }
    public string GetStringOne(String strPath, String key)
    {
        int a = strPath.LastIndexOf(key);
        if(a < 0)
        {
            return strPath;
        }
        else
        {
            return strPath.Substring(a + key.Length);
        }
        
    }
}

