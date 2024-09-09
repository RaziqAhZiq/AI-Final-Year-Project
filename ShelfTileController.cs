using UnityEngine;
using System.Collections;

public class ShelfTileController : MonoBehaviour
{
    public GameObject overlaySprite; // Reference to the OverlaySprite child object
    public float minLoadTime = 6f; // Minimum time it takes to load the OverlaySprite
    public float maxLoadTime = 25f; // Maximum time it takes to load the OverlaySprite
    public bool isReadyToCollect = false; // Tracks if the OverlaySprite is ready to be collected

    private BoxCollider2D shelfTileCollider;

    private void Start()
    {
        shelfTileCollider = GetComponent<BoxCollider2D>();

        if (shelfTileCollider == null)
        {
            Debug.LogError("ShelfTile does not have a BoxCollider2D.");
            return;
        }

        if (overlaySprite != null)
        {
            overlaySprite.SetActive(false); // Initially hide the OverlaySprite
            Debug.Log("Shelf Tile script started!");
        }
        StartCoroutine(LoadOverlaySprite());
    }

    public IEnumerator LoadOverlaySprite()
    {
        float loadTime = Random.Range(minLoadTime, maxLoadTime);
        yield return new WaitForSeconds(loadTime);
        
        overlaySprite.SetActive(true);
        SpriteRenderer overlaySpriteRenderer = overlaySprite.GetComponent<SpriteRenderer>();
        if (overlaySpriteRenderer != null)
        {
            overlaySpriteRenderer.enabled = true; // Re-enable the SpriteRenderer to show the sprite again
        }
        
        isReadyToCollect = true;
        Debug.Log("OverlaySprite is now active and ready to be collected.");
    }

    public void OnOverlayCollected()
    {
        // When the overlay sprite is collected, reset the state and start loading a new overlay
        Debug.Log("OnOverlayCollected() running");
        isReadyToCollect = false;
        overlaySprite.SetActive(false); // Hide the current overlay sprite
        StartCoroutine(LoadOverlaySprite()); // Start the loading process for the next overlay
    }
}
