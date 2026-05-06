using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    // Increased default sensitivity for the New Input System's pixel delta
    public float mouseSensitivity = 10f; 
    public Transform playerBody;

    private float xRotation = 0f;

    void Start()
    {
        // Lock the cursor to the center of the screen and hide it
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        // In the new Input System, Mouse.current.delta is raw pixel delta.
        // We use a sensitivity multiplier to make it feel right.
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Rotate the camera around its X axis (look up and down)
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        
        // Rotate the player body around its Y axis (look left and right)
        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
        
        // Re-lock cursor if it escapes (optional, helpful for debugging)
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
