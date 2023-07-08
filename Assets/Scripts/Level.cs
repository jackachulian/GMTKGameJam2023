using UnityEditor.Animations;
using UnityEngine;

[System.Serializable]
public class Level
{
    // List of battlers in this level. First index in the array will be the starting player.
    public BattlerStats[] battlers;
}

