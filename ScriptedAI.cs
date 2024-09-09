using System.Collections.Generic;
using UnityEngine;

public class ScriptedAI : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform sortingArea;
    [SerializeField] private Transform stackingArea;
    [SerializeField] private Transform deliveryArea;
    [SerializeField] private List<Transform> obstacles;
    [SerializeField] private MoveToGoalAgent agent; // Reference to the agent

    public float speed = 0.5f; // Speed variable

    private bool isHoldingTarget = false;
    private string instruction;

    private GameObject heldObject;
    private Transform originalParentTransform;
    private Vector3[] pathPoints;
    private int currentPointIndex = 0;

    void Start()
    {
        originalParentTransform = targetTransform.parent;
        SetInstruction();
        GeneratePath();
    }

    void Update()
    {
        // Check if the agent is holding the target. If yes, stop moving.
        if (agent != null && agent.IsHoldingTarget)
        {
            return;
        }

        MoveAlongPath();
    }

    private void SetInstruction()
    {
        int instructionIndex = Random.Range(0, 3);
        switch (instructionIndex)
        {
            case 0:
                instruction = "sorting";
                Debug.Log("Move to Sorting area");
                break;
            case 1:
                instruction = "stacking";
                Debug.Log("Move to Stacking area");
                break;
            case 2:
                instruction = "delivery";
                Debug.Log("Move to Delivery area");
                break;
        }
    }

    private void GeneratePath()
    {
        if (!isHoldingTarget)
        {
            pathPoints = new Vector3[] { targetTransform.position };
        }
        else
        {
            switch (instruction)
            {
                case "sorting":
                    pathPoints = new Vector3[] { sortingArea.position };
                    break;
                case "stacking":
                    pathPoints = new Vector3[] { stackingArea.position };
                    break;
                case "delivery":
                    pathPoints = new Vector3[] { deliveryArea.position };
                    break;
            }
        }
    }

    private void MoveAlongPath()
    {
        if (currentPointIndex < pathPoints.Length)
        {
            Vector3 targetPosition = pathPoints[currentPointIndex];
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                currentPointIndex++;
                if (currentPointIndex == pathPoints.Length)
                {
                    if (!isHoldingTarget)
                    {
                        PickUp();
                        GeneratePath();
                        currentPointIndex = 0;
                    }
                    else
                    {
                        Place();
                    }
                }
            }
        }
    }

    private void PickUp()
    {
        // Check if the agent is already holding the object
        if (agent.IsHoldingTarget)
        {
            return;
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.5f);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Goal"))
            {
                isHoldingTarget = true;
                heldObject = hitCollider.gameObject;
                heldObject.transform.SetParent(transform);
                heldObject.transform.localPosition = new Vector3(0, 1, 0);
                break;
            }
        }
    }

    private void Place()
    {
        isHoldingTarget = false;
        heldObject.transform.SetParent(originalParentTransform);
        heldObject.transform.localPosition = new Vector3(3.0f, 0, Random.Range(-1.5f, 1.5f));
        heldObject = null;
        SetInstruction();
        GeneratePath();
        currentPointIndex = 0;
    }
}
