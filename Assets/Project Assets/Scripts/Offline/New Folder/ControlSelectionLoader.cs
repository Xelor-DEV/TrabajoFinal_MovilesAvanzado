using UnityEngine;
using System.Threading.Tasks;

public class ControlSelectionLoader : MonoBehaviour
{
    [SerializeField] private string sceneName = "ControlSelection";
    [SerializeField] private FadeManager fadeManager;

    public async void LoadControlSelectionScene()
    {
        if (fadeManager != null)
        {
            await fadeManager.WaitForTaskAndShowLoading(
                GlobalSceneLoader.Instance.LoadSceneAsync(sceneName)
            );
        }
        else
        {
            await GlobalSceneLoader.Instance.LoadSceneAsync(sceneName);
        }
    }
}