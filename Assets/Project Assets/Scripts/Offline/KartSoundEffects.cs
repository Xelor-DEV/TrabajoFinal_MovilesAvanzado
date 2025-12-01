using UnityEngine;

public class KartSoundEffects : MonoBehaviour
{
    [Header("Referencias a Scripts")]
    [SerializeField] private KartMovement kartMovement;
    [SerializeField] private KartJump kartJump;
    [SerializeField] private KartTackleAttacker kartTackle;
    [SerializeField] private KartProgressTracker kartTracker;

    [Header("Configuración de Sonidos (Índices de Clips)")]
    [Tooltip("Índice en el array sfxClips del AudioManager")]
    [SerializeField] private int jumpClipIndex = 0;
    [SerializeField] private int voidFallClipIndex = 1;
    [SerializeField] private int checkpointClipIndex = 2;
    [SerializeField] private int tackleStartClipIndex = 3;
    [SerializeField] private int tackleHitClipIndex = 4;
    [SerializeField] private int engineRunClipIndex = 5;
    [SerializeField] private int driftClipIndex = 6;

    [Header("Audio Sources PROPIOS (Arrástralos aquí)")]
    [Tooltip("El AudioSource hijo de este Kart que reproducirá el motor")]
    [SerializeField] private AudioSource engineSource;

    [Tooltip("El AudioSource hijo de este Kart que reproducirá el derrape")]
    [SerializeField] private AudioSource driftSource;

    [Header("Umbrales de Activación")]
    [SerializeField] private float minInputForRunSound = 0.1f; // El umbral que pediste
    [SerializeField] private float minInputForTurnSound = 0.2f;

    // Estado interno para evitar llamadas repetitivas (Logic Flags)
    private bool isEnginePlaying = false;
    private bool isDriftPlaying = false;
    private Vector2 currentInput;

    private void Awake()
    {
        if (kartMovement == null) kartMovement = GetComponent<KartMovement>();
        if (kartJump == null) kartJump = GetComponent<KartJump>();
        if (kartTackle == null) kartTackle = GetComponent<KartTackleAttacker>();
        if (kartTracker == null) kartTracker = GetComponent<KartProgressTracker>();
    }

    private void Start()
    {
        // Al inicio, le pedimos al Manager que nos PRESTE el clip de audio, 
        // y se lo asignamos a nuestros sources locales.
        if (AudioManager.Instance != null)
        {
            // Configurar Motor
            AudioClip engineClip = AudioManager.Instance.GetClipByIndex(engineRunClipIndex);
            // Nota: Necesitarás un método GetClipByIndex en tu AudioManager o hacer pública la lista.
            // Si no puedes cambiar el AudioManager, asigna los Clips manualmente en el inspector de este script.

            // Si asignas los clips manualmente en el inspector del AudioSource, puedes borrar estas líneas.
            if (engineClip != null && engineSource != null) engineSource.clip = engineClip;

            // Configurar Derrape
            AudioClip driftClip = AudioManager.Instance.GetClipByIndex(driftClipIndex);
            if (driftClip != null && driftSource != null) driftSource.clip = driftClip;


            if (engineSource) engineSource.loop = true;
            if (driftSource) driftSource.loop = true;
        }
    }

    private void OnEnable()
    {
        if (kartJump != null) kartJump.OnJumpPerformed.AddListener(OnJump);
        if (kartTackle != null)
        {
            kartTackle.OnTackleStarted.AddListener(OnTackleStart);
            kartTackle.OnTargetHit.AddListener(OnTackleHit);
        }
        if (kartTracker != null)
        {
            kartTracker.OnCheckpointCollected.AddListener(OnCheckpoint);
            kartTracker.OnVoidFallDetected.AddListener(OnVoidFall);
        }
        if (kartMovement != null) kartMovement.OnVFXUpdate.AddListener(OnInputUpdate);
    }

    private void OnDisable()
    {
        if (kartJump != null) kartJump.OnJumpPerformed.RemoveListener(OnJump);
        if (kartTackle != null)
        {
            kartTackle.OnTackleStarted.RemoveListener(OnTackleStart);
            kartTackle.OnTargetHit.RemoveListener(OnTackleHit);
        }
        if (kartTracker != null)
        {
            kartTracker.OnCheckpointCollected.RemoveListener(OnCheckpoint);
            kartTracker.OnVoidFallDetected.RemoveListener(OnVoidFall);
        }
        if (kartMovement != null) kartMovement.OnVFXUpdate.RemoveListener(OnInputUpdate);

        if (engineSource != null) engineSource.Stop();
        if (driftSource != null) driftSource.Stop();
    }

    private void Update()
    {
        HandleEngineState();
        HandleDriftState();
    }

    // ---------------- LÓGICA DE ESTADOS (SOLUCIÓN AL PROBLEMA) ----------------

    private void HandleEngineState()
    {
        if (engineSource == null || engineSource.clip == null) return;

        // CAMBIO: En lugar de rb.linearVelocity, usamos el Input Vertical (Acelerar/Frenar)
        // currentInput.y suele ser 1 (Adelante), -1 (Atrás) o 0 (Nada)
        float throttleInput = Mathf.Abs(currentInput.y);

        // Si estamos pulsando el botón de acelerar más allá del umbral
        if (throttleInput > minInputForRunSound)
        {
            if (!isEnginePlaying)
            {
                engineSource.Play();
                isEnginePlaying = true;
            }
        }
        else
        {
            // Si soltamos el botón, paramos el sonido inmediatamente
            if (isEnginePlaying)
            {
                engineSource.Stop();
                isEnginePlaying = false;
            }
        }
    }

    private void HandleDriftState()
    {
        if (driftSource == null || driftSource.clip == null) return;

        // CAMBIO: Para saber si nos movemos, miramos si hay INPUT de aceleración, no velocidad física.
        bool isTryingToMove = Mathf.Abs(currentInput.y) > minInputForRunSound;

        // Input de giro (izquierda/derecha)
        float turnInput = Mathf.Abs(currentInput.x);

        // Suena si: Estamos girando el volante Y estamos pisando el acelerador
        if (turnInput > minInputForTurnSound && isTryingToMove)
        {
            if (!isDriftPlaying)
            {
                driftSource.Play();
                isDriftPlaying = true;
            }
        }
        else
        {
            if (isDriftPlaying)
            {
                driftSource.Stop();
                isDriftPlaying = false;
            }
        }
    }

    // Este método debe ser llamado desde el evento OnVFXUpdate del KartMovement
    public void OnInputUpdate(Vector2 input)
    {
        currentInput = input;
    }

    // ---------------- EVENTOS ONE SHOT ----------------

    private void OnJump()
    {
        PlayOneShot(jumpClipIndex);
    }

    private void OnTackleStart() => PlayOneShot(tackleStartClipIndex);
    private void OnTackleHit() => PlayOneShot(tackleHitClipIndex);
    private void OnCheckpoint() => PlayOneShot(checkpointClipIndex);
    private void OnVoidFall() => PlayOneShot(voidFallClipIndex);

    private void PlayOneShot(int index)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(index);
        }
    }
}