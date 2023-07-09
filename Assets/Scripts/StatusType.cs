using UnityEngine;

[CreateAssetMenu(fileName = "StatusType", menuName = "GMTKGameJam2023/StatusType", order = 0)]
public class StatusType : ScriptableObject {
    public Sprite icon;

    public int maxDuration = 9;

    public string statusName;
    public string description;
}