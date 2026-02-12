using GameFramework.Event;
public enum ClubDataType
{
    eve_LeagueDateUpdate,//俱乐部信息更新
    eve_LeagueUsersUpdate,//俱乐部成员更新
    eve_LeagueJackpotUpdate,//俱乐部奖池更新
    eve_LeagueSwitch,//俱乐部切换
}
public class ClubDataChangedEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(ClubDataChangedEventArgs).GetHashCode();
    public override int Id { get { return EventId; } }
    public ClubDataType Type { get; private set; }
    public object OldValue { get; private set; }
    public object Value { get; private set; }
    public override void Clear()
    {
        Type = default;
        Value = null;
        OldValue = null;
    }
    public ClubDataChangedEventArgs Fill(ClubDataType type,object oldV, object newV)
    {
        Type = type;
        OldValue = oldV;
        Value = newV;
        return this;
    }
}
