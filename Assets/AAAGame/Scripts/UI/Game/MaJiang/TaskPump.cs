using System;
using System.Collections.Generic;
using System.Threading;

namespace GameTask
{
    /// <summary>
    /// 任务数据处理回调委托
    /// </summary>
    /// <param name="data">任务数据</param>
    public delegate void TaskCallBack(TaskDataBase data);

    /// <summary>
    /// 任务消息类型枚举
    /// </summary>
    public enum TaskMsg
    {
        /// <summary>
        /// 数据消息
        /// </summary>
        TMSG_DATA,
        
        /// <summary>
        /// 退出消息
        /// </summary>
        TMSG_QUIT,
        
        /// <summary>
        /// 定时器开始消息
        /// </summary>
        TMSG_TIMER_START,
        
        /// <summary>
        /// 定时器运行消息
        /// </summary>
        TMSG_TIMER_RUN,
        
        /// <summary>
        /// 定时器停止消息
        /// </summary>
        TMSG_TIMER_STOP,
        
        /// <summary>
        /// 暂停消息
        /// </summary>
        TMSG_PAUSE
    }

    /// <summary>
    /// 任务数据类型枚举
    /// </summary>
    public enum TaskDataType
    {
        /// <summary>
        /// 定时器数据
        /// </summary>
        TDATA_TIMER,
        
        /// <summary>
        /// 通用数据
        /// </summary>
        TDATA_COMMON,    
    }

    /// <summary>
    /// 任务数据基类
    /// </summary>
    public class TaskDataBase
    {
        /// <summary>
        /// 任务数据类型，默认为通用数据
        /// </summary>
        public TaskDataType taskDataType = TaskDataType.TDATA_COMMON;
        
        /// <summary>
        /// 任务索引
        /// </summary>
        public int idx = 0;
    }

    /// <summary>
    /// 任务节点类
    /// </summary>
    public class TaskNode
    {
        /// <summary>
        /// 任务开始时间
        /// </summary>
        public DateTime startTime;
        
        /// <summary>
        /// 延迟执行时间（毫秒）
        /// </summary>
        public int delay;
        
        /// <summary>
        /// 处理数据回调函数
        /// </summary>
        public TaskCallBack processDataCallBack;
        
        /// <summary>
        /// 释放数据回调函数
        /// </summary>
        public TaskCallBack releaseDataCallBack;
        
        /// <summary>
        /// 任务数据
        /// </summary>
        public TaskDataBase data;
        
        /// <summary>
        /// 任务消息类型
        /// </summary>
        public TaskMsg msg;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="taskMsg">任务消息类型</param>
        public TaskNode(TaskMsg taskMsg)
        {
            msg = taskMsg;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="processDataCallBack">处理数据回调</param>
        /// <param name="releaseDataCallBack">释放数据回调</param>
        /// <param name="taskData">任务数据</param>
        /// <param name="msg">任务消息类型，默认为数据消息</param>
        public TaskNode( 
            TaskCallBack processDataCallBack,
            TaskCallBack releaseDataCallBack,
            TaskDataBase taskData, TaskMsg msg = TaskMsg.TMSG_DATA)
        {
            this.processDataCallBack = processDataCallBack;
            this.releaseDataCallBack = releaseDataCallBack;
            this.data = taskData;
            this.msg = msg;
        }
    }

    /// <summary>
    /// 任务队列类
    /// </summary>
    public class TaskQueue
    {
        /// <summary>
        /// 退出标志
        /// </summary>
        public int isquit;
        
        /// <summary>
        /// 读取任务列表
        /// </summary>
        public LinkedList<TaskNode> readList = new LinkedList<TaskNode>();
        
        /// <summary>
        /// 写入任务列表
        /// </summary>
        public LinkedList<TaskNode> writeList = new LinkedList<TaskNode>();

        /// <summary>
        /// 定时器读取任务列表
        /// </summary>
        public LinkedList<TaskNode> timerReadList = new LinkedList<TaskNode>();
        
        /// <summary>
        /// 定时器写入任务列表
        /// </summary>
        public LinkedList<TaskNode> timerWriteList = new LinkedList<TaskNode>();

        /// <summary>
        /// 线程锁对象
        /// </summary>
        public readonly object lockobj = new object();
    }

    /// <summary>
    /// 任务泵类，用于管理和调度任务队列
    /// </summary>
    public class TaskPump
    {
        /// <summary>
        /// 最大延迟时间常量
        /// </summary>
        readonly int MAX_DELAY_TIME = 0x7FFFFFFF;

        /// <summary>
        /// 类型，0表示线程模式，1表示非线程模式
        /// </summary>
        int type = 0;
        
        /// <summary>
        /// 任务处理线程
        /// </summary>
        Thread thread;
        
