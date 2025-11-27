using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "MatchData", menuName = "Game/MatchData")]
public class MatchDataSO : ScriptableObject
{
    [Header("Configuration")]
    public int maxPlayers = 2;
    public Color[] playerColors;

    [Header("Runtime Data")]
    public List<PlayerAssignment> assignedPlayers = new List<PlayerAssignment>();

    public void ClearData()
    {
        assignedPlayers.Clear();
    }

    public void SaveAssignment(int playerIndex, InputDevice device, string scheme)
    {
        assignedPlayers.Add(new PlayerAssignment
        {
            playerIndex = playerIndex,
            device = device,
            controlScheme = scheme
        });
    }
}

[Serializable]
public class PlayerAssignment
{
    public int playerIndex;
    public InputDevice device;
    public string controlScheme;
}