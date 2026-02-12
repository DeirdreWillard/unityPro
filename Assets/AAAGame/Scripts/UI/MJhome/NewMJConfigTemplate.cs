using System.Collections.Generic;
using NetMsg;

/// <summary>
/// 新麻将玩法配置模板
/// 当需要添加新的麻将玩法时，可以参考此模板
/// 
/// 添加新麻将玩法的标准流程：
/// 1. 继承BaseMJConfig创建新的配置类
/// 2. 在ConfigArrays中添加该玩法特有的配置数组
/// 3. 在MJConfigManager的InitializeConfigTypes中注册新玩法
/// 4. 在CreatMjPopup中添加对应的UI处理逻辑
/// 5. 定义protobuf配置结构并实现ToProtobufConfig方法
/// 6. 在MJConfigManager的GenerateMJConfig中添加新玩法的处理分支
/// </summary>
public partial class MJConfigManager
{
    /// <summary>
    /// 新麻将玩法配置模板
    /// 复制此类并修改为实际的玩法名称和配置
    /// </summary>
    public class NewMJConfigTemplate : BaseMJConfig
    {
        public override string ConfigName => "新麻将玩法"; // 修改为实际玩法名称

        #region 玩法特有配置属性
        // 示例：根据实际玩法需求添加配置属性
        public int ExampleConfig1 { get; set; } = 1; // 示例配置1
        public int ExampleConfig2 { get; set; } = 0; // 示例配置2
        public List<int> ExampleConfigList { get; set; } = new List<int>(); // 示例列表配置
        // 添加更多配置属性...
        #endregion

        #region 必须实现的基类方法
        public override NetMsg.MJMethod GetMJMethod()
        {
            // 返回对应的协议枚举值
            return NetMsg.MJMethod.Dazhong; // 修改为实际的枚举值
        }

        public override bool ValidateConfig()
        {
            // 验证配置的有效性
            if (PeopleNum < 2 || PeopleNum > 4) return false;
            if (PlayNum <= 0) return false;
            
            // 添加玩法特有的验证逻辑
            if (ExampleConfig1 < 1 || ExampleConfig1 > 3) return false;
            // 添加更多验证...
            
            return true;
        }

        public override void ApplyDefaultValues()
        {
            // 设置默认值
            PeopleNum = 4; // 根据玩法设置合适的默认人数
            PlayNum = 8; // 根据玩法设置合适的默认局数
            BaseCoin = 1.0f; // 设置默认底分
            
            // 设置玩法特有配置的默认值
            ExampleConfig1 = 1;
            ExampleConfig2 = 0;
            ExampleConfigList.Clear();
            // 设置更多默认值...
        }
        #endregion

        #region 可选实现的方法
        /// <summary>
        /// 转换为protobuf配置（如果需要网络传输）
        /// </summary>
        public object ToProtobufConfig()
        {
            // 创建对应的protobuf配置对象
            // 示例：
            /*
            var config = new NetMsg.NewMJ_Config();
            config.ExampleConfig1 = ExampleConfig1;
            config.ExampleConfig2 = ExampleConfig2;
            config.ExampleConfigList.AddRange(ExampleConfigList);
            return config;
            */
            
            // 如果暂时不需要protobuf配置，返回null
            return null;
        }

        /// <summary>
        /// 获取配置摘要（可选）
        /// </summary>
        public string GetConfigDetail()
        {
            var summary = $"玩法: {ConfigName}\n";
            summary += $"人数: {PeopleNum}, 局数: {PlayNum}\n";
            summary += $"示例配置1: {ExampleConfig1}\n";
            summary += $"示例配置2: {ExampleConfig2}\n";
            // 添加更多摘要信息...
            return summary;
        }
        #endregion
    }
}

/*
添加新麻将玩法的详细步骤：

=== 第一步：创建配置类 ===
1. 复制NewMJConfigTemplate类
2. 重命名为实际玩法名称（如SichuanMJConfigData）
3. 修改ConfigName属性
4. 添加玩法特有的配置属性
5. 实现GetMJMethod、ValidateConfig、ApplyDefaultValues方法

=== 第二步：扩展ConfigArrays ===
1. 在ConfigArrays.cs中添加新玩法特有的配置数组
2. 在GetConfigArray方法中添加新玩法的配置处理
3. 在GetConfigurableKeys方法中添加新玩法的配置键

=== 第三步：注册到MJConfigManager ===
1. 在MJConfigManager的InitializeConfigTypes方法中注册新玩法
2. 在GenerateMJConfig方法中添加新玩法的处理分支

=== 第四步：UI集成 ===
1. 在CreatMjPopup.cs中的toggleNames数组添加新玩法名称
2. 如果有特殊UI需求，在SetDefaultUIState中添加处理
3. 在CreatMjPoPConfig.cs中添加新玩法的默认配置方法（GetXXXDefaultConfig）

=== 第五步：协议支持（如果需要） ===
1. 定义对应的protobuf配置结构
2. 在NetMsg中添加对应的MJMethod枚举值
3. 实现ToProtobufConfig方法

=== 第六步：测试验证 ===
1. 测试配置的创建、修改、验证
2. 测试UI交互
3. 测试配置的保存和加载
4. 测试网络传输（如果有）

通过以上标准流程，可以确保新麻将玩法的添加过程清晰、规范，且不会影响现有功能。
*/
