using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TacticalPanel : MonoBehaviour {
  // Symbol position update period in seconds.
  private const float _symbolsUpdatePeriod = 0.1f;  // 10 Hz

  // Keyboard pan speed.
  private const float _keyboardPanSpeed = 500f;

  // Zoom speed.
  private const float _zoomSpeed = 10f;

  [SerializeField]
  [Tooltip("The UI group that contains the radar symbology elements")]
  private GameObject _radarUIGroup;

  private RectTransform _radarUIGroupRectTransform;

  private TacticalPolarGridGraphic _polarGridGraphic;

  // Coroutine to update the symbols.
  private Coroutine _symbolsCoroutine;

  // Dictionary from each agent to its tactical symbol.
  private Dictionary<IAgent, GameObject> _symbols = new Dictionary<IAgent, GameObject>();

  // Origin symbol.
  // This origin symbol is needed until an asset agent is implemented.
  private GameObject _origin;

  public static TacticalPanel Instance { get; private set; }

  public void Pan(in Vector2 delta) {
    Vector3 currentPosition = _radarUIGroupRectTransform.localPosition;
    _radarUIGroupRectTransform.localPosition =
        new Vector3(currentPosition.x + delta.x, currentPosition.y + delta.y, currentPosition.z);
  }

  public void PanWithKeyboard(in Vector2 direction) {
    Vector2 delta = direction * _keyboardPanSpeed * Time.unscaledDeltaTime;
    Pan(delta);
  }

  public void ZoomIn(float amount) {
    AdjustRadarScale(amount * _zoomSpeed);
  }

  public void ZoomOut(float amount) {
    AdjustRadarScale(-amount * _zoomSpeed);
  }

  public void CycleRangeUp() {
    _polarGridGraphic.CycleRangeUp();
  }

  public void CycleRangeDown() {
    _polarGridGraphic.CycleRangeDown();
  }

  private void Awake() {
    if (Instance != null && Instance != this) {
      Destroy(gameObject);
      return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);

    _radarUIGroupRectTransform = _radarUIGroup.GetComponent<RectTransform>();
    _polarGridGraphic = _radarUIGroup.GetComponent<TacticalPolarGridGraphic>();
  }

  private void Start() {
    _radarUIGroupRectTransform.localScale = Vector3.one;
    _radarUIGroupRectTransform.localPosition = Vector3.zero;

    SimManager.Instance.OnSimulationStarted += RegisterSimulationStarted;
    SimManager.Instance.OnSimulationEnded += RegisterSimulationEnded;
    SimManager.Instance.OnNewInterceptor += RegisterNewAgent;
    SimManager.Instance.OnNewThreat += RegisterNewAgent;

    InitializeOrigin();
  }

  private void OnDestroy() {
    if (_symbolsCoroutine != null) {
      StopCoroutine(_symbolsCoroutine);
    }
  }

  private void OnDisable() {
    DestroyAllSymbols();
  }

  private void RegisterSimulationStarted() {
    _symbolsCoroutine = StartCoroutine(SymbolsManager(_symbolsUpdatePeriod));
  }

  private void RegisterSimulationEnded() {
    DestroyAllSymbols();
  }

  public void RegisterNewAgent(IAgent agent) {
    agent.OnTerminated += DestroySymbol;
    CreateSymbol(agent);
  }

  private IEnumerator SymbolsManager(float period) {
    while (true) {
      UpdateSymbols();
      yield return new WaitForSeconds(period);
    }
  }

  private void InitializeOrigin() {
    GameObject origin = CreateSymbolPrefab();
    origin.GetComponent<TacticalSymbol>().SetSprite("friendly_destroyer_present");
    origin.GetComponent<TacticalSymbol>().DisableDirectionArrow();
    origin.GetComponent<TacticalSymbol>().SetSpeedAlt("");
    origin.GetComponent<TacticalSymbol>().SetType("");
    origin.GetComponent<TacticalSymbol>().SetUniqueDesignator("");
    _origin = origin;
  }

  private GameObject CreateSymbolPrefab() {
    return Instantiate(Resources.Load<GameObject>("Prefabs/Symbols/Symbol"),
                       _radarUIGroupRectTransform);
  }

  private void CreateSymbol(IAgent agent) {
    GameObject symbol = CreateSymbolPrefab();
    TacticalSymbol tacticalSymbol = symbol.GetComponent<TacticalSymbol>();

    // Set common properties.
    tacticalSymbol.SetSprite(agent.StaticConfig.VisualizationConfig?.SymbolPresent);
    tacticalSymbol.SetDirectionArrowRotation(Mathf.Atan2(agent.Velocity.z, agent.Velocity.x) *
                                             Mathf.Rad2Deg);

    // Set type-specific properties.
    switch (agent.StaticConfig.AgentType) {
      case Configs.AgentType.Vessel:
      case Configs.AgentType.ShoreBattery:
      case Configs.AgentType.CarrierInterceptor:
      case Configs.AgentType.MissileInterceptor: {
        tacticalSymbol.SetType("Interceptor");
        break;
      }
      case Configs.AgentType.FixedWingThreat: {
        tacticalSymbol.SetType("FixedWingThreat");
        break;
      }
      case Configs.AgentType.RotaryWingThreat: {
        tacticalSymbol.SetType("RotaryWingThreat");
        break;
      }
    }
    tacticalSymbol.SetUniqueDesignator(agent.gameObject.name);
    UpdateAgentSymbol(symbol, agent);
    _symbols.Add(agent, symbol);
  }

  private void DestroySymbol(IAgent agent) {
    if (_symbols.TryGetValue(agent, out GameObject symbol)) {
      Destroy(symbol);
      _symbols.Remove(agent);
    }
  }

  private void DestroyAllSymbols() {
    foreach (var symbol in _symbols.Values) {
      Destroy(symbol);
    }
    _symbols.Clear();
  }

  private void UpdateSymbols() {
    UpdateAgentSymbols(SimManager.Instance.Interceptors);
    UpdateAgentSymbols(SimManager.Instance.Threats);
  }

  private void UpdateAgentSymbols(IEnumerable<IAgent> agents) {
    foreach (var agent in agents) {
      if (_symbols.TryGetValue(agent, out GameObject symbol)) {
        UpdateAgentSymbol(symbol, agent);
      }
    }
  }

  private void UpdateAgentSymbol(GameObject symbol, IAgent agent) {
    // Division factor for real-world scaling, e.g., 1000 meters = 1 unit on the grid.
    const float scaleDivisionFactor = 1000f;

    if (_polarGridGraphic == null) {
      Debug.LogError("TacticalPolarGridGraphic component is missing.");
      return;
    }

    // Update the symbol position.
    Vector3 position = agent.Position;
    float scaleFactor = _polarGridGraphic.CurrentScaleFactor;
    Vector3 scaledPosition =
        new Vector3(position.z / scaleDivisionFactor, position.x / scaleDivisionFactor, 0f);
    symbol.transform.localPosition = scaledPosition * scaleFactor;

    // Update the symbol scale.
    UpdateSymbolScale(symbol);

    // Update the symbol rotation.
    Vector3 forward = agent.transform.forward;
    symbol.GetComponent<TacticalSymbol>().SetDirectionArrowRotation(
        -1 * Mathf.Atan2(forward.z, forward.x) * Mathf.Rad2Deg);

    // Update the symbol's speed alternate text.
    symbol.GetComponent<TacticalSymbol>().SetSpeedAlt(
        $"{Utilities.ConvertMpsToKnots(agent.Speed):F0} kts/" +
        $"{Utilities.ConvertMetersToFeet(agent.Position.y):F0} ft");
  }

  private void UpdateSymbolScale(GameObject symbol) {
    // Calculate the inverse scale to maintain constant visual size.
    float inverseScale = 2f / _radarUIGroupRectTransform.localScale.x;
    symbol.transform.localScale = new Vector3(inverseScale, inverseScale, 1f);
  }

  // Adjusts the radar scale by the specified amount.
  private void AdjustRadarScale(float amount) {
    Vector3 newScale = _radarUIGroupRectTransform.localScale + new Vector3(amount, amount, 0f);
    // Prevent negative or too small scaling.
    if (newScale.x < 0.01f || newScale.y < 0.01f) {
      return;
    }
    _radarUIGroupRectTransform.localScale = newScale;

    // Update all existing symbols' scales.
    foreach (var symbol in _symbols.Values) {
      UpdateSymbolScale(symbol);
    }
    UpdateSymbolScale(_origin);
  }
}
