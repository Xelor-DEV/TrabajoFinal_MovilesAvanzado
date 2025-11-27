using UnityEngine;

public class CanvasChannelLock : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas canvas;

    private void LateUpdate()
    {
        if (canvas != null)
        {
            if (canvas.additionalShaderChannels != AdditionalCanvasShaderChannels.None)
            {
                canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;

                Debug.Log($"[CanvasChannelLock] Se detectó un cambio no deseado en {canvas.name}. Corrigiendo a 'Nothing'.");
            }
        }
    }
}
