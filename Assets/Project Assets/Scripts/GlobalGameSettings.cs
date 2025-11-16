using UnityEngine;

[CreateAssetMenu(fileName = "GlobalGameSettings", menuName = "Scriptable Objects/GlobalGameSettings")]
public class GlobalGameSettings : ScriptableObject
{
    [Header("Player Settings")]
    public int maxGlobalPlayers = 2;
    public int minPlayersToStart = 2;

    [Header("Available Maps")]
    public Map[] selectableMaps;

    [Header("Available Game Modes")]
    public GameModes[] availableGameModes;
}

public enum Map
{
    Day,
    Night,
}

public enum GameModes
{
    Versus,
    Coop,
}