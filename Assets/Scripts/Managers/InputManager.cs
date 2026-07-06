using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour {
  public static InputManager Instance { get; private set; }

  public bool MouseActive { get; set; } = true;
  public bool LockUserInput { get; set; } = false;

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
        default: {
          Debug.LogError($"Invalid UI mode: {UIManager.Instance.UIMode}.");
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
      default: {
        Debug.LogError($"Invalid UI mode: {UIManager.Instance.UIMode}.");
        break;
      }
    }

    if (Keyboard.current.tabKey.wasPressedThisFrame) {
      UIManager.Instance.ToggleUIMode();
    }
  }

  private void Handle3DModeMouseInput() {
    var mouse = Mouse.current;
    Vector2 delta = mouse.delta.ReadValue();
    if (mouse.leftButton.isPressed) {
      CameraController.Instance.OrbitCamera(delta.x, delta.y);
    } else if (mouse.rightButton.isPressed) {
      CameraController.Instance.RotateCamera(delta.x, delta.y);
    }
  }

  private void Handle3DModeScrollWheelInput() {
    var mouse = Mouse.current;
    if (mouse.scroll.ReadValue().y != 0) {
      CameraController.Instance.ZoomCamera(mouse.scroll.ReadValue().y * 5);
    }
  }

  private void Handle3DModeLockableInput() {
    var keyboard = Keyboard.current;
    if (keyboard.leftShiftKey.isPressed) {
      CameraController.Instance.CameraSpeed = CameraController.Instance.CameraSpeedMax;
    } else {
      CameraController.Instance.CameraSpeed = CameraController.Instance.CameraSpeedNormal;
    }

    // Translational movement.
    if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) {
      CameraController.Instance.TranslateCamera(CameraController.TranslationInput.Forward);
    }
    if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) {
      CameraController.Instance.TranslateCamera(CameraController.TranslationInput.Left);
    }
    if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) {
      CameraController.Instance.TranslateCamera(CameraController.TranslationInput.Back);
    }
    if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) {
      CameraController.Instance.TranslateCamera(CameraController.TranslationInput.Right);
    }
    if (keyboard.qKey.isPressed) {
      CameraController.Instance.TranslateCamera(CameraController.TranslationInput.Up);
    }
    if (keyboard.eKey.isPressed) {
      CameraController.Instance.TranslateCamera(CameraController.TranslationInput.Down);
    }
  }

  private void HandleTacticalModeMouseInput() {
    var mouse = Mouse.current;
    // Start drag on right mouse button.
    if (mouse.rightButton.wasPressedThisFrame) {
      _isDragging = true;
      _lastMousePosition = mouse.position.ReadValue();
    }
    // End drag when button released.
    else if (mouse.rightButton.wasReleasedThisFrame) {
      _isDragging = false;
    }

    // Handle dragging.
    if (_isDragging) {
      Vector2 currentMousePos = mouse.position.ReadValue();
      Vector2 delta = currentMousePos - _lastMousePosition;
      TacticalPanel.Instance.Pan(delta);
      _lastMousePosition = currentMousePos;
    }
  }

  private void HandleTacticalModeScrollWheelInput() {
    var mouse = Mouse.current;
    if (mouse.scroll.ReadValue().y != 0) {
      TacticalPanel.Instance.ZoomIn(mouse.scroll.ReadValue().y * 0.001f);
    }
  }

  private void HandleTacticalModeLockableInput() {
    // Handle keyboard input for panning.
    var keyboard = Keyboard.current;
    Vector2 keyboardPanDirection = Vector2.zero;
    if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) {
      keyboardPanDirection.y += -1;
    }
    if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) {
      keyboardPanDirection.x += 1;
    }
    if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) {
      keyboardPanDirection.y += 1;
    }
    if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) {
      keyboardPanDirection.x += -1;
    }

    if (keyboard.qKey.wasPressedThisFrame) {
      TacticalPanel.Instance.CycleRangeUp();
    }
    if (keyboard.eKey.wasPressedThisFrame) {
      TacticalPanel.Instance.CycleRangeDown();
    }

    if (keyboardPanDirection != Vector2.zero) {
      TacticalPanel.Instance.PanWithKeyboard(keyboardPanDirection.normalized);
    }
  }

  private void HandleNonLockableInput() {
    var keyboard = Keyboard.current;
    if (keyboard.escapeKey.isPressed) {
      SimManager.Instance.QuitSimulation();
    }

    if (keyboard.rKey.isPressed) {
      SimManager.Instance.EndSimulation();
      SimManager.Instance.ResetAndStartSimulation();
    }

    if (keyboard.lKey.isPressed) {
      UIManager.Instance.ToggleConfigSelectorPanel();
    }

    if (keyboard.cKey.isPressed) {
      ParticleManager.Instance.ClearHitMarkers();
    }

    if (keyboard.pKey.isPressed) {
      CameraController.Instance.AutoRotate = !CameraController.Instance.AutoRotate;
    }

    if (keyboard.spaceKey.isPressed) {
      // Pause the time.
      if (!SimManager.Instance.IsPaused) {
        SimManager.Instance.PauseSimulation();
      } else {
        SimManager.Instance.ResumeSimulation();
      }
    }

    HandleCameraFollowInput(Key.Digit1, CameraFollowType.ALL_AGENTS);
    HandleCameraFollowInput(Key.Digit2, CameraFollowType.ALL_INTERCEPTORS);
    HandleCameraFollowInput(Key.Digit3, CameraFollowType.ALL_THREATS);
  }

  private void HandleCameraFollowInput(Key key, CameraFollowType followType) {
    var keyboard = Keyboard.current;
    if (keyboard[key].wasPressedThisFrame) {
      if (keyboard.leftCtrlKey.isPressed) {
        CameraController.Instance.Follow(followType);
      } else {
        CameraController.Instance.Snap(followType);
      }
    }
  }
}
