using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

[CreateAssetMenu(fileName = "MatchData", menuName = "Game/MatchData")]
public class MatchDataSO : ScriptableObject
{
    [Header("Configuration")]
    public int maxPlayers = 2;
    public Color[] playerColors;

    [Header("Runtime Data")]
    public List<PlayerAssignment> assignedPlayers = new List<PlayerAssignment>();

    // [NUEVO] Variable para persistir quién ganó entre escenas
    public int winningPlayerIndex = -1; 

    public void InitializeList()
    {
        assignedPlayers.Clear();
        winningPlayerIndex = -1; // Reseteamos ganador al iniciar
        for (int i = 0; i < maxPlayers; i++)
        {
            assignedPlayers.Add(null);
        }
    }

    public void SaveAssignment(int playerIndex, ReadOnlyArray<InputDevice> devices, string scheme)
    {
        PlayerAssignment newAssignment = new PlayerAssignment
        {
            playerIndex = playerIndex,
            controlScheme = scheme,
            deviceIds = new List<int>()
        };

        foreach (var dev in devices) newAssignment.deviceIds.Add(dev.deviceId);

        if (playerIndex < assignedPlayers.Count)
        {
            assignedPlayers[playerIndex] = newAssignment;
        }
    }
}

[Serializable]
public class PlayerAssignment
{
    public int playerIndex;
    public string controlScheme;
    public List<int> deviceIds; 
}