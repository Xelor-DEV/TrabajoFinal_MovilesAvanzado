using UnityEngine;
using TMPro;

public class GameOverDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MatchDataSO matchData;
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private FadeManager fadeManager;
    [Header("Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Start()
    {
        if (winnerText == null) return;

        if (matchData != null && matchData.winningPlayerIndex != -1)
        {
            // Mostramos el mensaje en inglés como pediste
            // Sumamos 1 porque winningPlayerIndex es base 0 (0, 1) -> (1, 2)
            winnerText.text = $"PLAYER {matchData.winningPlayerIndex + 1} WINS!";
        }
        else
        {
            winnerText.text = "NO WINNER DETECTED";
        }
    }
    
    // Método opcional para volver al menú
    public async void ReturnToMenu()
    {
        if (fadeManager != null && GlobalSceneLoader.Instance != null)
        {
            await fadeManager.WaitForTaskAndShowLoading(
                GlobalSceneLoader.Instance.LoadSceneAsync(mainMenuSceneName)
            );
        }
    }
}