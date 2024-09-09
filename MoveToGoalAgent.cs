using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class MoveToGoalAgent : Agent
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Transform environmentTransform;
    [SerializeField] private Material winMaterial;
    [SerializeField] private Material loseMaterial;
    [SerializeField] private Material pickMaterial;
    [SerializeField] private MeshRenderer floorMeshRenderer;
    [SerializeField] private Transform sortingArea;
    [SerializeField] private Transform stackingArea;
    [SerializeField] private Transform deliveryArea;
    [SerializeField] private List<Transform> obstacles;

    private Vector3 initialPosition;
    private Vector3 targetInitialPosition;
    private bool isHoldingTarget = false;
    private string instruction;

    private GameObject heldObject; // Reference to the held object
    private Transform originalParentTransform; // Store the original parent transform

    // Public property to access isHoldingTarget
    public bool IsHoldingTarget
    {
        get { return isHoldingTarget; }
    }

    public override void Initialize()
    {
        // Store the original parent transform
        originalParentTransform = targetTransform.parent;
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("OnEpisodeBegin called");
        ResetAgentAndEnvironment();
    }

    private void ResetAgentAndEnvironment()
    {
        Debug.Log("ResetAgentAndEnvironment called");

        // Detach goal object from agent
        if (targetTransform.parent == transform) // Check if targetTransform is a child of the agent
        {
            targetTransform.SetParent(originalParentTransform); // Reset its parent to the environment
            targetTransform.localPosition = new Vector3(3.0f, 0, Random.Range(-1.5f, 1.5f)); // Set new position
        }

        // Reset agent's position and holding status
        transform.localPosition = Vector3.zero;
        isHoldingTarget = false;

        // Reset obstacles
        foreach (var obstacle in obstacles)
        {
            obstacle.localPosition = new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1.5f, 1.5f));
        }

        // Provide instruction to the agent
        SetInstruction();
    }

    private void SetInstruction()
    {
        // Randomly select an instruction for the agent
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

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition / 10f);
        sensor.AddObservation(targetTransform.localPosition / 10f);
        sensor.AddObservation(sortingArea.localPosition / 10f);
        sensor.AddObservation(stackingArea.localPosition / 10f);
        sensor.AddObservation(deliveryArea.localPosition / 10f);
        sensor.AddObservation(isHoldingTarget ? 1 : 0);

        foreach (var obstacle in obstacles)
        {
            sensor.AddObservation(obstacle.localPosition / 10f);
            sensor.AddObservation(obstacle.localScale / 10f);
            sensor.AddObservation(obstacle.rotation.eulerAngles / 360f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];
        float pickOrPlace = actions.DiscreteActions[0];

        float moveSpeed = 2.0f;
        Vector3 moveVector = new Vector3(moveX, 0, moveZ).normalized;
        transform.localPosition = Vector3.Lerp(transform.localPosition, transform.localPosition + moveVector, Time.deltaTime * moveSpeed);

        // Check if agent follows instruction
        if (pickOrPlace == 1 && !isHoldingTarget) // Pick up
        {
            if (heldObject == null) // Check if there's no held object
            {
                PickUp();
                Debug.Log("Agent has pick up item");
            }
        }
        else if (pickOrPlace == 2 && isHoldingTarget) // Place
        {
            HandlePlacement();
            Debug.Log("Agent has place item");
        }

        // Reward based on distance to target if not holding
        if (!isHoldingTarget)
        {
            float distanceToTarget = Vector3.Distance(transform.localPosition, targetTransform.localPosition);
            SetReward(-distanceToTarget / 100);

            // Provide a small reward for moving towards the target
            float previousDistance = Vector3.Distance(transform.localPosition - new Vector3(moveX, 0, moveZ) * Time.deltaTime * moveSpeed, targetTransform.localPosition);
            if (distanceToTarget < previousDistance)
            {
                SetReward(0.1f);
                Debug.Log("Rewarded - Agent is moving towards the target");
            }
        }
    }

    private void HandlePlacement()
    {
        if (instruction == "sorting" && Vector3.Distance(transform.localPosition, sortingArea.localPosition) < 1.0f)
        {
            // Check if holding object and in correct area for sorting
            isHoldingTarget = false;
            heldObject.transform.SetParent(originalParentTransform);
            heldObject = null;
            Debug.Log("Sorting placement correct, ending episode");
            SetReward(1f);
            EndEpisode();
        }
        else if (instruction == "stacking" && Vector3.Distance(transform.localPosition, stackingArea.localPosition) < 1.0f)
        {
            // Check if holding object and in correct area for stacking
            isHoldingTarget = false;
            heldObject.transform.SetParent(originalParentTransform);
            heldObject = null;
            Debug.Log("Stacking placement correct, ending episode");
            SetReward(1f);
            EndEpisode();
        }
        else if (instruction == "delivery" && Vector3.Distance(transform.localPosition, deliveryArea.localPosition) < 1.0f)
        {
            // Check if holding object and in correct area for delivery
            isHoldingTarget = false;
            heldObject.transform.SetParent(originalParentTransform);
            heldObject = null;
            Debug.Log("Delivery placement correct, ending episode");
            SetReward(1f);
            EndEpisode();
        }
        else
        {
            // Punish agent for not following instruction
            Debug.Log("Placement incorrect, ending episode");
            SetReward(-0.5f);
            EndEpisode(); // End episode if agent takes wrong action while holding the object
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

        continuousActions[0] = Input.GetAxisRaw("Horizontal");
        continuousActions[1] = Input.GetAxisRaw("Vertical");

        if (Input.GetKey(KeyCode.Space))
        {
            discreteActions[0] = 1; // Pick up
        }
        else if (Input.GetKey(KeyCode.Return))
        {
            discreteActions[0] = 2; // Place
        }
        else
        {
            discreteActions[0] = 0; // No action
        }
    }

    private void PickUp()
    {
        if (!isHoldingTarget)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.5f);
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Goal"))
                {
                    isHoldingTarget = true;
                    Debug.Log("Picking up");
                    SetReward(1f);
                    floorMeshRenderer.material = pickMaterial;
                    heldObject = hitCollider.gameObject;
                    heldObject.transform.SetParent(transform);
                    heldObject.transform.localPosition = new Vector3(0, 1, 0);
                    break;
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collider entered: " + other.tag);

        if (other.CompareTag("Goal"))
        {
            Debug.Log("Goal touched");
            if (!isHoldingTarget)
            {
                PickUp();
            }
        }
        else if (other.CompareTag("SortingArea"))
        {
            Debug.Log("Entered Sorting Area");
            if (isHoldingTarget && instruction == "sorting")
            {
                SetReward(1f); // Reward for entering the correct area while holding the object
                floorMeshRenderer.material = winMaterial;
                Debug.Log("Sorting area entered, ending episode");
                EndEpisode();
            }
            else if (isHoldingTarget)
            {
                SetReward(-1f); // Punish for entering the wrong area while holding the object
                floorMeshRenderer.material = loseMaterial;
                Debug.Log("Wrong area entered, ending episode");
                EndEpisode();
            }
        }
        else if (other.CompareTag("StackingArea"))
        {
            Debug.Log("Entered Stacking Area");
            if (isHoldingTarget && instruction == "stacking")
            {
                SetReward(1f); // Reward for entering the correct area while holding the object
                floorMeshRenderer.material = winMaterial;
                Debug.Log("Stacking area entered, ending episode");
                EndEpisode();
            }
            else if (isHoldingTarget)
            {
                SetReward(-1f); // Punish for entering the wrong area while holding the object
                floorMeshRenderer.material = loseMaterial;
                Debug.Log("Wrong area entered, ending episode");
                EndEpisode();
            }
        }
        else if (other.CompareTag("DeliveryArea"))
        {
            Debug.Log("Entered Delivery Area");
            if (isHoldingTarget && instruction == "delivery")
            {
                SetReward(1f); // Reward for entering the correct area while holding the object
                floorMeshRenderer.material = winMaterial;
                Debug.Log("Delivery area entered, ending episode");
                EndEpisode();
            }
            else if (isHoldingTarget)
            {
                SetReward(-1f); // Punish for entering the wrong area while holding the object
                floorMeshRenderer.material = loseMaterial;
                Debug.Log("Wrong area entered, ending episode");
                EndEpisode();
            }
        }
        else if (other.TryGetComponent<Wall>(out Wall wall))
        {
            Debug.Log("Wall hit, ending episode");
            SetReward(-1f);
            floorMeshRenderer.material = loseMaterial;
            EndEpisode();
        }
    }
}
