using UnityEngine;
using System.Threading.Tasks;

public class ControlSelectionSceneTransition : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private ControlSelectionManager selectionManager;
    [SerializeField] private FadeManager fadeManager;

    [Header("Scene Settings")]
    [Tooltip("Nombre de la escena a cargar cuando todos los jugadores estén listos")]
    [SerializeField] private string targetSceneName = "GameScene";

    private void OnEnable()
    {
        if (selectionManager != null)
        {
            selectionManager.OnSelectionComplete.AddListener(OnSelectionCompleteHandler);
        }
    }

    private void OnDisable()
    {
        if (selectionManager != null)
        {
            selectionManager.OnSelectionComplete.RemoveListener(OnSelectionCompleteHandler);
        }
    }

    private async void OnSelectionCompleteHandler()
    {
        // Verificamos referencias
        if (fadeManager == null || GlobalSceneLoader.Instance == null)
        {
            Debug.LogError("Faltan referencias críticas (FadeManager o GlobalSceneLoader) en ControlSelectionSceneTransition");
            return;
        }

        // Llamamos al FadeManager.
        // Gracias a la modificación en FadeManager, esto hará:
        // 1. ShowAsync (bajar cortina)
        // 2. StartLoadingAnimation (Pop In del texto)
        // 3. Cargar Escena (esperando el tiempo mínimo del GlobalSceneLoader)
        // 4. HideAsync (subir cortina)
        await fadeManager.WaitForTaskAndShowLoading(
            GlobalSceneLoader.Instance.LoadSceneAsync(targetSceneName)
        );
    }
}