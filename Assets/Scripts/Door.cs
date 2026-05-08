using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class Door : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    public float openSpeed = 2f;
    public GameObject openParticlePrefab;
    
    [Header("Audio")]
    public AudioClip openClip;
    public AudioClip rockSlideClip;
    public AudioClip closeClip;
    private AudioSource audioSource;
    
    private bool isOpen = false;
    private bool isMoving = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D Sound
    }

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
        
        // Spawn the particle effect at the ground level (bottom of the collider)
        if (openParticlePrefab != null)
        {
            Vector3 groundLevelPos = new Vector3(startPos.x, GetComponent<Collider>().bounds.min.y, startPos.z);
            GameObject fx = Instantiate(openParticlePrefab, groundLevelPos, Quaternion.identity);
            
            // Scale the particle to match the door's width/depth (ignoring the door's height if you want the dust flat, or just copy full scale)
            fx.transform.localScale = transform.localScale;
            
            Destroy(fx, 4f); // Auto cleanup after 4 seconds
        }
        
        if (openClip != null) audioSource.PlayOneShot(openClip);
        if (rockSlideClip != null)
        {
            audioSource.clip = rockSlideClip;
            audioSource.loop = true;
            audioSource.Play();
        }

        float fraction = 0f;
        while (fraction < 1f)
        {
            fraction += Time.deltaTime * openSpeed;
            transform.position = Vector3.Lerp(startPos, endPos, fraction);
            yield return null;
        }
        
        if (rockSlideClip != null) audioSource.Stop();
        if (closeClip != null) audioSource.PlayOneShot(closeClip);

        transform.position = endPos;
        isMoving = false;
    }
}
