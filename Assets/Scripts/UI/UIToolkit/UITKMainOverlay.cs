using UnityEngine;
using UnityEngine.UIElements;
    
public class UITKMainOverlay : MonoBehaviour
{
    [SerializeField]
    VisualTreeAsset m_ListEntryTemplate;

    public UITKMessageListController MessageListController;

    void Awake()
    {
        MessageListController = new UITKMessageListController();
    }
    
    void OnEnable()
    {
        // The UXML is already instantiated by the UIDocument component
        var uiDocument = GetComponent<UIDocument>();
        MessageListController.Initialize(uiDocument.rootVisualElement.Q<ListView>("MessageListView"), m_ListEntryTemplate);

        var myButton = uiDocument.rootVisualElement.Q<Button>("ClearMarkersButton");
        myButton.RegisterCallback<ClickEvent>(ClearMarkers);
    }

    void ClearMarkers(ClickEvent evt) {
        ParticleManager.Instance.ClearHitMarkers();
    }
}