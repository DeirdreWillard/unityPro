using GameFramework.Event;

public enum GFEventType
{
    ApplicationQuit, //游戏退出
    TCPReconnectSuccess,//重连成功
    TCPReconnectFailed,//重连失败
    eve_SynDeskChat,//广播游戏内聊天信息
    eve_SynClubChat,//广播俱乐部内聊天信息
    eve_SynClubChatNum,//广播俱乐部内聊天信息数量通知
    eve_ReConnectGame,//请求重连游戏
    eve_AppPaused,//应用进入后台
    eve_AppResumed,//应用返回前台
}
public class GFEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(GFEventArgs).GetHashCode();
    public override int Id => EventId;
    public GFEventType EventType { get; private set; }
    public object UserData { get; private set; }
    public override void Clear()
    {
        UserData = null;
    }
    public GFEventArgs Fill(GFEventType eventType, object userDt = null)
    {
        this.EventType = eventType;
        this.UserData = userDt;
        return this;
    }
}
