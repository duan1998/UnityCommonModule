using ConsoleApp1.System;

namespace ConsoleApp1;

public sealed class MailRedDotBinder:IDisposable
{
    private readonly RedDotTree _tree;
    private readonly MailSystem _mail;

    public MailRedDotBinder(RedDotTree tree, MailSystem mail)
    {
        _tree = tree;
        _mail = mail;

        _mail.UnreadChanged += OnUnreadChanged;
        
        OnUnreadChanged(_mail.SystemUnread, _mail.FriendUnread);
    }

    public void Dispose()
    {
        _mail.UnreadChanged -= OnUnreadChanged;
    }

    private void OnUnreadChanged(int systemUnread, int friendUnread)
    {
        _tree.SetLeafValueDeferred(RedDotKeys.MailSystem, systemUnread);
        _tree.SetLeafValueDeferred(RedDotKeys.MailFriend, friendUnread);
    }
}