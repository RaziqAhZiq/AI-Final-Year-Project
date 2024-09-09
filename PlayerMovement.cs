using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 2f;

    public Sprite rightSprite;
    public Sprite leftSprite;
    public Sprite upSprite;
    public Sprite downSprite;

    public BoxCollider2D rightCol;
    public BoxCollider2D leftCol;
    public BoxCollider2D upCol;
    public BoxCollider2D downCol;

    public SpriteRenderer sr;
    public string horizontalAxis;
    public string verticalAxis;

    private Vector2 movement = Vector2.zero;
    private Rigidbody2D rb;

    public GameObject playerOverlaySprite; // Reference to the player's OverlaySprite child object
    private bool isCarryingOverlay = false; // Tracks if the player is carrying an OverlaySprite

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        movement.x = Input.GetAxisRaw(horizontalAxis);
        movement.y = Input.GetAxisRaw(verticalAxis);
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.deltaTime);

        if (movement.y > 0.1f)
        {
            sr.sprite = upSprite;
            upCol.enabled = true;
            downCol.enabled = false;
            rightCol.enabled = false;
            leftCol.enabled = false;
        }
        else if (movement.y < -0.1f)
        {
            sr.sprite = downSprite;
            upCol.enabled = false;
            downCol.enabled = true;
            rightCol.enabled = false;
            leftCol.enabled = false;
        }

        if (movement.x > 0.1f)
        {
            sr.sprite = rightSprite;
            upCol.enabled = false;
            downCol.enabled = false;
            rightCol.enabled = true;
            leftCol.enabled = false;
        }
        else if (movement.x < -0.1f)
        {
            sr.sprite = leftSprite;
            upCol.enabled = false;
            downCol.enabled = false;
            rightCol.enabled = false;
            leftCol.enabled = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the player collides with a ShelfTile that has an OverlaySprite ready to be collected
        if (collision.gameObject.CompareTag("ShelfTile") && !isCarryingOverlay)
        {
            ShelfTileController shelfTile = collision.GetComponent<ShelfTileController>();
            if (shelfTile != null && shelfTile.isReadyToCollect)
            {
                CollectOverlay(shelfTile);
            }
        }
    }

    private void CollectOverlay(ShelfTileController shelfTile)
    {
        // Move the overlay sprite from the shelf tile to the player
        GameObject collectedOverlay = shelfTile.overlaySprite;

        if (collectedOverlay != null)
        {
            // Get the sprite renderer and transform of the collected overlay
            SpriteRenderer collectedSpriteRenderer = collectedOverlay.GetComponent<SpriteRenderer>();
            Transform collectedTransform = collectedOverlay.transform;

            if (collectedSpriteRenderer != null)
            {
                // Update the player's overlay sprite renderer with the collected sprite
                SpriteRenderer playerOverlaySpriteRenderer = playerOverlaySprite.GetComponent<SpriteRenderer>();
                playerOverlaySpriteRenderer.sprite = collectedSpriteRenderer.sprite;

                // Match the scale of the collected sprite
                playerOverlaySpriteRenderer.transform.localScale = collectedTransform.localScale/5;
            }

            // Hide the original collected overlay sprite to avoid duplication
            collectedOverlay.SetActive(false);

            isCarryingOverlay = true; // The player is now carrying an OverlaySprite
        }
    }

    public void DropOverlay()
    {
        // Logic for dropping the overlay sprite, allowing the player to collect another one
        if (playerOverlaySprite.transform.childCount > 0)
        {
            Transform overlay = playerOverlaySprite.transform.GetChild(0);
            overlay.SetParent(null); // Detach it from the player
            overlay.gameObject.SetActive(false); // Hide it

            // Clear the player's overlay sprite renderer
            SpriteRenderer playerOverlaySpriteRenderer = playerOverlaySprite.GetComponent<SpriteRenderer>();
            playerOverlaySpriteRenderer.sprite = null;

            isCarryingOverlay = false; // The player can now collect another overlay sprite
        }
    }
}