using UnityEngine;

public abstract class PersistentSingleton<T> : MonoBehaviour where T : Component
{
    private static T instance;

    public static T Instance
    {
        get
        {
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Debug.LogWarning($"[Singleton] A duplicate instance of {typeof(T).Name} was found and destroyed.");
            Destroy(gameObject);
        }
    }
}