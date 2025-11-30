using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using DG.Tweening; // Asegúrate de tener DOTween instalado

public class KartSpellHandler : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private KartMovement kartMovement;
    [SerializeField] private KartTackleVictim tackleVictim;
    [SerializeField] private PlayerHUD hud;
    [SerializeField] private KartProgressTracker tracker;

    [Header("VFX References")]
    [SerializeField] private GameObject shield;
    [SerializeField] private GameObject shockwaveVisual; // El objeto visual de la onda
    [SerializeField] private Transform shockwaveOrigin;  // NUEVO: El centro exacto de la explosión/gizmo

    [Header("Settings")]
    [SerializeField] private float shockwaveRadius = 10f;
    [SerializeField] private float popAnimationDuration = 0.5f;

    [Header("Current State")]
    [SerializeField] private SpellData currentSpell;

    private bool hasSpell = false;

    public bool HasSpell => hasSpell;

    // Input System
    public void OnUseItem(InputAction.CallbackContext context)
    {
        if (context.performed && hasSpell && currentSpell != null)
        {
            CastSpell();
        }
    }

    public void ReceiveSpell(SpellData newSpell)
    {
        currentSpell = newSpell;
        hasSpell = true;
        if (hud != null) hud.UpdateSpellIcon(newSpell.icon, true);
    }

    private void CastSpell()
    {
        switch (currentSpell.spellType)
        {
            case SpellType.SpeedBoost:
                StartCoroutine(ApplySpeedBoost(currentSpell.duration, currentSpell.power));
                break;

            case SpellType.LeaderStun:
                CastLeaderStun(currentSpell.duration);
                break;

            case SpellType.Shield:
                StartCoroutine(ApplyShield(currentSpell.duration));
                break;

            case SpellType.SlowAhead:
                CastSlowOnTargetAhead(currentSpell.duration, currentSpell.power);
                break;

            case SpellType.AreaPush:
                CastAreaPush(currentSpell.power);
                break;
        }

        currentSpell = null;
        hasSpell = false;
        if (hud != null) hud.UpdateSpellIcon(null, false);
    }

    // --- LÓGICA DE LOS SPELLS ---

    // 1. VELOCIDAD
    private IEnumerator ApplySpeedBoost(float duration, float multiplier)
    {
        kartMovement.SetSpeedMultiplier(multiplier);
        yield return new WaitForSeconds(duration);
        kartMovement.SetSpeedMultiplier(1.0f);
    }

    // 2. ESCUDO (Pop In / Pop Out)
    private IEnumerator ApplyShield(float duration)
    {
        tackleVictim.SetInvulnerable(true);

        if (shield != null)
        {
            shield.SetActive(true);
            shield.transform.localScale = Vector3.zero;
            shield.transform.DOScale(Vector3.one, popAnimationDuration).SetEase(Ease.OutBack);
        }

        yield return new WaitForSeconds(duration - popAnimationDuration);

        if (shield != null)
        {
            yield return shield.transform.DOScale(Vector3.zero, popAnimationDuration).SetEase(Ease.InBack).WaitForCompletion();
            shield.SetActive(false);
        }

        tackleVictim.SetInvulnerable(false);
    }

    // 3. STUN AL LÍDER
    private void CastLeaderStun(float duration)
    {
        if (RaceManager.Instance == null) return;
        var racers = RaceManager.Instance.Racers;
        if (racers.Count == 0) return;

        KartProgressTracker leader = racers[0];

        if (leader != tracker)
        {
            KartMovement leaderMovement = leader.GetComponent<KartMovement>();
            PlayerHUD leaderHUD = leader.GetComponentInChildren<PlayerHUD>();

            if (leaderMovement != null)
            {
                leaderMovement.StartCoroutine(StunRoutine(leaderMovement, leaderHUD, duration));
            }
        }
    }

    private IEnumerator StunRoutine(KartMovement target, PlayerHUD targetHUD, float time)
    {
        target.DisableInput();
        if (targetHUD != null) targetHUD.ShowCenterMessage("ELECTROCUTED!", time);

        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        if (targetRb != null) targetRb.linearVelocity /= 4f;

        yield return new WaitForSeconds(time);

        target.EnableInput();
    }

    // 4. SLOW AL DE DELANTE
    private void CastSlowOnTargetAhead(float duration, float slowFactor)
    {
        if (RaceManager.Instance == null) return;
        var racers = RaceManager.Instance.Racers;
        int myIndex = racers.IndexOf(tracker);

        if (myIndex > 0)
        {
            KartProgressTracker target = racers[myIndex - 1];
            KartMovement targetMove = target.GetComponent<KartMovement>();

            if (targetMove != null)
            {
                targetMove.StartCoroutine(RemoteSlowRoutine(targetMove, duration, slowFactor));
            }
        }
    }

    private IEnumerator RemoteSlowRoutine(KartMovement target, float time, float factor)
    {
        target.SetSpeedMultiplier(factor);
        yield return new WaitForSeconds(time);
        target.SetSpeedMultiplier(1.0f);
    }

    // 5. AREA PUSH (Usando shockwaveOrigin)
    private void CastAreaPush(float force)
    {
        // Validación de seguridad por si olvidaste asignar el Transform
        Vector3 originPoint = (shockwaveOrigin != null) ? shockwaveOrigin.position : transform.position;

        // Lógica Física usando el punto de origen específico
        Collider[] hits = Physics.OverlapSphere(originPoint, shockwaveRadius);

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            KartTackleVictim victim = hit.GetComponent<KartTackleVictim>();
            if (victim != null)
            {
                // La dirección del empuje es desde el Origen de la onda hacia la víctima
                Vector3 dir = (hit.transform.position - originPoint).normalized;
                victim.ReceiveTackle(dir, force, 5f, 1f);
            }
        }

        // Lógica Visual
        if (shockwaveVisual != null)
        {
            // Opcional: Asegurarse de que el visual esté en la posición del origen
            if (shockwaveOrigin != null) shockwaveVisual.transform.position = shockwaveOrigin.position;

            StartCoroutine(AnimateShockwave());
        }
    }

    private IEnumerator AnimateShockwave()
    {
        shockwaveVisual.SetActive(true);
        shockwaveVisual.transform.localScale = Vector3.zero;

        shockwaveVisual.transform.DOScale(Vector3.one, popAnimationDuration).SetEase(Ease.OutBack);

        yield return new WaitForSeconds(0.5f);

        yield return shockwaveVisual.transform.DOScale(Vector3.zero, popAnimationDuration).SetEase(Ease.InBack).WaitForCompletion();

        shockwaveVisual.SetActive(false);
    }

    // --- GIZMOS MODIFICADO ---
    private void OnDrawGizmosSelected()
    {
        // Solo dibujamos si hay un origen asignado
        if (shockwaveOrigin != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawSphere(shockwaveOrigin.position, shockwaveRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(shockwaveOrigin.position, shockwaveRadius);
        }
    }
}