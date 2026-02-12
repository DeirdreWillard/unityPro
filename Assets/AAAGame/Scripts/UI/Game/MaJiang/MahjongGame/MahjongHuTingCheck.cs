using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NetMsg;
using UnityEngine;

    public struct TingData
    {
        public int tingCardIdx;
        public int huCardsEndIdx;
        public byte[] huCards;
    }



    public class MahjongHuPaiData
    {
        struct SingleKey
        {
            public byte count;
            public byte key1;
            public byte key2;
        }

        struct KeyPtr
        {
            public int key;
            public byte idx;
        }

        /// <summary>
        /// 赖子最大个数
        /// </summary>
        const int MAX_LAIZI_NUM = 7;

        /// <summary>
        /// 最大牌数量
        /// </summary>
        int max_pai_idx_num;

        /// <summary>
        /// 牌型最大胡牌数量
        /// </summary>
        public int MAX_HUPAI_NUM = 0;

        /// <summary>
        /// bit_flag
        /// </summary>
        const int BIT_VAL_FLAG = 0x07;


        /// <summary>
        ///一个值占的bit数 
        /// </summary>
        const int BIT_VAL_NUM = 3;

        int laziPaisCount = 30;
        int recordLaziDataLimitCount = 2;
        int[] laziLimits = new int[] { 1, 10, 30, 60, 90 };
        uint[] retSucessTempLaiDataData = new uint[1];

        /// <summary>
        /// 单个顺子+刻子,其中也包含赖子配对后的顺子和刻子
        /// </summary>
        HashSet<int> setSingle = null;
        Dictionary<int, SingleKey> dictLaiziSingleKeys = null;

        /// <summary>
        /// 单个将,其中也包含赖子配对后的将
        /// </summary>
        HashSet<int> setSingleJiang = null;
        Dictionary<int, byte> dictLaiziSingleJiangKeys = null;



        /// <summary>
        /// 组合单个key后的有效胡牌牌型的key值集合
        /// 其中key为不包含赖子参与计算的paikey
        /// 其中value为这组有效牌型用到的赖子的个数
        /// </summary>
        Dictionary<int, byte>[] dictHuPaiKeys = null;

        Dictionary<int, uint[]>[] dictLaiziKeys = null;


        byte[] _paiIndexs = null;
        int laziBitMask;

        bool isCreateLaiziDetailData = false;



        /// <summary>
        /// 是否生成单个顺子作为有效牌型
        /// </summary>
        public bool IsCreateSingleShunZiVaildPaiType { get; set; }

        /// <summary>
        /// 记录赖子数据的最大赖子个数
        /// </summary>
        public int RecordLaziDataLimitCount
        {
            get { return recordLaziDataLimitCount; }

            set
            {
                recordLaziDataLimitCount = value;
                laziPaisCount = laziLimits[recordLaziDataLimitCount];

                if (recordLaziDataLimitCount == 0)
                    IsCreateLaiziDetailData = false;
            }
        }

        /// <summary>
        /// 是否生成赖子的详细数据
        /// </summary>
        public bool IsCreateLaiziDetailData
        {
            get { return isCreateLaiziDetailData; }

            set
            {
                if (isCreateLaiziDetailData == value)
                    return;

                if (recordLaziDataLimitCount == 0)
                    isCreateLaiziDetailData = false;
                else
                    isCreateLaiziDetailData = value;

                if (isCreateLaiziDetailData)
                {
                    dictLaiziSingleKeys = new Dictionary<int, SingleKey>();
                    dictLaiziSingleJiangKeys = new Dictionary<int, byte>();

                    dictLaiziKeys = new Dictionary<int, uint[]>[MAX_HUPAI_NUM + 1];
                    for (int i = 0; i < dictLaiziKeys.Length; i++)
                    {
                        dictLaiziKeys[i] = new Dictionary<int, uint[]>();
                    }
                }
                else
                {
                    dictLaiziSingleKeys = null;
                    dictLaiziKeys = null;
                }
            }
        }


        /// <summary>
        /// 设置最大胡牌数量
        /// </summary>
        public int MaxHuPaiAmount
        {
            get
            {
                return MAX_HUPAI_NUM;
            }
            set
            {
                if (value == MAX_HUPAI_NUM)
                    return;

                MAX_HUPAI_NUM = value;

                dictHuPaiKeys = new Dictionary<int, byte>[MAX_HUPAI_NUM + 1];
                for (int i = 0; i < dictHuPaiKeys.Length; i++)
                {
                    dictHuPaiKeys[i] = new Dictionary<int, byte>();
                }

                IsCreateLaiziDetailData = false;
                IsCreateLaiziDetailData = true;
            }
        }

        public MahjongHuPaiData(int maxPaiIdxNum = 10, int maxHuaPaiNum = 14)
        {
            IsCreateSingleShunZiVaildPaiType = true;

            max_pai_idx_num = maxPaiIdxNum;
            MaxHuPaiAmount = maxHuaPaiNum;

            _paiIndexs = new byte[max_pai_idx_num];

            int n = 7 << ((max_pai_idx_num - 1) * BIT_VAL_NUM);
            laziBitMask = (int)(n ^ 0xFFFFFFFF);

            setSingle = new HashSet<int>();
            setSingleJiang = new HashSet<int>();
        }

        /// <summary>
        /// 根据牌型的索引数组获取牌型的key值
        /// </summary>
        /// <param name="indexs"></param>
        /// <returns></returns>
        int GetPaiKeyByPaiIndexs(byte[] indexs)
        {
            int nKey = 0;
            for (int i = 0; i < indexs.Length; ++i)
                nKey |= (indexs[i] & BIT_VAL_FLAG) << (BIT_VAL_NUM * i);
            return nKey;
        }


        /// <summary>
        /// 判断牌key值是否是有效能胡的key值
        /// </summary>
        /// <param name="paiKey"></param>
        /// <returns></returns>
        bool IsValidPaiKey(int paiKey)
        {
            for (int i = 0; i < _paiIndexs.Length; ++i)
                _paiIndexs[i] = (byte)((paiKey >> (BIT_VAL_NUM * i)) & BIT_VAL_FLAG);

            if (_paiIndexs[_paiIndexs.Length - 1] > MAX_LAIZI_NUM)
                return false;

            int count = 0;
            for (int i = 0; i < _paiIndexs.Length; ++i)
            {
                count += _paiIndexs[i];
                if (_paiIndexs[i] > 4 || count > MAX_HUPAI_NUM)
                    return false;
            }

            return count > 0;
        }


        /// <summary>
        /// 根据给定的牌型key值获取此牌型牌的数量
        /// </summary>
        /// <param name="paiKey"></param>
        /// <param name="isContainLaiZiPai">计算牌数量时，是否包含赖子牌</param>
        /// <returns></returns>
        byte GetPaiAmountByPaiKey(int paiKey, bool isContainLaiZiPai = false)
        {
            for (int i = 0; i < _paiIndexs.Length; ++i)
                _paiIndexs[i] = (byte)((paiKey >> (BIT_VAL_NUM * i)) & BIT_VAL_FLAG);

            byte amount = 0;
            int len = _paiIndexs.Length - 1;

            if (isContainLaiZiPai) //是否包含赖子牌的数量计算
                len++;

            for (int i = 0; i < len; ++i)
                amount += _paiIndexs[i];

            return amount;
        }


        /// <summary>
        /// 添加有效牌型key值到有效胡牌牌型的key值集合中
        /// </summary>
        /// <param name="mapTemp"></param>
        /// <param name="paiKey">不包含赖子参与计算的paikey</param>
        void AddPaiKeyToDictHuPaiKeys(int paiKey, uint laiziData)
        {
            byte amount = GetPaiAmountByPaiKey(paiKey, false);
            byte newLaiziAmount = (byte)((paiKey >> (BIT_VAL_NUM * (max_pai_idx_num - 1))) & BIT_VAL_FLAG);

            //去除高位赖子牌数量值，得到不包含赖子参与计算的paikey
            int paiKeyNotHaveLaizi = (paiKey & laziBitMask);
            bool isContains = dictHuPaiKeys[amount].ContainsKey(paiKeyNotHaveLaizi);
            byte orgLaiziAmount = 0;

            if (isContains)
            {
                orgLaiziAmount = dictHuPaiKeys[amount][paiKeyNotHaveLaizi];
                dictHuPaiKeys[amount][paiKeyNotHaveLaizi] = Math.Min(orgLaiziAmount, newLaiziAmount);
            }
            else
            {
                dictHuPaiKeys[amount][paiKeyNotHaveLaizi] = newLaiziAmount;
            }

            if (IsCreateLaiziDetailData == false)
                return;

            //生成赖子的详细数据
            if (isContains)
            {
                if (orgLaiziAmount == newLaiziAmount)
                {
                    if (GetLaiziCountFromCombData(laiziData) == 0)
                        return;

                    if (dictLaiziKeys[amount].ContainsKey(paiKeyNotHaveLaizi))
                    {
                        uint[] datas = dictLaiziKeys[amount][paiKeyNotHaveLaizi];
                        if (!CheckHavSameLaiziData(ref datas, laiziData))
                        {
                            datas[0]++;

                            if (newLaiziAmount <= RecordLaziDataLimitCount)
                                datas[datas[0]] = laiziData;
                        }
                    }
                    else
                    {
                        uint[] datas;
                        if (newLaiziAmount <= RecordLaziDataLimitCount)
                        {
                            datas = new uint[laziPaisCount];
                            datas[0] = 1;
                            datas[1] = laiziData;
                        }
                        else
                        {
                            datas = new uint[1];
                            datas[0] = 1;
                        }

                        dictLaiziKeys[amount][paiKeyNotHaveLaizi] = datas;
                    }
                }
                else if (newLaiziAmount < orgLaiziAmount)
                {
                    if (GetLaiziCountFromCombData(laiziData) == 0)
                    {
                        if (dictLaiziKeys[amount].ContainsKey(paiKeyNotHaveLaizi))
                            dictLaiziKeys[amount].Remove(paiKeyNotHaveLaizi);
                        return;
                    }

                    uint[] datas;
                    if (dictLaiziKeys[amount].ContainsKey(paiKeyNotHaveLaizi))
                    {
                        if (orgLaiziAmount > RecordLaziDataLimitCount)
                        {
                            datas = new uint[laziPaisCount];
                            dictLaiziKeys[amount][paiKeyNotHaveLaizi] = datas;
                        }
                        else
                        {
                            datas = dictLaiziKeys[amount][paiKeyNotHaveLaizi];
                        }

                        datas[0] = 1;
                        if (newLaiziAmount <= RecordLaziDataLimitCount)
                            datas[1] = laiziData;
                    }
                    else
                    {
                        if (newLaiziAmount <= RecordLaziDataLimitCount)
                        {
                            datas = new uint[laziPaisCount];
                            datas[0] = 1;
                            datas[1] = laiziData;
                        }
                        else
                        {
                            datas = new uint[1];
                            datas[0] = 1;
                        }

                        dictLaiziKeys[amount][paiKeyNotHaveLaizi] = datas;
                    }
                }
            }
            else
            {
                if (GetLaiziCountFromCombData(laiziData) == 0)
                    return;

                uint[] datas;
                if (newLaiziAmount <= RecordLaziDataLimitCount)
                {
                    datas = new uint[laziPaisCount];
                    datas[0] = 1;
                    datas[1] = laiziData;
                }
                else
                {
                    datas = new uint[1];
                    datas[0] = 1;
                }

                dictLaiziKeys[amount][paiKeyNotHaveLaizi] = datas;
            }
        }

        /// <summary>
        /// 添加有效牌型key值到有效胡牌牌型的key值集合中
        /// </summary>
        /// <param name="mapTemp"></param>
        /// <param name="paiKey">不包含赖子参与计算的paikey</param>
        void AddPaiKeyToDictHuPaiKeys(int paiKey, uint[] laiziDatas)
        {
            byte amount = GetPaiAmountByPaiKey(paiKey, false);
            byte newLaiziAmount = (byte)((paiKey >> (BIT_VAL_NUM * (max_pai_idx_num - 1))) & BIT_VAL_FLAG);

            //去除高位赖子牌数量值，得到不包含赖子参与计算的paikey
            int paiKeyNotHaveLaizi = (paiKey & laziBitMask);
            bool isContains = dictHuPaiKeys[amount].ContainsKey(paiKeyNotHaveLaizi);
            byte orgLaiziAmount = 0;

            if (isContains)
            {
                orgLaiziAmount = dictHuPaiKeys[amount][paiKeyNotHaveLaizi];
                dictHuPaiKeys[amount][paiKeyNotHaveLaizi] = Math.Min(orgLaiziAmount, newLaiziAmount);
            }
            else
            {
                dictHuPaiKeys[amount][paiKeyNotHaveLaizi] = newLaiziAmount;
            }


            if (IsCreateLaiziDetailData == false ||
                laiziDatas == null ||
                newLaiziAmount > RecordLaziDataLimitCount)
                return;

            //生成赖子的详细数据
            if (isContains)
            {
                if (orgLaiziAmount == newLaiziAmount)
                {
                    if (dictLaiziKeys[amount].ContainsKey(paiKeyNotHaveLaizi))
                    {
                        uint[] datas = dictLaiziKeys[amount][paiKeyNotHaveLaizi];
                        uint idx = datas[0];

                        if (newLaiziAmount <= RecordLaziDataLimitCount)
                        {
                            for (int i = 1; i <= laiziDatas[0]; i++)
                            {
                                if (!CheckHavSameLaiziData(ref datas, laiziDatas[i]))
                                    datas[++idx] = laiziDatas[i];
                            }
                        }
                        else
                        {
                            idx = datas[0] + laiziDatas[0];
                        }

                        datas[0] = idx;
                    }
                    else
                    {
                        dictLaiziKeys[amount][paiKeyNotHaveLaizi] = laiziDatas;
                    }
                }
                else if (newLaiziAmount < orgLaiziAmount)
                {
                    dictLaiziKeys[amount][paiKeyNotHaveLaizi] = laiziDatas;
                }
            }
            else
            {
                dictLaiziKeys[amount][paiKeyNotHaveLaizi] = laiziDatas;
            }
        }


        bool CheckHavSameLaiziData(ref uint[] datas, uint laiziData)
        {
            for (int i = 0; i < datas.Length; i++)
            {
                if (datas[i] == laiziData)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 组合赖子数据
        /// </summary>
        /// <param name="laziData"></param>
        /// <param name="lazi"></param>
        /// <returns></returns>
        uint CombLaiZiToData(uint laziData, byte lazi)
        {
            uint countInOldQue = GetLaiziCountFromCombData(laziData);
            if (countInOldQue == 0)
                return _CombLaiZiToData(laziData, lazi);

            uint newLaziData = 0;
            byte laziInOldQue;
            bool isAddNewLaizi = false;

            for (int i = 0; i < countInOldQue; i++)
            {
                laziInOldQue = GetLaiziFromCombData(laziData, i);

                if (!isAddNewLaizi && lazi <= laziInOldQue)
                {
                    newLaziData = _CombLaiZiToData(newLaziData, lazi);
                    isAddNewLaizi = true;
                }

                newLaziData = _CombLaiZiToData(newLaziData, laziInOldQue);
            }

            if (!isAddNewLaizi)
                newLaziData = _CombLaiZiToData(newLaziData, lazi);

            return newLaziData;
        }
        uint _CombLaiZiToData(uint laziData, byte lazi)
        {
            uint count = (laziData >> 27) & 0x1F;
            uint countHi = (laziData & 0x7FFFFFF) | (((count + 1) << 27) & 0xF8000000);
            int movBit = (int)count * 4;
            uint mask = ((uint)0xF << movBit);
            laziData = (((uint)lazi << movBit) & mask) | countHi;

            return laziData;
        }
        uint GetLaiziCountFromCombData(uint laziData)
        {
            return (laziData >> 27) & 0x1F;
        }
        byte GetLaiziFromCombData(uint laziData, int idx)
        {
            return (byte)((laziData >> (idx * 4)) & 0xF);
        }

        uint CombLaiZiToDataByKey(uint laziData, KeyPtr keyPtr)
        {
            if (!dictLaiziSingleKeys.ContainsKey(keyPtr.key))
                return laziData;

            SingleKey singleKey = dictLaiziSingleKeys[keyPtr.key];
            byte lazi;
            if (keyPtr.idx == 0) { lazi = singleKey.key1; }
            else { lazi = singleKey.key2; }

            return CombLaiZiToData(laziData, lazi);
        }

        uint CombJiangLaiZiToDataByKey(uint laziData, int key)
        {
            if (!dictLaiziSingleJiangKeys.ContainsKey(key))
                return laziData;

            return CombLaiZiToData(laziData, dictLaiziSingleJiangKeys[key]);
        }

        void AddToDictLaiziSingleKeys(int key, byte value)
        {
            if (dictLaiziSingleKeys.ContainsKey(key))
            {
                SingleKey singleKey = dictLaiziSingleKeys[key];
                singleKey.count = 2;
                singleKey.key2 = value;
                dictLaiziSingleKeys[key] = singleKey;
            }
            else
            {
                SingleKey singleKey = new SingleKey();
                singleKey.count = 1;
                singleKey.key1 = value;
                dictLaiziSingleKeys[key] = singleKey;
            }
        }

        KeyPtr[] CreateKeyPtrs()
        {
            int singlePaiKeyCount = setSingle.Count;
            int[] singlePaiKey = setSingle.ToArray();

            List<KeyPtr> keyPtrList = new List<KeyPtr>();
            SingleKey singleKey;
            KeyPtr keyPtr;
            for (int i = 0; i < singlePaiKey.Length; i++)
            {
                if (dictLaiziSingleKeys == null ||
                    !dictLaiziSingleKeys.ContainsKey(singlePaiKey[i]))
                {
                    keyPtr = new KeyPtr();
                    keyPtr.key = singlePaiKey[i];
                    keyPtr.idx = 0;
                    keyPtrList.Add(keyPtr);
                }
                else
                {
                    singleKey = dictLaiziSingleKeys[singlePaiKey[i]];

                    if (singleKey.count == 2)
                    {
                        keyPtr = new KeyPtr();
                        keyPtr.key = singlePaiKey[i];
                        keyPtr.idx = 1;
                        keyPtrList.Add(keyPtr);
                    }

                    keyPtr = new KeyPtr();
                    keyPtr.key = singlePaiKey[i];
                    keyPtr.idx = 0;
                    keyPtrList.Add(keyPtr);
                }
            }

            return keyPtrList.ToArray();
        }


        /// <summary>
        /// 生成单个有效牌型的key值
        /// </summary>
        void CreateSingleVaildPaiTypeKey()
        {
            if (setSingle.Count != 0)
                return;

            byte[] paiIndexs = new byte[max_pai_idx_num];
            int key;

            //三个赖子作顺子，或刻子
            Array.Clear(paiIndexs, 0, paiIndexs.Length);
            paiIndexs[paiIndexs.Length - 1] = 3;
            setSingle.Add(GetPaiKeyByPaiIndexs(paiIndexs));

            //刻子
            for (int i = 0; i < paiIndexs.Length - 1; ++i)
            {
                Array.Clear(paiIndexs, 0, paiIndexs.Length);

                for (int n = 0; n < 3; ++n)
                {
                    paiIndexs[i] = (byte)(3 - n);
                    paiIndexs[paiIndexs.Length - 1] = (byte)n;

                    key = GetPaiKeyByPaiIndexs(paiIndexs);
                    setSingle.Add(key);

                    if (IsCreateLaiziDetailData && n != 0)
                        AddToDictLaiziSingleKeys(key, (byte)i);
                }
            }


            if (IsCreateSingleShunZiVaildPaiType)
            {
                //顺子 没赖子
                for (int i = 0; i < paiIndexs.Length - 3; ++i)
                {
                    Array.Clear(paiIndexs, 0, paiIndexs.Length);

                    paiIndexs[i] = 1;
                    paiIndexs[i + 1] = 1;
                    paiIndexs[i + 2] = 1;

                    setSingle.Add(GetPaiKeyByPaiIndexs(paiIndexs));
                }


                //顺子 1个赖子 (2个赖子时也就是刻子,上面已经添加刻子)
                for (int i = 0; i < paiIndexs.Length - 3; ++i)
                {
                    for (int n = 0; n < 3; ++n)
                    {
                        Array.Clear(paiIndexs, 0, paiIndexs.Length);
                        paiIndexs[i] = 1;
                        paiIndexs[i + 1] = 1;
                        paiIndexs[i + 2] = 1;

                        paiIndexs[i + n] = 0;
                        paiIndexs[paiIndexs.Length - 1] = 1;

                        key = GetPaiKeyByPaiIndexs(paiIndexs);
                        setSingle.Add(key);

                        if (IsCreateLaiziDetailData)
                            AddToDictLaiziSingleKeys(key, (byte)(i + n));
                    }
                }

            }

            //将牌 两个赖子作将
            Array.Clear(paiIndexs, 0, paiIndexs.Length);
            paiIndexs[paiIndexs.Length - 1] = 2;
            setSingleJiang.Add(GetPaiKeyByPaiIndexs(paiIndexs));

            //将牌 (包含一个赖子作将的情况)
            for (int i = 0; i < paiIndexs.Length - 1; ++i)
            {
                Array.Clear(paiIndexs, 0, paiIndexs.Length);

                for (int n = 0; n < 2; ++n)
                {
                    paiIndexs[i] = (byte)(2 - n);
                    paiIndexs[paiIndexs.Length - 1] = (byte)n;

                    key = GetPaiKeyByPaiIndexs(paiIndexs);
                    setSingleJiang.Add(key);

                    if (IsCreateLaiziDetailData && n != 0)
                        dictLaiziSingleJiangKeys[key] = (byte)i;
                }
            }
        }


        /// <summary>
        /// 生成组合牌类型的有效key到dictHuPaiKeys
        /// 主要算法为：把所有的单个有效key值进行一个4组或5组的遍历组合，剔除掉组合后无效的key值组。
        /// key值之间能组合相加，主要是因为单张牌数量相加不能超过3个bit位，即7张牌的数量，
        /// 如果有进位组合相加的算法会有问题
        /// </summary>
        /// <param name="dictHuPaiKeys"></param>
        void CreateCombPaiTypeVaildKeyToDict()
        {

            KeyPtr[] singlePaiKey = CreateKeyPtrs();
            int singlePaiKeyCount = singlePaiKey.Length;
            int[] combPaiKey = new int[6];
            uint[] laziData = new uint[6];

            //组合所有可能的顺子,刻子组合
            for (int i1 = 0; i1 < singlePaiKeyCount; ++i1)
            {
                if (IsCreateLaiziDetailData)
                    laziData[1] = CombLaiZiToDataByKey(0, singlePaiKey[i1]);

                AddPaiKeyToDictHuPaiKeys(singlePaiKey[i1].key, laziData[1]);

                for (int i2 = i1; i2 < singlePaiKeyCount; ++i2)
                {
                    combPaiKey[2] = singlePaiKey[i1].key + singlePaiKey[i2].key;
                    if (!IsValidPaiKey(combPaiKey[2]))
                        continue;

                    if (IsCreateLaiziDetailData)
                        laziData[2] = CombLaiZiToDataByKey(laziData[1], singlePaiKey[i2]);

                    AddPaiKeyToDictHuPaiKeys(combPaiKey[2], laziData[2]);

                    for (int i3 = i2; i3 < singlePaiKeyCount; ++i3)
                    {
                        combPaiKey[3] = combPaiKey[2] + singlePaiKey[i3].key;
                        if (!IsValidPaiKey(combPaiKey[3]))
                            continue;

                        if (IsCreateLaiziDetailData)
                            laziData[3] = CombLaiZiToDataByKey(laziData[2], singlePaiKey[i3]);

                        AddPaiKeyToDictHuPaiKeys(combPaiKey[3], laziData[3]);

                        for (int i4 = i3; i4 < singlePaiKeyCount; ++i4)
                        {
                            combPaiKey[4] = combPaiKey[3] + singlePaiKey[i4].key;

                            if (!IsValidPaiKey(combPaiKey[4]))
                                continue;

                            if (IsCreateLaiziDetailData)
                                laziData[4] = CombLaiZiToDataByKey(laziData[3], singlePaiKey[i4]);

                            AddPaiKeyToDictHuPaiKeys(combPaiKey[4], laziData[4]);


                            if (MAX_HUPAI_NUM > 14)
                            {
                                for (int i5 = i4; i5 < singlePaiKeyCount; ++i5)
                                {
                                    combPaiKey[5] = combPaiKey[4] + singlePaiKey[i5].key;

                                    if (!IsValidPaiKey(combPaiKey[5]))
                                        continue;

                                    if (IsCreateLaiziDetailData)
                                        laziData[5] = CombLaiZiToDataByKey(laziData[4], singlePaiKey[i5]);

                                    AddPaiKeyToDictHuPaiKeys(combPaiKey[5], laziData[5]);
                                }
                            }
                        }
                    }
                }
            }


            //组合将,顺子,刻子
            int singleJiangPaiKeyCount = setSingleJiang.Count;
            int[] singleJiangPaiKey = setSingleJiang.ToArray();

            Dictionary<int, byte>[] tmpDictHuPaiKeys = new Dictionary<int, byte>[MAX_HUPAI_NUM + 1];
            Dictionary<int, uint[]>[] tmpDictLaiziKeys = new Dictionary<int, uint[]>[MAX_HUPAI_NUM + 1];

            for (int j = 0; j < tmpDictHuPaiKeys.Length; ++j)
            {
                tmpDictHuPaiKeys[j] = new Dictionary<int, byte>(dictHuPaiKeys[j]);

                if (IsCreateLaiziDetailData)
                {
                    tmpDictLaiziKeys[j] = new Dictionary<int, uint[]>();
                    foreach (int key in dictLaiziKeys[j].Keys)
                    {
                        uint[] obj = (uint[])dictLaiziKeys[j][key].Clone();
                        tmpDictLaiziKeys[j].Add(key, obj);
                    }
                }
            }


            uint lzData = 0;
            bool isHaveJiangLaizi = false;
            byte jiangLaizi = 0;
            uint[] laziDatas = null;

            for (int i = 0; i < singleJiangPaiKeyCount; ++i)
            {
                //直接把将作为有效组合牌型存入dictHuPaiKeys
                if (IsCreateLaiziDetailData)
                {
                    isHaveJiangLaizi = dictLaiziSingleJiangKeys.ContainsKey(singleJiangPaiKey[i]);
                    if (isHaveJiangLaizi)
                    {
                        jiangLaizi = dictLaiziSingleJiangKeys[singleJiangPaiKey[i]];
                        lzData = CombLaiZiToData(0, jiangLaizi);
                    }
                }

                AddPaiKeyToDictHuPaiKeys(singleJiangPaiKey[i], lzData);

                //组合将,顺子,刻子(形成：顺子-将，刻子-将，顺子-刻子-将 的有效组合)
                for (int j = 0; j < tmpDictHuPaiKeys.Length; ++j)
                {
                    foreach (var item in tmpDictHuPaiKeys[j])
                    {
                        laziDatas = null;
                        int nTemp = singleJiangPaiKey[i] + item.Key + ((item.Value & BIT_VAL_FLAG) << ((max_pai_idx_num - 1) * BIT_VAL_NUM));

                        if (!IsValidPaiKey(nTemp))
                            continue;

                        if (!IsCreateLaiziDetailData)
                        {
                            AddPaiKeyToDictHuPaiKeys(nTemp, lzData);
                        }
                        else
                        {
                            if (tmpDictLaiziKeys[j].ContainsKey(item.Key))
                            {
                                laziDatas = tmpDictLaiziKeys[j][item.Key];

                                byte laiziAmount = (byte)((nTemp >> (BIT_VAL_NUM * (max_pai_idx_num - 1))) & BIT_VAL_FLAG);
                                uint[] newLaziDatas;

                                if (isHaveJiangLaizi)
                                {
                                    if (laiziAmount <= recordLaziDataLimitCount)
                                    {
                                        newLaziDatas = new uint[laziPaisCount];

                                        for (int k = 1; k <= laziDatas[0]; k++)
                                            newLaziDatas[k] = CombLaiZiToData(laziDatas[k], jiangLaizi);
                                    }
                                    else
                                    {
                                        newLaziDatas = new uint[1];
                                    }
                                }
                                else
                                {
                                    if (laiziAmount <= recordLaziDataLimitCount)
                                    {
                                        newLaziDatas = new uint[laziPaisCount];
                                        for (int k = 1; k <= laziDatas[0]; k++)
                                            newLaziDatas[k] = laziDatas[k];
                                    }
                                    else
                                    {
                                        newLaziDatas = new uint[1];
                                    }
                                }
                                newLaziDatas[0] = laziDatas[0];

                                AddPaiKeyToDictHuPaiKeys(nTemp, newLaziDatas);
                            }
                            else
                            {
                                AddPaiKeyToDictHuPaiKeys(nTemp, lzData);
                            }
                        }
                    }
                }
            }
        }

        public uint[] CheckCanHuSingle(MahjongHuaSe type, byte[] paiIndexs, ref byte outNeedLaiziCount, byte laiziMaxCount)
        {
            int paiKey = GetPaiKeyByPaiIndexs(paiIndexs);
            if (type == MahjongHuaSe.FENG)
                paiKey &= 0x1FFFFF;

            byte count = GetPaiAmountByPaiKey(paiKey);

            if (dictHuPaiKeys[count].ContainsKey(paiKey))
            {
                outNeedLaiziCount = dictHuPaiKeys[count][paiKey];

                if (outNeedLaiziCount <= laiziMaxCount)
                {
                    uint[] data = retSucessTempLaiDataData;
                    if (dictLaiziKeys != null && dictLaiziKeys[count].ContainsKey(paiKey))
                        data = dictLaiziKeys[count][paiKey];
                    return data;
                }
            }

            outNeedLaiziCount = 0;
            return null;
        }

        public void Train()
        {
            if (setSingle.Count != 0)
                return;

            CreateSingleVaildPaiTypeKey();
            CreateCombPaiTypeVaildKeyToDict();
        }


        public void CreateKeyDataToFile(string fileName)
        {
            byte val;
            // C:\Users\username\AppData\LocalLow\company name\product name
            FileStream fs = File.Open(Application.persistentDataPath + "\\" + fileName, FileMode.Create, FileAccess.Write);
            BinaryWriter bw = new BinaryWriter(fs);

            bw.Write((byte)MAX_HUPAI_NUM);

            for (int i = 0; i < dictHuPaiKeys.Length; ++i)
            {
                foreach (var item in dictHuPaiKeys[i])
                {
                    val = (byte)(((i & 0x1f) << 3) | (item.Value & 0x7));
                    bw.Write(val);
                    bw.Write(item.Key);
                }
            }

            bw.Flush();//清除缓冲区
            bw.Close();//关闭流
        }

        /// <summary>
        /// 从字节数组加载缓存数据
        /// 用于支持热更新系统从自定义路径加载
        /// </summary>
        /// <param name="data">二进制缓存数据</param>
        /// <returns>是否加载成功</returns>
        public bool LoadFromBytes(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                Debug.LogError("加载失败: 数据为空");
                return false;
            }

            try
            {
                using (MemoryStream ms = new MemoryStream(data))
                using (BinaryReader br = new BinaryReader(ms))
                {
                    if (br.PeekChar() > -1)
                    {
                        int hupaiNum = br.ReadByte();
                        if (hupaiNum != MAX_HUPAI_NUM)
                        {
                            MAX_HUPAI_NUM = hupaiNum;
                            dictHuPaiKeys = new Dictionary<int, byte>[MAX_HUPAI_NUM + 1];
                            for (int i = 0; i < dictHuPaiKeys.Length; i++)
                                dictHuPaiKeys[i] = new Dictionary<int, byte>();
                        }
                    }

                    int idx;
                    byte value;
                    byte val;
                    int key;

                    while (br.PeekChar() > -1)
                    {
                        val = br.ReadByte();
                        key = br.ReadInt32();
                        idx = val >> 3;
                        value = (byte)(val & 0x7);
                        dictHuPaiKeys[idx][key] = value;
                    }
                }

                GF.LogInfo_gsc($"成功从字节数组加载麻将缓存数据: {data.Length} 字节");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"从字节数组加载缓存失败: {e.Message}");
                return false;
            }
        }

    }


    public class MahjongHuTingCheck
    {
        #region 全局单例
        
        private static MahjongHuTingCheck _instance;
        private static bool _isInitialized = false;
        
        /// <summary>
        /// 全局单例实例（用于回放等场景）
        /// 首次访问时会自动创建并初始化
        /// </summary>
        public static MahjongHuTingCheck Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MahjongHuTingCheck();
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 检查单例是否已初始化（数据是否已加载）
        /// </summary>
        public static bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// 异步初始化全局单例（加载缓存数据）
        /// 游戏启动时应调用此方法
        /// </summary>
        public static async Cysharp.Threading.Tasks.UniTask InitializeGlobalInstanceAsync()
        {
            if (_isInitialized)
                return;
                
            var instance = Instance;
            bool cacheLoaded = await instance.LoadFromConfigCacheAsync();
            
            if (!cacheLoaded)
            {
                Debug.LogWarning("从配置加载麻将缓存失败,使用Train方式初始化全局单例");
                instance.Train();
            }
            
            _isInitialized = true;
            GF.LogInfo_gsc("MahjongHuTingCheck 全局单例初始化完成");
        }
        
        /// <summary>
        /// 设置已有实例为全局单例（游戏中创建的实例可共享给回放使用）
        /// </summary>
        public static void SetGlobalInstance(MahjongHuTingCheck instance)
        {
            if (instance != null)
            {
                _instance = instance;
                _isInitialized = true;
                GF.LogInfo_gsc("MahjongHuTingCheck 全局单例已设置");
            }
        }
        
        #endregion
        
        MahjongHuPaiData mjHuPaiData;
        MahjongHuPaiData mjHuPaiDataFengZi;
        
        // 添加规则引用，用于花色筛选
        private IMahjongRule currentRule;
        // 添加游戏管理器引用，用于花色筛选
        private BaseMahjongGameManager baseMahjongGameManager;

        public int RecordLaziDataLimitCount
        {
            get
            {
                return mjHuPaiData.RecordLaziDataLimitCount;
            }
            set
            {
                mjHuPaiData.RecordLaziDataLimitCount = value;
                mjHuPaiDataFengZi.RecordLaziDataLimitCount = value;
            }
        }

        public bool IsCreateLaiziDetailData
        {
            get
            {
                return mjHuPaiData.IsCreateLaiziDetailData;
            }
            set
            {
                mjHuPaiData.IsCreateLaiziDetailData = value;
                mjHuPaiDataFengZi.IsCreateLaiziDetailData = value;
            }
        }

        public int MaxHuPaiAmount
        {
            get
            {
                return mjHuPaiData.MaxHuPaiAmount;
            }
            set
            {
                mjHuPaiData.MaxHuPaiAmount = value;
                mjHuPaiDataFengZi.MaxHuPaiAmount = value;
            }
        }

        public MahjongHuTingCheck()
        {
            mjHuPaiData = new MahjongHuPaiData(10);
            mjHuPaiData.IsCreateLaiziDetailData = true;

            mjHuPaiDataFengZi = new MahjongHuPaiData(8);
            mjHuPaiDataFengZi.IsCreateSingleShunZiVaildPaiType = false;
            mjHuPaiDataFengZi.IsCreateLaiziDetailData = true;
        }

        /// <summary>
        /// 设置当前麻将规则，用于花色筛选
        /// </summary>
        /// <param name="rule">当前使用的麻将规则</param>
        /// <param name="baseMahjongGameManager"></param>
        public void SetCurrentRule(IMahjongRule rule, BaseMahjongGameManager baseMahjongGameManager)
        {
            // this.currentRule = rule;
            this.baseMahjongGameManager = baseMahjongGameManager;
        }

        public void Train()
        {
            mjHuPaiData.Train();
            mjHuPaiDataFengZi.Train();
        }

        /// <summary>
        /// 从热更新配置异步加载预生成的缓存数据
        /// 使用GF.Resource系统加载.bytes文件
        /// </summary>
        public async Cysharp.Threading.Tasks.UniTask<bool> LoadFromConfigCacheAsync()
        {
            try
            {
                // 加载普通牌型数据
                var normalAsset = await GFBuiltin.Resource.LoadAssetAwait<TextAsset>(
                    UtilityBuiltin.AssetsPath.GetConfigPath("MahjongHuPaiData", true));
                
                if (normalAsset == null)
                {
                    Debug.LogWarning("无法加载麻将缓存配置: MahjongHuPaiData.bytes");
                    return false;
                }
                
                bool normalLoaded = mjHuPaiData.LoadFromBytes(normalAsset.bytes);
                
                // 加载风字牌型数据
                var fengziAsset = await GFBuiltin.Resource.LoadAssetAwait<TextAsset>(
                    UtilityBuiltin.AssetsPath.GetConfigPath("MahjongHuPaiDataFengZi", true));
                
                if (fengziAsset == null)
                {
                    Debug.LogWarning("无法加载麻将缓存配置: MahjongHuPaiDataFengZi.bytes");
                    return false;
                }
                
                bool fengziLoaded = mjHuPaiDataFengZi.LoadFromBytes(fengziAsset.bytes);
                
                if (normalLoaded && fengziLoaded)
                {
                    GF.LogInfo_gsc("麻将胡听检查数据已从热更新配置加载");
                    return true;
                }
                else
                {
                    Debug.LogWarning("从配置加载麻将缓存数据失败");
                    return false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"加载麻将缓存配置失败: {e.Message}");
                return false;
            }
        }

        public void CreateKeyDataToFile(string keyDataFileName, string keyDataFengZiFileName)
        {
            mjHuPaiData.CreateKeyDataToFile(keyDataFileName);
            mjHuPaiDataFengZi.CreateKeyDataToFile(keyDataFengZiFileName);
        }

        public bool CheckCanHu(byte[] cardSrc, byte laiziIndex)
        {
            byte[] wanCard = new byte[9];
            byte[] tongCard = new byte[9];
            byte[] tiaoCard = new byte[9];
            byte[] fengziCard = new byte[7];

            byte[][] cardArray = new byte[][]
            {
            wanCard, tongCard, tiaoCard, fengziCard
            };

            Array.Copy(cardSrc, wanCard, 9);
            Array.Copy(cardSrc, 9, tongCard, 0, 9);
            Array.Copy(cardSrc, 18, tiaoCard, 0, 9);
            Array.Copy(cardSrc, 27, fengziCard, 0, 7);

            byte richLaiziCount = 0;

            if (laiziIndex >= 0 && laiziIndex < 9)
            {
                richLaiziCount = wanCard[laiziIndex];
                wanCard[laiziIndex] = 0;
            }
            else if (laiziIndex < 18)
            {
                richLaiziCount = tongCard[laiziIndex - 9];
                tongCard[laiziIndex - 9] = 0;
            }
            else if (laiziIndex < 27)
            {
                richLaiziCount = tiaoCard[laiziIndex - 18];
                tiaoCard[laiziIndex - 18] = 0;
            }
            else if (laiziIndex < 34)
            {
                richLaiziCount = fengziCard[laiziIndex - 27];
                fengziCard[laiziIndex - 27] = 0;
            }

            byte jiangCount = 0;
            byte needLaiziCount = 0;
            uint[] laiziDatas;

            for (int cor = 0; cor < (int)MahjongHuaSe.Max; ++cor)
            {
                int nMax = (cor == (int)MahjongHuaSe.FENG) ? 7 : 9;
                int totalCardCount = 0;

                for (int i = 0; i < nMax; ++i)
                    totalCardCount += cardArray[cor][i];

                if (totalCardCount == 0)
                    continue;

                if (cor == (int)MahjongHuaSe.FENG)
                    laiziDatas = mjHuPaiDataFengZi.CheckCanHuSingle((MahjongHuaSe)cor, cardArray[cor], ref needLaiziCount, richLaiziCount);
                else
                    laiziDatas = mjHuPaiData.CheckCanHuSingle((MahjongHuaSe)cor, cardArray[cor], ref needLaiziCount, richLaiziCount);

                if (laiziDatas == null)
                    return false;


                richLaiziCount -= needLaiziCount;

                if ((totalCardCount + needLaiziCount) % 3 == 2)
                    jiangCount += 1;


                if (jiangCount > richLaiziCount + 1)
                    return false;
            }

            return jiangCount > 0 || richLaiziCount >= 2;
        }

        void AddDataToTingPaiDict(Dictionary<int, List<int>> tingPaiDict, int key, int value)
        {
            if (tingPaiDict == null)
                return;

            List<int> tingHuPaiList;

            if (tingPaiDict.ContainsKey(key))
            {
                tingHuPaiList = tingPaiDict[key];
            }
            else
            {
                tingHuPaiList = new List<int>();
                tingPaiDict[key] = tingHuPaiList;
            }

            for (int i = 0; i < tingHuPaiList.Count; i++)
            {
                if (tingHuPaiList[i] == value)
                    return;
            }

            tingHuPaiList.Add(value);
        }

        public void CheckTing(byte[] cards, byte laiziIndex, ref TingData[] tingDatas)
        {
            int tingIdx = 0;
            tingDatas[0].tingCardIdx = -1;
            tingDatas[0].huCardsEndIdx = -1;

            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == 0)
                    continue;

                cards[i]--;
                
                // 根据当前规则获取支持的花色
                var validColors = GetValidColorsFromRule();
                
                // 统计听牌
                for (int cor = 0; cor < (int)MahjongHuaSe.Max; ++cor)
                {
                    // 检查当前花色是否被规则支持
                    var currentColor = (MahjongHuaSe)cor;
                    if (!IsColorValid(validColors, currentColor))
                        continue;

                    int nMax = GetMaxCardCountForColor(currentColor);
                    int offset = cor * 9;

                    for (int j = 0; j < nMax; j++)
                    {
                        // 根据开关决定是否显示已出完的牌
                        if (!MahjongSettings.showExhaustedCardsInHuTips && cards[offset + j] >= 4)
                            continue;

                        // 根据开关决定是否显示赖子牌
                        if (!MahjongSettings.showLaiziInHuTips && offset + j == laiziIndex)
                            continue;

                        cards[offset + j]++;

                        if (CheckCanHu(cards, laiziIndex))
                        {
                            if (tingDatas == null)
                                tingDatas = CreateTingDataMemory();

                            // 检查是否需要扩容
                            if (tingIdx >= tingDatas.Length - 1)
                            {
                                int newSize = tingDatas.Length * 2; // 扩容为原来的2倍
                                tingDatas = ResizeTingDataArray(tingDatas, newSize);
                            }

                            AddDataToTingDatas(tingDatas, tingIdx, i, (byte)(offset + j), laiziIndex);
                        }

                        cards[offset + j]--;
                    }
                }

                cards[i]++;

                if (tingDatas[tingIdx].tingCardIdx != -1)
                    tingIdx++;

            }
        }

        public TingData[] CreateTingDataMemory()
        {
            // 当有赖子时，听牌数量可能很多，适当增加数组大小
            int arraySize = Math.Max(mjHuPaiData.MAX_HUPAI_NUM + 1, 50); // 最少50个元素
            // 每个tingData的huCards数组也要足够大，能容纳所有可能的胡牌（最多34种牌）
            int huCardsSize = Math.Max(mjHuPaiData.MAX_HUPAI_NUM + 1, 34); // 最多34种麻将牌
            
            TingData[] tingDatas = new TingData[arraySize];
            for (int m = 0; m < arraySize; m++)
                tingDatas[m].huCards = new byte[huCardsSize];

            tingDatas[0].tingCardIdx = -1;
            tingDatas[0].huCardsEndIdx = -1;

            return tingDatas;
        }

        /// <summary>
        /// 动态扩容听牌数据数组
        /// </summary>
        /// <param name="originalArray">原数组</param>
        /// <param name="newSize">新大小</param>
        /// <returns>扩容后的数组</returns>
        TingData[] ResizeTingDataArray(TingData[] originalArray, int newSize)
        {
            if (originalArray == null || newSize <= originalArray.Length)
                return originalArray;

            // huCards数组大小，能容纳所有可能的胡牌（最多34种牌）
            int huCardsSize = Math.Max(mjHuPaiData.MAX_HUPAI_NUM + 1, 34);
            TingData[] newArray = new TingData[newSize];
            
            // 复制原有数据
            for (int i = 0; i < originalArray.Length; i++)
            {
                newArray[i] = originalArray[i];
            }
            
            // 初始化新增的元素
            for (int m = originalArray.Length; m < newSize; m++)
            {
                newArray[m].huCards = new byte[huCardsSize];
                newArray[m].tingCardIdx = -1;
                newArray[m].huCardsEndIdx = -1;
            }

            return newArray;
        }

        void AddDataToTingDatas(TingData[] tingDatas, int tingIdx, int tingCardIdx, byte huCardIdx, byte laiziIndex = 255)
        {
            if (tingDatas == null)
                return;

            tingDatas[tingIdx].tingCardIdx = tingCardIdx;
            
            // 检查是否已经存在这张胡牌，避免重复添加
            for (int i = 0; i <= tingDatas[tingIdx].huCardsEndIdx; i++)
            {
                if (tingDatas[tingIdx].huCards[i] == huCardIdx)
                {
                    // 已存在，不重复添加
                    tingDatas[tingIdx + 1].tingCardIdx = -1;
                    tingDatas[tingIdx + 1].huCardsEndIdx = -1;
                    return;
                }
            }
            
            // 检查是否是赖子牌
            bool isLaiziCard = huCardIdx == laiziIndex;
            
            // 如果是赖子牌，放在数组末尾
            if (isLaiziCard)
            {
                tingDatas[tingIdx].huCardsEndIdx++;
                tingDatas[tingIdx].huCards[tingDatas[tingIdx].huCardsEndIdx] = huCardIdx;
            }
            else
            {
                // 非赖子牌，插入到合适位置，保持排序且赖子在最后
                InsertHuCardSorted(tingDatas[tingIdx].huCards, ref tingDatas[tingIdx].huCardsEndIdx, huCardIdx, laiziIndex);
            }

            tingDatas[tingIdx + 1].tingCardIdx = -1;
            tingDatas[tingIdx + 1].huCardsEndIdx = -1;
        }

        /// <summary>
        /// 将胡牌按顺序插入，赖子放在最后
        /// </summary>
        void InsertHuCardSorted(byte[] huCards, ref int huCardsEndIdx, byte huCardIdx, byte laiziIndex)
        {
            huCardsEndIdx++;
            
            // 找到合适的插入位置
            int insertPos = 0;
            
            // 找到插入位置：保持非赖子牌按牌值排序，赖子在最后
            for (int i = 0; i < huCardsEndIdx; i++)
            {
                if (huCards[i] == laiziIndex) // 遇到赖子牌，插在赖子前面
                {
                    insertPos = i;
                    break;
                }
                else if (huCards[i] > huCardIdx) // 遇到更大的非赖子牌，插在前面
                {
                    insertPos = i;
                    break;
                }
                else // 当前牌更大或相等，继续查找
                {
                    insertPos = i + 1;
                }
            }
            
            // 将插入位置后的元素后移
            for (int i = huCardsEndIdx; i > insertPos; i--)
            {
                huCards[i] = huCards[i - 1];
            }
            
            // 在找到的位置插入新牌
            huCards[insertPos] = huCardIdx;
        }

        /// <summary>
        /// 获取能胡的牌
        /// </summary>
        /// <param name="cards">手牌数组</param>
        /// <param name="laiziIndex">赖子索引</param>
        /// <param name="getUsedCardCount">获取场上已使用牌数的回调函数（可选）</param>
        /// <returns></returns>
        public HuPaiTipsInfo[] GetHuCards(byte[] cards, byte laiziIndex)
        {
            // 检查当前手牌是否已经胡牌，如果已经胡牌则不需要显示胡牌提示
            if (CheckCanHu(cards, laiziIndex))
            {
                return new HuPaiTipsInfo[0]; // 已经胡牌，返回空数组
            }

            List<HuPaiTipsInfo> huCards = new List<HuPaiTipsInfo>();

            // 根据当前规则获取支持的花色
            var validColors = GetValidColorsFromRule();

            for (int cor = 0; cor < (int)MahjongHuaSe.Max; ++cor)
            {
                // 检查当前花色是否被规则支持
                var currentColor = (MahjongHuaSe)cor;
                if (!IsColorValid(validColors, currentColor))
                    continue;

                int nMax = GetMaxCardCountForColor(currentColor);
                int offset = cor * 9;

                for (int j = 0; j < nMax; j++)
                {
                    // 根据开关决定是否显示赖子牌
                    if (!MahjongSettings.showLaiziInHuTips && offset + j == laiziIndex)
                        continue;

                    // 跳过已经出完的牌
                    if (!MahjongSettings.showExhaustedCardsInHuTips && cards[offset + j] >= 4)
                        continue;

                cards[offset + j]++;  // 模拟摸到这张牌

                if (CheckCanHu(cards, laiziIndex))  // 检测是否能胡
                {
                    HuPaiTipsInfo info = new HuPaiTipsInfo();
                    info.faceValue = (MahjongFaceValue)(offset + j);  // 设置牌面值
                    
                    // 计算番数 - 如果是卡五星玩法则使用KWXFanCalculator
                    if (baseMahjongGameManager != null && 
                        baseMahjongGameManager.mjConfig != null && 
                        baseMahjongGameManager.mjConfig.MjMethod == MJMethod.Kwx)
                    {
                        // 获取副露数据
                        List<int> meldCards = new List<int>();
                        var mySeat = baseMahjongGameManager.mahjongGameUI?.seats2D[0];
                        if (mySeat != null && mySeat.MeldContainer != null)
                        {
                            for (int i = 0; i < mySeat.MeldContainer.childCount; i++)
                            {
                                Transform child = mySeat.MeldContainer.GetChild(i);
                                var mjCard = child.GetComponent<MjCard_2D>();
                                if (mjCard != null)
                                {
                                    meldCards.Add(mjCard.cardValue);
                                }
                            }
                        }
                        
                        KWXFanCalculator fanCalc = new KWXFanCalculator(baseMahjongGameManager.mjConfig.Kwx);
                        // 传入是否亮牌信息（从座位状态读取）
                        bool hasLiang = mySeat != null && mySeat.HasLiangPai;
                        // 传入胡牌索引用于判断卡五星
                        int huCardIndex = offset + j;
                        info.fanAmount = fanCalc.CalculateFan(cards, laiziIndex, meldCards, hasLiang, huCardIndex);
                        // 如果开启两番起胡，且番数不足，则不计入胡牌提示（视为不能胡）
                        if (fanCalc != null && baseMahjongGameManager.mjConfig != null && baseMahjongGameManager.mjConfig.Kwx != null)
                        {
                            var playList = baseMahjongGameManager.mjConfig.Kwx.Play;
                            bool liangFanQiHu = playList.Contains(12);
                            if (liangFanQiHu && info.fanAmount < 2)
                            {
                                // 番数不足，两番起胡规则：跳过不添加该胡牌
                                cards[offset + j]--;  // 恢复牌后继续
                                continue;
                            }
                        }
                    }
                    // 计算番数 - 如果是血流麻将玩法则使用XueLiuFanCalculator
                    else if (baseMahjongGameManager != null && 
                        baseMahjongGameManager.mjConfig != null && 
                        baseMahjongGameManager.mjConfig.MjMethod == MJMethod.Xl)
                    {
                        // 获取副露数据
                        List<int> meldCards = new List<int>();
                        var mySeat = baseMahjongGameManager.mahjongGameUI?.seats2D[0];
                        if (mySeat != null && mySeat.MeldContainer != null)
                        {
                            for (int i = 0; i < mySeat.MeldContainer.childCount; i++)
                            {
                                Transform child = mySeat.MeldContainer.GetChild(i);
                                var mjCard = child.GetComponent<MjCard_2D>();
                                if (mjCard != null)
                                {
                                    meldCards.Add(mjCard.cardValue);
                                }
                            }
                        }
                        
                        XueLiuFanCalculator fanCalc = new XueLiuFanCalculator(baseMahjongGameManager.mjConfig.XlConfig);
                        // 传入胡牌索引
                        int huCardIndex = offset + j;
                        info.fanAmount = fanCalc.CalculateFan(cards, meldCards, huCardIndex);
                    }
                    else
                    {
                        info.fanAmount = 1;  // 其他玩法默认1番
                    }
                    
                    // 计算剩余张数
                    int selfHasCount = cards[offset + j] - 1;  // 减1是因为前面+1模拟摸牌了
                    int usedInField = 0;
                    // 只有在游戏中（非回放）才计算场上已使用的牌数
                    if (baseMahjongGameManager != null && baseMahjongGameManager.mahjongGameUI != null)
                    {
                        usedInField = baseMahjongGameManager.mahjongGameUI.GetUsedCardCountInField((MahjongFaceValue)(offset + j));  // 场上其他玩家已使用的张数
                    }
                    // 剩余张数 = 总数4 - 自己手里的 - 场上其他人使用的
                    info.zhangAmount = System.Math.Max(0, 4 - selfHasCount - usedInField);

                    huCards.Add(info);  // 记录这张牌的信息
                    }
                    cards[offset + j]--;  // 恢复牌
                }
            }

            return huCards.ToArray();
        }

        #region 花色筛选辅助方法

        /// <summary>
        /// 从当前规则获取支持的花色列表
        /// </summary>
        private List<MahjongHuaSe> GetValidColorsFromRule()
        {
            // if (currentRule != null)
            // {
                // return currentRule.GetValidColors();
            // }
            
            // 默认支持所有花色
            return new List<MahjongHuaSe> { MahjongHuaSe.WAN, MahjongHuaSe.TONG, MahjongHuaSe.TIAO, MahjongHuaSe.FENG };
        }

        /// <summary>
        /// 检查指定花色是否有效
        /// </summary>
        private bool IsColorValid(List<MahjongHuaSe> validColors, MahjongHuaSe color)
        {
            return validColors == null || validColors.Contains(color);
        }

        /// <summary>
        /// 获取指定花色的最大牌数
        /// </summary>
        private int GetMaxCardCountForColor(MahjongHuaSe color)
        {
            // if (currentRule != null)
            // {
                // return currentRule.GetMaxCardCountForColor(color);
            // }
            
            // 默认实现
            switch (color)
            {
                case MahjongHuaSe.WAN:
                case MahjongHuaSe.TONG:
                case MahjongHuaSe.TIAO:
                    return 9; // 万、条、筒各有9种牌
                case MahjongHuaSe.FENG:
                    return 7; // 风字牌：4个风牌 + 3个箭牌（中发白）
                default:
                    return 0;
            }
        }

        #endregion

         /// <summary>
        /// 计算需要亮出的牌（听牌搭子）
        /// 亮牌定义：只有听牌搭子亮着，已成型的组合（顺子、刻子、将牌对子）都变暗
        /// 例如：手牌 456万 123条 55条 67条 听 58条，则只有 67条 亮着，其他变暗
        /// </summary>
        /// <param name="cardArray">手牌数组（byte[34]格式，已移除暗铺牌）</param>
        /// <param name="laiziIndex">赖子索引，255表示无赖子</param>
        /// <returns>需要亮出的牌的索引列表（MahjongFaceValue索引）</returns>
        public List<int> CalculateLiangPaiCards(byte[] cardArray, byte laiziIndex)
        {
            List<int> liangPaiIndices = new List<int>();
            
            // 复制一份用于计算
            byte[] workArray = (byte[])cardArray.Clone();
            
            // 获取原始听牌列表
            HuPaiTipsInfo[] originalHuCards = GetHuCards(workArray, laiziIndex);
            if (originalHuCards == null || originalHuCards.Length == 0)
            {
                // 不能听牌，所有牌都亮着
                for (int i = 0; i < 34; i++)
                {
                    for (int j = 0; j < cardArray[i]; j++)
                    {
                        liangPaiIndices.Add(i);
                    }
                }
                return liangPaiIndices;
            }
            
            // 记录原始可胡牌
            HashSet<MahjongFaceValue> originalHuSet = new HashSet<MahjongFaceValue>();
            foreach (var hu in originalHuCards)
            {
                originalHuSet.Add(hu.faceValue);
            }
            
            // 记录需要变暗的牌索引
            List<int> cardsToRemove = new List<int>();
            
            // 第一阶段：贪心地移除所有能移除的完整组合（顺子和刻子）
            bool removed = true;
            while (removed)
            {
                removed = false;
                
                // 尝试移除刻子（优先级最高）
                for (int i = 0; i < 34 && !removed; i++)
                {
                    if (workArray[i] >= 3)
                    {
                        byte[] testArray = (byte[])workArray.Clone();
                        testArray[i] -= 3;
                        
                        HuPaiTipsInfo[] testHuCards = GetHuCards(testArray, laiziIndex);
                        if (testHuCards != null && testHuCards.Length > 0)
                        {
                            HashSet<MahjongFaceValue> testHuSet = new HashSet<MahjongFaceValue>();
                            foreach (var hu in testHuCards)
                                testHuSet.Add(hu.faceValue);
                            
                            if (testHuSet.SetEquals(originalHuSet))
                            {
                                // 可以移除这个刻子
                                cardsToRemove.Add(i);
                                cardsToRemove.Add(i);
                                cardsToRemove.Add(i);
                                workArray[i] -= 3;
                                removed = true;
                                GF.LogInfo_gsc($"[亮牌计算] 移除刻子: {(MahjongFaceValue)i}");
                            }
                        }
                    }
                }
                if (removed) continue;
                
                // 尝试移除顺子（只处理万条筒，索引0-26）
                for (int i = 0; i < 25 && !removed; i++)
                {
                    // 确保不跨花色
                    int suit = i / 9;
                    if ((i + 1) / 9 != suit || (i + 2) / 9 != suit)
                        continue;
                    
                    if (workArray[i] > 0 && workArray[i + 1] > 0 && workArray[i + 2] > 0)
                    {
                        byte[] testArray = (byte[])workArray.Clone();
                        testArray[i]--;
                        testArray[i + 1]--;
                        testArray[i + 2]--;
                        
                        HuPaiTipsInfo[] testHuCards = GetHuCards(testArray, laiziIndex);
                        GF.LogInfo_gsc($"[亮牌计算] 尝试移除顺子 {(MahjongFaceValue)i}{(MahjongFaceValue)(i+1)}{(MahjongFaceValue)(i+2)}, 听牌数={testHuCards?.Length ?? 0}");
                        if (testHuCards != null && testHuCards.Length > 0)
                        {
                            HashSet<MahjongFaceValue> testHuSet = new HashSet<MahjongFaceValue>();
                            foreach (var hu in testHuCards)
                                testHuSet.Add(hu.faceValue);
                            
                            bool matches = testHuSet.SetEquals(originalHuSet);
                            GF.LogInfo_gsc($"[亮牌计算] 听牌集合匹配={matches}, 原始={string.Join(",", originalHuSet)}, 测试={string.Join(",", testHuSet)}");
                            if (matches)
                            {
                                // 可以移除这个顺子
                                cardsToRemove.Add(i);
                                cardsToRemove.Add(i + 1);
                                cardsToRemove.Add(i + 2);
                                workArray[i]--;
                                workArray[i + 1]--;
                                workArray[i + 2]--;
                                removed = true;
                                GF.LogInfo_gsc($"[亮牌计算] 成功移除顺子: {(MahjongFaceValue)i}{(MahjongFaceValue)(i+1)}{(MahjongFaceValue)(i+2)}");
                            }
                        }
                    }
                }
            }
            
            // 第二阶段：处理剩余的牌（应该是 将牌+听牌搭子 的形式）
            // 统计剩余牌数
            int remainingCount = 0;
            for (int i = 0; i < 34; i++)
            {
                remainingCount += workArray[i];
            }
            GF.LogInfo_gsc($"[亮牌计算] 第一阶段后剩余牌数={remainingCount}");
            
            // 如果剩余牌数是 4 或 5 张（将牌2张 + 搭子2或3张），尝试分离将牌和搭子
            if (remainingCount >= 4 && remainingCount <= 5)
            {
                // 尝试找出哪个对子是将牌（移除后剩余的牌是听牌搭子）
                for (int i = 0; i < 34; i++)
                {
                    if (workArray[i] >= 2)
                    {
                        byte[] testArray = (byte[])workArray.Clone();
                        testArray[i] -= 2;
                        
                        // 计算移除对子后剩余的牌数
                        int afterRemoveCount = 0;
                        for (int k = 0; k < 34; k++)
                            afterRemoveCount += testArray[k];
                        
                        // 检查：移除对子后，剩余的牌加上每张能胡的牌是否都能组成完整的顺子/刻子
                        // 这意味着剩余的牌是一个有效的听牌搭子
                        bool allHuCardsCanFormMelds = true;
                        foreach (var huCard in originalHuCards)
                        {
                            byte[] huTestArray = (byte[])testArray.Clone();
                            int huIdx = (int)huCard.faceValue;
                            if (huIdx >= 0 && huIdx < 34)
                            {
                                huTestArray[huIdx]++;
                                if (!CanDecomposeToMeldsOnly(huTestArray))
                                {
                                    allHuCardsCanFormMelds = false;
                                    break;
                                }
                            }
                        }
                        
                        GF.LogInfo_gsc($"[亮牌计算] 尝试移除将牌 {(MahjongFaceValue)i}, 剩余{afterRemoveCount}张, 所有胡牌能组成顺子刻子={allHuCardsCanFormMelds}");
                        
                        if (!allHuCardsCanFormMelds)
                            continue;
                        
                        // 如果移除对子后只剩2或3张牌（即听牌搭子），
                        // 且所有胡牌都能让剩余牌组成顺子/刻子，
                        // 那么这个对子就是有效的将牌
                        if (afterRemoveCount <= 3 && allHuCardsCanFormMelds)
                        {
                            GF.LogInfo_gsc($"[亮牌计算] 成功移除将牌: {(MahjongFaceValue)i}");
                            cardsToRemove.Add(i);
                            cardsToRemove.Add(i);
                            workArray[i] -= 2;
                            break;
                        }
                        
                        // 如果剩余牌数较多，还需要验证听牌集合是否一致
                        // （防止误移除参与听牌的对子，如 1122 听 12 的情况）
                        HuPaiTipsInfo[] testHuCards = GetHuCards(testArray, laiziIndex);
                        
                        bool huSetMatches = false;
                        if (testHuCards != null && testHuCards.Length > 0)
                        {
                            HashSet<MahjongFaceValue> testHuSet = new HashSet<MahjongFaceValue>();
                            foreach (var hu in testHuCards)
                                testHuSet.Add(hu.faceValue);
                            
                            huSetMatches = testHuSet.SetEquals(originalHuSet);
                        }
                        
                        if (huSetMatches)
                        {
                            GF.LogInfo_gsc($"[亮牌计算] 成功移除将牌(听牌匹配): {(MahjongFaceValue)i}");
                            cardsToRemove.Add(i);
                            cardsToRemove.Add(i);
                            workArray[i] -= 2;
                            break;
                        }
                    }
                }
            }
            
            // 计算亮牌：原始手牌中不在 cardsToRemove 中的牌
            List<int> tempRemove = new List<int>(cardsToRemove);
            for (int i = 0; i < 34; i++)
            {
                for (int j = 0; j < cardArray[i]; j++)
                {
                    if (tempRemove.Contains(i))
                    {
                        tempRemove.Remove(i);
                    }
                    else
                    {
                        liangPaiIndices.Add(i);
                    }
                }
            }
            
            return liangPaiIndices;
        }
        
        /// <summary>
        /// 检查给定的牌能否完全拆分成顺子和刻子（没有剩余）
        /// 用于判断移除将牌后的牌是否都是已成型的组合
        /// </summary>
        private bool CanDecomposeToMeldsOnly(byte[] cardArray)
        {
            // 计算总牌数
            int totalCards = 0;
            for (int i = 0; i < 34; i++)
            {
                totalCards += cardArray[i];
            }
            
            // 必须是 3 的倍数才能完全拆成顺子/刻子
            if (totalCards % 3 != 0)
                return false;
            
            // 如果没有牌，认为可以拆完
            if (totalCards == 0)
                return true;
            
            // 复制一份用于计算
            byte[] work = (byte[])cardArray.Clone();
            
            // 递归尝试拆分
            return TryDecomposeMelds(work);
        }
        
        /// <summary>
        /// 递归尝试将牌拆分成顺子和刻子
        /// </summary>
        private bool TryDecomposeMelds(byte[] cards)
        {
            // 找到第一张非零的牌
            int firstIdx = -1;
            for (int i = 0; i < 34; i++)
            {
                if (cards[i] > 0)
                {
                    firstIdx = i;
                    break;
                }
            }
            
            // 没有牌了，拆分成功
            if (firstIdx == -1)
                return true;
            
            // 尝试拆成刻子
            if (cards[firstIdx] >= 3)
            {
                cards[firstIdx] -= 3;
                if (TryDecomposeMelds(cards))
                    return true;
                cards[firstIdx] += 3;
            }
            
            // 尝试拆成顺子（只有万条筒可以，索引0-26）
            int suit = firstIdx / 9;
            if (suit < 3 && firstIdx % 9 <= 6) // 确保在同一花色内且能组成顺子
            {
                int idx1 = firstIdx;
                int idx2 = firstIdx + 1;
                int idx3 = firstIdx + 2;
                
                // 确保三张牌在同一花色
                if (idx2 / 9 == suit && idx3 / 9 == suit)
                {
                    if (cards[idx1] > 0 && cards[idx2] > 0 && cards[idx3] > 0)
                    {
                        cards[idx1]--;
                        cards[idx2]--;
                        cards[idx3]--;
                        if (TryDecomposeMelds(cards))
                            return true;
                        cards[idx1]++;
                        cards[idx2]++;
                        cards[idx3]++;
                    }
                }
            }
            
            return false;
        }
    }

    /// <summary>
    /// 卡五星番数计算器
    /// </summary>
    public class KWXFanCalculator
    {
        private KWX_Config kwxConfig;
        
        public KWXFanCalculator(KWX_Config config)
        {
            this.kwxConfig = config;
        }
        
        /// <summary>
        /// 计算卡五星番数
        /// </summary>
        /// <param name="cards">手牌数组</param>
        /// <param name="laiziIndex">赖子索引</param>
        /// <param name="meldCards">副露区的牌（碰杠的牌）</param>
        /// <param name="hasLiangPai">是否亮牌</param>
        /// <param name="huCardIndex">胡牌索引（用于判断卡五星），-1表示不检查卡五星</param>
        public int CalculateFan(byte[] cards, byte laiziIndex, List<int> meldCards, bool hasLiangPai, int huCardIndex = -1)
        {
            if (kwxConfig == null)
                return 1; // 默认1番
            int totalFan = 0;
            
            // 打印手牌信息
            System.Text.StringBuilder cardInfo = new System.Text.StringBuilder();
            cardInfo.Append("[番数计算] 手牌: ");
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] > 0)
                {
                    cardInfo.Append($"[{i}]={cards[i]} ");
                }
            }
            cardInfo.Append($", 赖子索引={laiziIndex}, 亮牌={hasLiangPai}");
            if (meldCards != null && meldCards.Count > 0)
            {
                cardInfo.Append($", 副露区=[{string.Join(",", meldCards)}]");
            }
            GF.LogInfo_gsc(cardInfo.ToString());
            
            // 判断牌型
            bool isPengPengHu = IsPengPengHu(cards, laiziIndex);
            bool isQiDui = IsQiDui(cards, laiziIndex);
            bool hasKaWuXing = HasKaWuXing(cards, meldCards, huCardIndex);
            bool isQingYiSe = IsQingYiSe(cards, meldCards);
            
            // 四归判定移到后面，需要先获取全频道开关
            int mingSiGuiCount = 0;
            int anSiGuiCount = 0;
            
            bool hasXiaoSanYuan = HasXiaoSanYuan(cards, meldCards);
            bool hasDaSanYuan = HasDaSanYuan(cards, meldCards);
            bool isShouZhuaYi = IsShouZhuaYi(cards);
            
            // 打印牌型判定结果（四归数量在后面计算后打印）
            GF.LogInfo_gsc($"[番数计算] 牌型判定: 碰碰胡={isPengPengHu}, 七对={isQiDui}, 卡五星={hasKaWuXing}, " +
                      $"小三元={hasXiaoSanYuan}, 大三元={hasDaSanYuan}, 手抓一={isShouZhuaYi}, 清一色={isQingYiSe}");
            
            // 获取玩法配置
            var playList = kwxConfig.Play;
            bool gangShangHuaX4 = playList.Contains(1);      // 杠上花*4
            bool kaWuXingX4 = playList.Contains(2);          // 卡五星*4（勾选4番，不勾选2番）
            bool pengPengHuX4 = playList.Contains(3);        // 碰碰胡*4（勾选4番，不勾选2番）
            bool xiaoSanYuanQiDuiX8 = playList.Contains(4);  // 小三元七对*8
            bool daSanYuanQiDuiX16 = playList.Contains(5);   // 大三元七对*16
            bool haidiLaoX2 = playList.Contains(6);          // 海底捞/炮*2
            bool gangShangPaoFanFan = playList.Contains(7);  // 杠上炮翻番
            bool duiLiangFanFan = playList.Contains(8);      // 对亮翻番
            bool quanPinDao = playList.Contains(9);          // 全频道（勾选全频道，不勾选半频道）
            bool shuKan = playList.Contains(10);             // 数坎
            bool shuangGui = playList.Contains(11);          // 双归
            bool liangFanQiHu = playList.Contains(12);       // 两番起胡
            bool guoShouPeng = playList.Contains(13);        // 过手碰
            bool huanSanZhang = playList.Contains(14);       // 换三张
            
            // 四归判定需要全频道开关
            // 明四归：碰了3张 + 胡第4张（不需要其他条件），副露区的明杠不算
            // 暗四归：手牌3张 + 胡第4张（不需要其他条件），或者手牌4张且（全频道开启 或 清一色）
            mingSiGuiCount = CountMingSiGui(meldCards, cards, huCardIndex);
            anSiGuiCount = CountAnSiGui(cards, huCardIndex, quanPinDao, isQingYiSe);
            
            GF.LogInfo_gsc($"[番数计算] 四归统计: 明四归数量={mingSiGuiCount}, 暗四归数量={anSiGuiCount}, 双归开关={shuangGui}");
            
            // ==================== 番数计算（番数相乘） ====================
            // 基础番数：屁胡1番
            totalFan = 1;
            
            // 清一色：4番
            if (isQingYiSe)
            {
                totalFan *= 4;
            }
            
            // 碰碰胡：根据配置2番或4番
            if (isPengPengHu)
            {
                int pengPengHuFan = pengPengHuX4 ? 4 : 2;
                totalFan *= pengPengHuFan;
            }
            
            // 七对：4番
            if (isQiDui)
            {
                totalFan *= 4;
            }
            
            // 小三元：4番（独立牌型，不需要七对）
            if (hasXiaoSanYuan)
            {
                int xiaoSanYuanFan = 4;
                // 如果同时是七对且勾选了小三元七对*8，则用8番代替4番
                if (isQiDui && xiaoSanYuanQiDuiX8)
                {
                    // 已经乘了七对4番，现在用8番替换，所以除以4再乘8，等于乘2
                    totalFan *= 2;
                }
                else
                {
                    totalFan *= xiaoSanYuanFan;
                }
            }
            
            // 大三元：8番（独立牌型，不需要七对）
            if (hasDaSanYuan)
            {
                int daSanYuanFan = 8;
                // 如果同时是七对且勾选了大三元七对*16，则用16番代替8番
                if (isQiDui && daSanYuanQiDuiX16)
                {
                    // 已经乘了七对4番，现在用16番替换，所以除以4再乘16，等于乘4
                    totalFan *= 4;
                }
                else
                {
                    totalFan *= daSanYuanFan;
                }
            }
            
            // 四归番数计算（双归模式或普通模式）
            // 暗四归4番 > 明四归2番，优先算暗四归
            if (shuangGui)
            {
                // 双归模式：最多算2个四归，优先暗四归（4番）再明四归（2番）
                int siGuiTotal = anSiGuiCount + mingSiGuiCount;
                int siGuiToCount = Math.Min(siGuiTotal, 2); // 最多2个
                
                // 优先用暗四归
                int anSiGuiUsed = Math.Min(anSiGuiCount, siGuiToCount);
                int mingSiGuiUsed = Math.Min(mingSiGuiCount, siGuiToCount - anSiGuiUsed);
                
                // 计算番数：暗四归4番，明四归2番
                for (int i = 0; i < anSiGuiUsed; i++)
                {
                    totalFan *= 4;
                }
                for (int i = 0; i < mingSiGuiUsed; i++)
                {
                    totalFan *= 2;
                }
                
                GF.LogInfo_gsc($"[番数计算] 双归模式: 总四归={siGuiTotal}, 计算{siGuiToCount}个(暗四归{anSiGuiUsed}个*4番, 明四归{mingSiGuiUsed}个*2番)");
            }
            else
            {
                // 非双归模式：有暗四归算4番，有明四归算2番
                if (anSiGuiCount > 0)
                {
                    totalFan *= 4;
                }
                if (mingSiGuiCount > 0)
                {
                    totalFan *= 2;
                }
            }
            
            // 卡五星：根据配置2番或4番
            if (hasKaWuXing)
            {
                int kaWuXingFan = kaWuXingX4 ? 4 : 2;
                totalFan *= kaWuXingFan;
            }
            
            // 手抓一：8番
            if (isShouZhuaYi)
            {
                totalFan *= 8;
            }
            
            // 打印番数计算明细
            GF.LogInfo_gsc($"[番数计算] 番数明细: 清一色={isQingYiSe}(4番), 碰碰胡={isPengPengHu}({(pengPengHuX4 ? 4 : 2)}番), " +
                      $"七对={isQiDui}(4番), 小三元={hasXiaoSanYuan}(4番), 大三元={hasDaSanYuan}(8番), " +
                      $"明四归数量={mingSiGuiCount}(2番), 暗四归数量={anSiGuiCount}(4番), 双归={shuangGui}, 卡五星={hasKaWuXing}({(kaWuXingX4 ? 4 : 2)}番)");
            
            // 亮牌翻番：如果玩家处于亮牌状态，则在所有基础上再翻1倍
            if (hasLiangPai)
            {
                totalFan *= 2;
            }

            GF.LogInfo_gsc($"[番数计算] 最终番数={totalFan}");

            // 封顶限制
            int topOut = kwxConfig.TopOut;
            if (topOut > 0 && totalFan > topOut)
            {
                totalFan = topOut;
            }

            GF.LogInfo_gsc($"[番数计算] 封顶={topOut}");
            return totalFan;
        }
        
        /// <summary>
        /// 判断是否碰碰胡
        /// 碰碰胡：手牌符合 3X + 2Y（X≥0，Y≥1），即全是刻子+至少一个对子，没有顺子
        /// </summary>
        private bool IsPengPengHu(byte[] cards, byte laiziIndex)
        {
            // 碰碰胡：手牌全是刻子（3张相同）或对子（2张相同），没有顺子
            // 每种牌的数量必须能被拆分成2和3的组合，且至少有一个对子
            bool hasDuizi = false;
            
            for (int i = 0; i < cards.Length; i++)
            {
                if (i == laiziIndex) continue; // 跳过赖子
                
                int count = cards[i];
                if (count == 0) continue;
                
                // 检查count能否被拆分成2和3的组合（不能有余数1）
                // count % 3 == 1 时，无法拆分（如1,4,7...）
                if (count % 3 == 1)
                {
                    return false;
                }
                
                // 检查是否有对子（count % 3 == 2 表示有对子）
                if (count % 3 == 2)
                {
                    hasDuizi = true;
                }
            }
            
            // 必须至少有一个对子（Y≥1）
            return hasDuizi;
        }
        
        /// <summary>
        /// 判断是否七对
        /// </summary>
        private bool IsQiDui(byte[] cards, byte laiziIndex)
        {
            int duiziCount = 0;
            
            for (int i = 0; i < cards.Length; i++)
            {
                if (i == laiziIndex) continue; // 跳过赖子
                
                if (cards[i] >= 2)
                {
                    duiziCount += cards[i] / 2;
                }
            }
            
            return duiziCount >= 7;
        }
        
        /// <summary>
        /// 判断是否卡五星
        /// 卡五星规则：胡的牌是5（任意花色），且是通过46搭子卡5胡牌
        /// 即：手牌中存在一个456顺子，其中的5是胡进来的那张
        /// 判定方法：移除456顺子后，剩余牌+胡牌前的牌不能组成合法牌型（说明需要这张5）
        /// </summary>
        /// <param name="cards">手牌数组（已包含胡的牌）</param>
        /// <param name="meldCards">副露区的牌</param>
        /// <param name="huCardIndex">胡牌的索引</param>
        private bool HasKaWuXing(byte[] cards, List<int> meldCards, int huCardIndex)
        {
            if (huCardIndex < 0) return false;
            
            // 检查胡的牌是否是5（5万=4, 5条=22, 5筒=13）
            int cardInSuit = huCardIndex % 9; // 在花色中的位置（0-8对应1-9）
            if (cardInSuit != 4) // 5在花色中的位置是4（0=1,1=2,2=3,3=4,4=5）
            {
                return false; // 胡的不是5，不是卡五星
            }
            
            // 检查胡的牌是否在万条筒范围内（0-26），风字牌没有卡五星
            if (huCardIndex >= 27)
            {
                return false;
            }
            
            // 确定花色起始索引
            int suitStart = (huCardIndex / 9) * 9; // 0=万, 9=条, 18=筒
            int fourIndex = suitStart + 3;  // 4的索引
            int fiveIndex = huCardIndex;    // 5的索引（就是胡牌索引）
            int sixIndex = suitStart + 5;   // 6的索引
            
            int fiveCountAfterHu = cards[fiveIndex];  // 胡牌后5的数量
            int fiveCountBeforeHu = fiveCountAfterHu - 1;  // 胡牌前5的数量
            int fourCount = cards[fourIndex];
            int sixCount = cards[sixIndex];
            
            GF.LogInfo_gsc($"[卡五星判定] huCardIndex={huCardIndex}, 4索引={fourIndex}(数量={fourCount}), 5索引={fiveIndex}(胡前={fiveCountBeforeHu},胡后={fiveCountAfterHu}), 6索引={sixIndex}(数量={sixCount})");
            
            // 基本条件：胡牌后必须有4和6，才可能形成456顺子
            if (fourCount < 1 || sixCount < 1)
            {
                GF.LogInfo_gsc($"[卡五星判定] 失败: 4或6不足");
                return false;
            }
            
            // 卡五星的核心判定：
            // 胡牌前的手牌不能胡（缺5），胡了5之后才能胡
            // 这意味着胡进来的这张5是"关键的一张"，用于补全46搭子
            
            // 方法：检查胡牌前的牌能否胡
            // 如果胡牌前不能胡，说明需要这张5，就是卡五星
            byte[] cardsBeforeHu = new byte[cards.Length];
            Array.Copy(cards, cardsBeforeHu, cards.Length);
            cardsBeforeHu[huCardIndex]--;  // 减去胡的那张5
            
            // 如果胡牌前就能胡，说明不是必须要这张5，不是卡五星
            if (MahjongHuTingCheck.Instance.CheckCanHu(cardsBeforeHu, 255))
            {
                GF.LogInfo_gsc($"[卡五星判定] 失败: 胡牌前就能胡，不需要这张5");
                return false;
            }
            
            // 胡牌前不能胡，现在验证是否是通过456顺子胡的
            // 复制一份手牌（已包含胡牌），减去456顺子
            byte[] remainingCards = new byte[cards.Length];
            Array.Copy(cards, remainingCards, cards.Length);
            remainingCards[fourIndex]--;
            remainingCards[fiveIndex]--;
            remainingCards[sixIndex]--;
            
            // 检查剩余牌是否能胡（使用已有的胡牌判断逻辑，赖子索引255表示无赖子）
            if (MahjongHuTingCheck.Instance.CheckCanHu(remainingCards, 255))
            {
                GF.LogInfo_gsc($"[卡五星判定] 成功: 提出456后剩余牌可以胡");
                return true;
            }
            else
            {
                GF.LogInfo_gsc($"[卡五星判定] 失败: 提出456后剩余牌无法胡");
                return false;
            }
        }
        
        /// <summary>
        /// 计算明四归数量
        /// 规则：碰了3张 + 胡第4张 = 明四归（不需要清一色）
        /// 注意：meldCards用的是服务器牌值，huCardIndex用的是枚举索引
        /// 由于明四归只能是碰+胡，所以最多只能有1个明四归
        /// </summary>
        /// <param name="meldCards">副露区的牌列表（服务器牌值：1-9万, 11-19筒, 21-29条）</param>
        /// <param name="cards">手牌数组（已包含胡牌，枚举索引）</param>
        /// <param name="huCardIndex">胡牌索引（枚举索引：0-8万, 9-17筒, 18-26条）</param>
        private int CountMingSiGui(List<int> meldCards, byte[] cards, int huCardIndex)
        {
            if (meldCards == null || meldCards.Count == 0)
                return 0;
            
            // 统计副露区每张牌的数量（服务器牌值）
            Dictionary<int, int> meldCardCount = new Dictionary<int, int>();
            foreach (int cardValue in meldCards)
            {
                if (!meldCardCount.ContainsKey(cardValue))
                    meldCardCount[cardValue] = 0;
                meldCardCount[cardValue]++;
            }
            
            // 把胡牌枚举索引转换为服务器牌值
            int huCardServerValue = ConvertEnumIndexToServerValue(huCardIndex);
            
            // 检查是否有明四归
            // 明四归只有一种情况：副露区碰了3张 + 胡的是第4张
            // 副露区的明杠（4张）不算明四归，手里有第4张也不算
            // 由于明四归必须胡第4张，所以最多只有1个
            foreach (var kvp in meldCardCount)
            {
                int serverValue = kvp.Key;
                int meldCount = kvp.Value;
                
                // 只检查碰牌（3张），明杠（4张）不算
                if (meldCount == 3)
                {
                    // 检查胡的是不是第4张（比较服务器牌值）
                    if (huCardServerValue == serverValue)
                    {
                        // 碰3张 + 胡第4张 = 明四归
                        return 1;
                    }
                }
            }
            
            return 0;
        }
        
        /// <summary>
        /// 服务器牌值转换为枚举索引
        /// 服务器：1-9万, 11-19筒, 21-29条, 31-34风, 41-43中发白
        /// 枚举：0-8万, 9-17筒, 18-26条, 27-30风, 31-33中发白
        /// </summary>
        private int ConvertServerValueToEnumIndex(int serverValue)
        {
            if (serverValue >= 1 && serverValue <= 9)
                return serverValue - 1;           // 万：1-9 -> 0-8
            else if (serverValue >= 11 && serverValue <= 19)
                return serverValue - 11 + 9;      // 筒：11-19 -> 9-17
            else if (serverValue >= 21 && serverValue <= 29)
                return serverValue - 21 + 18;     // 条：21-29 -> 18-26
            else if (serverValue >= 31 && serverValue <= 34)
                return serverValue - 31 + 27;     // 风：31-34 -> 27-30
            else if (serverValue >= 41 && serverValue <= 43)
                return serverValue - 41 + 31;     // 中发白：41-43 -> 31-33
            return -1;
        }
        
        /// <summary>
        /// 枚举索引转换为服务器牌值
        /// 枚举：0-8万, 9-17筒, 18-26条, 27-30风, 31-33中发白
        /// 服务器：1-9万, 11-19筒, 21-29条, 31-34风, 41-43中发白
        /// </summary>
        private int ConvertEnumIndexToServerValue(int enumIndex)
        {
            if (enumIndex >= 0 && enumIndex <= 8)
                return enumIndex + 1;             // 万：0-8 -> 1-9
            else if (enumIndex >= 9 && enumIndex <= 17)
                return enumIndex - 9 + 11;        // 筒：9-17 -> 11-19
            else if (enumIndex >= 18 && enumIndex <= 26)
                return enumIndex - 18 + 21;       // 条：18-26 -> 21-29
            else if (enumIndex >= 27 && enumIndex <= 30)
                return enumIndex - 27 + 31;       // 风：27-30 -> 31-34
            else if (enumIndex >= 31 && enumIndex <= 33)
                return enumIndex - 31 + 41;       // 中发白：31-33 -> 41-43
            return -1;
        }
        
        /// <summary>
        /// 判断是否有暗四归
        /// 规则：
        /// 1. 手上3张 + 胡第4张 = 暗四归（不需要其他条件）
        /// 2. 手上本来就有4张 = 全频道开启直接算，否则需要清一色才算暗四归
        /// </summary>
        /// <param name="cards">手牌数组（已包含胡牌）</param>
        /// <param name="huCardIndex">胡牌索引</param>
        /// <param name="quanPinDao">是否开启全频道</param>
        /// <param name="isQingYiSe">是否清一色</param>
        private int CountAnSiGui(byte[] cards, int huCardIndex, bool quanPinDao, bool isQingYiSe)
        {
            int count = 0;
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] >= 4)
                {
                    // 手牌有4张一样的
                    if (huCardIndex == i)
                    {
                        // 胡的就是第4张（手上原本3张 + 胡第4张）= 暗四归（不需要其他条件）
                        count++;
                    }
                    else
                    {
                        // 手上本来就有4张（不是胡的牌）
                        // 全频道开启直接算暗四归，否则需要清一色
                        if (quanPinDao || isQingYiSe)
                            count++;
                    }
                }
            }
            
            return count;
        }
        
        /// <summary>
        /// 判断是否小三元（中发白三种箭牌中有2种是刻子，1种是对子）
        /// </summary>
        private bool HasXiaoSanYuan(byte[] cards, List<int> meldCards)
        {
            // 中发白的枚举索引：31=中，32=发，33=白
            // 服务器牌值：41=中，42=发，43=白
            if (cards.Length < 34) return false;
            
            // 统计中发白的数量（手牌）
            int zhong = cards[31];
            int fa = cards[32];
            int bai = cards[33];
            
            // 加上副露区的牌（副露区用的是服务器牌值）
            if (meldCards != null)
            {
                foreach (int serverValue in meldCards)
                {
                    if (serverValue == 41) zhong++;       // 中
                    else if (serverValue == 42) fa++;     // 发
                    else if (serverValue == 43) bai++;    // 白
                }
            }
            
            int keziCount = 0;
            int duiziCount = 0;
            
            if (zhong >= 3) keziCount++;
            else if (zhong == 2) duiziCount++;
            
            if (fa >= 3) keziCount++;
            else if (fa == 2) duiziCount++;
            
            if (bai >= 3) keziCount++;
            else if (bai == 2) duiziCount++;
            
            // 小三元：2个刻子 + 1个对子
            return keziCount == 2 && duiziCount == 1;
        }
        
        /// <summary>
        /// 判断是否大三元（中发白都是刻子）
        /// </summary>
        private bool HasDaSanYuan(byte[] cards, List<int> meldCards)
        {
            // 中发白的枚举索引：31=中，32=发，33=白
            // 服务器牌值：41=中，42=发，43=白
            if (cards.Length < 34) return false;
            
            // 统计中发白的数量（手牌）
            int zhong = cards[31];
            int fa = cards[32];
            int bai = cards[33];
            
            // 加上副露区的牌（副露区用的是服务器牌值）
            if (meldCards != null)
            {
                foreach (int serverValue in meldCards)
                {
                    if (serverValue == 41) zhong++;       // 中
                    else if (serverValue == 42) fa++;     // 发
                    else if (serverValue == 43) bai++;    // 白
                }
            }
            
            // 大三元：中发白都是刻子
            return zhong >= 3 && fa >= 3 && bai >= 3;
        }
        
        /// <summary>
        /// 判断是否手抓一（抓到的牌直接胡，不需要打出）
        /// </summary>
        private bool IsShouZhuaYi(byte[] cards)
        {
            // 这个需要根据游戏状态判断，这里简化处理
            // 实际应该在胡牌时传入额外信息
            return false;
        }
        
        /// <summary>
        /// 判断是否清一色（只有一种花色的数牌，不能有风字牌或中发白）
        /// 枚举索引：万=0-8, 筒=9-17, 条=18-26, 风字=27-30, 中发白=31-33
        /// 服务器牌值：万=1-9, 筒=11-19, 条=21-29, 风=31-34, 中发白=41-43
        /// </summary>
        private bool IsQingYiSe(byte[] cards, List<int> meldCards)
        {
            bool hasWan = false;
            bool hasTong = false;
            bool hasTiao = false;
            bool hasZiPai = false;  // 风字牌或中发白
            
            // 检查手牌中的万（枚举索引0-8）
            for (int i = 0; i < 9 && i < cards.Length; i++)
            {
                if (cards[i] > 0)
                {
                    hasWan = true;
                    break;
                }
            }
            
            // 检查手牌中的筒（枚举索引9-17）
            for (int i = 9; i < 18 && i < cards.Length; i++)
            {
                if (cards[i] > 0)
                {
                    hasTong = true;
                    break;
                }
            }
            
            // 检查手牌中的条（枚举索引18-26）
            for (int i = 18; i < 27 && i < cards.Length; i++)
            {
                if (cards[i] > 0)
                {
                    hasTiao = true;
                    break;
                }
            }
            
            // 检查手牌中的风字牌和中发白（枚举索引27-33）
            for (int i = 27; i < 34 && i < cards.Length; i++)
            {
                if (cards[i] > 0)
                {
                    hasZiPai = true;
                    break;
                }
            }
            
            // 检查副露区的牌（副露区用的是服务器牌值：1-9万, 11-19筒, 21-29条, 31-34风, 41-43中发白）
            if (meldCards != null)
            {
                foreach (int serverValue in meldCards)
                {
                    if (serverValue >= 1 && serverValue <= 9)
                    {
                        hasWan = true;
                    }
                    else if (serverValue >= 11 && serverValue <= 19)
                    {
                        hasTong = true;
                    }
                    else if (serverValue >= 21 && serverValue <= 29)
                    {
                        hasTiao = true;
                    }
                    else if ((serverValue >= 31 && serverValue <= 34) || (serverValue >= 41 && serverValue <= 43))
                    {
                        // 风字牌（31-34）或中发白（41-43）
                        hasZiPai = true;
                    }
                }
            }
            
            // 如果有字牌（风字或中发白），则不是清一色
            if (hasZiPai)
            {
                GF.LogInfo_gsc($"[清一色判定] 有字牌，不是清一色");
                return false;
            }
            
            // 只有一种花色才是清一色
            int colorCount = (hasWan ? 1 : 0) + (hasTong ? 1 : 0) + (hasTiao ? 1 : 0);
            GF.LogInfo_gsc($"[清一色判定] 万={hasWan}, 筒={hasTong}, 条={hasTiao}, 花色数={colorCount}");
            return colorCount == 1;
        }
    }

    /// <summary>
    /// 血流麻将番数计算器
    /// 复用 XueLiuChengHeRule 中的番数计算逻辑
    /// </summary>
    public class XueLiuFanCalculator
    {
        private NetMsg.XL_Config xlConfig;
        
        public XueLiuFanCalculator(NetMsg.XL_Config config)
        {
            this.xlConfig = config;
        }
        
        /// <summary>
        /// 计算血流麻将番数
        /// </summary>
        /// <param name="cards">手牌数组（已包含胡牌）</param>
        /// <param name="meldCards">副露区的牌（可能包含甩牌+碰杠的牌）</param>
        /// <param name="huCardIndex">胡牌索引</param>
        public int CalculateFan(byte[] cards, List<int> meldCards, int huCardIndex = -1)
        {
            if (xlConfig == null)
                return 0; // 默认屁胡
            
            // 处理甩牌逻辑：如果开启了甩牌（check包含3），副露区前3张是甩掉的牌，不参与番数计算
            List<int> validMeldCards = meldCards;
            bool hasShuaiPai = xlConfig.Check != null && xlConfig.Check.Contains(3);
            if (hasShuaiPai && meldCards != null && meldCards.Count > 3)
            {
                // 跳过前3张甩牌
                validMeldCards = meldCards.Skip(3).ToList();
                GF.LogInfo_gsc($"[血流番数计算] 检测到甩牌配置，排除前3张甩牌: [{string.Join(",", meldCards.Take(3))}]");
            }
            
            // 打印手牌信息
            System.Text.StringBuilder cardInfo = new System.Text.StringBuilder();
            cardInfo.Append("[血流番数计算] 手牌: ");
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] > 0)
                {
                    cardInfo.Append($"[{i}]={cards[i]} ");
                }
            }
            if (validMeldCards != null && validMeldCards.Count > 0)
            {
                cardInfo.Append($", 有效副露=[{string.Join(",", validMeldCards)}]");
            }
            GF.LogInfo_gsc(cardInfo.ToString());
            
            // 判断牌型（使用有效的副露牌）
            bool isQingYiSe = IsQingYiSe(cards, validMeldCards);
            bool isPengPengHu = IsPengPengHu(cards, validMeldCards);
            
            GF.LogInfo_gsc($"[血流番数计算] 牌型判定: 清一色={isQingYiSe}, 蹦蹦胡={isPengPengHu}");
            
            // 根据牌型确定胡牌类型
            int huType;
            if (isQingYiSe && isPengPengHu)
            {
                huType = 2; // 清一色+蹦蹦胡
            }
            else if (isQingYiSe || isPengPengHu)
            {
                huType = 1; // 清一色或蹦蹦胡
            }
            else
            {
                huType = 0; // 屁胡
            }
            
            // 复用 XueLiuChengHeRule 中的番数计算逻辑（直接计算，避免创建对象）
            int totalFan = CalculateFanScoreByConfig(xlConfig.Fan, huType);
            
            // 打印番数计算结果
            GF.LogInfo_gsc($"[血流番数计算] 番数配置={xlConfig.Fan}, 清一色={isQingYiSe}, 蹦蹦胡={isPengPengHu}, 胡牌类型={huType}, 最终番数={totalFan}");
            
            return totalFan;
        }
        
        /// <summary>
        /// 根据番数配置和胡牌类型计算番数（复用 XueLiuChengHeRule.CalculateFanScore 的逻辑）
        /// fan配置: 1:2,4,6 2:3,6,9 3:4,8,12
        /// 胡牌类型: 0-屁胡，1-清一色或蹦蹦胡，2-清一色+蹦蹦胡
        /// </summary>
        private int CalculateFanScoreByConfig(int fan, int huType)
        {
            // 根据fan配置获取对应的番数数组
            int[] fanValues;
            switch (fan)
            {
                case 1:
                    fanValues = new int[] { 2, 4, 6 }; // 屁胡2, 清一色/蹦蹦胡4, 清一色+蹦蹦胡6
                    break;
                case 2:
                    fanValues = new int[] { 3, 6, 9 }; // 屁胡3, 清一色/蹦蹦胡6, 清一色+蹦蹦胡9
                    break;
                case 3:
                    fanValues = new int[] { 4, 8, 12 }; // 屁胡4, 清一色/蹦蹦胡8, 清一色+蹦蹦胡12
                    break;
                default:
                    fanValues = new int[] { 2, 4, 6 }; // 默认使用配置1
                    break;
            }

            // 限制huType在有效范围内
            huType = System.Math.Max(0, System.Math.Min(huType, 2));
            return fanValues[huType];
        }
        
        /// <summary>
        /// 判断是否清一色（只有一种花色的数牌）
        /// 血流麻将只有万条筒，无风字牌
        /// </summary>
        private bool IsQingYiSe(byte[] cards, List<int> meldCards)
        {
            bool hasWan = false;
            bool hasTong = false;
            bool hasTiao = false;
            
            // 检查手牌中的万（枚举索引0-8）
            for (int i = 0; i < 9 && i < cards.Length; i++)
            {
                if (cards[i] > 0)
                {
                    hasWan = true;
                    break;
                }
            }
            
            // 检查手牌中的筒（枚举索引9-17）
            for (int i = 9; i < 18 && i < cards.Length; i++)
            {
                if (cards[i] > 0)
                {
                    hasTong = true;
                    break;
                }
            }
            
            // 检查手牌中的条（枚举索引18-26）
            for (int i = 18; i < 27 && i < cards.Length; i++)
            {
                if (cards[i] > 0)
                {
                    hasTiao = true;
                    break;
                }
            }
            
            // 检查副露区的牌（副露区用的是服务器牌值：1-9万, 11-19筒, 21-29条）
            if (meldCards != null)
            {
                foreach (int serverValue in meldCards)
                {
                    if (serverValue >= 1 && serverValue <= 9)
                    {
                        hasWan = true;
                    }
                    else if (serverValue >= 11 && serverValue <= 19)
                    {
                        hasTong = true;
                    }
                    else if (serverValue >= 21 && serverValue <= 29)
                    {
                        hasTiao = true;
                    }
                }
            }
            
            // 只有一种花色才是清一色
            int colorCount = (hasWan ? 1 : 0) + (hasTong ? 1 : 0) + (hasTiao ? 1 : 0);
            return colorCount == 1;
        }
        
        /// <summary>
        /// 判断是否蹦蹦胡（碰碰胡）
        /// 蹦蹦胡：手牌全是刻子（3张相同）或杠（4张相同）+ 一个对子，没有顺子
        /// </summary>
        private bool IsPengPengHu(byte[] cards, List<int> meldCards)
        {
            // 统计手牌和副露区所有牌的数量
            int[] allCardCounts = new int[34];
            
            // 统计手牌
            for (int i = 0; i < cards.Length && i < 34; i++)
            {
                allCardCounts[i] = cards[i];
            }
            
            // 统计副露区的牌（服务器牌值转枚举索引）
            if (meldCards != null)
            {
                foreach (int serverValue in meldCards)
                {
                    int enumIndex = ConvertServerValueToEnumIndex(serverValue);
                    if (enumIndex >= 0 && enumIndex < 34)
                    {
                        allCardCounts[enumIndex]++;
                    }
                }
            }
            
            // 蹦蹦胡判定：每种牌的数量必须是0, 2, 3或4
            // 且只能有一种牌的数量是2（对子）
            bool hasDuizi = false;
            
            for (int i = 0; i < 34; i++)
            {
                int count = allCardCounts[i];
                if (count == 0)
                    continue;
                
                if (count == 1)
                    return false; // 有单张，不是蹦蹦胡
                
                if (count == 2)
                {
                    if (hasDuizi)
                        return false; // 已经有一个对子了，又出现一个，不是蹦蹦胡
                    hasDuizi = true;
                }
                // count == 3 或 count == 4 是刻子或杠，符合蹦蹦胡
            }
            
            // 必须有且只有一个对子
            return hasDuizi;
        }
        
        /// <summary>
        /// 服务器牌值转换为枚举索引
        /// 服务器：1-9万, 11-19筒, 21-29条
        /// 枚举：0-8万, 9-17筒, 18-26条
        /// </summary>
        private int ConvertServerValueToEnumIndex(int serverValue)
        {
            if (serverValue >= 1 && serverValue <= 9)
                return serverValue - 1;           // 万：1-9 -> 0-8
            else if (serverValue >= 11 && serverValue <= 19)
                return serverValue - 11 + 9;      // 筒：11-19 -> 9-17
            else if (serverValue >= 21 && serverValue <= 29)
                return serverValue - 21 + 18;     // 条：21-29 -> 18-26
            return -1;
        }
    }