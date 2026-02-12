using NetMsg;
using System.Text;
using System.Linq;

/// <summary>
/// 麻将规则工厂 - 根据玩法类型创建对应的规则实例
/// </summary>
public static class MahjongRuleFactory
{
    /// <summary>
    /// 创建麻将规则实例
    /// </summary>
    /// <param name="mjMethod">麻将玩法类型</param>
    /// <returns>对应的规则实例</returns>
    public static DefaultMahjongRule CreateRule(MJMethod mjMethod)
    {
        switch (mjMethod)
        {
            case MJMethod.Dazhong:
                return new DazhongMahjongRule();

            case MJMethod.Huanghuang:
                return new XianTaoHuangHuangRule();

            case MJMethod.Kwx:
                return new KaWuXingRule();

            case MJMethod.Xl:
                return new XueLiuChengHeRule();

            case MJMethod.Xz:
                return new XueZhanDaoDiRule();

            default:
                GF.LogInfo_gsc("未知麻将玩法，使用默认大众麻将", mjMethod.ToString());
                return new DazhongMahjongRule();
        }
    }

    /// <summary>
    /// 获取玩法名称
    /// </summary>
    /// <param name="mjMethod">麻将玩法类型</param>
    /// <returns>玩法名称</returns>
    public static string GetRuleName(MJMethod mjMethod)
    {
        switch (mjMethod)
        {
            case MJMethod.Dazhong:
                return "大众麻将";

            case MJMethod.Huanghuang:
                return "仙桃晃晃";

            case MJMethod.Kwx:
                return "卡五星";

            case MJMethod.Xl:
                return "血流成河";

            case MJMethod.Xz:
                return "血战到底";

            default:
                return "未知玩法";
        }
    }

    /// <summary>
    /// 根据进入桌子数据动态生成当前房间（规则）描述，供房间信息面板展示。
    /// </summary>
    public static string BuildRoomDescription(Desk_BaseInfo baseInfo)
    {
        try
        {
            if (baseInfo?.MjConfig == null)
            {
                return "";
            }

            var mjMethod = baseInfo.MjConfig.MjMethod;
            switch (mjMethod)
            {
                case MJMethod.Huanghuang:
                    return BuildXianTaoHuangHuangDesc(baseInfo);
                case MJMethod.Kwx:
                    return BuildKaWuXingDesc(baseInfo);
                case MJMethod.Xl:
                    return BuildXueLiuChengHeDesc(baseInfo);
                case MJMethod.Xz:
                    return BuildXueZhanDaoDiDesc(baseInfo);
                case MJMethod.Dazhong:
                    return BuildDazhongDesc(baseInfo);
                default:
                    return BuildDazhongDesc(baseInfo);
            }
        }
        catch (System.Exception ex)
        {
            GF.LogError("房间描述生成异常", ex.Message);
            return BuildDazhongDesc(baseInfo);
        }
    }

    private static string BuildDazhongDesc(Desk_BaseInfo baseInfo)
    {
        // 大众麻将暂时使用简略描述
        var sb = new StringBuilder();
        var baseCfg = baseInfo.BaseConfig;

        sb.Append($"{baseCfg.PlayerNum}人  底分:{baseCfg.BaseCoin}");

        // IP/GPS限制
        if (baseCfg.IpLimit && baseCfg.GpsLimit)
        {
            Append(sb, "IP/GPS限制");
        }
        else if (baseCfg.IpLimit)
        {
            Append(sb, "IP限制");
        }
        else if (baseCfg.GpsLimit)
        {
            Append(sb, "GPS限制");
        }
        Append(sb, "1分钟自动出牌");

        return sb.ToString();
    }

