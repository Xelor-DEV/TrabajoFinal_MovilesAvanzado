using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

[CreateAssetMenu(fileName = "MatchData", menuName = "Game/MatchData")]
public class MatchDataSO : ScriptableObject
{
    [Header("Configuration Limits")]
    // [NUEVO] Límites definidos en el SO
    public int minPlayers = 2;
    public int maxPossiblePlayers = 6;

    [Header("Current Settings")]
    // [NUEVO] La selección actual que modificaremos en el menú
    public int currentPlayerSelection = 2;

    public Color[] playerColors;

    [Header("Runtime Data")]
    public List<PlayerAssignment> assignedPlayers = new List<PlayerAssignment>();
    public int winningPlayerIndex = -1;

    public void InitializeList()
    {
        assignedPlayers.Clear();
        winningPlayerIndex = -1;

        // Usamos la selección actual para inicializar la lista
        for (int i = 0; i < currentPlayerSelection; i++)
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