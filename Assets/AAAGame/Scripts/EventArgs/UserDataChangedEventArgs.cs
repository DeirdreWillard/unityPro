using GameFramework.Event;
public enum UserDataType
{
    MONEY = 1,
    DIAMOND,
    Nick,
    SafeCodeState,
    VipState,
    Msg,
    avatar,
    TICKETS,
    LEAGUE_COIN,
}
public class UserDataChangedEventArgs : GameEventArgs
{
    public static readonly int EventId = typeof(UserDataChangedEventArgs).GetHashCode();
    public override int Id { get { return EventId; } }
    public UserDataType Type { get; private set; }
    public object OldValue { get; private set; }
    public object Value { get; private set; }
    public override void Clear()
    {
        Type = default;
        Value = null;
        OldValue = null;
    }
    public UserDataChangedEventArgs Fill(UserDataType type,object oldV, object newV)
    {
        Type = type;
        OldValue = oldV;
        Value = newV;
        return this;
    }
}
