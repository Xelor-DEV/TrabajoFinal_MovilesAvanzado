using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class GlobalSceneLoader : PersistentSingleton<GlobalSceneLoader>
{
    [SerializeField] private float minimumLoadTime = 2.0f;

    public async Task LoadSceneAsync(string sceneName)
    {
        // Iniciar el temporizador mínimo
        var minimumTimer = Task.Delay((int)(minimumLoadTime * 1000));

        // Iniciar la carga de la escena
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // Esperar a que la escena esté casi cargada
        while (asyncLoad.progress < 0.9f)
        {
            await Task.Yield();
        }

        // Esperar a que termine el tiempo mínimo
        await minimumTimer;

        // Activar la escena
        asyncLoad.allowSceneActivation = true;

        // Esperar a que la carga termine completamente
        while (!asyncLoad.isDone)
        {
            await Task.Yield();
        }
    }
}