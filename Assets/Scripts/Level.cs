using UnityEditor.Animations;
using UnityEngine;

[System.Serializable]
public class Level
{
    // List of battlers in this level. First x indexes (given by playerAmount) in the array will be non-target
    public BattlerStats[] battlers;

    // how many battlers in a level are "player team" / not targets
    public int playerAmount = 1;

    // Lines of dialogue to display before the level begins
    [TextArea]
    public string levelStartDialogue;

    // Lines of dialogue to display before the level begins
    [TextArea]
    public string levelEndDialogue;
}

