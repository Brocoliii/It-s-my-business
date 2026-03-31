using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    
    [SerializeField] private CameraController cameraController;

    private void Update()
    {
        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            if (cameraController != null)
            {
                cameraController.PanCamera(mouseDelta);
            }
        }
    }
}