        /// <summary>
        /// 任务队列
        /// </summary>
        TaskQueue taskQue;
 
        /// <summary>
        /// 读取信号量
        /// </summary>
        EventWaitHandle readSem = new EventWaitHandle(false, EventResetMode.ManualReset);
        
        /// <summary>
        /// 暂停信号量
        /// </summary>
        EventWaitHandle pauseSem = new EventWaitHandle(false, EventResetMode.ManualReset);
        
        /// <summary>
        /// 退出信号量
        /// </summary>
        EventWaitHandle quitSem = new EventWaitHandle(false, EventResetMode.ManualReset);

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_type">类型，0表示线程模式，1表示非线程模式</param>
        public TaskPump(int _type = 0)
        {
            type = _type;
            taskQue = new TaskQueue();

            if (type == 0)
            {
                thread = new Thread(new ThreadStart(Run));
                thread.Start();
            }
        }

        /// <summary>
        /// 任务处理线程主函数
        /// </summary>
        public void Run()
        {
            LinkedList<TaskNode> readList = taskQue.readList;
            LinkedList<TaskNode> writeList = taskQue.writeList;

            LinkedList<TaskNode> timerReadList = taskQue.timerReadList;
            LinkedList<TaskNode> timerWriteList = taskQue.timerWriteList;

            DateTime curTime;
            Timer timer;
            int minDelay = MAX_DELAY_TIME;
            int curdelay;
            TaskNode node;
            LinkedListNode<TaskNode> tmp;

            for (;;)
            {
                curTime = DateTime.Now;

                // 处理定时器任务
                for (LinkedListNode<TaskNode> linkedNode = timerReadList.First; linkedNode != null; )
                {
                    node = linkedNode.Value;
                    curdelay = (int)(curTime - node.startTime).TotalMilliseconds;

                    if (node.delay > 0 && curdelay < node.delay)
                    {
                        if (node.delay - curdelay < minDelay)
                            minDelay = node.delay - curdelay;

                        linkedNode = linkedNode.Next;
                        continue;
                    }

                    if (node.msg == TaskMsg.TMSG_TIMER_STOP)
                    {
                        timer = (Timer)node.data;
                        if (timer.taskNode != null){
                            timerReadList.Remove(timer.taskNode);       
                        }
                    }

                    tmp = linkedNode.Next;
                    timerReadList.Remove(linkedNode);
                    linkedNode = tmp;

                    if (ProcessTaskNodeData(node) == 1) 
                        goto end;
                }

                // 处理普通任务
                for (LinkedListNode<TaskNode> linkedNode = readList.First; linkedNode != null; )
                {
                    LinkedListNode<TaskNode> tmp2 = linkedNode.Next;
                    readList.RemoveFirst();

                    if (ProcessTaskNodeData(linkedNode.Value) == 1)
                        goto end;
                        
                    linkedNode = tmp2;
                }

                // 非线程模式且无任务时退出
                if(type == 1 && 
                    writeList.Count == 0 && 
                    timerWriteList.Count == 0)
                {
                    goto end;
                }


                Monitor.Enter(taskQue.lockobj);

                // 等待新任务到达
                while (type == 0 &&            
                    writeList.Count == 0 &&        
                    timerWriteList.Count == 0)
                {
                    Monitor.Exit(taskQue.lockobj);

                    if (minDelay == MAX_DELAY_TIME)
                    {
                        readSem.WaitOne();
                        readSem.Reset();

                    }
                    else
                    {
                        readSem.WaitOne(minDelay);
                        readSem.Reset();

                        Monitor.Enter(taskQue.lockobj);
                        minDelay = MAX_DELAY_TIME;
                        break;
                    }

                    Monitor.Enter(taskQue.lockobj);
                }


                // 交换普通任务列表
                if (writeList.Count > 0)
                {
                    taskQue.writeList = readList;
                    taskQue.readList = writeList;

                    readList = taskQue.readList;
                    writeList = taskQue.writeList;
                }


                // 交换定时器任务列表
                if (timerWriteList.Count > 0)
                {
                    minDelay = MAX_DELAY_TIME;

                    if (timerReadList.Count > 0)
                    {
                        for (LinkedListNode<TaskNode> linkedNode = timerWriteList.First; linkedNode != null; linkedNode = timerWriteList.First)
                        {
                            timerWriteList.RemoveFirst();
                            timerReadList.AddLast(linkedNode);
                        }
                    }
                    else
                    {
                        taskQue.timerWriteList = timerReadList;
                        taskQue.timerReadList = timerWriteList;
                        timerReadList = taskQue.timerReadList;
                        timerWriteList = taskQue.timerWriteList;
                    }
                }


                Monitor.Exit(taskQue.lockobj);

                if (type != 0)
                    break;
            }

            end:

            if (type == 0)
                quitSem.Set();

        }

