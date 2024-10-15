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
  private GameObject _agentStatusPanel;
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

  private int _intrHitCount = 0;
  private int _intrMissCount = 0;
  private int _intrRemainCount = 0;
  private int _thrtRemainCount = 0;
  public TMP_FontAsset Font;

  private UIMode curMode = UIMode.NONE;

  // Start is called before the first frame update
  void Awake() {
    // singleton
    if (Instance == null)
      Instance = this;
    else
      Destroy(gameObject);
  }

  void Start() {
    _configSelectorPanel.SetActive(false);
    SetupConfigSelectorPanel();
    // inputManager = InputManager.Instance;
    // worldManager = WorldManager.Instance;
    SimManager.Instance.OnNewInterceptor += RegisterNewInterceptor;
    SimManager.Instance.OnNewThreat += RegisterNewThreat;
    SimManager.Instance.OnSimulationEnded += RegisterSimulationEnded;
    actionMessageTextHandle.text = "";
    pActionMessageTextHandle.text = "";
    ppActionMessageTextHandle.text = "";
  }

  public void LogAction(string message, Color color) {
    // Shift existing messages to older slots with faded colors
    ppActionMessageTextHandle.text = pActionMessageTextHandle.text;
    ppActionMessageTextHandle.color = pActionMessageTextHandle.color * 0.5f; // Fade color by 50%

    pActionMessageTextHandle.text = actionMessageTextHandle.text;
    pActionMessageTextHandle.color = actionMessageTextHandle.color * 0.75f; // Fade color by 25%

    // Set new message
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
    string configPath = Path.Combine(Application.streamingAssetsPath, "Configs");
    string[] configFiles = Directory.GetFiles(configPath, "*.json");

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
    // if(!InputManager.Instance.mouseActive){
    //     InputManager.Instance.mouseActive = true;
    // }
  }

  public void SetUIMode(UIMode mode) {
    curMode = mode;
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
    simTimeText.text =
        "Elapsed Sim Time: " + SimManager.Instance.GetElapsedSimulationTime().ToString("F2");
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
    _intrRemainCount++;
    interceptor.OnInterceptHit += RegisterInterceptorHit;
    interceptor.OnInterceptMiss += RegisterInterceptorMiss;
    interceptor.OnTerminated += RegisterAgentTerminated;
    UpdateSummaryText();
  }

  private void RegisterNewThreat(Threat threat) {
    _thrtRemainCount++;
    threat.OnTerminated += RegisterAgentTerminated;
    UpdateSummaryText();
  }

  private void RegisterAgentTerminated(Agent agent) 
  {
    if (agent is Interceptor) {
      _intrRemainCount--;
    } else if (agent is Threat) {
      _thrtRemainCount--;
    }
    UpdateSummaryText();
  }

  private void RegisterInterceptorHit(Interceptor interceptor, Threat threat) {
    _intrHitCount++;
    UpdateSummaryText();
  }

  private void RegisterInterceptorMiss(Interceptor interceptor, Threat threat) {
    _intrMissCount++;
    UpdateSummaryText();
  }

  // Update is called once per frame
  void Update() {
    //UpdateSwarmPanel();
    UpdateSimTimeText();
    UpdateTotalCostText();
  }
}

public enum UIMode { NONE, BUILD, MINE }
