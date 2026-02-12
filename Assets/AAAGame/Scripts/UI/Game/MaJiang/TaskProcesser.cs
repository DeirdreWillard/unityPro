using System.Reflection;

namespace GameTask
{
    /// <summary>
    /// 任务处理器抽象基类
    /// </summary>
    public abstract class TaskProcesser
    {
        /// <summary>
        /// 启动任务处理器
        /// </summary>
        /// <returns>启动是否成功</returns>
        public abstract bool Start();
        
        /// <summary>
        /// 停止任务处理器
        /// </summary>
        public abstract void Stop();
        
        /// <summary>
        /// 暂停任务处理器
        /// </summary>
        public abstract void Pause();
        
        /// <summary>
        /// 继续执行任务处理器
        /// </summary>
        public abstract void Continue();
        
        /// <summary>
        /// 发布任务节点到任务处理器
        /// </summary>
        /// <param name="taskNode">任务节点</param>
        /// <param name="delay">延迟时间（毫秒）</param>
        /// <returns>操作结果，0表示成功</returns>
        public abstract int PostTask(TaskNode taskNode, int delay = 0);
        
        /// <summary>
        /// 发布任务到任务处理器（只有处理回调）
        /// </summary>
        /// <param name="processDataCallBack">处理数据回调</param>
        /// <param name="taskData">任务数据</param>
        /// <param name="msg">任务消息类型</param>
        /// <param name="delay">延迟时间（毫秒）</param>
        /// <returns>操作结果，0表示成功</returns>
        public abstract int PostTask(TaskCallBack processDataCallBack, TaskDataBase taskData, TaskMsg msg = TaskMsg.TMSG_DATA, int delay = 0);
        
        /// <summary>
        /// 发布任务到任务处理器（包含处理和释放回调）
        /// </summary>
        /// <param name="processDataCallBack">处理数据回调</param>
        /// <param name="releaseDataCallBack">释放数据回调</param>
        /// <param name="taskData">任务数据</param>
        /// <param name="msg">任务消息类型</param>
        /// <param name="delay">延迟时间（毫秒）</param>
        /// <returns>操作结果，0表示成功</returns>
        public abstract int PostTask(TaskCallBack processDataCallBack, TaskCallBack releaseDataCallBack, TaskDataBase taskData, TaskMsg msg = TaskMsg.TMSG_DATA, int delay = 0);

        /// <summary>
        /// 清空所有任务（可选实现）
        /// </summary>
        public virtual void ClearAllTasks() { }
    }


    /// <summary>
    /// 通用任务处理器，TaskProcesser的具体实现
    /// </summary>
    public class CommonTaskProcesser: TaskProcesser
    {
        /// <summary>
        /// 任务泵实例
        /// </summary>
        TaskPump taskPump = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_taskPump">任务泵实例</param>
        public CommonTaskProcesser(TaskPump _taskPump)
        {
            taskPump = _taskPump;
        }

        /// <summary>
        /// 启动任务处理器
        /// </summary>
        /// <returns>启动是否成功</returns>
        public override bool Start()
        {
            if (taskPump != null)
                return true;

            taskPump = new TaskPump();
            return true;
        }

        /// <summary>
        /// 停止任务处理器
        /// </summary>
        public override void Stop()
        {
            if (taskPump != null)
            {
                taskPump.Quit();
                taskPump = null;
            }
        }

        /// <summary>
        /// 暂停任务处理器
        /// </summary>
        public override void Pause()
        {
            if (taskPump != null)
                taskPump.Pause();
        }
        
        /// <summary>
        /// 继续执行任务处理器
        /// </summary>
        public override void Continue()
        {
            if (taskPump != null)
                taskPump.Continue();
        }

        /// <summary>
        /// 发布任务节点到任务处理器
        /// </summary>
        /// <param name="taskNode">任务节点</param>
        /// <param name="delay">延迟时间（毫秒）</param>
        /// <returns>操作结果，0表示成功，-1表示失败</returns>
        public override int PostTask(TaskNode taskNode, int delay = 0)
        {
            if (taskPump == null)
                return -1;

            return taskPump.PostTask(taskNode, delay);
        }

