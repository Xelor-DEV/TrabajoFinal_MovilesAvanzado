using UnityEngine;
using Unity.Cinemachine;

public class CameraSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cm;
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private CinemachineCamera cinemachineCamera;
}