        /// <summary>
        /// 处理任务节点数据
        /// </summary>
        /// <param name="node">任务节点</param>
        /// <returns>0表示继续处理，1表示退出</returns>
        int ProcessTaskNodeData(TaskNode node)
        {
            if (node.processDataCallBack != null)
                node.processDataCallBack(node.data);

            if (node.releaseDataCallBack != null)
                node.releaseDataCallBack(node.data);

            TaskMsg taskMsg = node.msg;

            switch (taskMsg)
            {
                case TaskMsg.TMSG_QUIT:
                    quitSem.Set();
                    return 1;

                case TaskMsg.TMSG_PAUSE:
                    pauseSem.WaitOne();
                    pauseSem.Reset();
                    break;
            }

            return 0;

        }

        /// <summary>
        /// 发布任务到任务队列
        /// </summary>
        /// <param name="taskNode">任务节点</param>
        /// <param name="delay">延迟时间（毫秒）</param>
        /// <returns>操作结果，0表示成功</returns>
        public int PostTask(TaskNode taskNode, int delay = 0)
        {
            int ret = 0;

            Monitor.Enter(taskQue.lockobj);

            switch (taskNode.msg)
            {
                case TaskMsg.TMSG_TIMER_START:
                    {
                        ret = PostTimerTask(taskNode, -2000);
                    }
                    break;

                case TaskMsg.TMSG_TIMER_RUN:
                    {
                        ret = PostTimerTask(taskNode, delay);
                    }
                    break;

                case TaskMsg.TMSG_TIMER_STOP:
                    {
                        ret = PostCommonTask(taskNode, -1000);
                    }
                    break;

                default:
                    ret = PostCommonTask(taskNode, delay);
                    break;
            }

            Monitor.Exit(taskQue.lockobj);

            //设置任务处理可读信号
            readSem.Set();

            return ret;
        }

        /// <summary>
        /// 发布普通任务
        /// </summary>
        /// <param name="taskNode">任务节点</param>
        /// <param name="delay">延迟时间（毫秒）</param>
        /// <returns>操作结果，0表示成功</returns>
        int PostCommonTask(TaskNode taskNode, int delay)
        {
            int ret = 0;

            taskNode.delay = delay;
            if (delay != 0)
                taskNode.startTime = DateTime.Now;

            if (delay == 0)
            {
                taskQue.writeList.AddLast(taskNode);
            }
            else
            {
                taskQue.timerWriteList.AddLast(taskNode);
            }

            return ret;
        }

        /// <summary>
        /// 发布定时器任务
        /// </summary>
        /// <param name="taskNode">任务节点</param>
        /// <param name="delay">延迟时间（毫秒）</param>
        /// <returns>操作结果，0表示成功</returns>
        int PostTimerTask(TaskNode taskNode, int delay)
        {
            taskNode.delay = delay;
            taskNode.startTime = DateTime.Now;
            taskQue.timerWriteList.AddLast(taskNode);
            return 0;

        }

        /// <summary>
        /// 退出任务处理
        /// </summary>
        /// <returns>操作结果，0表示成功</returns>
        public int Quit()
        {
            Continue();

            TaskNode node = new TaskNode(TaskMsg.TMSG_QUIT);
            if (PostTask(node, 0) != 0)
                return -1;

            quitSem.WaitOne();
            quitSem.Reset();
            return 0;
        }

        /// <summary>
        /// 暂停任务处理
        /// </summary>
        /// <returns>操作结果，0表示成功</returns>
        public int Pause()
        {
            TaskNode node = new TaskNode(TaskMsg.TMSG_PAUSE);
            if (PostTask(node, 0) != 0)
                return -1;

            return 0;
        }

        /// <summary>
        /// 继续任务处理
        /// </summary>
        /// <returns>操作结果，0表示成功</returns>
        public int Continue()
        {
            pauseSem.Set();
            return 0;
        }

        /// <summary>
        /// 立即清空所有未执行与延迟中的任务（读/写队列与定时器队列）
        /// 不会影响当前正在执行的任务（因为已在本帧取出处理）。
        /// 用于需要立刻打断后续队列（例如胡牌后无需再播放打牌/过牌等动作）。
        /// </summary>
        public void ClearAllTasks()
        {
            Monitor.Enter(taskQue.lockobj);
            try
            {
                taskQue.readList.Clear();
                taskQue.writeList.Clear();
                taskQue.timerReadList.Clear();
                taskQue.timerWriteList.Clear();
            }
            finally
            {
                Monitor.Exit(taskQue.lockobj);
            }
        }
    }
}
