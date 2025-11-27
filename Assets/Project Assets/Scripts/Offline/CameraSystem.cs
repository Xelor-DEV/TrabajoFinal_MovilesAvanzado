using UnityEngine;
using Unity.Cinemachine;

public class CameraSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cm;
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private Canvas canvas;

    public Camera CM => cm;
    public CinemachineBrain CinemachineBrain => cinemachineBrain;
    public CinemachineCamera CinemachineCamera => cinemachineCamera;
    public Canvas Canvas => canvas;
}
