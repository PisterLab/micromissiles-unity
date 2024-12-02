using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public class TacticalPanelController : MonoBehaviour {
  public static TacticalPanelController Instance { get; private set; }

  private void Awake() {
    if (Instance == null) {
      Instance = this;
      InitializeController();
    } else {
      Destroy(gameObject);
    }
  }

  [Tooltip("The UI group that contains the radar symbology elements")]
  [SerializeField]
  private GameObject _radarUIGroup;

  [SerializeField]
  [Tooltip("How often to refresh symbol positions (in seconds)")]
  private float _refreshRate = 0.1f;

  [SerializeField]
  private float _keyboardPanSpeed = 500f;  // Adjust this value in the inspector

  [SerializeField]
  private float _panelZoomSpeed = 10.0f;

  private RectTransform _radarUIGroupRectTransform;
  private IADS _iads;
  private float _timeSinceLastRefresh;
  private readonly Dictionary<ThreatData, GameObject> _activeSymbols =
      new Dictionary<ThreatData, GameObject>();

  private readonly Dictionary<InterceptorData, GameObject> _activeInterceptorSymbols =
      new Dictionary<InterceptorData, GameObject>();

  private List<GameObject> _originSymbols = new List<GameObject>();

  private void Start() {
    _iads = IADS.Instance;
    SetupRadarUIGroup();
    _timeSinceLastRefresh = 0f;
    CreateOriginSymbol();
  }

  private void Update() {
    HandleRefreshTimer();
  }

  private void OnDisable() {
    ClearAllSymbols();
  }

  private void InitializeController() {
    // Initialize any controller-specific settings here if needed
  }

  private void SetupRadarUIGroup() {
    _radarUIGroupRectTransform = _radarUIGroup.GetComponent<RectTransform>();
    ResetRadarUITransform();
  }

  private void ResetRadarUITransform() {
    _radarUIGroupRectTransform.localScale = Vector3.one;
    _radarUIGroupRectTransform.localPosition = Vector3.zero;
  }

  private void HandleRefreshTimer() {
    _timeSinceLastRefresh += Time.deltaTime;

    if (_timeSinceLastRefresh >= _refreshRate) {
      UpdateSymbols();
      _timeSinceLastRefresh = 0f;
    }
  }

  private void UpdateSymbols() {
    UpdateThreatSymbols();
    UpdateInterceptorSymbols();
  }

  private void UpdateThreatSymbols() {
    var currentThreats = _iads.GetThreatTable();

    RemoveInactiveSymbols(currentThreats);
    UpdateOrCreateActiveSymbols(currentThreats);
  }

  private void RemoveInactiveSymbols(List<ThreatData> currentThreats) {
    var threatsToRemove =
        _activeSymbols.Keys.Where(threat => !currentThreats.Contains(threat)).ToList();

    foreach (var threat in threatsToRemove) {
      Destroy(_activeSymbols[threat]);
      _activeSymbols.Remove(threat);
    }
  }

  private void UpdateOrCreateActiveSymbols(List<ThreatData> currentThreats) {
    foreach (var threatData in currentThreats) {
      if (threatData.Status == ThreatStatus.DESTROYED) {
        RemoveSymbol(threatData);
        continue;
      }

      if (_activeSymbols.TryGetValue(threatData, out GameObject symbolObj)) {
        UpdateSymbolPosition(symbolObj, threatData.Threat.transform.position);
        UpdateSymbolScale(symbolObj);
      } else {
        CreateSymbol(threatData);
      }
    }
  }

  private GameObject CreateSymbolPrefab() {
    return Instantiate(Resources.Load<GameObject>("Prefabs/Symbols/SymbolPrefab"),
                       _radarUIGroupRectTransform);
  }

  private void CreateOriginSymbol() {
    GameObject symbolPrefab = CreateSymbolPrefab();
    symbolPrefab.GetComponent<TacticalSymbol>().SetSprite("friendly_destroyer_present");
    symbolPrefab.GetComponent<TacticalSymbol>().DisableDirectionArrow();
    symbolPrefab.GetComponent<TacticalSymbol>().SetSpeedAlt("");
    symbolPrefab.GetComponent<TacticalSymbol>().SetType("");
    symbolPrefab.GetComponent<TacticalSymbol>().SetUniqueDesignator("");
    _originSymbols.Add(symbolPrefab);
  }

  private void CreateSymbol(ThreatData threatData) {
    GameObject symbolPrefab = CreateSymbolPrefab();

    TacticalSymbol tacticalSymbol = symbolPrefab.GetComponent<TacticalSymbol>();
    tacticalSymbol.SetSprite(threatData.Threat.staticAgentConfig.symbolPresent);

    // Set rotation to the direction of the velocity vector projected onto the horizontal plane
    // where Z is right and X is up on the new radar plane
    tacticalSymbol.SetDirectionArrowRotation(
        Mathf.Atan2(threatData.Threat.GetVelocity().z, threatData.Threat.GetVelocity().x) *
        Mathf.Rad2Deg);

    // Set the speed and altitude text
    // Typically "SPD/ALT"
    tacticalSymbol.SetSpeedAlt(
        $"{ConvertMpsToKnots(threatData.Threat.GetVelocity().magnitude):F0}kts" +
        $"/{ConvertMetersToFeet(threatData.Threat.transform.position.y):F0}ft");

    // Set the type text
    tacticalSymbol.SetType(threatData.Threat.staticAgentConfig.agentClass.ToString());

    // Set the unique designator text
    tacticalSymbol.SetUniqueDesignator(threatData.ThreatID);

    UpdateSymbolPosition(symbolPrefab, threatData.Threat.transform.position);
    UpdateSymbolRotation(symbolPrefab, threatData.Threat.transform.forward);
    UpdateSymbolScale(symbolPrefab);
    _activeSymbols.Add(threatData, symbolPrefab);
  }

  private float ConvertMetersToFeet(float meters) {
    return meters * 3.28084f;
  }

  private void UpdateSymbolPosition(GameObject symbolObj, Vector3 threatPosition) {
    symbolObj.transform.localPosition =
        new Vector3(threatPosition.z / 1000.0f, threatPosition.x / 1000.0f, 0f);
  }
  private void UpdateSymbolRotation(GameObject symbolObj, Vector3 forward) {
    symbolObj.GetComponent<TacticalSymbol>().SetDirectionArrowRotation(
        -1 * Mathf.Atan2(forward.z, forward.x) * Mathf.Rad2Deg);
  }

  private float ConvertMpsToKnots(float mps) {
    return mps * 1.94384f;
  }

  private void RemoveSymbol(ThreatData threatData) {
    if (_activeSymbols.TryGetValue(threatData, out GameObject symbolObj)) {
      Destroy(symbolObj);
      _activeSymbols.Remove(threatData);
    }
  }

  private void ClearAllSymbols() {
    foreach (var symbol in _activeSymbols.Values) {
      Destroy(symbol);
    }
    _activeSymbols.Clear();
  }

  public void ZoomIn(float amount) {
    AdjustRadarScale(amount * _panelZoomSpeed);
  }

  public void ZoomOut(float amount) {
    AdjustRadarScale(-amount * _panelZoomSpeed);
  }

  public void Pan(Vector2 delta) {
    Vector3 currentPos = _radarUIGroupRectTransform.localPosition;
    _radarUIGroupRectTransform.localPosition =
        new Vector3(currentPos.x + delta.x, currentPos.y + delta.y, currentPos.z);
  }

  public void PanWithKeyboard(Vector2 direction) {
    Vector2 delta = direction * _keyboardPanSpeed * Time.deltaTime;
    Pan(delta);
  }

  private void AdjustRadarScale(float amount) {
    Vector3 newScale = _radarUIGroupRectTransform.localScale + new Vector3(amount, amount, 0f);

    // Prevent negative or too small scaling
    if (newScale.x < 0.01f || newScale.y < 0.01f) {
      return;
    }

    _radarUIGroupRectTransform.localScale = newScale;

    // Update all existing symbols' scales
    foreach (var symbol in _activeSymbols.Values) {
      UpdateSymbolScale(symbol);
    }
    foreach (var symbol in _activeInterceptorSymbols.Values) {
      UpdateSymbolScale(symbol);
    }
    foreach (var symbol in _originSymbols) {
      UpdateSymbolScale(symbol);
    }
  }

  private void UpdateSymbolScale(GameObject symbolObj) {
    // Calculate inverse scale to maintain constant visual size
    float inverseScale = 2f / _radarUIGroupRectTransform.localScale.x;
    symbolObj.transform.localScale = new Vector3(inverseScale, inverseScale, 1f);
  }

  private void UpdateInterceptorSymbols() {
    var currentInterceptors = _iads.GetInterceptorTable();

    RemoveInactiveInterceptorSymbols(currentInterceptors);
    UpdateOrCreateActiveInterceptorSymbols(currentInterceptors);
  }

  private void RemoveInactiveInterceptorSymbols(List<InterceptorData> currentInterceptors) {
    var interceptorsToRemove = _activeInterceptorSymbols.Keys.Where(interceptor => !currentInterceptors.Contains(interceptor)).ToList();

    foreach (var interceptor in interceptorsToRemove) {
      Destroy(_activeInterceptorSymbols[interceptor]);
      _activeInterceptorSymbols.Remove(interceptor);
    }
  }

  private void UpdateOrCreateActiveInterceptorSymbols(List<InterceptorData> currentInterceptors) {
    foreach (var interceptorData in currentInterceptors) {
      if (interceptorData.Status == InterceptorStatus.DESTROYED) {
        RemoveInterceptorSymbol(interceptorData);
        continue;
      }

      if (_activeInterceptorSymbols.TryGetValue(interceptorData, out GameObject symbolObj)) {
        UpdateSymbolPosition(symbolObj, interceptorData.Interceptor.transform.position);
        UpdateSymbolScale(symbolObj);
      } else {
        CreateInterceptorSymbol(interceptorData);
      }
    }
  }

  private void CreateInterceptorSymbol(InterceptorData interceptorData) {
    GameObject symbolPrefab = CreateSymbolPrefab();

    TacticalSymbol tacticalSymbol = symbolPrefab.GetComponent<TacticalSymbol>();

    tacticalSymbol.SetSprite(interceptorData.Interceptor.staticAgentConfig.symbolPresent);

    // Set rotation to the direction of the velocity vector
    tacticalSymbol.SetDirectionArrowRotation(
        Mathf.Atan2(interceptorData.Interceptor.GetVelocity().z, interceptorData.Interceptor.GetVelocity().x) * Mathf.Rad2Deg
    );

    // Set the speed and altitude text
    tacticalSymbol.SetSpeedAlt(
        $"{ConvertMpsToKnots(interceptorData.Interceptor.GetVelocity().magnitude):F0}kts" +
        $"/{ConvertMetersToFeet(interceptorData.Interceptor.transform.position.y):F0}ft"
    );

    // Set the type text
    tacticalSymbol.SetType("Interceptor");

    // Set the unique designator text
    tacticalSymbol.SetUniqueDesignator(interceptorData.InterceptorID);

    UpdateSymbolPosition(symbolPrefab, interceptorData.Interceptor.transform.position);
    UpdateSymbolRotation(symbolPrefab, interceptorData.Interceptor.transform.forward);
    UpdateSymbolScale(symbolPrefab);
    _activeInterceptorSymbols.Add(interceptorData, symbolPrefab);
  }

  private void RemoveInterceptorSymbol(InterceptorData interceptorData) {
    if (_activeInterceptorSymbols.TryGetValue(interceptorData, out GameObject symbolObj)) {
      Destroy(symbolObj);
      _activeInterceptorSymbols.Remove(interceptorData);
    }
  }
}
