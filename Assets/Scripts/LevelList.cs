using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LeveList", menuName = "GMTKGameJam2023/Level List", order = 0)]
public class LevelList : ScriptableObject {
    public List<Level> levels;
}