using UnityEngine;
using UnityEngine.UIElements;
    
public class UITKMainOverlay : MonoBehaviour
{
    [SerializeField]
    VisualTreeAsset m_ListEntryTemplate;

    public UITKMessageListController MessageListController;

    public UIDialog LoadConfigDialog;

    void Awake()
    {
        MessageListController = new UITKMessageListController();
    }
    
    void OnEnable()
    {
        // The UXML is already instantiated by the UIDocument component
        var uiDocument = GetComponent<UIDocument>();
        MessageListController.Initialize(uiDocument.rootVisualElement.Q<ListView>("MessageListView"), m_ListEntryTemplate);

        var clearMarkersButton = uiDocument.rootVisualElement.Q<Button>("ClearMarkersButton");
        clearMarkersButton.RegisterCallback<ClickEvent>(ClearMarkersEvent);

        var restartButton = uiDocument.rootVisualElement.Q<Button>("RestartButton");
        restartButton.RegisterCallback<ClickEvent>(RestartSimEvent);

        var loadConfigButton = uiDocument.rootVisualElement.Q<Button>("LoadConfigButton");
        loadConfigButton.RegisterCallback<ClickEvent>(LoadConfigEvent);

        var quitAppButton = uiDocument.rootVisualElement.Q<Button>("QuitAppButton");
        quitAppButton.RegisterCallback<ClickEvent>(QuitAppEvent);
    }

    void ClearMarkersEvent(ClickEvent evt) {
        ParticleManager.Instance.ClearHitMarkers();
    }

    void RestartSimEvent(ClickEvent evt) {
        SimManager.Instance.RestartSimulation();
    }

    void LoadConfigEvent(ClickEvent evt) {
        UIManager.Instance.ToggleConfigSelectorPanel();
    }

    void QuitAppEvent(ClickEvent evt) {
        SimManager.Instance.QuitSimulation();
    }

    public void UpdateSimTimeText(string simTimeText) {
        var uiDocument = GetComponent<UIDocument>();
        uiDocument.rootVisualElement.Q<Label>("SimTimeLabel").text = simTimeText;
    }
}