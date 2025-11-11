using UnityEngine;

[CreateAssetMenu(fileName = "GameVersion", menuName = "Scriptable Objects/GameVersion")]
public class GameVersion : ScriptableObject
{
    [Header("Game Version Settings")]
    public string currentVersion = "1.0.0";

    [TextArea(3, 10)]
    public string versionNotes = "Initial version";
}