/// <summary>
/// An instanse of a status effect with a type (burn poison etc) and a duration
/// </summary>
public class StatusEffect
{
    public StatusType type;
    public enum StatusType {
        Poison, Burn
    }

    public int remainingDuration;
}