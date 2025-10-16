using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.UIElements;

public class UIManager : MonoBehaviour {
  public static UIManager Instance { get; private set; }

  [SerializeField]
  [Tooltip("The UI panel that renders the tactical view for TACTICAL mode")]
  private GameObject _tacticalPanel;

  [SerializeField]
  [Tooltip("The UI panel that renders the camera view for THREE_DIMENSIONAL mode")]
  private GameObject _cameraPanel;

  [SerializeField]

  private GameObject _configSelectorPanel;
  private TMP_Dropdown _configDropdown;
  public TextMeshProUGUI agentPanelText;
  public TextMeshProUGUI simTimeText;
  public TextMeshProUGUI interceptorCostText;
  public TextMeshProUGUI threatCostText;
  public TextMeshProUGUI netCostText;

  public TextMeshProUGUI intrHitTextHandle;
  public TextMeshProUGUI intrMissTextHandle;
  public TextMeshProUGUI intrRemainTextHandle;
  public TextMeshProUGUI thrtRemainTextHandle;

  public TextMeshProUGUI actionMessageTextHandle;
  public TextMeshProUGUI pActionMessageTextHandle;
  public TextMeshProUGUI ppActionMessageTextHandle;
  public TextMeshProUGUI ppppActionMessageTextHandle;
  public TextMeshProUGUI pppppActionMessageTextHandle;

  private int _intrHitCount = 0;
  private int _intrMissCount = 0;
  private int _intrRemainCount = 0;
  private int _thrtRemainCount = 0;
  public TMP_FontAsset GlobalFont;

  private UIMode curMode = UIMode.THREE_DIMENSIONAL;

  private VisualElement _root;
  [SerializeField]
  private UIDocument _mainOverlay;
  private MultiColumnListView _messageListView;

  // Start is called before the first frame update
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

    _mainOverlay.GetComponent<UITKMainOverlay>().MessageListController.LogNewMessage(message);
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
    _configSelectorPanel.GetComponentInChildren<UnityEngine.UI.Button>().onClick.AddListener(
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
    SimManager.Instance.LoadNewConfig(selectedConfig);
    _configSelectorPanel.SetActive(false);
  }

  public void ToggleUIMode() {
    SetUIMode(curMode == UIMode.THREE_DIMENSIONAL ? UIMode.TACTICAL : UIMode.THREE_DIMENSIONAL);
  }

  public void SetUIMode(UIMode mode) {
    curMode = mode;
    _cameraPanel.SetActive(mode == UIMode.THREE_DIMENSIONAL);
    _tacticalPanel.SetActive(mode == UIMode.TACTICAL);
  }

  public UIMode GetUIMode() {
    return curMode;
  }

  public void SetAgentPanelText(string text) {
    agentPanelText.text = text;
  }

  public string GetSwarmPanelText() {
    return agentPanelText.text;
  }

  private void UpdateSwarmPanel() {
    string agentPanelText = "";
    foreach (Agent agent in SimManager.Instance.GetActiveAgents()) {
      string jobText = agent.name + "| Phase: " + agent.GetFlightPhase().ToString();
      agentPanelText += jobText + "\n";
    }
    SetAgentPanelText(agentPanelText);
  }

  private void UpdateSimTimeText() {
    string simTimeText =
        "Elapsed Sim Time: " + SimManager.Instance.GetElapsedSimulationTime().ToString("F2");
    float expectedSimTimeAdvance = Time.unscaledDeltaTime * Time.timeScale;
    float actualSimTimeAdvance = Time.deltaTime;

    // Allow a small epsilon to account for floating-point precision errors
    // if (actualSimTimeAdvance < expectedSimTimeAdvance - 0.001f) {
    //   simTimeText.text += "\nThrottling time to meet physics rate";
    // }
    _mainOverlay.GetComponent<UITKMainOverlay>().UpdateSimTimeText(simTimeText);
  }

  private void UpdateTotalCostText() {
    double interceptorCost = SimManager.Instance.GetCostLaunchedInterceptors();
    double threatCost = SimManager.Instance.GetCostDestroyedThreats();
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
    _intrRemainCount = 0;
    _thrtRemainCount = 0;
    _intrHitCount = 0;
    _intrMissCount = 0;
    UpdateSummaryText();
  }

  private void UpdateSummaryText() {
    intrRemainTextHandle.text = _intrRemainCount.ToString();
    thrtRemainTextHandle.text = _thrtRemainCount.ToString();
    intrHitTextHandle.text = _intrHitCount.ToString();
    intrMissTextHandle.text = _intrMissCount.ToString();
  }

  private void RegisterNewInterceptor(Interceptor interceptor) {
    ++_intrRemainCount;
    interceptor.OnInterceptHit += RegisterInterceptorHit;
    interceptor.OnInterceptMiss += RegisterInterceptorMiss;
    interceptor.OnTerminated += RegisterAgentTerminated;
    UpdateSummaryText();
  }

  private void RegisterNewThreat(Threat threat) {
    ++_thrtRemainCount;
    threat.OnTerminated += RegisterAgentTerminated;
    UpdateSummaryText();
  }

  private void RegisterAgentTerminated(Agent agent) {
    if (agent is Interceptor) {
      --_intrRemainCount;
    } else if (agent is Threat) {
      --_thrtRemainCount;
    }
    UpdateSummaryText();
  }

  private void RegisterInterceptorHit(Interceptor interceptor, Threat threat) {
    ++_intrHitCount;
    UpdateSummaryText();
  }

  private void RegisterInterceptorMiss(Interceptor interceptor, Threat threat) {
    ++_intrMissCount;
    UpdateSummaryText();
  }

  void Update() {
    // UpdateSwarmPanel();
    UpdateSimTimeText();
    UpdateTotalCostText();
  }
}

public enum UIMode { THREE_DIMENSIONAL, TACTICAL }
