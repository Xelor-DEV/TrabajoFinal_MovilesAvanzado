using DG.Tweening;
using System.Collections;
using UnityEngine;

public class SpellPickupBox : MonoBehaviour
{
    [Header("Configuración de Spells")]
    public SpellData spellSpeedBoost;
    public SpellData spellLeaderStun;
    public SpellData spellShield;
    public SpellData spellSlowAhead;
    public SpellData spellAreaPush;

    [Header("Probabilidades (0.0 a 1.0)")]
    [Tooltip("Probabilidad de obtener Escudo yendo PRIMERO")]
    [Range(0f, 1f)] public float firstPlace_ShieldChance = 0.6f;

    [Tooltip("Probabilidad de Rayo yendo ÚLTIMO")]
    [Range(0f, 1f)] public float lastPlace_StunChance = 0.4f;
    [Tooltip("Probabilidad de Velocidad yendo ÚLTIMO")]
    [Range(0f, 1f)] public float lastPlace_BoostChance = 0.4f;

    [Tooltip("Probabilidad de Slow yendo EN MEDIO")]
    [Range(0f, 1f)] public float mid_SlowChance = 0.3f;
    [Tooltip("Probabilidad de Empuje yendo EN MEDIO")]
    [Range(0f, 1f)] public float mid_PushChance = 0.3f;
    [Tooltip("Probabilidad de Velocidad yendo EN MEDIO")]
    [Range(0f, 1f)] public float mid_BoostChance = 0.2f;

    [Header("Box Settings")]
    [SerializeField] private GameObject visualModel;
    [SerializeField] private Collider triggerCollider;
    [SerializeField] private float respawnTime = 5f;

    [Header("Animation Settings")]
    [SerializeField] private float vanishDuration = 0.3f;
    [SerializeField] private float appearDuration = 0.5f;

    // --- NUEVA SECCIÓN PARA MOVIMIENTO IDLE ---
    [Header("Idle Animation Settings")]
    [SerializeField] private float floatAmplitude = 0.25f; // Cuánto sube y baja
    [SerializeField] private float floatDuration = 1.5f;   // Tiempo en subir y bajar
    [SerializeField] private float rotateDuration = 4.0f;  // Tiempo en dar una vuelta completa

    private Vector3 originalScale;
    private Vector3 initialLocalPos; // Para saber la altura base

    private void Awake()
    {
        if (visualModel != null)
        {
            originalScale = visualModel.transform.localScale;
            initialLocalPos = visualModel.transform.localPosition;
        }
    }

    private void Start()
    {
        // Iniciamos la animación de flotar y rotar al comenzar el juego
        AnimateIdle();
    }

    // --- MÉTODO NUEVO: Maneja el movimiento continuo ---
    private void AnimateIdle()
    {
        if (visualModel == null) return;

        // Aseguramos que no se acumulen tweens anteriores de posición/rotación
        // IMPORTANTE: No matamos el de escala aquí para que no interfiera con AnimateAppear
        visualModel.transform.DOKill(false);

        // 1. Animación de flotar (Arriba y abajo)
        visualModel.transform
            .DOLocalMoveY(initialLocalPos.y + floatAmplitude, floatDuration)
            .SetLoops(-1, LoopType.Yoyo) // Bucle infinito tipo Yoyo (ida y vuelta)
            .SetEase(Ease.InOutSine);    // Suavizado para que parezca flotar natural

        // 2. Animación de rotación (Eje Y)
        visualModel.transform
            .DOLocalRotate(new Vector3(0, 360, 0), rotateDuration, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Restart) // Bucle infinito reiniciando
            .SetRelative(true)              // Relativo para que sume rotación
            .SetEase(Ease.Linear);          // Velocidad constante
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!triggerCollider.enabled) return;

        KartSpellHandler handler = other.GetComponent<KartSpellHandler>();
        KartProgressTracker tracker = other.GetComponent<KartProgressTracker>();

        if (handler != null && tracker != null)
        {
            // NUEVO: Verificar si ya tiene un spell.
            // Si ya tiene spell, NO hacemos nada (ni damos spell, ni desaparecemos).
            if (handler.HasSpell)
            {
                return;
            }

            // Si NO tiene spell, procedemos normalmente
            SpellData selectedSpell = DetermineSpellByRank(tracker);

            if (selectedSpell != null)
            {
                handler.ReceiveSpell(selectedSpell);
                triggerCollider.enabled = false;
                AnimateVanish();
            }
        }
    }

    private void AnimateVanish()
    {
        // Al desaparecer, mantenemos la rotación/flote visualmente hasta que llegue a escala 0
        visualModel.transform
            .DOScale(0f, vanishDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                visualModel.SetActive(false);
                StartCoroutine(RespawnRoutine());
            });
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnTime);

        visualModel.SetActive(true);

        // Al reaparecer, reiniciamos el movimiento idle para que se mueva mientras crece
        AnimateIdle();
        AnimateAppear();
    }

    private void AnimateAppear()
    {
        visualModel.transform.localScale = Vector3.zero;

        // Nota: Esto corre en paralelo a AnimateIdle (que maneja posición y rotación)
        visualModel.transform
            .DOScale(originalScale, appearDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                triggerCollider.enabled = true;
            });
    }

    private SpellData DetermineSpellByRank(KartProgressTracker tracker)
    {
        if (RaceManager.Instance == null) return spellSpeedBoost;

        int totalPlayers = RaceManager.Instance.Racers.Count;
        int myRank = -1;

        for (int i = 0; i < totalPlayers; i++)
        {
            if (RaceManager.Instance.Racers[i] == tracker)
            {
                myRank = i + 1;
                break;
            }
        }

        float randomVal = Random.value;

        // --- CASO 1: Voy PRIMERO ---
        if (myRank == 1)
        {
            if (randomVal < firstPlace_ShieldChance) return spellShield;
            return spellAreaPush;
        }
        // --- CASO 2: Voy ÚLTIMO ---
        else if (myRank == totalPlayers || (totalPlayers > 2 && myRank >= totalPlayers - 1))
        {
            float threshold1 = lastPlace_StunChance;
            float threshold2 = threshold1 + lastPlace_BoostChance;

            if (randomVal < threshold1) return spellLeaderStun;
            if (randomVal < threshold2) return spellSpeedBoost;
            return spellSlowAhead;
        }
        // --- CASO 3: Voy EN MEDIO ---
        else
        {
            float t1 = mid_SlowChance;
            float t2 = t1 + mid_PushChance;
            float t3 = t2 + mid_BoostChance;

            if (randomVal < t1) return spellSlowAhead;
            if (randomVal < t2) return spellAreaPush;
            if (randomVal < t3) return spellSpeedBoost;
            return spellShield;
        }
    }

    private void OnDestroy()
    {
        if (visualModel != null)
            visualModel.transform.DOKill();
    }
}