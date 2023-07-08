using System;
/// <summary>
/// An instanse of a status effect with a type (burn poison etc) and a duration
/// </summary>
[Serializable]
public class StatusEffect
{
    public StatusType type;

    public int duration;

    /// <summary>
    /// The first turn status effects are added, duration will not decrement that turn.
    /// Only after the first status tick this will be set to false and duraion will resume as normal.
    /// </summary>
    private bool durationPaused = true;

    public bool DurationPaused
    {
        get { return durationPaused; }
        set { durationPaused = value; }
    }
}