using UnityEngine;
using UnityEngine.InputSystem;
using System.Threading.Tasks;

public class RaceCompletionHandler : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private MatchDataSO matchData;
    [SerializeField] private FadeManager fadeManager;
    
    [Header("Settings")]
    [SerializeField] private string gameOverSceneName = "GameOver";

    private void OnEnable()
    {
        // Nos suscribimos al evento del Singleton
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnRaceEnded.AddListener(HandleRaceEnd);
        }
    }

    private void OnDisable()
    {
        // Buena práctica desuscribirse para evitar errores
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.OnRaceEnded.RemoveListener(HandleRaceEnd);
        }
    }

    private async void HandleRaceEnd(int winnerIndex)
{
    Debug.Log($"RaceCompletionHandler: Iniciando secuencia de fin de juego. Ganador: {winnerIndex}");

    // [CORRECCIÓN CRÍTICA] Guardar el ganador en el ScriptableObject
    if (matchData != null)
    {
        matchData.winningPlayerIndex = winnerIndex;
        Debug.Log($"Guardado en MatchDataSO: WinningPlayerIndex = {matchData.winningPlayerIndex}");
    }
    else
    {
        Debug.LogError("MatchData no está asignado en el inspector de RaceCompletionHandler");
    }

    // 2. Desactivar Inputs de TODOS los jugadores
    var allRacers = RaceManager.Instance.Racers;

    foreach (var racer in allRacers)
    {
        var playerInput = racer.GetComponent<PlayerInput>();

        if (playerInput != null)
        {
            playerInput.DeactivateInput();
        }
    }

    // 3. Ejecutar Fade y Cargar Escena
    if (fadeManager != null && GlobalSceneLoader.Instance != null)
    {
        await fadeManager.WaitForTaskAndShowLoading(
            GlobalSceneLoader.Instance.LoadSceneAsync(gameOverSceneName)
        );
    }
    else
    {
        Debug.LogError("Falta asignar FadeManager o GlobalSceneLoader no existe.");
    }
}
}