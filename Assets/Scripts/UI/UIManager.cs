using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {
  private sealed class ActionLogEntry {
    public string Message { get; }
    public Color Color { get; }
    public string AgentId { get; }

    public ActionLogEntry(string message, Color color, string agentId) {
      Message = message;
      Color = color;
      AgentId = agentId;
    }
  }

  private const int _maxActionLogHistory = 500;

  public static UIManager Instance { get; private set; }

  [SerializeField]
  [Tooltip("The UI panel that renders the camera view for the THREE_DIMENSIONAL mode")]
  private GameObject _cameraPanel = null!;

  [SerializeField]
  [Tooltip("The UI panel that renders the tactical view for the TACTICAL mode")]
  private GameObject _tacticalPanel = null!;

  [SerializeField]
  private GameObject _configSelectorPanel = null!;
  private TMP_Dropdown _configDropdown;
  public TextMeshProUGUI simTimeText;
  public TextMeshProUGUI interceptorCostText;
  public TextMeshProUGUI threatCostText;
  public TextMeshProUGUI netCostText;

  public TextMeshProUGUI interceptorHitTextHandle;
  public TextMeshProUGUI interceptorMissTextHandle;
  public TextMeshProUGUI interceptorRemainingTextHandle;
  public TextMeshProUGUI threatRemainingTextHandle;

  public TextMeshProUGUI actionMessageTextHandle;
  public TextMeshProUGUI pActionMessageTextHandle;
  public TextMeshProUGUI ppActionMessageTextHandle;
  public TextMeshProUGUI ppppActionMessageTextHandle;
  public TextMeshProUGUI pppppActionMessageTextHandle;

  public TMP_FontAsset GlobalFont;
  private int _numInterceptorHits = 0;
  private int _numInterceptorMisses = 0;
  private int _numInterceptorsRemaining = 0;
  private int _numThreatsRemaining = 0;
  private readonly List<ActionLogEntry> _actionLogHistory = new List<ActionLogEntry>();
  private readonly HashSet<string> _agentLogFilterIds = new HashSet<string>();
  private string _followedAgentDescription = "";
  private IAgent _followedAgent = null;
  private bool _isAgentLogFilterActive = false;

  private UIMode _uiMode = UIMode.THREE_DIMENSIONAL;

  public UIMode UIMode {
    get => _uiMode;
    set {
      _uiMode = value;
      _cameraPanel.SetActive(_uiMode == UIMode.THREE_DIMENSIONAL);
      _tacticalPanel.SetActive(_uiMode == UIMode.TACTICAL);
    }
  }

  public void ToggleUIMode() {
    Array uiModeValues = Enum.GetValues(typeof(UIMode));
    int currentIndex = Array.IndexOf(uiModeValues, UIMode);
    int nextIndex = (currentIndex + 1) % uiModeValues.Length;
    UIMode = (UIMode)uiModeValues.GetValue(nextIndex);
  }

  public void ToggleConfigSelectorPanel() {
    _configSelectorPanel.SetActive(!_configSelectorPanel.activeSelf);
  }

  public void LogAction(string message, Color color, IAgent agent = null) {
    _actionLogHistory.Add(new ActionLogEntry(message, color, agent?.AgentId));
    if (_actionLogHistory.Count > _maxActionLogHistory) {
      _actionLogHistory.RemoveAt(0);
    }
    RenderActionLog();
  }

  public void LogActionMessage(string message, IAgent agent = null) {
    LogAction(message, Color.white, agent);
  }

  public void LogActionWarning(string message, IAgent agent = null) {
    LogAction(message, Color.yellow, agent);
  }

  public void LogActionError(string message, IAgent agent = null) {
    LogAction(message, Color.red, agent);
  }

  // Show only updates associated with the followed agent. The newest HUD line is reserved for the
  // persistent camera focus indicator.
  public void SetAgentLogFilter(IAgent followedAgent) {
    if (followedAgent == null) {
      ClearAgentLogFilter();
      return;
    }

    ClearFollowedAgentTargetSubscription();
    _followedAgent = followedAgent;
    if (_followedAgent.HierarchicalAgent != null) {
      _followedAgent.HierarchicalAgent.OnTargetChanged += RegisterFollowedAgentTargetChanged;
    }

    _agentLogFilterIds.Clear();
    string agentId = followedAgent.AgentId;
    if (!string.IsNullOrWhiteSpace(agentId)) {
      _agentLogFilterIds.Add(agentId);
    }

    string agentType = followedAgent.StaticConfig?.AgentType.ToString();
    string agentDescription =
        !string.IsNullOrWhiteSpace(agentId) ? agentId : followedAgent.gameObject.name;
    _followedAgentDescription = string.IsNullOrWhiteSpace(agentType)
                                    ? agentDescription
                                    : $"{agentDescription} ({agentType})";
    _isAgentLogFilterActive = true;
    RenderActionLog();
  }

  public void ClearAgentLogFilter() {
    ClearFollowedAgentTargetSubscription();
    _isAgentLogFilterActive = false;
    _followedAgentDescription = "";
    _agentLogFilterIds.Clear();
    RenderActionLog();
  }

  private void RenderActionLog() {
    TextMeshProUGUI[] handles = {
      actionMessageTextHandle,     pActionMessageTextHandle,     ppActionMessageTextHandle,
      ppppActionMessageTextHandle, pppppActionMessageTextHandle,
    };
    foreach (TextMeshProUGUI handle in handles) {
      if (handle == null) {
        continue;
      }
      handle.text = "";
      handle.color = Color.white;
    }

    int handleIndex = 0;
    if (_isAgentLogFilterActive && handles[handleIndex] != null) {
      handles[handleIndex].text =
          $"[CAM] CENTERED ON {_followedAgentDescription} | {FormatFollowedAgentTargets()}";
      handles[handleIndex].color = Color.cyan;
      ++handleIndex;
    }

    for (int i = _actionLogHistory.Count - 1; i >= 0 && handleIndex < handles.Length; --i) {
      ActionLogEntry entry = _actionLogHistory[i];
      if (_isAgentLogFilterActive && (string.IsNullOrWhiteSpace(entry.AgentId) ||
                                      !_agentLogFilterIds.Contains(entry.AgentId))) {
        continue;
      }
      if (handles[handleIndex] != null) {
        handles[handleIndex].text = entry.Message;
        handles[handleIndex].color = entry.Color * Mathf.Pow(0.85f, handleIndex);
      }
      ++handleIndex;
    }
  }

  private void Awake() {
    if (Instance != null && Instance != this) {
      Destroy(gameObject);
      return;
    }
    Instance = this;
  }

  private void Start() {
    UIMode = UIMode.THREE_DIMENSIONAL;
    _configSelectorPanel.SetActive(false);
    SetupConfigSelectorPanel();
    SimManager.Instance.OnNewInterceptor += RegisterNewInterceptor;
    SimManager.Instance.OnNewThreat += RegisterNewThreat;
    SimManager.Instance.OnSimulationEnded += RegisterSimulationEnded;
    RenderActionLog();
  }

  private void Update() {
    UpdateSimTimeText();
    UpdateTotalCostText();
  }

  private void SetupConfigSelectorPanel() {
    _configSelectorPanel.GetComponentInChildren<Button>().onClick.AddListener(
        delegate { LoadSelectedConfig(); });
    _configDropdown = _configSelectorPanel.GetComponentInChildren<TMP_Dropdown>();
    PopulateConfigDropdown();
  }

  private void PopulateConfigDropdown() {
    _configDropdown.ClearOptions();
    string configPath = ConfigLoader.GetStreamingAssetsFilePath("Configs/Simulations");
    string[] configFiles = Directory.GetFiles(configPath, "*.pbtxt");

    List<string> configFileNames = new List<string>();
    foreach (string configFile in configFiles) {
      configFileNames.Add(Path.GetFileName(configFile));
    }
    _configDropdown.AddOptions(configFileNames);
  }

  private void LoadSelectedConfig() {
    string selectedConfig = _configDropdown.options[_configDropdown.value].text;
    SimManager.Instance.LoadNewSimulationConfig(selectedConfig);
    _configSelectorPanel.SetActive(false);
  }

  private void UpdateSimTimeText() {
    simTimeText.text = "Elapsed Time: " + SimManager.Instance.ElapsedTime.ToString("F2");
    float expectedSimTimeAdvance = Time.unscaledDeltaTime * Time.timeScale;
    float actualSimTimeAdvance = Time.deltaTime;

    // Allow a small epsilon to account for floating-point precision errors.
    if (actualSimTimeAdvance < expectedSimTimeAdvance - 0.001f) {
      simTimeText.text += "\nThrottling time to meet physics rate";
    }
  }

  private void UpdateTotalCostText() {
    double interceptorCost = SimManager.Instance.CostLaunchedInterceptors;
    double threatCost = SimManager.Instance.CostDestroyedThreats;
    double netCost = interceptorCost - threatCost;

    interceptorCostText.text = $"Interceptors\n(launched)\n${FormatCost(interceptorCost)}";
    threatCostText.text = $"Threats\n(destroyed)\n${FormatCost(threatCost)}";
    netCostText.text = $"Cost\ndifference\n${FormatCost(netCost)}";
    if (netCost < 0) {
      netCostText.color = Color.green;
    } else {
      netCostText.color = Color.red;
    }
  }

  private string FormatCost(double cost) {
    double absCost = Math.Abs(cost);
    if (absCost >= 1e9)
      return $"{cost / 1e9:F2}B";
    if (absCost >= 1e6)
      return $"{cost / 1e6:F2}M";
    if (absCost >= 1e3)
      return $"{cost / 1e3:F2}k";
    return $"{cost:F2}";
  }

  private void UpdateSummaryText() {
    interceptorRemainingTextHandle.text = _numInterceptorsRemaining.ToString();
    threatRemainingTextHandle.text = _numThreatsRemaining.ToString();
    interceptorHitTextHandle.text = _numInterceptorHits.ToString();
    interceptorMissTextHandle.text = _numInterceptorMisses.ToString();
  }

  private void RegisterNewInterceptor(IInterceptor interceptor) {
    ++_numInterceptorsRemaining;
    interceptor.HierarchicalAgent.OnTargetChanged += RegisterTargetChanged;
    interceptor.OnHit += RegisterInterceptorHit;
    interceptor.OnMiss += RegisterInterceptorMiss;
    interceptor.OnTerminated += RegisterAgentTerminated;
    UpdateSummaryText();
  }

  private void RegisterNewThreat(IThreat threat) {
    ++_numThreatsRemaining;
    threat.OnTerminated += RegisterAgentTerminated;
    UpdateSummaryText();
  }

  private void RegisterInterceptorHit(IInterceptor interceptor) {
    ++_numInterceptorHits;
    UpdateSummaryText();
  }

  private void RegisterInterceptorMiss(IInterceptor interceptor) {
    ++_numInterceptorMisses;
    UpdateSummaryText();
  }

  private void RegisterAgentTerminated(IAgent agent) {
    if (agent is IInterceptor) {
      --_numInterceptorsRemaining;
      agent.HierarchicalAgent.OnTargetChanged -= RegisterTargetChanged;
    } else if (agent is IThreat) {
      --_numThreatsRemaining;
    }
    UpdateSummaryText();
  }

  private void RegisterSimulationEnded() {
    ClearAgentLogFilter();
    _numInterceptorsRemaining = 0;
    _numThreatsRemaining = 0;
    _numInterceptorHits = 0;
    _numInterceptorMisses = 0;
    UpdateSummaryText();
  }

  private void RegisterTargetChanged(IAgent agent, IReadOnlyList<string> previousTargetIds,
                                     IReadOnlyList<string> targetIds) {
    string previousTargets = FormatTargetIds(previousTargetIds);
    string targets = FormatTargetIds(targetIds);
    LogActionMessage($"[TARGET] {agent.AgentId}: [{previousTargets}] -> [{targets}].", agent);
  }

  private void RegisterFollowedAgentTargetChanged(IAgent agent,
                                                  IReadOnlyList<string> previousTargetIds,
                                                  IReadOnlyList<string> targetIds) {
    if (ReferenceEquals(agent, _followedAgent)) {
      RenderActionLog();
    }
  }

  private void ClearFollowedAgentTargetSubscription() {
    if (_followedAgent?.HierarchicalAgent != null) {
      _followedAgent.HierarchicalAgent.OnTargetChanged -= RegisterFollowedAgentTargetChanged;
    }
    _followedAgent = null;
  }

  private string FormatFollowedAgentTargets() {
    IReadOnlyList<string> targetIds = _followedAgent?.TargetIds;
    if (targetIds == null || targetIds.Count == 0) {
      return "TARGET: none";
    }
    if (targetIds.Count == 1) {
      return $"TARGET: {targetIds[0]}";
    }
    if (targetIds.Count <= 3) {
      return $"TARGETS: {string.Join(", ", targetIds)}";
    }
    return $"TARGETS: {targetIds[0]} ... {targetIds[targetIds.Count - 1]} " +
           $"({targetIds.Count} agents)";
  }

  private static string FormatTargetIds(IReadOnlyList<string> targetIds) {
    return targetIds == null || targetIds.Count == 0 ? "none" : string.Join(", ", targetIds);
  }
}
