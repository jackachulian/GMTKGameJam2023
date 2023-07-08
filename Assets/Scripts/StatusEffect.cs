using System;
/// <summary>
/// An instanse of a status effect with a type (burn poison etc) and a duration
/// </summary>
[Serializable]
public class StatusEffect
{
    public StatusType type;

    public int remainingDuration;
}