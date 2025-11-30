using UnityEngine;

public enum SpellType
{
    SpeedBoost,     // Aumentar velocidad propia (Haste)
    LeaderStun,     // Congelar al primero (Rayo)
    Shield,         // Protegerse de golpes (Escudo)
    SlowAhead,      // Relentizar al que va delante (Maldición)
    AreaPush        // Empujar a los que están cerca (Onda expansiva)
}

[CreateAssetMenu(fileName = "New Spell", menuName = "Kart/Spell Data")]
public class SpellData : ScriptableObject
{
    public string spellName;
    public Sprite icon;
    public SpellType spellType;

    [Header("Parámetros Genéricos")]
    [Tooltip("Duración del efecto (ej: 5s de velocidad, 3s de stun)")]
    public float duration = 3f;

    [Tooltip("Potencia (ej: 1.5x velocidad, 0.5x slow, 30f fuerza empuje)")]
    public float power = 1.5f;
}