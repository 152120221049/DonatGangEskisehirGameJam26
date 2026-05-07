using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class Door : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    public float openSpeed = 2f;
    
    private bool isOpen = false;
    private bool isMoving = false;

    public void Interact(GameObject player)
    {
        // Only open if it's currently closed and not already moving
        if (!isOpen && !isMoving)
        {
            StartCoroutine(OpenDoorSequence());
        }
    }

    private IEnumerator OpenDoorSequence()
    {
        isMoving = true;
        isOpen = true;
        
        Vector3 startPos = transform.position;
        
        // Dynamically calculate the door's height based on its collider
        float doorHeight = GetComponent<Collider>().bounds.size.y;
        
        // Target position is straight up, exactly the height of the door
        Vector3 endPos = startPos + Vector3.up * doorHeight;
        
        float fraction = 0f;
        while (fraction < 1f)
        {
            fraction += Time.deltaTime * openSpeed;
            transform.position = Vector3.Lerp(startPos, endPos, fraction);
            yield return null;
        }
        
        transform.position = endPos;
        isMoving = false;
    }
}
