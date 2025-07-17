using System.Collections.Generic;
using UnityEngine.UIElements;
    
public class UITKMessageListController
{
    VisualTreeAsset m_ListEntryTemplate;
    ListView m_MessageListView;
    
    // Data source for messages
    private List<string> m_Messages = new List<string>();
    
    public void Initialize(ListView messageListView, VisualTreeAsset listEntryTemplate)
    {
        m_MessageListView = messageListView;
        m_ListEntryTemplate = listEntryTemplate;
        
        // Set up the ListView once during initialization
        SetupListView();
    }
    
    private void SetupListView()
    {
        // Set the data source
        m_MessageListView.itemsSource = m_Messages;
        
        // Define how to create each item (called when ListView needs new visual elements)
        m_MessageListView.makeItem = () => 
        {
            var newEntry = m_ListEntryTemplate.Instantiate();
            var controller = new UITKMessageListEntryController();
            newEntry.userData = controller;
            controller.SetVisualElement(newEntry);
            return newEntry;
        };
        
        // Define how to bind data to visual elements (called when ListView recycles elements)
        m_MessageListView.bindItem = (element, index) => 
        {
            var controller = (UITKMessageListEntryController)element.userData;
            controller.SetMessageData(m_Messages[index]);
        };
        
        // Optional: Define how to unbind when elements are recycled
        m_MessageListView.unbindItem = (element, index) => 
        {
            // Clean up if needed
        };
    }
    
    public void LogNewMessage(string message)
    {
        // Add message to data source
        m_Messages.Add(message);
        
        // Refresh the ListView to show the new item
        m_MessageListView.RefreshItems();
        
        // Optional: Scroll to the latest message
        m_MessageListView.ScrollToItem(m_Messages.Count - 1);
    }
    
    public void ClearMessages()
    {
        m_Messages.Clear();
        m_MessageListView.RefreshItems();
    }
}