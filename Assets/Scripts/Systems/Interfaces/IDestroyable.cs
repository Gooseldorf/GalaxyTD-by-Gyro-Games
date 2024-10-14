using CardTD.Utilities;
using System;

public interface IDestroyable
{
    Action OnDestroy { get; }

    bool IsNeedToDestroy { get; set; }

    void Destroy()
    {
        this.OnDestroy?.Invoke();
        Messenger<object>.Broadcast(GameEvents.ObjectDestroyed, this, MessengerMode.DONT_REQUIRE_LISTENER);
    }
}