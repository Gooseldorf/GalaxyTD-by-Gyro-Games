using I2.Loc;
using System;

public sealed class NoObstacleTag : Tag
{
    public override string GetDescription() => LocalizationManager.GetTranslation("Tags/NoObstacle");
}