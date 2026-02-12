using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// 游戏对象池，用于对象复用以提高性能
/// </summary>
public class GameObjectPool
{
    /// <summary>
    /// 对象池中的游戏对象列表
    /// </summary>
    public List<GameObject> goList = new List<GameObject>();
    
    /// <summary>
    /// 对象池当前索引位置
    /// </summary>
    int ogListIdx = 0;
    
    /// <summary>
    /// 原始游戏对象模板
    /// </summary>
    GameObject orgGo;
    
    /// <summary>
    /// 对象池父节点
    /// </summary>
    Transform parent = null;
    
    /// <summary>
    /// 创建对象池
    /// </summary>
    /// <param name="orgGo">原始游戏对象模板</param>
    /// <param name="Count">初始对象数量</param>
    /// <param name="parent">对象池父节点</param>
    public void CreatePool(GameObject orgGo, int Count, Transform parent = null)
    {
        this.orgGo = orgGo;
        GameObject go;
        this.parent = parent; 

        for (int i = 0; i < Count; i++)
        {
            if(parent == null)
                go = Object.Instantiate(orgGo);
            else
                go = Object.Instantiate(orgGo, parent);

            go.SetActive(false);
            goList.Add(go); 
        }
    }

    /// <summary>
    /// 从对象池中获取一个游戏对象
    /// </summary>
    /// <returns>激活状态的游戏对象</returns>
    public GameObject PopGameObject()
    {
        for (int i = 0; i < goList.Count; ++i) 
        {
            int temI = (ogListIdx + i) % goList.Count;
            if (!goList[temI].activeInHierarchy)
            {
                ogListIdx = (temI + 1) % goList.Count;
                return goList[temI];
            }
        }

        // 如果没有可用对象，则创建新对象
        GameObject go;

        if (parent == null)
            go = Object.Instantiate(orgGo);
        else
            go = Object.Instantiate(orgGo, parent);

        goList.Add(go);
        return go;
    }

    /// <summary>
    /// 将游戏对象回收到对象池
    /// </summary>
    /// <param name="go">要回收的游戏对象</param>
    public void PushGameObject(GameObject go)
    { 
        go.SetActive(false);
        go.transform.SetParent(parent);
    }

    /// <summary>
    /// 回收已停止的粒子系统游戏对象
    /// </summary>
    public void RecoverGameObjectsForParticles()
    {
        ParticleSystem ps;
        for (int i = 0; i < goList.Count; i++)
        {
            ps = goList[i].GetComponent<ParticleSystem>();
            if (goList[i].activeSelf && ps.isStopped)
                goList[i].SetActive(false);
        }
    }

    /// <summary>
    /// 获取对象池中的游戏对象列表
    /// </summary>
    /// <returns>游戏对象列表</returns>
    public List<GameObject> GetGameObjectList()
    {
        return goList;
    }

    /// <summary>
    /// 销毁对象池中的所有对象并清空池
    /// </summary>
    public void Destory()
    {
        for (int i = 0; i < goList.Count; i++)
        {
            Object.Destroy(goList[i]);
        }

        goList.Clear();
        orgGo = null;
    }
}

