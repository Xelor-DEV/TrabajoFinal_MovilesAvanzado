using UnityEngine;

[CreateAssetMenu(fileName = "KartStats", menuName = "Kart/Kart Stats")]
public class KartStats : ScriptableObject
{
    [Header("Physics Settings")]
    public float mass = 100f;
    public float linearDrag = 0.5f;
    public float angularDrag = 2f;

    [Header("Movement Settings")]
    public float maxSpeed = 25f;
    public float acceleration = 12f;
    public float deceleration = 8f;
    public float steerSpeed = 3f;

    [Header("Steering Settings")]
    public float minSteerSpeed = 1f;
    public float minInputThreshold = 0.1f;

    [Header("Visual Rotation Settings")]
    public float maxTiltZ = 35f;
    public float maxTiltX = 20f;
    public float visualRotationSpeed = 10f;

    [Header("Boost Settings")]
    public float boostTimeThreshold = 15f;
    public float boostActivationInput = 0.8f;
    public float boostDeactivationInput = 0.8f;
    public float boostedMaxSpeed = 35f;
    public float boostedSteerSpeed = 1.5f;
    public float boostedDeceleration = 2f;
}