using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraController : MonoBehaviour {
  public enum TranslationInput {
    Forward,
    Left,
    Back,
    Right,
    Up,
    Down,
  }

  // Mapping of translation inputs to movement vectors.
  private Dictionary<TranslationInput, Vector3> _translationInputToVectorMap;

  public static CameraController Instance { get; private set; }

  // Forward movement vector.
  private readonly Vector3 _wVector = Vector3.forward;

  // Left movement vector.
  private readonly Vector3 _aVector = Vector3.left;

  // Backward movement vector.
  private readonly Vector3 _sVector = Vector3.back;

  // Right movement vector.
  private readonly Vector3 _dVector = Vector3.right;

  // Normal speed of the camera movement.
  [SerializeField]
  private float _cameraSpeedNormal = 100f;

  // Maximum speed of the camera movement.
  [SerializeField]
  private float _cameraSpeedMax = 1000f;

  // Horizontal rotation speed.
  [SerializeField]
  private float _speedH = 2f;

  // Vertical rotation speed.
  [SerializeField]
  private float _speedV = 2f;

  // If true, the camera should auto-rotate.
  [SerializeField]
  private bool _autoRotate = false;

  // Target transform for orbit rotation.
  [SerializeField]
  private Transform _target;

  // Distance from the camera to the orbit target.
  [SerializeField]
  private float _orbitDistance = 5f;

  // Horizontal orbit rotation speed.
  [SerializeField]
  private float _orbitXSpeed = 120f;

  // Vertical orbit rotation speed.
  [SerializeField]
  private float _orbitYSpeed = 120f;

  // Minimum vertical angle limit for orbit.
  [SerializeField]
  private float _orbitYMinLimit = -20f;

  // Maximum vertical angle limit for orbit.
  [SerializeField]
  private float _orbitYMaxLimit = 80f;

  // Minimum distance for orbit.
  [SerializeField]
  private float _orbitDistanceMin = 10f;

  // Maximum distance for orbit.
  [SerializeField]
  private float _orbitDistanceMax = 20000f;

  // Speed of camera zoom.
  [SerializeField]
  private float _zoomSpeed = 500f;

  // Renderer for the orbit target.
  [SerializeField]
  private Renderer _targetRenderer;

  // Renderer for the floor.
  [SerializeField]
  private Renderer _floorRenderer;

  // Speed of camera movement during autoplay.
  [SerializeField]
  private float _autoplayCamSpeed = 0.02f;

  // Current horizontal orbit angle.
  private float _orbitX = 0f;

  // Current vertical orbit angle.
  private float _orbitY = 0f;

  // Current yaw angle of the camera.
  private float _yaw = 0f;

  // Current pitch angle of the camera.
  private float _pitch = 0f;

  // Coroutine for autoplay functionality.
  private Coroutine _autoplayRoutine;

  // Angle between forward vector and camera direction.
  [SerializeField]
  private float _forwardToCameraAngle;

  [SerializeField]
  private CameraMode _cameraMode = CameraMode.FREE;

  [SerializeField]
  private CameraFollowType _cameraFollowType = CameraFollowType.ALL_AGENTS;

  private Vector3 _lastCentroid;
  private Vector3 _currentCentroid;
  private Vector3 _targetCentroid;

  [SerializeField]
  public float _centroidUpdateFrequency = 0.1f;
  [SerializeField]
  public float _defaultInterpolationSpeed = 5f;
  [SerializeField]
  private float _currentInterpolationSpeed;

  [SerializeField]
  private float _iirFilterCoefficient = 0.9f;

  private Coroutine _centroidUpdateCoroutine;

  public float CameraSpeed { get; set; }

  public float CameraSpeedMax => _cameraSpeedMax;

  public float CameraSpeedNormal => _cameraSpeedNormal;

  public bool AutoRotate {
    get => _autoRotate;
    set {
      if (value && !_autoRotate) {
        _autoRotate = true;
        _autoplayRoutine = StartCoroutine(AutoPlayRoutine());
      } else if (!value && _autoRotate) {
        _autoRotate = false;
        if (_autoplayRoutine != null) {
          StopCoroutine(_autoplayRoutine);
          _autoplayRoutine = null;
        }
      }
    }
  }

  public CameraMode CameraMode {
    get => _cameraMode;
    set {
      switch (value) {
        case CameraMode.FREE: {
          if (_centroidUpdateCoroutine != null) {
            StopCoroutine(_centroidUpdateCoroutine);
            _centroidUpdateCoroutine = null;
          }
          break;
        }
        case CameraMode.FOLLOW: {
          _currentCentroid = _target.position;
          _targetCentroid = _target.position;
          break;
        }
        default: {
          break;
        }
      }
      _cameraMode = value;
    }
  }

  public CameraFollowType CameraFollowType {
    get => _cameraFollowType;
    set { _cameraFollowType = value; }
  }

  public void Follow(CameraFollowType type) {
    Snap(type);
    CameraMode = CameraMode.FOLLOW;
    CameraFollowType = type;
    StartCentroidUpdateCoroutine();

    switch (type) {
      case CameraFollowType.ALL_AGENTS: {
        UIManager.Instance.LogActionMessage("[CAM] Follow center of all agents.");
        break;
      }
      case CameraFollowType.ALL_INTERCEPTORS: {
        UIManager.Instance.LogActionMessage("[CAM] Follow center of all interceptors.");
        break;
      }
      case CameraFollowType.ALL_THREATS: {
        UIManager.Instance.LogActionMessage("[CAM] Follow center of all threats.");
        break;
      }
      default: {
        UIManager.Instance.LogActionMessage("[CAM] Follow center of all agents.");
        break;
      }
    }
  }

  public void Snap(CameraFollowType type) {
    IEnumerable<IAgent> agents;
    switch (type) {
      case CameraFollowType.ALL_AGENTS: {
        agents = SimManager.Instance.Agents;
        UIManager.Instance.LogActionMessage("[CAM] Snap to center all agents.");
        break;
      }
      case CameraFollowType.ALL_INTERCEPTORS: {
        agents = SimManager.Instance.Interceptors;
        UIManager.Instance.LogActionMessage("[CAM] Snap to center all interceptors.");
        break;
      }
      case CameraFollowType.ALL_THREATS: {
        agents = SimManager.Instance.Threats;
        UIManager.Instance.LogActionMessage("[CAM] Snap to center all threats.");
        break;
      }
      default: {
        agents = SimManager.Instance.Agents;
        UIManager.Instance.LogActionMessage("[CAM] Snap to center all agents.");
        break;
      }
    }
    Vector3 centroid = FindCentroid(agents);
    SetCameraTargetPosition(centroid);
  }

  public void OrbitCamera(float xOrbit, float yOrbit) {
    if (_target) {
      _orbitX += xOrbit * _orbitXSpeed * _orbitDistance * 0.02f;
      _orbitY -= yOrbit * _orbitYSpeed * _orbitDistance * 0.02f;

      _orbitY = ClampAngle(_orbitY, _orbitYMinLimit, _orbitYMaxLimit);
      UpdateCameraPosition(_orbitX, _orbitY);
    }
  }

  public void RotateCamera(float xRotate, float yRotate) {
    _yaw += xRotate * _speedH;
    _pitch -= yRotate * _speedV;
    transform.eulerAngles = new Vector3(_pitch, _yaw, 0f);
  }

  public void TranslateCamera(TranslationInput input) {
    if (CameraMode != CameraMode.FREE) {
      CameraMode = CameraMode.FREE;
    }
    UpdateDirectionVectors();
    _target.Translate(_translationInputToVectorMap[input] * Time.unscaledDeltaTime * CameraSpeed);
    UpdateCameraPosition(_orbitX, _orbitY);
  }

  public void ZoomCamera(float zoom) {
    _orbitDistance =
        Mathf.Clamp(_orbitDistance - zoom * _zoomSpeed, _orbitDistanceMin, _orbitDistanceMax);
    UpdateCameraPosition(_orbitX, _orbitY);
  }

  private void Awake() {
    if (Instance != null && Instance != this) {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);

    _currentInterpolationSpeed = _defaultInterpolationSpeed;
  }

  private void Start() {
    Vector3 angles = transform.eulerAngles;
    _orbitX = angles.y;
    _orbitY = angles.x;

    UpdateTargetAlpha();
    ResetCameraTarget();

    _translationInputToVectorMap = new Dictionary<TranslationInput, Vector3> {
      { TranslationInput.Forward, _wVector }, { TranslationInput.Left, _aVector },
      { TranslationInput.Back, _sVector },    { TranslationInput.Right, _dVector },
      { TranslationInput.Up, Vector3.up },    { TranslationInput.Down, Vector3.down }
    };
    CameraMode = CameraMode.FREE;
  }

  private void Update() {
    if (CameraMode != CameraMode.FREE) {
      // Use MoveTowards for smoother and more predictable movement.
      _currentCentroid = Vector3.MoveTowards(_currentCentroid, _targetCentroid,
                                             _currentInterpolationSpeed * Time.unscaledDeltaTime);
      SetCameraTargetPosition(_currentCentroid);
    }
  }

  private void SetCameraRotation(in Quaternion rotation) {
    transform.rotation = rotation;
    _pitch = rotation.eulerAngles.x;
    _yaw = rotation.eulerAngles.y;
  }

  private void UpdateCameraPosition(float x, float y) {
    Quaternion rotation = Quaternion.Euler(y, x, 0);
    if (Physics.Linecast(_target.position, transform.position, out RaycastHit hit,
                         ~LayerMask.GetMask("Floor"), QueryTriggerInteraction.Ignore)) {
      _orbitDistance -= hit.distance;
    }
    Vector3 negDistance = new Vector3(0f, 0f, -_orbitDistance);
    Vector3 position = rotation * negDistance + _target.position;
    _orbitDistance = Mathf.Clamp(_orbitDistance, _orbitDistanceMin, _orbitDistanceMax);
    UpdateTargetAlpha();

    SetCameraRotation(rotation);
    transform.position = position;
  }

  private void SetCameraTargetPosition(in Vector3 position) {
    _target.transform.position = position;
    UpdateCameraPosition(_orbitX, _orbitY);
  }

  private void ResetCameraTarget() {
    RaycastHit hit;
    if (Physics.Raycast(transform.position, transform.forward, out hit, float.MaxValue,
                        LayerMask.GetMask("Floor"), QueryTriggerInteraction.Ignore)) {
      _target.transform.position = hit.point;
      _orbitDistance = hit.distance;
      Vector3 angles = transform.eulerAngles;
      _orbitX = angles.y;
      _orbitY = angles.x;
      UpdateCameraPosition(_orbitX, _orbitY);
    } else {
      _target.transform.position = transform.position + (transform.forward * 100);
      _orbitDistance = 100;
      Vector3 angles = transform.eulerAngles;
      _orbitX = angles.y;
      _orbitY = angles.x;
    }
  }

  private void UpdateTargetAlpha() {
    float matAlpha = (_orbitDistance - _orbitDistanceMin) / (_orbitDistanceMax - _orbitDistanceMin);
    matAlpha = Mathf.Max(Mathf.Sqrt(matAlpha) - 0.5f, 0);
    Color matColor = _targetRenderer.material.color;
    matColor.a = matAlpha;
    _targetRenderer.material.color = matColor;
  }

  private static float ClampAngle(float angle, float min, float max) {
    if (angle < -360f)
      angle += 360f;
    if (angle > 360f)
      angle -= 360f;
    return Mathf.Clamp(angle, min, max);
  }

  private void StartCentroidUpdateCoroutine() {
    _centroidUpdateCoroutine ??= StartCoroutine(UpdateCentroidCoroutine());
  }

  private IEnumerator UpdateCentroidCoroutine() {
    while (true) {
      UpdateTargetCentroid();
      yield return new WaitForSeconds(_centroidUpdateFrequency);
    }
  }

  private void UpdateTargetCentroid() {
    _lastCentroid = _currentCentroid;

    switch (CameraFollowType) {
      case CameraFollowType.ALL_AGENTS: {
        _targetCentroid = FindCentroid(SimManager.Instance.Agents);
        break;
      }
      case CameraFollowType.ALL_INTERCEPTORS: {
        _targetCentroid = FindCentroid(SimManager.Instance.Interceptors);
        break;
      }
      case CameraFollowType.ALL_THREATS: {
        _targetCentroid = FindCentroid(SimManager.Instance.Threats);
        break;
      }
      default: {
        _targetCentroid = FindCentroid(SimManager.Instance.Agents);
        break;
      }
    }

    // Apply IIR filter to adjust interpolation speed.
    float distance = Mathf.Abs(Vector3.Distance(_lastCentroid, _targetCentroid));
    float targetSpeed = Mathf.Clamp(distance, 1f, 100000f);
    _currentInterpolationSpeed = _iirFilterCoefficient * _currentInterpolationSpeed +
                                 (1 - _iirFilterCoefficient) * targetSpeed;
  }

  private IEnumerator AutoPlayRoutine() {
    while (true) {
      _orbitX += Time.unscaledDeltaTime * _autoplayCamSpeed * _orbitDistance * 0.02f;
      UpdateCameraPosition(_orbitX, _orbitY);
      yield return null;
    }
  }

  private void UpdateDirectionVectors() {
    Vector3 cameraToTarget = _target.position - transform.position;
    cameraToTarget.y = 0;
    _forwardToCameraAngle = Vector3.SignedAngle(Vector3.forward, cameraToTarget, Vector3.down);

    if (_forwardToCameraAngle > -45f && _forwardToCameraAngle <= 45f) {
      _translationInputToVectorMap[TranslationInput.Forward] = Vector3.forward;
      _translationInputToVectorMap[TranslationInput.Left] = Vector3.left;
      _translationInputToVectorMap[TranslationInput.Back] = Vector3.back;
      _translationInputToVectorMap[TranslationInput.Right] = Vector3.right;
    } else if (_forwardToCameraAngle > 45f && _forwardToCameraAngle <= 135f) {
      _translationInputToVectorMap[TranslationInput.Forward] = Vector3.left;
      _translationInputToVectorMap[TranslationInput.Left] = Vector3.back;
      _translationInputToVectorMap[TranslationInput.Back] = Vector3.right;
      _translationInputToVectorMap[TranslationInput.Right] = Vector3.forward;
    } else if (_forwardToCameraAngle > 135f || _forwardToCameraAngle <= -135f) {
      _translationInputToVectorMap[TranslationInput.Forward] = Vector3.back;
      _translationInputToVectorMap[TranslationInput.Left] = Vector3.right;
      _translationInputToVectorMap[TranslationInput.Back] = Vector3.forward;
      _translationInputToVectorMap[TranslationInput.Right] = Vector3.left;
    } else if (_forwardToCameraAngle > -135f && _forwardToCameraAngle <= -45f) {
      _translationInputToVectorMap[TranslationInput.Forward] = Vector3.right;
      _translationInputToVectorMap[TranslationInput.Left] = Vector3.forward;
      _translationInputToVectorMap[TranslationInput.Back] = Vector3.left;
      _translationInputToVectorMap[TranslationInput.Right] = Vector3.back;
    }
  }

  private Vector3 FindCentroid(IEnumerable<IAgent> agents) {
    var positions = agents.Select(agent => agent.Position).ToList();
    if (positions.Count == 0) {
      return Vector3.zero;
    }

    var sum = Vector3.zero;
    foreach (var position in positions) {
      sum += position;
    }
    return sum / positions.Count;
  }
}

public enum CameraMode {
  FREE,
  FOLLOW,
}

public enum CameraFollowType {
  ALL_AGENTS,
  ALL_INTERCEPTORS,
  ALL_THREATS,
}
