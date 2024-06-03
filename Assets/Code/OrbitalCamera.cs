using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitalCamera : MonoBehaviour, InputSystem_Actions.ICameraActions {

    public float SpeedRotateX = 5 * 0.05f;
    public float SpeedRotateY = 2.4f * 0.05f;
    public float SpeedPan = 0.5f;
    public float SpeedZoom = 10f;

    public float Distance;

    public Transform MainCamera;
    public Transform PivotArm;

    InputSystem_Actions.CameraActions input;

    private void Start() {
        Distance = -MainCamera.localPosition.z;

        input = new InputSystem_Actions().Camera;
        input.SetCallbacks(this);
        input.Enable();
    }

    private void OnDestroy() {
        input.Disable();
    }

    public void OnCameraLook(InputAction.CallbackContext context) {
        var rotate = context.ReadValue<Vector2>();
        this.transform.Rotate(this.transform.up, rotate.x * SpeedRotateX, Space.World);
        PivotArm.Rotate(PivotArm.right, -rotate.y * SpeedRotateY, Space.World);
        PivotArm.localEulerAngles = new Vector3(ClampAngle(PivotArm.localEulerAngles.x, -80, 80), 0, 0);
    }

    public void OnCameraPan(InputAction.CallbackContext context) {
        var pan = context.ReadValue<Vector2>();
        this.transform.Translate(-pan.x * SpeedPan * Time.unscaledDeltaTime * MainCamera.right, Space.World);
        this.transform.Translate(-pan.y * SpeedPan * Time.unscaledDeltaTime * MainCamera.up, Space.World);
    }

    public void OnCameraZoom(InputAction.CallbackContext context) {
        var delta = Mathf.Clamp(context.ReadValue<float>(), -1, 1); // either 120 or 1?
        var zoom = delta * Distance * SpeedZoom * Time.unscaledDeltaTime;
        Distance = Mathf.Clamp(Distance + zoom, 1, 100);
        MainCamera.transform.localPosition = new(0, 0, -Distance);
    }

    private float ClampAngle(float angle, float min, float max) {
        while (angle > 180) {
            angle -= 360;
        }
        while (angle < -180) {
            angle += 360;
        }
        return Mathf.Clamp(angle, min, max);
    }

}
