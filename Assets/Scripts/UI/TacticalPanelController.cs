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
  private readonly Dictionary<TrackFileData, GameObject> _trackSymbols =
      new Dictionary<TrackFileData, GameObject>();

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
    UpdateTrackSymbols(_iads.GetThreatTracks());
    UpdateTrackSymbols(_iads.GetInterceptorTracks());
  }

  private void UpdateTrackSymbols<T>(List<T> currentTracks) where T : TrackFileData {
    // Remove inactive symbols
    var tracksToRemove = _trackSymbols.Keys
        .OfType<T>()
        .Where(track => !currentTracks.Contains(track))
        .ToList();

    foreach (var track in tracksToRemove) {
      RemoveTrackSymbol(track);
    }

    // Update or create active symbols
    foreach (var track in currentTracks) {
      if (track.Status == TrackStatus.DESTROYED) {
        RemoveTrackSymbol(track);
        continue;
      }

      if (_trackSymbols.TryGetValue(track, out GameObject symbolObj)) {
        UpdateSymbolPosition(symbolObj, track.Agent.transform.position);
        UpdateSymbolScale(symbolObj);
        UpdateSymbolRotation(symbolObj, track.Agent.transform.forward);
        UpdateSymbolSpeedAlt(symbolObj.GetComponent<TacticalSymbol>(), track);
      } else {
        CreateTrackSymbol(track);
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

  private void CreateTrackSymbol(TrackFileData trackFile) {
    GameObject symbolObj = CreateSymbolPrefab();
    TacticalSymbol tacticalSymbol = symbolObj.GetComponent<TacticalSymbol>();

    // Set common properties
    tacticalSymbol.SetSprite(trackFile.Agent.staticAgentConfig.symbolPresent);
    tacticalSymbol.SetDirectionArrowRotation(
        Mathf.Atan2(trackFile.Agent.GetVelocity().z, 
                   trackFile.Agent.GetVelocity().x) * Mathf.Rad2Deg);

    UpdateSymbolSpeedAlt(tacticalSymbol, trackFile);

    // Set type-specific properties
    if (trackFile is ThreatData) {
      tacticalSymbol.SetType(trackFile.Agent.staticAgentConfig.agentClass.ToString());
    } else if (trackFile is InterceptorData) {
      tacticalSymbol.SetType("Interceptor");
    }
 
    tacticalSymbol.SetUniqueDesignator(trackFile.TrackID);

    UpdateSymbolPosition(symbolObj, trackFile.Agent.transform.position);
    UpdateSymbolRotation(symbolObj, trackFile.Agent.transform.forward);
    UpdateSymbolScale(symbolObj);
    
    _trackSymbols.Add(trackFile, symbolObj);
  }

  private void UpdateSymbolSpeedAlt(TacticalSymbol tacticalSymbol, TrackFileData trackFile) {
    tacticalSymbol.SetSpeedAlt(
        $"{Utilities.ConvertMpsToKnots(trackFile.Agent.GetVelocity().magnitude):F0}kts/" +
        $"{Utilities.ConvertMetersToFeet(trackFile.Agent.transform.position.y):F0}ft");
  }

  private void UpdateSymbolPosition(GameObject symbolObj, Vector3 threatPosition) {
    symbolObj.transform.localPosition =
        new Vector3(threatPosition.z / 1000.0f, threatPosition.x / 1000.0f, 0f);
  }
  private void UpdateSymbolRotation(GameObject symbolObj, Vector3 forward) {
    symbolObj.GetComponent<TacticalSymbol>().SetDirectionArrowRotation(
        -1 * Mathf.Atan2(forward.z, forward.x) * Mathf.Rad2Deg);
  }

  private void RemoveTrackSymbol(TrackFileData trackFile) {
    if (_trackSymbols.TryGetValue(trackFile, out GameObject symbolObj)) {
      Destroy(symbolObj);
      _trackSymbols.Remove(trackFile);
    }
  }

  private void ClearAllSymbols() {
    foreach (var symbol in _trackSymbols.Values) {
      Destroy(symbol);
    }
    _trackSymbols.Clear();
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
    foreach (var symbol in _trackSymbols.Values) {
      UpdateSymbolScale(symbol);
    }
    // Temporarily necessary until we implement IADS vessel system
    UpdateSymbolScale(_originSymbols[0]);
  }

  private void UpdateSymbolScale(GameObject symbolObj) {
    // Calculate inverse scale to maintain constant visual size
    float inverseScale = 2f / _radarUIGroupRectTransform.localScale.x;
    symbolObj.transform.localScale = new Vector3(inverseScale, inverseScale, 1f);
  }
}
