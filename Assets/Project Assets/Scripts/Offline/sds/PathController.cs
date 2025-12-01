using UnityEngine;

public class PathController : MonoBehaviour
{
    [Header("Object References")]
    [Tooltip("Drag the object blocking the LEFT path here")]
    [SerializeField] private GameObject leftObstacle;

    [Tooltip("Drag the object blocking the RIGHT path here")]
    [SerializeField] private GameObject rightObstacle;

    void Start()
    {
        // execute logic when the game starts
        RandomizePath();
    }

    public void RandomizePath()
    {
        // Generate a random number: 0 or 1
        int decision = Random.Range(0, 2);

        if (decision == 0)
        {
            // CASE A: Block Left, Open Right
            leftObstacle.SetActive(true);   // Active (Blocks path)
            rightObstacle.SetActive(false); // Inactive (Open path)
            Debug.Log("Path: Left Blocked / Right Open");
        }
        else
        {
            // CASE B: Open Left, Block Right
            leftObstacle.SetActive(false);  // Inactive (Open path)
            rightObstacle.SetActive(true);  // Active (Blocks path)
            Debug.Log("Path: Left Open / Right Blocked");
        }
    }
}