namespace GameTask
{
    /// <summary>
    /// 定时器类，用于定时执行任务
    /// </summary>
    public class Timer : TaskDataBase
    {
        /// <summary>
        /// 定时器回调委托
        /// </summary>
        /// <param name="data">任务数据</param>
        /// <returns>执行结果，true表示成功</returns>
        public delegate bool TimerCallBack(TaskDataBase data);

        /// <summary>
        /// 定时器持续时间（毫秒）
        /// </summary>
        int durationMS = 0;
        
        /// <summary>
        /// 任务处理器
        /// </summary>
        TaskProcesser taskProcesser = null;
        
        /// <summary>
        /// 定时器回调函数
        /// </summary>
        TimerCallBack timerCB = null;
        
        /// <summary>
        /// 回调函数参数
        /// </summary>
        TaskDataBase param = null;
        
        /// <summary>
        /// 是否已停止
        /// </summary>
        bool isStop = true;
        
        /// <summary>
        /// 是否重复执行
        /// </summary>
        bool isRepeat = true;
        
        /// <summary>
        /// 定时器对应的任务节点
        /// </summary>
        public TaskNode taskNode = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="taskProcesser">任务处理器</param>
        /// <param name="durationMS">定时器持续时间（毫秒）</param>
        /// <param name="timerCB">定时器回调函数</param>
        /// <param name="param">回调函数参数</param>
        /// <param name="isRepeat">是否重复执行，默认为true</param>
        public Timer(TaskProcesser taskProcesser, int durationMS, TimerCallBack timerCB, TaskDataBase param, bool isRepeat = true)
        {
            taskDataType = TaskDataType.TDATA_TIMER;

            this.param = param;
            this.timerCB = timerCB;

            this.taskProcesser = taskProcesser;
            this.durationMS = durationMS;
            this.isRepeat = isRepeat;
        }

        /// <summary>
        /// 启动定时器
        /// </summary>
        public void Start()
        {
            taskProcesser.PostTask(StartTask, null, this, TaskMsg.TMSG_TIMER_START);
        }
        
        /// <summary>
        /// 停止定时器
        /// </summary>
        public void Stop()
        {
            taskProcesser.PostTask(StopTask, null, this, TaskMsg.TMSG_TIMER_STOP);
        }

        /// <summary>
        /// 发布定时器任务
        /// </summary>
        /// <param name="data">任务数据</param>
        void PostTask(TaskDataBase data)
        {
            taskNode = new TaskNode(RunTask, null, data, TaskMsg.TMSG_TIMER_RUN);
            taskProcesser.PostTask(taskNode, durationMS);
        }

        /// <summary>
        /// 运行定时器任务
        /// </summary>
        /// <param name="data">任务数据</param>
        void RunTask(TaskDataBase data)
        {
            if (isStop)
                return;

            timerCB(param);
            taskNode = null;

            if (isRepeat)
                PostTask(data);
            else
                isStop = true;
        }

        /// <summary>
        /// 启动定时器任务
        /// </summary>
        /// <param name="data">任务数据</param>
        void StartTask(TaskDataBase data)
        {
            if (isStop == false)
                return;

            isStop = false;
            PostTask(data);
        }

        /// <summary>
        /// 停止定时器任务
        /// </summary>
        /// <param name="data">任务数据</param>
        void StopTask(object data)
        {
            taskNode = null;
            if (isStop)
                return;
            isStop = true;
        }
    }
}
