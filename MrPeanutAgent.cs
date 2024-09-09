using UnityEngine;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class MrPeanutAgent : Agent
{
    public float moveSpeed = 1f;
    private Rigidbody2D rb;
    public Sprite rightSprite, leftSprite, upSprite, downSprite;
    public BoxCollider2D rightCol, leftCol, upCol, downCol;
    public SpriteRenderer sr;
    public GameObject overlayHolder; // Holder to store the collected overlay sprite
    public Vector3 overlaySpriteScale = new Vector3(1f, 1f, 1f);

    // Store initial position
    private Vector3 initialPosition;
    private bool hasOverlay = false; // Flag to check if Mr. Peanut has collected an overlay

    // Target area
    private string targetAreaTag;

    private int maxSteps = 200000;

    // Define tags or names for the target areas
    private string[] areas = new string[] { "StackingArea", "SortingArea", "DeliveryArea" };

    // Timer to track how long Mr. Peanut has been holding the overlay
    private float overlayHoldTime = 0f;
    private const float penaltyInterval = 5f; // Apply a penalty every 5 seconds

    // Timer to track how long Mr. Peanut has been stationary without an overlay
    private float stationaryTime = 0f;
    private const float stationaryPenaltyInterval = 10f; // Apply a penalty every 10 seconds

    // Track the last position to detect if Mr. Peanut is stationary
    private Vector2 lastPosition;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        initialPosition = transform.localPosition;
    }

    // private void Start() 
    // {
    //     Time.timeScale = 0.1f;
    // }

    public override void OnEpisodeBegin()
    {
        rb.velocity = Vector2.zero;
        ResetPosition();
        hasOverlay = false;
        overlayHolder.GetComponent<SpriteRenderer>().sprite = null;
        overlayHoldTime = 0f;
        stationaryTime = 0f;
        lastPosition = transform.localPosition;

        // Randomly select a target area for delivery after collecting the overlay
        SetRandomTargetArea();
    }

    private void ResetPosition()
    {
        float randomOffsetX = Random.Range(-3.5f, 3.5f);
        float randomOffsetY = Random.Range(-1.5f, 1.5f);
        transform.localPosition = new Vector3(
            initialPosition.x + randomOffsetX,
            initialPosition.y + randomOffsetY,
            initialPosition.z
        );
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition.x);
        sensor.AddObservation(transform.localPosition.y);
        sensor.AddObservation(rb.velocity.x);
        sensor.AddObservation(rb.velocity.y);
        sensor.AddObservation(hasOverlay ? 1.0f : 0.0f);

        // Encode the target area into observations
        sensor.AddObservation(targetAreaTag == "StackingArea" ? 1.0f : 0.0f);
        sensor.AddObservation(targetAreaTag == "SortingArea" ? 1.0f : 0.0f);
        sensor.AddObservation(targetAreaTag == "DeliveryArea" ? 1.0f : 0.0f);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float moveX = actionBuffers.ContinuousActions[0];
        float moveY = actionBuffers.ContinuousActions[1];

        Vector2 direction = new Vector2(moveX, moveY).normalized;
        rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);

        UpdateDirectionAndColliders(direction);

        // Small penalty for existing (encourages movement)
        AddReward(-0.01f / maxSteps);

        // If Mr. Peanut is holding the overlay, increment the overlay hold timer
        if (hasOverlay)
        {
            overlayHoldTime += Time.fixedDeltaTime;
            if (overlayHoldTime >= penaltyInterval)
            {
                AddReward(-0.5f / maxSteps); // Apply a small penalty every penaltyInterval seconds
                overlayHoldTime = 0f; // Reset the overlay hold timer
                Debug.Log("Punishing for holding too long");
            }

            // Find the target area position
            GameObject targetAreaObject = GameObject.FindGameObjectWithTag(targetAreaTag);
            if (targetAreaObject != null)
            {
                Vector2 targetPosition = targetAreaObject.transform.position;
                float directionToTarget = Vector2.Dot(direction, (targetPosition - rb.position).normalized);
                AddReward(directionToTarget * 0.1f / maxSteps);
            }
        }

        // If Mr. Peanut is stationary without an overlay, increment the stationary timer
        if (!hasOverlay && lastPosition == (Vector2)transform.localPosition)
        {
            stationaryTime += Time.fixedDeltaTime;
            if (stationaryTime >= stationaryPenaltyInterval)
            {
                AddReward(-0.5f / maxSteps); // Apply a small penalty every stationaryPenaltyInterval seconds
                stationaryTime = 0f; // Reset the stationary timer
                Debug.Log("Punishing for staying stationary too long without overlay");
            }
        }
        else
        {
            // Reset the stationary timer if Mr. Peanut moves
            stationaryTime = 0f;
        }

        // Update last position to the current position for the next check
        lastPosition = transform.localPosition;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            // Increase penalty for hitting walls
            SetReward(-2f / maxSteps);
            EndEpisode();
        }
        else if (collision.gameObject.CompareTag("ShelfTile"))
        {
            if (!hasOverlay)
            {
                ShelfTileController shelfTileController = collision.gameObject.GetComponent<ShelfTileController>();
                if (shelfTileController.isReadyToCollect)
                {
                    CollectOverlaySprite(shelfTileController.gameObject);
                }
                else
                {
                    // Slightly increase the penalty for collecting from an unready tile
                    SetReward(-1.5f / maxSteps);
                    EndEpisode();
                }
            }
        }
        else if (hasOverlay && collision.gameObject.CompareTag(targetAreaTag))
        {
            // Increase the reward for delivering to the correct area
            AddReward(8f);
            Debug.Log("Mr. Peanut has delivered the overlay to " + targetAreaTag);
            EndEpisode();
        }
        else if (hasOverlay && (collision.gameObject.CompareTag("StackingArea") || collision.gameObject.CompareTag("SortingArea") || collision.gameObject.CompareTag("DeliveryArea")))
        {
            // Increase the penalty for delivering to the wrong area
            SetReward(-2f / maxSteps);
            Debug.Log("Mr. Peanut touched the wrong area: " + collision.gameObject.tag);
        }
    }

    private void CollectOverlaySprite(GameObject shelfTile)
    {
        ShelfTileController shelfTileController = shelfTile.GetComponent<ShelfTileController>();
        Transform overlayTransform = shelfTile.transform.Find("OverlaySprite");

        if (overlayTransform != null)
        {
            SpriteRenderer overlaySpriteRenderer = overlayTransform.GetComponent<SpriteRenderer>();
            overlayHolder.GetComponent<SpriteRenderer>().sprite = overlaySpriteRenderer.sprite; 
            overlayHolder.transform.localScale = overlaySpriteScale / 4;
            hasOverlay = true;

            overlaySpriteRenderer.enabled = false; 
            shelfTileController.OnOverlayCollected();

            AddReward(5f);

            // Debug log for the target area
            Debug.Log("Mr. Peanut needs to deliver to " + targetAreaTag);
        }
        else
        {
            SetReward(-1f / maxSteps);
            EndEpisode();
        }
    }

    private void SetRandomTargetArea()
    {
        targetAreaTag = areas[Random.Range(0, areas.Length)];
        Debug.Log("Mr. Peanut's new target area: " + targetAreaTag);
    }

    private void UpdateDirectionAndColliders(Vector2 direction)
    {
        if (Mathf.Abs(direction.y) > Mathf.Abs(direction.x))
        {
            if (direction.y > 0)
            {
                sr.sprite = upSprite;
                upCol.enabled = true;
                downCol.enabled = false;
                rightCol.enabled = false;
                leftCol.enabled = false;
            }
            else
            {
                sr.sprite = downSprite;
                upCol.enabled = false;
                downCol.enabled = true;
                rightCol.enabled = false;
                leftCol.enabled = false;
            }
        }
        else
        {
            if (direction.x > 0)
            {
                sr.sprite = rightSprite;
                upCol.enabled = false;
                downCol.enabled = false;
                rightCol.enabled = true;
                leftCol.enabled = false;
            }
            else
            {
                sr.sprite = leftSprite;
                upCol.enabled = false;
                downCol.enabled = false;
                rightCol.enabled = false;
                leftCol.enabled = true;
            }
        }
    }
}