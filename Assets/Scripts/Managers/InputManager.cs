using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class InputManager : MonoBehaviour {
  public static InputManager Instance { get; private set; }

  public bool mouseActive = true;
  [System.Serializable]
  public struct CameraPreset {
    public Vector3 position;
    public Quaternion rotation;
  }

  public bool lockUserInput = false;

  private void Awake() {
    if (Instance == null) {
      Instance = this;
      DontDestroyOnLoad(gameObject);
    } else {
      Destroy(gameObject);
    }
  }

  private Vector2 _lastMousePosition;
  private bool _isDragging = false;

  void Start() {}

  void Update() {
    HandleInput();
  }

  private void Handle3DModeMouseInput() {
    if (Input.GetMouseButton(0)) {
      CameraController.Instance.OrbitCamera(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

    } else if (Input.GetMouseButton(1)) {
      CameraController.Instance.RotateCamera(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    }
  }

  private void HandleInput() {
    if (!lockUserInput) {
      HandleLockableInput();
    }
    HandleNonLockableInput();
  }

  void Handle3DModeScrollWheelInput() {
    if (Input.GetAxis("Mouse ScrollWheel") != 0) {
      CameraController.Instance.ZoomCamera(Input.GetAxis("Mouse ScrollWheel") * 500);
    }
  }

  void HandleTacticalModeScrollWheelInput() {
    if (Input.GetAxis("Mouse ScrollWheel") != 0) {
      TacticalPanelController.Instance.ZoomIn(Input.GetAxis("Mouse ScrollWheel") * 0.1f);
    }
  }

  private void HandleTacticalModeMouseInput() {
    // Start drag on right mouse button
    if (Input.GetMouseButtonDown(1)) {
      _isDragging = true;
      _lastMousePosition = Input.mousePosition;
    }
    // End drag when button released
    else if (Input.GetMouseButtonUp(1)) {
      _isDragging = false;
    }

    // Handle dragging
    if (_isDragging) {
      Vector2 currentMousePos = Input.mousePosition;
      Vector2 delta = currentMousePos - _lastMousePosition;
      TacticalPanelController.Instance.Pan(delta);
      _lastMousePosition = currentMousePos;
    }
  }

  void Handle3DModeLockableInput() {
    if (Input.GetKey(KeyCode.LeftShift)) {
      CameraController.Instance.SetCameraSpeed(CameraController.Instance.GetCameraSpeedMax());
    } else {
      CameraController.Instance.SetCameraSpeed(CameraController.Instance.GetCameraSpeedNormal());
    }

    // Translational movement.
    if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) {
      CameraController.Instance.TranslateCamera(CameraController.TranslationInput.Forward);
    }
    if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) {
      CameraController.Instance.TranslateCamera(CameraController.TranslationInput.Left);
    }
    if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) {
      CameraController.Instance.TranslateCamera(CameraController.TranslationInput.Back);
    }
    if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) {
      CameraController.Instance.TranslateCamera(CameraController.TranslationInput.Right);
    }
    if (Input.GetKey(KeyCode.Q)) {
      CameraController.Instance.TranslateCamera(CameraController.TranslationInput.Up);
    }
    if (Input.GetKey(KeyCode.E)) {
      CameraController.Instance.TranslateCamera(CameraController.TranslationInput.Down);
    }
  }

  void HandleTacticalModeLockableInput() {
    // Handle keyboard input for panning
    Vector2 keyboardPanDirection = Vector2.zero;

    if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) {
      keyboardPanDirection.y += -1;
    }
    if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) {
      keyboardPanDirection.y += 1;
    }
    if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) {
      keyboardPanDirection.x += -1;
    }
    if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) {
      keyboardPanDirection.x += 1;
    }

    if (Input.GetKeyDown(KeyCode.Q)) {
      TacticalPanelController.Instance.CycleRangeUp();
    }
    if (Input.GetKeyDown(KeyCode.E)) {
      TacticalPanelController.Instance.CycleRangeDown();
    }

    if (keyboardPanDirection != Vector2.zero) {
      TacticalPanelController.Instance.PanWithKeyboard(keyboardPanDirection.normalized);
    }
  }

  void HandleLockableInput() {
    if (mouseActive) {
      if (UIManager.Instance.GetUIMode() == UIMode.THREE_DIMENSIONAL) {
        Handle3DModeMouseInput();
        Handle3DModeScrollWheelInput();
      } else {
        HandleTacticalModeMouseInput();
        HandleTacticalModeScrollWheelInput();
      }
    }

    if (UIManager.Instance.GetUIMode() == UIMode.THREE_DIMENSIONAL) {
      Handle3DModeLockableInput();
    } else {
      HandleTacticalModeLockableInput();
    }

    if (Input.GetKeyDown(KeyCode.Tab)) {
      UIManager.Instance.ToggleUIMode();
    }
  }

  void HandleNonLockableInput() {
    if (Input.GetKeyDown(KeyCode.Escape)) {
      SimManager.Instance.QuitSimulation();
    }

    if (Input.GetKeyDown(KeyCode.C)) {}

    if (Input.GetKeyDown(KeyCode.R)) {
      SimManager.Instance.RestartSimulation();
    }

    if (Input.GetKeyDown(KeyCode.L)) {
      UIManager.Instance.ToggleConfigSelectorPanel();
    }

    if (Input.GetKeyDown(KeyCode.C)) {
      ParticleManager.Instance.ClearHitMarkers();
    }

    if (Input.GetKeyDown(KeyCode.Space)) {
      // Pause the time.

      if (!SimManager.Instance.IsSimulationPaused()) {
        SimManager.Instance.PauseSimulation();
      } else {
        SimManager.Instance.ResumeSimulation();
      }
    }

    if (Input.GetKeyDown(KeyCode.P)) {
      CameraController.Instance.SetAutoRotate(!CameraController.Instance.IsAutoRotate());
    }

    if (Input.GetKeyDown(KeyCode.Alpha1)) {
      if (Input.GetKey(KeyCode.LeftControl)) {
        CameraController.Instance.FollowNextInterceptorSwarm();
      } else {
        CameraController.Instance.SnapToNextInterceptorSwarm();
      }
    }

    if (Input.GetKeyDown(KeyCode.Alpha2)) {
      if (Input.GetKey(KeyCode.LeftControl)) {
        CameraController.Instance.FollowNextThreatSwarm();
      } else {
        CameraController.Instance.SnapToNextThreatSwarm();
      }
    }

    if (Input.GetKeyDown(KeyCode.Alpha3)) {
      if (Input.GetKey(KeyCode.LeftControl)) {
        CameraController.Instance.FollowCenterAllAgents();
      } else {
        CameraController.Instance.SnapToCenterAllAgents();
      }
    }

    if (Input.GetKeyDown(KeyCode.Alpha4)) {
      // Set pre-set view 4.
    }

    if (Input.GetKeyDown(KeyCode.Alpha5)) {
      // Set pre-set view 5.
    }

    if (Input.GetKeyDown(KeyCode.Alpha6)) {
      // Set pre-set view 6.
    }
  }
}