    /// <summary>
    /// 生成卡五星规则描述
    /// 顺序：人数 → 封顶 → 买马 → 选漂 → 底分 → 换三张
    /// </summary>
    private static string BuildKaWuXingDesc(Desk_BaseInfo baseInfo)
    {
        var sb = new StringBuilder();
        var baseCfg = baseInfo.BaseConfig;
        var kwxCfg = baseInfo.MjConfig.Kwx;

        // 1. 人数
        sb.Append($"{baseCfg.PlayerNum}人");

        if (kwxCfg != null)
        {
            // 2. 封顶
            if (kwxCfg.TopOut > 0)
                Append(sb, $"封顶{kwxCfg.TopOut}番");
            else
                Append(sb, "不封顶");

            // 3. 买马
            string buyHorseStr = kwxCfg.BuyHorse switch
            {
                1 => "亮倒自摸买马",
                2 => "自摸买马",
                _ => ""
            };
            if (!string.IsNullOrEmpty(buyHorseStr))
                Append(sb, buyHorseStr);

            // 4. 选漂
            string piaoStr = kwxCfg.ChoicePiao switch
            {
                1 => "每局选漂",
                2 => "首局定漂",
                3 => "不漂",
                4 => "固定飘1",
                5 => "固定飘2",
                6 => "固定飘3",
                _ => ""
            };
            if (!string.IsNullOrEmpty(piaoStr))
                Append(sb, piaoStr);

            // 5. 底分
            Append(sb, $"底分:{baseCfg.BaseCoin}分");

            // 6. 换三张（仅显示换三张，不显示牌型）
            if (kwxCfg.Play != null && kwxCfg.Play.Contains(14))
            {
                Append(sb, "换三张");
            }
        }
        else
        {
            Append(sb, $"底分:{baseCfg.BaseCoin}分");
        }

        // 7. IP/GPS限制
        if (baseCfg.IpLimit && baseCfg.GpsLimit)
        {
            Append(sb, "IP/GPS限制");
        }
        else if (baseCfg.IpLimit)
        {
            Append(sb, "IP限制");
        }
        else if (baseCfg.GpsLimit)
        {
            Append(sb, "GPS限制");
        }
        Append(sb, "1分钟自动出牌");

        return sb.ToString();
    }

    /// <summary>
    /// 生成血流成河规则描述
    /// </summary>
    private static string BuildXueLiuChengHeDesc(Desk_BaseInfo baseInfo)
    {
        var sb = new StringBuilder();
        var baseCfg = baseInfo.BaseConfig;
        var cfg = baseInfo.MjConfig.XlConfig; // 血流配置

        sb.Append($"{baseCfg.PlayerNum}人");

        // 番数配置
        string fanDesc = cfg.Fan switch
        {
            1 => "2,4,6番",
            2 => "3,6,9番",
            3 => "4,8,12番",
            _ => "默认番"
        };
        Append(sb, fanDesc);

        // 飘配置
        string piaoDesc = cfg.Piao switch
        {
            0 => "自由飘",
            1 => "定漂1",
            2 => "定漂2",
            3 => "定漂3",
            4 => "定漂4",
            5 => "定漂5",
            6 => "不飘",
            _ => "不飘"
        };
        Append(sb, piaoDesc);

        // 换三张
        if (cfg.ChangeCard == 0)
        {
            Append(sb, "换三张");
        }
        else
        {
            Append(sb, "不换牌");
        }

        // 特殊规则
        if (cfg.Check.Contains(1)) Append(sb, "过手碰");
        if (cfg.Check.Contains(2)) Append(sb, "胡后可点杠");
        if (cfg.Check.Contains(3)) Append(sb, "甩3张");
        if (cfg.Check.Contains(4)) Append(sb, "接炮算门清");

        Append(sb, $"底分:{baseCfg.BaseCoin}分");

        // IP/GPS限制
        if (baseCfg.IpLimit && baseCfg.GpsLimit)
        {
            Append(sb, "IP/GPS限制");
        }
        else if (baseCfg.IpLimit)
        {
            Append(sb, "IP限制");
        }
        else if (baseCfg.GpsLimit)
        {
            Append(sb, "GPS限制");
        }
        Append(sb, "1分钟自动出牌");

        return sb.ToString();
    }

    /// <summary>
    /// 生成血战到底规则描述
    /// </summary>
    private static string BuildXueZhanDaoDiDesc(Desk_BaseInfo baseInfo)
    {
        var sb = new StringBuilder();
        var baseCfg = baseInfo.BaseConfig;
        // TODO: 等服务器添加 XZ_Config 后读取配置
        sb.Append($"{baseCfg.PlayerNum}人");
        Append(sb, "换三张");
        Append(sb, "必须定缺");
        Append(sb, "可胡多次");
        Append(sb, "血战到底");
        Append(sb, $"底分:{baseCfg.BaseCoin}分");

        // IP/GPS限制
        if (baseCfg.IpLimit && baseCfg.GpsLimit)
        {
            Append(sb, "IP/GPS限制");
        }
        else if (baseCfg.IpLimit)
        {
            Append(sb, "IP限制");
        }
        else if (baseCfg.GpsLimit)
        {
            Append(sb, "GPS限制");
        }
        Append(sb, "1分钟自动出牌");

        return sb.ToString();
    }

