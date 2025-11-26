using UnityEngine;

public abstract class NonPersistentSingleton<T> : MonoBehaviour where T : Component
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                FindOrCreateInstance();
            }

            return instance;
        }
    }

    private static void FindOrCreateInstance()
    {
        T[] existingInstances = FindObjectsByType<T>(FindObjectsSortMode.None);

        if (existingInstances.Length > 0)
        {
            instance = existingInstances[0];

            if (existingInstances.Length > 1)
            {
                Debug.LogWarning($"[Singleton] Multiple {typeof(T).Name} instances found. Extra instances will be destroyed.");

                for (int i = 1; i < existingInstances.Length; ++i)
                {
                    if (existingInstances[i] != null)
                    {
                        Destroy(existingInstances[i].gameObject);
                    }
                }
            }

            return;
        }

        // Crear una nueva instancia
        GameObject obj = new GameObject(typeof(T).Name + " (Singleton)");
        // Opcional: HideFlags puede dificultar la depuración, úsalo con precaución
        obj.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
        instance = obj.AddComponent<T>();
    }
}