using System.Collections.Generic;
using Tags;

public interface ITowerFactory
{
    ISlot Ammo { get; }
    IReadOnlyList<ISlot> Directives { get; }
    IReadOnlyList<ISlot> Parts { get; }
    int Level { get; }  
    AllEnums.TowerId TowerId { get; }
    MenuUpgrade NextUpgrade { get; }
    Tower GetBaseTower();
    Tower GetAssembledTower();
}