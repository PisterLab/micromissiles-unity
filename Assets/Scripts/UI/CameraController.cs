using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CameraController : MonoBehaviour {
#region Singleton

  // Singleton instance of the camera controller.
  public static CameraController Instance { get; private set; }

#endregion

#region Camera Settings

  // Determines if mouse input is active for camera control.
  public bool mouseActive = true;

  // Locks user input for camera control.
  public bool lockUserInput = false;

  // Normal speed of camera movement.
  [SerializeField]
  private float _cameraSpeedNormal = 100.0f;

  // Maximum speed of camera movement.
  [SerializeField]
  private float _cameraSpeedMax = 1000.0f;

  // Current speed of camera movement.
  private float _cameraSpeed;

  // Horizontal rotation speed.
  public float _speedH = 2.0f;

  // Vertical rotation speed.
  public float _speedV = 2.0f;

  // Current yaw angle of the camera.
  private float _yaw = 0.0f;

  // Current pitch angle of the camera.
  private float _pitch = 0.0f;

#endregion

#region Orbit Settings

  // Determines if the camera should auto-rotate.
  public bool _autoRotate = false;

  // Threat transform for orbit rotation.
  public Transform target;

  // Distance from the camera to the orbit target.
  [SerializeField]
  private float _orbitDistance = 5.0f;

  // Horizontal orbit rotation speed.
  [SerializeField]
  private float _orbitXSpeed = 120.0f;

  // Vertical orbit rotation speed.
  [SerializeField]
  private float _orbitYSpeed = 120.0f;

  // Speed of camera zoom.
  [SerializeField]
  private float _zoomSpeed = 500.0f;

  // Minimum vertical angle limit for orbit.
  public float orbitYMinLimit = -20f;

  // Maximum vertical angle limit for orbit.
  public float orbitYMaxLimit = 80f;

  // Minimum distance for orbit.
  private float _orbitDistanceMin = 10f;

  // Maximum distance for orbit.
  [SerializeField]
  private float _orbitDistanceMax = 20000f;

  // Current horizontal orbit angle.
  private float _orbitX = 0.0f;

  // Current vertical orbit angle.
  private float _orbitY = 0.0f;

#endregion

#region Rendering

  // Renderer for the orbit target.
  public Renderer targetRenderer;

  // Renderer for the floor.
  public Renderer floorRenderer;

  // Alpha value for material transparency.
  public float matAlpha;

#endregion

#region Autoplay Settings

  // Speed of camera movement during autoplay.
  public float autoplayCamSpeed = 2f;

  // Duration of horizontal auto-rotation.
  public float xAutoRotateTime = 5f;

  // Duration of vertical auto-rotation.
  public float yAutoRotateTime = 5f;

  // Coroutine for autoplay functionality.
  private Coroutine autoplayRoutine;

#endregion

#region Camera Presets

  // Represents a preset camera position and rotation.
  [System.Serializable]
  public struct CameraPreset {
    public Vector3 position;
    public Quaternion rotation;
  }

  // Preset camera position for key 4.
  CameraPreset fourPos = new CameraPreset();

  // Preset camera position for key 5.
  CameraPreset fivePos = new CameraPreset();

  // Preset camera position for key 6.
  CameraPreset sixPos = new CameraPreset();

#endregion

#region Movement

  // Mapping of translation inputs to movement vectors.
  private Dictionary<TranslationInput, Vector3> _translationInputToVectorMap;

  // Forward movement vector.
  Vector3 wVector = Vector3.forward;

  // Left movement vector.
  Vector3 aVector = Vector3.left;

  // Backward movement vector.
  Vector3 sVector = Vector3.back;

  // Right movement vector.
  Vector3 dVector = Vector3.right;

  // Angle between forward vector and camera direction.
  public float forwardToCameraAngle;

  public CameraMode cameraMode = CameraMode.FREE;

  public int _selectedInterceptorSwarmIndex = -1;
  public int _selectedThreatSwarmIndex = -1;

  private Vector3 _lastCentroid;
  private Vector3 _currentCentroid;
  private Vector3 _targetCentroid;

  public float _centroidUpdateFrequency = 0.1f;
  public float _defaultInterpolationSpeed = 5f;
  [SerializeField]
  private float _currentInterpolationSpeed;

  [SerializeField]
  private float _iirFilterCoefficient = 0.9f;

  private Coroutine _centroidUpdateCoroutine;

#endregion

  void SetCameraRotation(Quaternion rotation) {
    transform.rotation = rotation;
    _pitch = rotation.eulerAngles.x;
    _yaw = rotation.eulerAngles.y;
  }

  public void SetCameraSpeed(float speed) {
    _cameraSpeed = speed;
  }

  public float GetCameraSpeedMax() {
    return _cameraSpeedMax;
  }

  public float GetCameraSpeedNormal() {
    return _cameraSpeedNormal;
  }

  public bool IsAutoRotate() {
    return _autoRotate;
  }

  public void SetAutoRotate(bool autoRotate) {
    if (autoRotate && !_autoRotate) {
      _autoRotate = true;
      autoplayRoutine = StartCoroutine(AutoPlayRoutine());
    } else if (!autoRotate && _autoRotate) {
      _autoRotate = false;
      StopCoroutine(autoplayRoutine);
    }
  }

  public static float ClampAngle(float angle, float min, float max) {
    if (angle < -360F)
      angle += 360F;
    if (angle > 360F)
      angle -= 360F;
    return Mathf.Clamp(angle, min, max);
  }

  private void Awake() {
    if (Instance == null) {
      Instance = this;
      DontDestroyOnLoad(gameObject);
    } else {
      Destroy(gameObject);
    }

    _translationInputToVectorMap = new Dictionary<TranslationInput, Vector3> {
      { TranslationInput.Forward, wVector }, { TranslationInput.Left, aVector },
      { TranslationInput.Back, sVector },    { TranslationInput.Right, dVector },
      { TranslationInput.Up, Vector3.up },   { TranslationInput.Down, Vector3.down }
    };
    _currentInterpolationSpeed = _defaultInterpolationSpeed;
  }

  void Start() {
    fourPos.position = new Vector3(0, 0, 0);
    fourPos.rotation = Quaternion.Euler(0, 0, 0);
    fivePos.position = new Vector3(0, 0, 0);
    fivePos.rotation = Quaternion.Euler(0, 0, 0);
    sixPos.position = new Vector3(0, 0, 0);
    sixPos.rotation = Quaternion.Euler(0, 0, 0);

    Vector3 angles = transform.eulerAngles;
    _orbitX = angles.y;
    _orbitY = angles.x;

    UpdateTargetAlpha();
    ResetCameraTarget();

    SetCameraMode(CameraMode.FREE);
  }

  public void SnapToSwarm(List<Agent> swarm, bool forceFreeMode = true) {
    Vector3 swarmCenter = SimManager.Instance.GetSwarmCenter(swarm);
    SetCameraTargetPosition(swarmCenter);
    if (forceFreeMode) {
      SetCameraMode(CameraMode.FREE);
    }
  }

  public void SnapToNextInterceptorSwarm(bool forceFreeMode = true) {
    if (SimManager.Instance.GetInterceptorSwarms().Count == 0) {
      UIManager.Instance.LogActionWarning("[CAM] No interceptor swarms to follow");
      return;
    }

    // Set pre-set view 1
    _selectedInterceptorSwarmIndex += 1;
    _selectedThreatSwarmIndex = -1;
    if (_selectedInterceptorSwarmIndex >= SimManager.Instance.GetInterceptorSwarms().Count) {
      _selectedInterceptorSwarmIndex = 0;
    }
    List<(Agent, bool)> swarm =
        SimManager.Instance.GetInterceptorSwarms()[_selectedInterceptorSwarmIndex];
    string swarmTitle = SimManager.Instance.GenerateInterceptorSwarmTitle(swarm);

    // Filter out inactive agents
    List<(Agent, bool)> activeAgents = swarm.FindAll(tuple => tuple.Item2);
    List<Agent> activeAgentsList = activeAgents.ConvertAll(tuple => tuple.Item1);
    Vector3 swarmCenter = SimManager.Instance.GetSwarmCenter(activeAgentsList);
    SetCameraTargetPosition(swarmCenter);
    if (forceFreeMode) {
      SetCameraMode(CameraMode.FREE);
    }
    UIManager.Instance.LogActionMessage($"[CAM] Snap to interceptor swarm: {swarmTitle}");
  }

  public void SnapToNextThreatSwarm(bool forceFreeMode = true) {
    if (SimManager.Instance.GetThreatSwarms().Count == 0) {
      return;
    }
    _selectedInterceptorSwarmIndex = -1;
    _selectedThreatSwarmIndex += 1;
    if (_selectedThreatSwarmIndex >= SimManager.Instance.GetThreatSwarms().Count) {
      _selectedThreatSwarmIndex = 0;
    }
    List<(Agent, bool)> swarm = SimManager.Instance.GetThreatSwarms()[_selectedThreatSwarmIndex];
    string swarmTitle = SimManager.Instance.GenerateThreatSwarmTitle(swarm);
    // Filter out inactive agents
    List<(Agent, bool)> activeAgents = swarm.FindAll(tuple => tuple.Item2);
    List<Agent> activeAgentsList = activeAgents.ConvertAll(tuple => tuple.Item1);
    Vector3 swarmCenter = SimManager.Instance.GetSwarmCenter(activeAgentsList);
    SetCameraTargetPosition(swarmCenter);
    if (forceFreeMode) {
      SetCameraMode(CameraMode.FREE);
    }
    UIManager.Instance.LogActionMessage($"[CAM] Snap to threat swarm: {swarmTitle}");
  }

  public void SnapToCenterAllAgents(bool forceFreeMode = true) {
    Vector3 swarmCenter = SimManager.Instance.GetAllAgentsCenter();
    SetCameraTargetPosition(swarmCenter);
    if (forceFreeMode) {
      SetCameraMode(CameraMode.FREE);
    }
    UIManager.Instance.LogActionMessage("[CAM] Snap to center all agents");
  }

  public void SetCameraMode(CameraMode mode) {
    if (cameraMode == CameraMode.FREE) {
      if (_centroidUpdateCoroutine != null) {
        StopCoroutine(_centroidUpdateCoroutine);
        _centroidUpdateCoroutine = null;
      }
    } else {
      _currentCentroid = _targetCentroid = target.position;
    }
    cameraMode = mode;
  }

  private void StartCentroidUpdateCoroutine() {
    if (_centroidUpdateCoroutine == null) {
      _centroidUpdateCoroutine = StartCoroutine(UpdateCentroidCoroutine());
    }
  }

  public void FollowNextInterceptorSwarm() {
    SnapToNextInterceptorSwarm(false);
    StartCentroidUpdateCoroutine();
    SetCameraMode(CameraMode.FOLLOW_INTERCEPTOR_SWARM);
    UIManager.Instance.LogActionMessage("[CAM] Follow next interceptor swarm");
  }

  public void FollowNextThreatSwarm() {
    SnapToNextThreatSwarm(false);
    SetCameraMode(CameraMode.FOLLOW_THREAT_SWARM);
    StartCentroidUpdateCoroutine();
    UIManager.Instance.LogActionMessage("[CAM] Follow next threat swarm");
  }

  public void FollowCenterAllAgents() {
    SnapToCenterAllAgents(false);
    SetCameraMode(CameraMode.FOLLOW_ALL_AGENTS);
    StartCentroidUpdateCoroutine();
    UIManager.Instance.LogActionMessage("[CAM] Follow center all agents");
  }

  private IEnumerator UpdateCentroidCoroutine() {
    while (true) {
      UpdateTargetCentroid();
      yield return new WaitForSeconds(_centroidUpdateFrequency);
    }
  }

  private void UpdateTargetCentroid() {
    _lastCentroid = _currentCentroid;

    if (cameraMode == CameraMode.FOLLOW_INTERCEPTOR_SWARM) {
      if (_selectedInterceptorSwarmIndex == -1) {
        _selectedInterceptorSwarmIndex = 0;
      }
      if (SimManager.Instance.GetInterceptorSwarms().Count == 0) {
        return;
      }
      _targetCentroid = SimManager.Instance.GetSwarmCenter(
          SimManager.Instance.GetInterceptorSwarms() [_selectedInterceptorSwarmIndex].ConvertAll(
              tuple => tuple.Item1));
    } else if (cameraMode == CameraMode.FOLLOW_THREAT_SWARM) {
      if (_selectedThreatSwarmIndex == -1) {
        _selectedThreatSwarmIndex = 0;
      }
      if (SimManager.Instance.GetThreatSwarms().Count == 0) {
        return;
      }
      _targetCentroid = SimManager.Instance.GetSwarmCenter(
          SimManager.Instance.GetThreatSwarms() [_selectedThreatSwarmIndex].ConvertAll(
              tuple => tuple.Item1));
    } else if (cameraMode == CameraMode.FOLLOW_ALL_AGENTS) {
      _targetCentroid = SimManager.Instance.GetAllAgentsCenter();
    }
    // Apply IIR filter to adjust interpolation speed
    float distance = Mathf.Abs(Vector3.Distance(_lastCentroid, _targetCentroid));
    float targetSpeed = Mathf.Clamp(distance, 1f, 100000f);
    _currentInterpolationSpeed = _iirFilterCoefficient * _currentInterpolationSpeed +
                                 (1 - _iirFilterCoefficient) * targetSpeed;
  }

  IEnumerator AutoPlayRoutine() {
    while (true) {
      float elapsedTime = 0f;
      while (elapsedTime <= xAutoRotateTime) {
        _orbitX += Time.unscaledDeltaTime * autoplayCamSpeed * _orbitDistance * 0.02f;
        UpdateCamPosition(_orbitX, _orbitY);
        elapsedTime += Time.unscaledDeltaTime;
        yield return null;
      }
      elapsedTime = 0f;
      while (elapsedTime <= yAutoRotateTime) {
        _orbitY -= Time.unscaledDeltaTime * autoplayCamSpeed * _orbitDistance * 0.02f;
        UpdateCamPosition(_orbitX, _orbitY);
        elapsedTime += Time.unscaledDeltaTime;
        yield return null;
      }
      elapsedTime = 0f;
      while (elapsedTime <= xAutoRotateTime) {
        _orbitX -= Time.unscaledDeltaTime * autoplayCamSpeed * _orbitDistance * 0.02f;
        UpdateCamPosition(_orbitX, _orbitY);
        elapsedTime += Time.unscaledDeltaTime;
        yield return null;
      }
      elapsedTime = 0f;
      while (elapsedTime <= yAutoRotateTime) {
        _orbitY += Time.unscaledDeltaTime * autoplayCamSpeed * _orbitDistance * 0.02f;
        UpdateCamPosition(_orbitX, _orbitY);
        elapsedTime += Time.unscaledDeltaTime;
        yield return null;
      }
      yield return null;
    }
  }

  public void SetCameraTargetPosition(Vector3 position) {
    target.transform.position = position;
    UpdateCamPosition(_orbitX, _orbitY);
  }

  void ResetCameraTarget() {
    RaycastHit hit;
    if (Physics.Raycast(transform.position, transform.forward, out hit, float.MaxValue,
                        LayerMask.GetMask("Floor"), QueryTriggerInteraction.Ignore)) {
      target.transform.position = hit.point;
      _orbitDistance = hit.distance;
      Vector3 angles = transform.eulerAngles;
      _orbitX = angles.y;
      _orbitY = angles.x;
      UpdateCamPosition(_orbitX, _orbitY);
    } else {
      target.transform.position = transform.position + (transform.forward * 100);
      _orbitDistance = 100;
      Vector3 angles = transform.eulerAngles;
      _orbitX = angles.y;
      _orbitY = angles.x;
      // UpdateCamPosition();
    }
  }

  public void EnableTargetRenderer(bool enable) {
    targetRenderer.enabled = enable;
  }

  public void EnableFloorGridRenderer(bool enable) {
    floorRenderer.enabled = enable;
  }

  public void OrbitCamera(float xOrbit, float yOrbit) {
    if (target) {
      _orbitX += xOrbit * _orbitXSpeed * _orbitDistance * 0.02f;
      _orbitY -= yOrbit * _orbitYSpeed * _orbitDistance * 0.02f;

      _orbitY = ClampAngle(_orbitY, orbitYMinLimit, orbitYMaxLimit);
      UpdateCamPosition(_orbitX, _orbitY);
    }
  }

  public void RotateCamera(float xRotate, float yRotate) {
    _yaw += xRotate * _speedH;
    _pitch -= yRotate * _speedV;
    transform.eulerAngles = new Vector3(_pitch, _yaw, 0.0f);
  }

  private void UpdateCamPosition(float x, float y) {
    Quaternion rotation = Quaternion.Euler(y, x, 0);
    RaycastHit hit;
    // Debug.DrawLine(target.position, transform.position, Color.red);
    if (Physics.Linecast(target.position, transform.position, out hit, ~LayerMask.GetMask("Floor"),
                         QueryTriggerInteraction.Ignore)) {
      _orbitDistance -= hit.distance;
    }
    Vector3 negDistance = new Vector3(0.0f, 0.0f, -_orbitDistance);
    Vector3 position = rotation * negDistance + target.position;
    _orbitDistance = Mathf.Clamp(_orbitDistance, _orbitDistanceMin, _orbitDistanceMax);
    UpdateTargetAlpha();

    SetCameraRotation(rotation);
    transform.position = position;
  }

  public void ZoomCamera(float zoom) {
    _orbitDistance =
        Mathf.Clamp(_orbitDistance - zoom * _zoomSpeed, _orbitDistanceMin, _orbitDistanceMax);
    UpdateCamPosition(_orbitX, _orbitY);
  }

  void UpdateTargetAlpha() {
    matAlpha = (_orbitDistance - _orbitDistanceMin) / (_orbitDistanceMax - _orbitDistanceMin);
    matAlpha = Mathf.Max(Mathf.Sqrt(matAlpha) - 0.5f, 0);
    Color matColor = targetRenderer.material.color;
    matColor.a = matAlpha;
    targetRenderer.material.color = matColor;
  }

  void UpdateDirectionVectors() {
    Vector3 cameraToTarget = target.position - transform.position;
    cameraToTarget.y = 0;
    forwardToCameraAngle = Vector3.SignedAngle(Vector3.forward, cameraToTarget, Vector3.down);

    if (forwardToCameraAngle > -45f && forwardToCameraAngle <= 45f) {
      _translationInputToVectorMap[TranslationInput.Forward] = Vector3.forward;
      _translationInputToVectorMap[TranslationInput.Left] = Vector3.left;
      _translationInputToVectorMap[TranslationInput.Back] = Vector3.back;
      _translationInputToVectorMap[TranslationInput.Right] = Vector3.right;
    } else if (forwardToCameraAngle > 45f && forwardToCameraAngle <= 135f) {
      _translationInputToVectorMap[TranslationInput.Forward] = Vector3.left;
      _translationInputToVectorMap[TranslationInput.Left] = Vector3.back;
      _translationInputToVectorMap[TranslationInput.Back] = Vector3.right;
      _translationInputToVectorMap[TranslationInput.Right] = Vector3.forward;
    } else if (forwardToCameraAngle > 135f || forwardToCameraAngle <= -135f) {
      _translationInputToVectorMap[TranslationInput.Forward] = Vector3.back;
      _translationInputToVectorMap[TranslationInput.Left] = Vector3.right;
      _translationInputToVectorMap[TranslationInput.Back] = Vector3.forward;
      _translationInputToVectorMap[TranslationInput.Right] = Vector3.left;
    } else if (forwardToCameraAngle > -135f && forwardToCameraAngle <= -45f) {
      _translationInputToVectorMap[TranslationInput.Forward] = Vector3.right;
      _translationInputToVectorMap[TranslationInput.Left] = Vector3.forward;
      _translationInputToVectorMap[TranslationInput.Back] = Vector3.left;
      _translationInputToVectorMap[TranslationInput.Right] = Vector3.back;
    }
  }

  public enum TranslationInput { Forward, Left, Back, Right, Up, Down }

  public void TranslateCamera(TranslationInput input) {
    if (cameraMode != CameraMode.FREE) {
      SetCameraMode(CameraMode.FREE);
    }
    UpdateDirectionVectors();
    target.Translate(_translationInputToVectorMap[input] * Time.unscaledDeltaTime * _cameraSpeed);
    UpdateCamPosition(_orbitX, _orbitY);
  }

  protected void Update() {
    if (cameraMode != CameraMode.FREE) {
      // Use MoveTowards for smoother and more predictable movement
      _currentCentroid = Vector3.MoveTowards(_currentCentroid, _targetCentroid,
                                             _currentInterpolationSpeed * Time.unscaledDeltaTime);
      SetCameraTargetPosition(_currentCentroid);
    }
  }
}

public enum CameraMode { FREE, FOLLOW_INTERCEPTOR_SWARM, FOLLOW_THREAT_SWARM, FOLLOW_ALL_AGENTS }
