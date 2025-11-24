using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class KartVFX : MonoBehaviour
{
    [Header("Nitro VFX")]
    [SerializeField] private ParticleSystem nitroLeftParticles;
    [SerializeField] private ParticleSystem nitroRightParticles;
    [SerializeField] private Light nitroLight;
    [SerializeField] private float lightFadeSpeed = 2f;

    [Header("Drift VFX")]
    [SerializeField] private TrailRenderer driftLeftTrail;
    [SerializeField] private TrailRenderer driftRightTrail;

    [Header("Speed VFX")]
    [SerializeField] private ParticleSystem speedLeftParticles;
    [SerializeField] private ParticleSystem speedRightParticles;

    [Header("Kart Stats Reference")]
    [SerializeField] private KartStats kartStats;

    private float originalLightIntensity;
    private Coroutine nitroLightCoroutine;

    // Control de estado de partículas
    private bool nitroParticlesShouldBeEmitting = false;
    private bool speedParticlesShouldBeEmitting = false;
    private bool driftTrailsShouldBeEmitting = false;

    // Módulos de emisión
    private ParticleSystem.EmissionModule nitroLeftEmission;
    private ParticleSystem.EmissionModule nitroRightEmission;
    private ParticleSystem.EmissionModule speedLeftEmission;
    private ParticleSystem.EmissionModule speedRightEmission;

    private void Awake()
    {
        // Store original light intensity and disable light
        if (nitroLight != null)
        {
            originalLightIntensity = nitroLight.intensity;
            nitroLight.intensity = 0f;
        }

        // Initialize particle emission modules
        InitializeParticleSystems();

        // Disable all effects on start
        SetNitroParticlesEmission(false);
        SetDriftTrails(false);
        SetSpeedParticlesEmission(false);
    }

    private void InitializeParticleSystems()
    {
        // Nitro particles
        if (nitroLeftParticles != null)
        {
            nitroLeftEmission = nitroLeftParticles.emission;
            nitroLeftEmission.enabled = false;
            if (!nitroLeftParticles.isPlaying) nitroLeftParticles.Play();
        }

        if (nitroRightParticles != null)
        {
            nitroRightEmission = nitroRightParticles.emission;
            nitroRightEmission.enabled = false;
            if (!nitroRightParticles.isPlaying) nitroRightParticles.Play();
        }

        // Speed particles
        if (speedLeftParticles != null)
        {
            speedLeftEmission = speedLeftParticles.emission;
            speedLeftEmission.enabled = false;
            if (!speedLeftParticles.isPlaying) speedLeftParticles.Play();
        }

        if (speedRightParticles != null)
        {
            speedRightEmission = speedRightParticles.emission;
            speedRightEmission.enabled = false;
            if (!speedRightParticles.isPlaying) speedRightParticles.Play();
        }
    }

    // Método para suscribir via inspector al UnityEvent OnVFXUpdate del KartMovement
    public void HandleVFXData(Vector2 input)
    {
        HandleDriftTrails(input.x);
        HandleSpeedParticles(input.y);
    }

    private void HandleDriftTrails(float inputX)
    {
        bool shouldDrift = Mathf.Abs(inputX) > kartStats.minInputThreshold;

        if (shouldDrift != driftTrailsShouldBeEmitting)
        {
            driftTrailsShouldBeEmitting = shouldDrift;
            SetDriftTrails(shouldDrift);
        }
    }

    private void HandleSpeedParticles(float inputY)
    {
        bool shouldEmitSpeed = Mathf.Abs(inputY) >= kartStats.speedParticlesInputThreshold;

        if (shouldEmitSpeed != speedParticlesShouldBeEmitting)
        {
            speedParticlesShouldBeEmitting = shouldEmitSpeed;
            SetSpeedParticlesEmission(shouldEmitSpeed);
        }
    }

    // Método para activar/desactivar nitro (para uso futuro)
    public void ActivateNitro()
    {
        SetNitroParticlesEmission(true);
        if (nitroLight != null)
        {
            if (nitroLightCoroutine != null) StopCoroutine(nitroLightCoroutine);
            nitroLightCoroutine = StartCoroutine(ChangeLightIntensity(originalLightIntensity));
        }
    }

    // Método para desactivar nitro (para uso futuro)
    public void DeactivateNitro()
    {
        SetNitroParticlesEmission(false);
        if (nitroLight != null)
        {
            if (nitroLightCoroutine != null) StopCoroutine(nitroLightCoroutine);
            nitroLightCoroutine = StartCoroutine(ChangeLightIntensity(0f));
        }
    }

    private void SetNitroParticlesEmission(bool shouldEmit)
    {
        // Only change state if it's different from current state
        if (shouldEmit == nitroParticlesShouldBeEmitting) return;

        nitroParticlesShouldBeEmitting = shouldEmit;

        if (nitroLeftParticles != null)
        {
            nitroLeftEmission.enabled = shouldEmit;
        }

        if (nitroRightParticles != null)
        {
            nitroRightEmission.enabled = shouldEmit;
        }
    }

    private void SetDriftTrails(bool active)
    {
        if (driftLeftTrail != null) driftLeftTrail.emitting = active;
        if (driftRightTrail != null) driftRightTrail.emitting = active;
    }

    private void SetSpeedParticlesEmission(bool shouldEmit)
    {
        if (speedLeftParticles != null)
        {
            speedLeftEmission.enabled = shouldEmit;
        }

        if (speedRightParticles != null)
        {
            speedRightEmission.enabled = shouldEmit;
        }
    }

    private IEnumerator ChangeLightIntensity(float targetIntensity)
    {
        if (nitroLight == null) yield break;

        float startIntensity = nitroLight.intensity;
        float elapsedTime = 0f;

        while (elapsedTime < 1f)
        {
            nitroLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, elapsedTime);
            elapsedTime += Time.deltaTime * lightFadeSpeed;
            yield return null;
        }

        nitroLight.intensity = targetIntensity;
    }
}