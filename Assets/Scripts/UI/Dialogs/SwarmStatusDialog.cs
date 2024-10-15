using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.AI;
public class SwarmStatusDialog : UIDialog {
  // Start is called before the first frame update

  bool wasStarted = false;

  private List<List<(Agent, bool)>> interceptorSwarms = new List<List<(Agent, bool)>>();
  private List<List<(Agent, bool)>> submunitionsSwarms = new List<List<(Agent, bool)>>();
  private List<List<(Agent, bool)>> threatsSwarms = new List<List<(Agent, bool)>>();
  protected void Awake()
  {
    
  }

  public override void Start() {
    

    base.Start();

    InitDialog();
    wasStarted = true;

  }

  public void InitDialog()
  {
    SimManager.Instance.OnInterceptorSwarmChanged += RegisterInterceptorSwarmChanged;
    SimManager.Instance.OnSubmunitionsSwarmChanged += RegisterSubmunitionsSwarmChanged;
    SimManager.Instance.OnThreatSwarmChanged += RegisterThreatSwarmChanged;
    RedrawFullDialog();

    AddDialogTab("All", () => {});
    AddDialogTab("Interceptors", () => {});
    AddDialogTab("Submunitions", () => {});
    AddDialogTab("Threats", () => {});
  }

  private void RedrawFullDialog() {
    ClearDialogEntries();
    List<UISelectableEntry> parents = CreateParentEntries(); 
    List<UISelectableEntry> interceptorChildren = CreateInterceptorSwarmEntries(interceptorSwarms);
    List<UISelectableEntry> submunitionsChildren = CreateSubmunitionsSwarmEntries(submunitionsSwarms);
    List<UISelectableEntry> threatsChildren = CreateThreatSwarmEntries(threatsSwarms);
    parents[1].SetChildEntries(interceptorChildren);
    parents[2].SetChildEntries(submunitionsChildren);
    parents[3].SetChildEntries(threatsChildren);
    SetDialogEntries(parents);
  }

  private List<UISelectableEntry> CreateParentEntries() {
    List<UISelectableEntry> children = new List<UISelectableEntry>();

    UISelectableEntry masterTitleBar = CreateSelectableEntry();
    masterTitleBar.SetTextContent(new List<string>(new string[] { "Swarm Name", "Active Agents" }));
    masterTitleBar.SetIsSelectable(false);

    UISelectableEntry interceptorParentEntry = CreateSelectableEntry();
    interceptorParentEntry.SetTextContent(new List<string>(new string[] { "Interceptor Swarms" }));
    interceptorParentEntry.SetIsSelectable(false);

    UISelectableEntry submunitionsParentEntry = CreateSelectableEntry();
    submunitionsParentEntry.SetTextContent(new List<string>(new string[] { "Submunitions Swarms" }));
    submunitionsParentEntry.SetIsSelectable(false);

    UISelectableEntry threatsParentEntry = CreateSelectableEntry();
    threatsParentEntry.SetTextContent(new List<string>(new string[] { "Threat Swarms" }));
    threatsParentEntry.SetIsSelectable(false);

    children.Add(masterTitleBar);
    children.Add(interceptorParentEntry);
    children.Add(submunitionsParentEntry);
    children.Add(threatsParentEntry);
    return children;
  }

  private void OnClickSwarmEntry(object swarm) {
    List<(Agent, bool)> swarmTuple = (List<(Agent, bool)>)swarm;
    List<Agent> swarmAgents = swarmTuple.ConvertAll(agent => agent.Item1);
    CameraController.Instance.SnapToSwarm(swarmAgents, false);
  }

  private List<UISelectableEntry> CreateInterceptorSwarmEntries(List<List<(Agent, bool)>> swarms) {
    List<UISelectableEntry> children = new List<UISelectableEntry>();
    foreach (var swarm in swarms) {
      string swarmTitle = SimManager.Instance.GenerateInterceptorSwarmTitle(swarm);
      int activeCount = swarm.Count(agent => agent.Item2);
      UISelectableEntry entry = CreateSelectableEntry();
      entry.SetTextContent(new List<string>(new string[] { swarmTitle, activeCount.ToString() }));
      entry.SetParent(this);
      entry.SetClickCallback(OnClickSwarmEntry, swarm);
      children.Add(entry);
    }
    return children;
  }

  private List<UISelectableEntry> CreateSubmunitionsSwarmEntries(List<List<(Agent, bool)>> swarms) {
    List<UISelectableEntry> children = new List<UISelectableEntry>();
    foreach (var swarm in swarms) {
      int interceptorSwarmIndex = SimManager.Instance.LookupSubmunitionSwarnIndexInInterceptorSwarm(swarm);
      string swarmTitle = SimManager.Instance.GenerateSubmunitionsSwarmTitle(swarm);
      int activeCount = swarm.Count(agent => agent.Item2);
      UISelectableEntry entry = CreateSelectableEntry();
      entry.SetTextContent(new List<string>(new string[] { swarmTitle, activeCount.ToString() }));
      entry.SetParent(this);
      entry.SetClickCallback(OnClickSwarmEntry, swarm);
      children.Add(entry);
    }
    return children;
  }

  private List<UISelectableEntry> CreateThreatSwarmEntries(List<List<(Agent, bool)>> swarms) {
    List<UISelectableEntry> children = new List<UISelectableEntry>();
    foreach (var swarm in swarms) {
      string swarmTitle = SimManager.Instance.GenerateThreatSwarmTitle(swarm);
      int activeCount = swarm.Count(agent => agent.Item2);
      UISelectableEntry entry = CreateSelectableEntry();
      entry.SetTextContent(new List<string>(new string[] { swarmTitle, activeCount.ToString() }));
      entry.SetParent(this);
      entry.SetClickCallback(OnClickSwarmEntry, swarm);
      children.Add(entry);
    }
    return children;
  }

  private void RegisterInterceptorSwarmChanged(List<List<(Agent, bool)>> swarms) 
  {
    if(isActiveAndEnabled) {
      interceptorSwarms = swarms;
      RedrawFullDialog();
    }
  }

  private void RegisterSubmunitionsSwarmChanged(List<List<(Agent, bool)>> swarms) {
    if(isActiveAndEnabled) {
      submunitionsSwarms = swarms;
      RedrawFullDialog();
    }
  }

  private void RegisterThreatSwarmChanged(List<List<(Agent, bool)>> swarms) {
    if(isActiveAndEnabled) {
      threatsSwarms = swarms;
      RedrawFullDialog();
    }
  }

  protected override void OnEnable() {
    if(!wasStarted) {
      base.Start();
      InitDialog();
    }
    base.OnEnable();
    interceptorSwarms = SimManager.Instance.GetInterceptorSwarms();
    submunitionsSwarms = SimManager.Instance.GetSubmunitionsSwarms();
    threatsSwarms = SimManager.Instance.GetThreatSwarms();
    RedrawFullDialog();
  }
  // Update is called once per frame
  void Update() {}
}
