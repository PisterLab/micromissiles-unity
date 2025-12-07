using UnityEngine;

public class InputManager : MonoBehaviour {
  public static InputManager Instance { get; private set; }

  public bool MouseActive = true;
  public bool LockUserInput = false;

  private Vector2 _lastMousePosition;
  private bool _isDragging = false;

  private void Awake() {
    if (Instance != null && Instance != this) {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);
  }

  private void Update() {
    HandleInput();
  }

  private void HandleInput() {
    if (!LockUserInput) {
      HandleLockableInput();
    }
    HandleNonLockableInput();
  }

  private void HandleLockableInput() {
    if (MouseActive) {
      switch (UIManager.Instance.UIMode) {
        case UIMode.THREE_DIMENSIONAL: {
          Handle3DModeMouseInput();
          Handle3DModeScrollWheelInput();
          break;
        }
        case UIMode.TACTICAL: {
          HandleTacticalModeMouseInput();
          HandleTacticalModeScrollWheelInput();
          break;
        }
      }
    }

    switch (UIManager.Instance.UIMode) {
      case UIMode.THREE_DIMENSIONAL: {
        Handle3DModeLockableInput();
        break;
      }
      case UIMode.TACTICAL: {
        HandleTacticalModeLockableInput();
        break;
      }
    }

    if (Input.GetKeyDown(KeyCode.Tab)) {
      UIManager.Instance.ToggleUIMode();
    }
  }

  private void HandleNonLockableInput() {
    if (Input.GetKeyDown(KeyCode.Escape)) {
      SimManager.Instance.QuitSimulation();
    }

    if (Input.GetKeyDown(KeyCode.R)) {
      SimManager.Instance.EndSimulation();
      SimManager.Instance.RestartSimulation();
    }

    if (Input.GetKeyDown(KeyCode.L)) {
      UIManager.Instance.ToggleConfigSelectorPanel();
    }

    if (Input.GetKeyDown(KeyCode.C)) {
      ParticleManager.Instance.ClearHitMarkers();
    }

    if (Input.GetKeyDown(KeyCode.P)) {
      CameraController.Instance.AutoRotate = !CameraController.Instance.AutoRotate;
    }

    if (Input.GetKeyDown(KeyCode.Space)) {
      // Pause the time.
      if (!SimManager.Instance.IsPaused) {
        SimManager.Instance.PauseSimulation();
      } else {
        SimManager.Instance.ResumeSimulation();
      }
    }

    if (Input.GetKeyDown(KeyCode.Alpha1)) {
      if (Input.GetKey(KeyCode.LeftControl)) {
        CameraController.Instance.FollowCenterAllAgents();
      } else {
        CameraController.Instance.SnapToCenterAllAgents();
      }
    }
  }

  private void Handle3DModeMouseInput() {
    if (Input.GetMouseButton(0)) {
      CameraController.Instance.OrbitCamera(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    } else if (Input.GetMouseButton(1)) {
      CameraController.Instance.RotateCamera(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    }
  }

  private void Handle3DModeScrollWheelInput() {
    if (Input.GetAxis("Mouse ScrollWheel") != 0) {
      CameraController.Instance.ZoomCamera(Input.GetAxis("Mouse ScrollWheel") * 500);
    }
  }

  private void Handle3DModeLockableInput() {
    if (Input.GetKey(KeyCode.LeftShift)) {
      CameraController.Instance.CameraSpeed = CameraController.Instance.CameraSpeedMax;
    } else {
      CameraController.Instance.CameraSpeed = CameraController.Instance.CameraSpeedNormal;
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

  private void HandleTacticalModeMouseInput() {
    // Start drag on right mouse button.
    if (Input.GetMouseButtonDown(1)) {
      _isDragging = true;
      _lastMousePosition = Input.mousePosition;
    }
    // End drag when button released.
    else if (Input.GetMouseButtonUp(1)) {
      _isDragging = false;
    }

    // Handle dragging.
    if (_isDragging) {
      Vector2 currentMousePos = Input.mousePosition;
      Vector2 delta = currentMousePos - _lastMousePosition;
      TacticalPanel.Instance.Pan(delta);
      _lastMousePosition = currentMousePos;
    }
  }

  private void HandleTacticalModeScrollWheelInput() {
    if (Input.GetAxis("Mouse ScrollWheel") != 0) {
      TacticalPanel.Instance.ZoomIn(Input.GetAxis("Mouse ScrollWheel") * 0.1f);
    }
  }

  private void HandleTacticalModeLockableInput() {
    // Handle keyboard input for panning.
    Vector2 keyboardPanDirection = Vector2.zero;
    if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) {
      keyboardPanDirection.y += -1;
    }
    if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) {
      keyboardPanDirection.x += 1;
    }
    if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) {
      keyboardPanDirection.y += 1;
    }
    if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) {
      keyboardPanDirection.x += -1;
    }

    if (Input.GetKeyDown(KeyCode.Q)) {
      TacticalPanel.Instance.CycleRangeUp();
    }
    if (Input.GetKeyDown(KeyCode.E)) {
      TacticalPanel.Instance.CycleRangeDown();
    }

    if (keyboardPanDirection != Vector2.zero) {
      TacticalPanel.Instance.PanWithKeyboard(keyboardPanDirection.normalized);
    }
  }
}
