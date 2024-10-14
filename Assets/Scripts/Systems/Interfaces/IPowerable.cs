using System;

public interface IPowerable: IIdentifiable
{
    bool IsPowered { get; }
    event Action OnTogglePower;
    /*void TurnOn();
    void TurnOff();*/

    void TogglePower();
}