    /// <summary>
    /// 生成仙桃晃晃详细规则描述
    /// 示例: 三人两门、一赖到底、飘赖有奖、不锁牌、杠开翻倍、捉铳可胡、底分:0.5分、不封顶、不托管
    /// </summary>
    private static string BuildXianTaoHuangHuangDesc(Desk_BaseInfo baseInfo)
    {
        var sb = new StringBuilder();
        var baseCfg = baseInfo.BaseConfig;
        var cfg = baseInfo.MjConfig.Xthh; // protobuf配置
        // 1. 人数 & 门数
        int pNum = baseCfg.PlayerNum;
        string men = pNum == 4 ? "三门" : "两门"; // 4人三门，2/3人两门
        sb.Append($"{pNum}人{men}");
        Append(sb, "1分钟自动出牌");

        // 2. 赖子玩法
        if (cfg != null)
        {
            string laiPlayStr = cfg.LaiPlay switch
            {
                1 => "一赖到底",
                2 => "土豪必掷",
                _ => "一赖到底"
            };
            Append(sb, laiPlayStr);

            // 3. 飘癞
            string piaoStr = cfg.PiaoLai switch
            {
                1 => "飘赖有奖",
                2 => "飘赖无奖",
                _ => "飘赖有奖"
            };
            Append(sb, piaoStr);

            // 4. 锁牌
            string lockStr = cfg.LockCard switch
            {
                1 => "不锁牌",
                2 => "2赖锁牌",
                3 => "3赖锁牌",
                4 => "4赖锁牌",
                _ => "不锁牌"
            };
            Append(sb, lockStr);

            // 5. 杠开
            string gangKaiStr = cfg.GangKai switch
            {
                1 => "杠开翻倍",
                2 => "杠开不翻倍",
                _ => "杠开翻倍"
            };
            Append(sb, gangKaiStr);

            // 6. 捉铳
            string zhuoStr = cfg.Zhuo switch
            {
                1 => "捉铳可胡",
                2 => "捉铳不可胡",
                _ => "捉铳可胡"
            };
            Append(sb, zhuoStr);

            // 7. 复选扩展（仅在有值时追加） 1:杠不随飘 2：4赖胡牌不加倍 3：黑夹黑
            if (cfg.Check != null && cfg.Check.Count > 0)
            {
                foreach (var opt in cfg.Check)
                {
                    switch (opt)
                    {
                        case 1: Append(sb, "杠不随飘"); break;
                        case 2: Append(sb, "4赖胡不加倍"); break;
                        case 3: Append(sb, "黑夹黑"); break;
                        case 4: Append(sb, "258翻倍"); break;
                    }
                }
            }

            // 8. 底分
            Append(sb, $"底分:{baseCfg.BaseCoin}分");

            // 9. 封顶倍数
            Append(sb, cfg.MaxTime > 0 ? $"封顶{cfg.MaxTime}倍" : "不封顶");

            // 10. 托管（proto里: Tuoguan） 0:不托管 其他:托管X秒 (推测)
            // string tuoStr = cfg.Tuoguan == 0 ? "不托管" : $"托管{cfg.Tuoguan}";
            // Append(sb, tuoStr);
        }
        else
        {
            Append(sb, $"底分:{baseCfg.BaseCoin}");
        }

        // 11. IP/GPS限制
        if (baseCfg.IpLimit && baseCfg.GpsLimit)
        {
            Append(sb, "IP/GPS限制");
        }
        else if (baseCfg.IpLimit)
        {
            Append(sb, "IP限制");
        }
        else if (baseCfg.GpsLimit)
        {
            Append(sb, "GPS限制");
        }
        Append(sb, "1分钟自动出牌");
        return sb.ToString();
    }

    private static void Append(StringBuilder sb, string seg)
    {
        if (string.IsNullOrEmpty(seg)) return;
        if (sb.Length > 0) sb.Append('、');
        sb.Append(seg);
    }
}
