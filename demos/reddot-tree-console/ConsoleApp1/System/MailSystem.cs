namespace ConsoleApp1.System;

public sealed class MailSystem
{
    public event Action<int, int>? UnreadChanged;
    
    public int SystemUnread { get; private set; }
    public int FriendUnread { get; private set; }

    public void SetUnread(int systemUnread, int friendUnread)
    {
        SystemUnread = systemUnread;
        FriendUnread = friendUnread;
        UnreadChanged?.Invoke(SystemUnread, FriendUnread);
    }
}