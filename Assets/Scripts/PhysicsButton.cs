using UnityEngine;
using UnityEngine.Events;

public class PhysicsButton : MonoBehaviour
{
    [Header("Settings")]
    public float cooldown = 1f;
    private float timer = 0f;

    [Header("Events")]
    public UnityEvent<GameObject> OnButtonPressed; // Passes the object that hit it

    private void OnCollisionEnter(Collision collision)
    {
        if (timer > 0) return;

        // Trigger the button
        OnButtonPressed?.Invoke(collision.gameObject);
        timer = cooldown;
        
        Debug.Log($"[PhysicsButton] Button hit by {collision.gameObject.name}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (timer > 0) return;

        // Also detect triggers (for the returning book which becomes a trigger)
        OnButtonPressed?.Invoke(other.gameObject);
        timer = cooldown;
        
        Debug.Log($"[PhysicsButton] Button trigger hit by {other.gameObject.name}");
    }

    private void Update()
    {
        if (timer > 0) timer -= Time.deltaTime;
    }
}
