using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UIManager : MonoBehaviour {
  public static UIManager Instance { get; private set; }

  [SerializeField]
  [Tooltip("The UI panel that renders the camera view for THREE_DIMENSIONAL mode")]
  private GameObject _cameraPanel = null!;

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

  private int _numInterceptorHits = 0;
  private int _numInterceptorMisses = 0;
  private int _numInterceptorsRemaining = 0;
  private int _numThreatsRemaining = 0;
  public TMP_FontAsset GlobalFont;

  private UIMode curMode = UIMode.THREE_DIMENSIONAL;

  void Awake() {
    if (Instance == null)
      Instance = this;
    else
      Destroy(gameObject);
  }

  void Start() {
    SetUIMode(UIMode.THREE_DIMENSIONAL);
    _configSelectorPanel.SetActive(false);
    SetupConfigSelectorPanel();
    SimManager.Instance.OnNewInterceptor += RegisterNewInterceptor;
    SimManager.Instance.OnNewThreat += RegisterNewThreat;
    SimManager.Instance.OnSimulationEnded += RegisterSimulationEnded;
    actionMessageTextHandle.text = "";
    pActionMessageTextHandle.text = "";
    ppActionMessageTextHandle.text = "";
    ppppActionMessageTextHandle.text = "";
    pppppActionMessageTextHandle.text = "";
  }

  public void LogAction(string message, Color color) {
    // Shift existing messages to older slots with faded colors.
    pppppActionMessageTextHandle.text = ppppActionMessageTextHandle.text;
    pppppActionMessageTextHandle.color =
        ppppActionMessageTextHandle.color * 0.8f;  // Fade color by 20%.

    ppppActionMessageTextHandle.text = ppActionMessageTextHandle.text;
    ppppActionMessageTextHandle.color = ppActionMessageTextHandle.color * 0.85f;

    ppActionMessageTextHandle.text = pActionMessageTextHandle.text;
    ppActionMessageTextHandle.color = pActionMessageTextHandle.color * 0.85f;

    pActionMessageTextHandle.text = actionMessageTextHandle.text;
    pActionMessageTextHandle.color = actionMessageTextHandle.color * 0.9f;

    // Set new message.
    actionMessageTextHandle.text = message;
    actionMessageTextHandle.color = color;
  }

  public void LogActionMessage(string message) {
    LogAction(message, Color.white);
  }

  public void LogActionWarning(string message) {
    LogAction(message, Color.yellow);
  }

  public void LogActionError(string message) {
    LogAction(message, Color.red);
  }

  public void ToggleConfigSelectorPanel() {
    _configSelectorPanel.SetActive(!_configSelectorPanel.activeSelf);
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

  public void ToggleUIMode() {
    SetUIMode(curMode == UIMode.THREE_DIMENSIONAL ? UIMode.TACTICAL : UIMode.THREE_DIMENSIONAL);
  }

  public void SetUIMode(UIMode mode) {
    curMode = mode;
    _cameraPanel.SetActive(mode == UIMode.THREE_DIMENSIONAL);
  }

  public UIMode GetUIMode() {
    return curMode;
  }

  private void UpdateSimTimeText() {
    simTimeText.text =
        "Elapsed Sim Time: " + SimManager.Instance.ElapsedSimulationTime.ToString("F2");
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

  private void RegisterSimulationEnded() {
    _numInterceptorsRemaining = 0;
    _numThreatsRemaining = 0;
    _numInterceptorHits = 0;
    _numInterceptorMisses = 0;
    UpdateSummaryText();
  }

  private void UpdateSummaryText() {
    interceptorRemainingTextHandle.text = _numInterceptorsRemaining.ToString();
    threatRemainingTextHandle.text = _numThreatsRemaining.ToString();
    interceptorHitTextHandle.text = _numInterceptorHits.ToString();
    interceptorMissTextHandle.text = _numInterceptorMisses.ToString();
  }

  private void RegisterNewInterceptor(IInterceptor interceptor) {
    ++_numInterceptorsRemaining;
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

  private void RegisterAgentTerminated(IAgent agent) {
    if (agent is IInterceptor) {
      --_numInterceptorsRemaining;
    } else if (agent is IThreat) {
      --_numThreatsRemaining;
    }
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

  void Update() {
    UpdateSimTimeText();
    UpdateTotalCostText();
  }
}

public enum UIMode { THREE_DIMENSIONAL, TACTICAL }
