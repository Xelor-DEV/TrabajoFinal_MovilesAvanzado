using UnityEngine;

[CreateAssetMenu(fileName = "KartStats", menuName = "Kart/Kart Stats")]
public class KartStats : ScriptableObject
{
    [Header("Movement Settings")]
    public float maxSpeed = 15f;
    public float acceleration = 5f;
    public float deceleration = 3f;
    public float drag = 0.95f;
    public float steerSpeed = 3f;

    [Header("Visual Rotation Settings")]
    public float maxTiltZ = 35f;
    public float maxTiltX = 20f;
    public float visualRotationSpeed = 8f;
}