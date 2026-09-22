using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour {
  private enum IndividualFollowType {
    INTERCEPTORS,
    THREATS,
  }

  public static InputManager Instance { get; private set; }

  public bool MouseActive { get; set; } = true;
  public bool LockUserInput { get; set; } = false;

  private Vector2 _lastMousePosition;
  private bool _isDragging = false;
  private IndividualFollowType _individualFollowType = IndividualFollowType.INTERCEPTORS;

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
    if (keyboard.escapeKey.wasPressedThisFrame) {
      SimManager.Instance.QuitSimulation();
    }

    if (keyboard.rKey.wasPressedThisFrame) {
      SimManager.Instance.EndSimulation();
      SimManager.Instance.ResetAndStartSimulation();
    }

    if (keyboard.lKey.wasPressedThisFrame) {
      UIManager.Instance.ToggleConfigSelectorPanel();
    }

    if (keyboard.cKey.wasPressedThisFrame) {
      ParticleManager.Instance.ClearHitMarkers();
    }

    if (keyboard.pKey.wasPressedThisFrame) {
      CameraController.Instance.AutoRotate = !CameraController.Instance.AutoRotate;
    }

    if (keyboard.spaceKey.wasPressedThisFrame) {
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

    if (keyboard.backslashKey.wasPressedThisFrame) {
      ToggleIndividualFollowType();
    } else if (keyboard.leftBracketKey.wasPressedThisFrame) {
      FollowAdjacentIndividualAgent(direction: -1);
    } else if (keyboard.rightBracketKey.wasPressedThisFrame) {
      FollowAdjacentIndividualAgent(direction: 1);
    } else if (keyboard.equalsKey.wasPressedThisFrame &&
               _individualFollowType == IndividualFollowType.INTERCEPTORS) {
      // The main keyboard '+' character shares the physical '=' key.
      FollowChildInterceptor();
    } else if (keyboard.minusKey.wasPressedThisFrame &&
               _individualFollowType == IndividualFollowType.INTERCEPTORS) {
      FollowParentInterceptor();
    } else if (keyboard.digit0Key.wasPressedThisFrame) {
      CameraController.Instance.StopFollowingAgent();
    }
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

  private void ToggleIndividualFollowType() {
    if (_individualFollowType == IndividualFollowType.INTERCEPTORS) {
      _individualFollowType = IndividualFollowType.THREATS;
      FollowAdjacentThreat(direction: 1);
    } else {
      _individualFollowType = IndividualFollowType.INTERCEPTORS;
      FollowAdjacentSiblingInterceptor(direction: 1);
    }
  }

  private void FollowAdjacentIndividualAgent(int direction) {
    if (_individualFollowType == IndividualFollowType.THREATS) {
      FollowAdjacentThreat(direction);
    } else {
      FollowAdjacentSiblingInterceptor(direction);
    }
  }

  private void FollowAdjacentThreat(int direction) {
    var activeThreats = new List<IAgent>();
    foreach (IAgent agent in SimManager.Instance.Threats) {
      if (agent is IThreat threat && !threat.IsTerminated) {
        activeThreats.Add(threat);
      }
    }

    if (activeThreats.Count == 0) {
      UIManager.Instance?.LogActionWarning("[CAM] No active threats are available to follow.");
      return;
    }

    IAgent followedAgent = CameraController.Instance.FollowedAgent;
    int currentIndex = -1;
    for (int i = 0; i < activeThreats.Count; ++i) {
      if (ReferenceEquals(activeThreats[i], followedAgent)) {
        currentIndex = i;
        break;
      }
    }

    int startIndex = currentIndex;
    if (startIndex < 0) {
      startIndex = direction > 0 ? -1 : 0;
    }

    int nextIndex = (startIndex + direction) % activeThreats.Count;
    if (nextIndex < 0) {
      nextIndex += activeThreats.Count;
    }
    CameraController.Instance.FollowAgent(activeThreats[nextIndex]);
  }

  private void FollowAdjacentSiblingInterceptor(int direction) {
    var interceptors = SimManager.Instance.Interceptors;
    if (interceptors.Count == 0) {
      UIManager.Instance?.LogActionWarning("[CAM] No interceptors are available to follow.");
      return;
    }

    IAgent followedAgent = CameraController.Instance.FollowedAgent;
    var followedInterceptor = followedAgent as IInterceptor;
    bool selectTopLevelLauncher =
        followedInterceptor == null || followedInterceptor.ParentCommsNode == null;
    CommsNode iadsCommsNode = selectTopLevelLauncher ? IADS.Instance?.CommsNode : null;
    var siblingInterceptors = new List<IAgent>();
    foreach (IAgent agent in interceptors) {
      if (agent is not IInterceptor interceptor || interceptor.IsTerminated) {
        continue;
      }
      if (selectTopLevelLauncher) {
        if (iadsCommsNode == null || !ReferenceEquals(interceptor.ParentCommsNode, iadsCommsNode)) {
          continue;
        }
      } else {
        if (interceptor.StaticConfig.AgentType != followedInterceptor.StaticConfig.AgentType ||
            !ReferenceEquals(interceptor.ParentCommsNode, followedInterceptor.ParentCommsNode)) {
          continue;
        }
      }
      siblingInterceptors.Add(interceptor);
    }
    if (siblingInterceptors.Count == 0) {
      string warning = selectTopLevelLauncher
                           ? "[CAM] No active interceptor launchers are available to follow."
                           : "[CAM] No active sibling interceptors are available to follow.";
      UIManager.Instance?.LogActionWarning(warning);
      return;
    }
    if (!selectTopLevelLauncher && siblingInterceptors.Count == 1) {
      UIManager.Instance?.LogActionWarning(
          $"[CAM] {followedInterceptor.gameObject.name} has no other active sibling interceptor.");
      return;
    }

    int currentIndex = -1;
    for (int i = 0; i < siblingInterceptors.Count; ++i) {
      if (ReferenceEquals(siblingInterceptors[i], followedAgent)) {
        currentIndex = i;
        break;
      }
    }

    int startIndex = currentIndex;
    if (startIndex < 0) {
      startIndex = direction > 0 ? -1 : 0;
    }

    int nextIndex = (startIndex + direction) % siblingInterceptors.Count;
    if (nextIndex < 0) {
      nextIndex += siblingInterceptors.Count;
    }
    CameraController.Instance.FollowAgent(siblingInterceptors[nextIndex]);
  }

  private void FollowChildInterceptor() {
    if (CameraController.Instance.FollowedAgent is not IInterceptor followedInterceptor) {
      UIManager.Instance?.LogActionWarning(
          "[CAM] Select an interceptor before moving down the hierarchy.");
      return;
    }
    if (followedInterceptor.ParentCommsNode == null) {
      // Defended assets also implement IInterceptor, but they sit outside the launcher hierarchy.
      FollowAdjacentSiblingInterceptor(direction: 1);
      return;
    }

    foreach (IAgent agent in SimManager.Instance.Interceptors) {
      if (agent is IInterceptor childInterceptor && !childInterceptor.IsTerminated &&
          ReferenceEquals(childInterceptor.ParentCommsNode, followedInterceptor.CommsNode)) {
        CameraController.Instance.FollowAgent(childInterceptor);
        return;
      }
    }

    UIManager.Instance?.LogActionWarning(
        $"[CAM] {followedInterceptor.gameObject.name} has no active child interceptor.");
  }

  private void FollowParentInterceptor() {
    if (CameraController.Instance.FollowedAgent is not IInterceptor followedInterceptor) {
      UIManager.Instance?.LogActionWarning(
          "[CAM] Select an interceptor before moving up the hierarchy.");
      return;
    }

    foreach (IAgent agent in SimManager.Instance.Interceptors) {
      if (agent is IInterceptor parentInterceptor && !parentInterceptor.IsTerminated &&
          ReferenceEquals(parentInterceptor.CommsNode, followedInterceptor.ParentCommsNode)) {
        CameraController.Instance.FollowAgent(parentInterceptor);
        return;
      }
    }

    UIManager.Instance?.LogActionWarning(
        $"[CAM] {followedInterceptor.gameObject.name} has no active parent interceptor.");
  }
}
