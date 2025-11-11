using UnityEngine;

[CreateAssetMenu(fileName = "PlayerIcons", menuName = "Scriptable Objects/PlayerIcons")]
public class PlayerIcons : ScriptableObject
{
    public Sprite[] icons;

    public Sprite GetIcon(int index)
    {
        if (icons == null || icons.Length == 0)
        {
            Debug.LogError("No icons defined in PlayerIcons");
            return null;
        }

        if (index < 0 || index >= icons.Length)
        {
            Debug.LogWarning($"Icon index {index} out of range, using default");
            return icons[0];
        }

        return icons[index];
    }

    public int GetNextIconIndex(int currentIndex)
    {
        if (icons == null || icons.Length == 0) return 0;
        return (currentIndex + 1) % icons.Length;
    }

    public int GetPreviousIconIndex(int currentIndex)
    {
        if (icons == null || icons.Length == 0) return 0;
        return (currentIndex - 1 + icons.Length) % icons.Length;
    }
}