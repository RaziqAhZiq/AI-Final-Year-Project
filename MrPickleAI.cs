using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MrPickleAI : MonoBehaviour
{
    private CustomLogger customLogger;
    private float cumulativeReward = 0f;
    public float moveSpeed = 2f;
    public Node startNode;

    public Sprite rightSprite, leftSprite, upSprite, downSprite;
    public BoxCollider2D rightCol, leftCol, upCol, downCol;
    public SpriteRenderer sr;
    public GameObject overlayHolder; // Holder to store the collected overlay sprite
    public Vector3 overlaySpriteScale = new Vector3(1f, 1f, 1f);

    private Node currentNode;
    private Node targetNode;
    private Rigidbody2D rb;

    private bool hasOverlay = false; // Flag to check if Mr. Pickle has collected an overlay
    private string targetArea; // The target area Mr. Pickle should go to

    // Define tags or names for the target areas
    private string[] areas = new string[] { "StackingArea", "DeliveryArea", "SortingArea" };

    // Timer to track how long Mr. Pickle has been holding the overlay
    private float overlayHoldTime = 0f;
    private const float penaltyInterval = 5f; // Apply a penalty every 5 seconds
    private int maxSteps = 200000;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentNode = startNode;
        SetRandomTargetArea(); // Set a random target area for Mr. Pickle
        MoveToNextNode();

        // Find the CustomLogger in the scene
        customLogger = FindObjectOfType<CustomLogger>();
        if (customLogger == null)
        {
            Debug.LogError("CustomLogger is not found in the scene. Please ensure a GameObject with CustomLogger script is present.");
        }
    }

    private void Update()
    {
        if (targetNode != null)
        {
            MoveTowardsTarget();
            float reward = 0.1f; // Small reward for moving
            cumulativeReward += reward;
            customLogger.LogReward(reward, "MrPickle");
        }
    }

    private void MoveTowardsTarget()
    {
        Vector2 direction = ((Vector2)targetNode.transform.position - rb.position).normalized;
        rb.MovePosition(rb.position + direction * moveSpeed * Time.deltaTime);

        UpdateDirectionAndColliders(direction);

        if (Vector2.Distance(rb.position, targetNode.transform.position) < 0.1f)
        {
            currentNode = targetNode;
            MoveToNextNode();
        }

        // Penalize for holding the overlay too long
        if (hasOverlay)
        {
            overlayHoldTime += Time.deltaTime;
            if (overlayHoldTime >= penaltyInterval)
            {
                float penalty = -0.5f / maxSteps;
                cumulativeReward += penalty;
                customLogger.LogReward(penalty, "MrPickle");
                overlayHoldTime = 0f;
                Debug.Log("Punishing for holding too long");
            }
        }
    }

    private void MoveToNextNode()
    {
        if (currentNode.connectedNodes.Count > 0)
        {
            targetNode = currentNode.connectedNodes[Random.Range(0, currentNode.connectedNodes.Count)];
        }
        else
        {
            targetNode = null;
            Debug.Log("No more connected nodes to move to.");
        }
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("ShelfTile"))
        {
            if (!hasOverlay) // Check if Mr. Pickle already has an overlay
            {
                CollectOverlaySprite(collision.gameObject);
            }
            else
            {
                Debug.Log("Mr. Pickle already has an overlay. Ignoring other overlay sprites.");
                customLogger.LogReward(-0.2f, "MrPickle");
            }
        }
        else if (collision.gameObject.CompareTag(targetArea))
        {
            if (hasOverlay)
            {
                RemoveOverlaySprite(); // Remove the overlay sprite if Mr. Pickle reaches the correct area
                Debug.Log("Mr. Pickle has delivered the overlay to " + targetArea);
                float reward = 8f;
                cumulativeReward += reward;
                customLogger.LogReward(reward, "MrPickle");
                customLogger.EndEpisode(cumulativeReward, "MrPickle"); // Log end of the episode
                cumulativeReward = 0f; // Reset for the next episode
                SetRandomTargetArea(); // Set a new random target area for Mr. Pickle
            }
        }
        // Penalize for hitting a wall
        else if (collision.gameObject.CompareTag("Wall"))
        {
            float penalty = -2f / maxSteps;
            cumulativeReward += penalty;
            customLogger.LogReward(penalty, "MrPickle");
            Debug.Log("Mr. Pickle hit a wall.");
        }
    }

    private void CollectOverlaySprite(GameObject shelfTile)
    {
        ShelfTileController shelfTileController = shelfTile.GetComponent<ShelfTileController>();
        if (shelfTileController != null && shelfTileController.isReadyToCollect)
        {
            Transform overlayTransform = shelfTile.transform.Find("OverlaySprite");
            if (overlayTransform != null)
            {
                SpriteRenderer overlaySpriteRenderer = overlayTransform.GetComponent<SpriteRenderer>();
                if (overlaySpriteRenderer != null && overlaySpriteRenderer.sprite != null)
                {
                    overlayHolder.GetComponent<SpriteRenderer>().sprite = overlaySpriteRenderer.sprite; // Store the collected sprite
                    overlayHolder.transform.localScale = overlaySpriteScale / 2;
                    hasOverlay = true; // Set flag indicating Mr. Pickle has collected an overlay
                    Debug.Log("Collected Overlay Sprite: " + overlaySpriteRenderer.sprite.name);
                    float reward = 5f;
                    cumulativeReward += reward;
                    customLogger.LogReward(reward, "MrPickle");

                    overlaySpriteRenderer.enabled = false; // Hide the overlay sprite by disabling its SpriteRenderer

                    shelfTileController.OnOverlayCollected(); // Notify the shelf tile that the overlay has been collected
                }
                else
                {
                    Debug.LogWarning("No valid SpriteRenderer or Sprite found on OverlaySprite child.");
                }
            }
            else
            {
                Debug.LogWarning("No child named 'OverlaySprite' found on ShelfTile.");
            }
        }
        else
        {
            Debug.LogWarning("ShelfTileController is not found or the overlay is not ready to collect.");
            customLogger.LogReward(-0.2f / maxSteps, "MrPickle");
        }
    }

    private void RemoveOverlaySprite()
    {
        overlayHolder.GetComponent<SpriteRenderer>().sprite = null; // Remove the sprite from the holder
        hasOverlay = false; // Reset the flag
    }

    private void SetRandomTargetArea()
    {
        targetArea = areas[Random.Range(0, areas.Length)];
        Debug.Log("Mr. Pickle's new target area: " + targetArea);
    }
}