        /// <summary>
        /// 发布任务到任务处理器（只有处理回调）
        /// </summary>
        /// <param name="processDataCallBack">处理数据回调</param>
        /// <param name="taskData">任务数据</param>
        /// <param name="msg">任务消息类型</param>
        /// <param name="delay">延迟时间（毫秒）</param>
        /// <returns>操作结果，0表示成功，-1表示失败</returns>
        public override int PostTask(TaskCallBack processDataCallBack, TaskDataBase taskData, TaskMsg msg =TaskMsg.TMSG_DATA, int delay = 0)
        {
            if (taskPump == null)
                return -1;

            TaskNode taskNode = new TaskNode(processDataCallBack, null, taskData, msg);
            return taskPump.PostTask(taskNode, delay);
        }

        /// <summary>
        /// 发布任务到任务处理器（包含处理和释放回调）
        /// </summary>
        /// <param name="processDataCallBack">处理数据回调</param>
        /// <param name="releaseDataCallBack">释放数据回调</param>
        /// <param name="taskData">任务数据</param>
        /// <param name="msg">任务消息类型</param>
        /// <param name="delay">延迟时间（毫秒）</param>
        /// <returns>操作结果，0表示成功，-1表示失败</returns>
        public override int PostTask(TaskCallBack processDataCallBack, TaskCallBack releaseDataCallBack, TaskDataBase taskData, TaskMsg msg = TaskMsg.TMSG_DATA, int delay = 0)
        {
            if (taskPump == null)
                return -1;

            // 打印taskData数据
            if (taskData != null)
            {
                try
                {
                    // 使用反射获取对象的所有属性和字段
                    var type = taskData.GetType();
                    var properties = type.GetProperties();
                    var fields = type.GetFields();
                    
                    string dataInfo = $"类型: {type.Name}";
                    
                    // 打印字段信息
                    if (fields.Length > 0)
                    {
                        dataInfo += ", 字段: {";
                        for (int i = 0; i < fields.Length; i++)
                        {
                            var field = fields[i];
                            var value = field.GetValue(taskData);
                            dataInfo += $"{field.Name}={value ?? "null"}";
                            if (i < fields.Length - 1) dataInfo += ", ";
                        }
                        dataInfo += "}";
                    }
                    
                    // 打印属性信息
                    if (properties.Length > 0)
                    {
                        dataInfo += ", 属性: {";
                        for (int i = 0; i < properties.Length; i++)
                        {
                            var property = properties[i];
                            if (property.CanRead)
                            {
                                try
                                {
                                    var value = property.GetValue(taskData);
                                    dataInfo += $"{property.Name}={value ?? "null"}";
                                    if (i < properties.Length - 1) dataInfo += ", ";
                                }
                                catch
                                {
                                    dataInfo += $"{property.Name}=获取失败";
                                    if (i < properties.Length - 1) dataInfo += ", ";
                                }
                            }
                        }
                        dataInfo += "}";
                    }
                    
                    GF.LogInfo_gsc("TaskData详细信息", dataInfo);
                }
                catch (System.Exception ex)
                {
                    GF.LogInfo_gsc("TaskData信息", $"类型: {taskData.GetType().Name}, 反射获取详细信息失败: {ex.Message}");
                }
            }
            else
            {
                GF.LogInfo_gsc("TaskData信息", "taskData为null");
            }

            GF.LogInfo_gsc("发布任务到任务处理器", taskData.ToString());
            TaskNode taskNode = new TaskNode(processDataCallBack, releaseDataCallBack, taskData, msg);
            return taskPump.PostTask(taskNode, delay);
        }

        /// <summary>
        /// 清空任务队列
        /// </summary>
        public override void ClearAllTasks()
        {
            if (taskPump != null)
            {
                taskPump.ClearAllTasks();
                GF.LogInfo_gsc("TaskProcesser","已清空任务队列");
            }
        }
    }
}
