using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class UIDialog : MonoBehaviour {
  [SerializeField]
  private string dialogTitle;

  [SerializeField]

  private TextMeshProUGUI dialogTitleHandle;
  [SerializeField]
  private RectTransform contentHandle;

  /// TABS
  [SerializeField]
  private float _tabWidth = 15f;
  [SerializeField]
  private float _tabHeight = 8f;
  // List of dialog tabs
  private List<GameObject> dialogTabs;

  /// ENTRIES
  private List<UISelectableEntry> entries;

  [SerializeField]
  private float entryHeight = 8f;
  [SerializeField]
  private float entryIndentWidth = 4f;

  private List<UISelectableEntry> cleanupPool;

  private bool isOpen;

  // Start is called before the first frame update
  public virtual void Start() {
    dialogTitleHandle.text = dialogTitle;
    dialogTitleHandle.font = UIManager.Instance.GlobalFont;
    isOpen = gameObject.activeSelf;
    if (dialogTabs != null) {
      foreach (GameObject tab in dialogTabs) {
        Destroy(tab);
      }
    }

    if (cleanupPool != null) {
      ClearDialogEntries();
    }
    /*
    if(entries != null) {
      foreach(UISelectableEntry entry in entries) {
        Destroy(entry.gameObject);
      }
    }
    */
    dialogTabs = new List<GameObject>();
    entries = new List<UISelectableEntry>();
    cleanupPool = new List<UISelectableEntry>();
  }

  internal RectTransform GetContentHandle() {
    return contentHandle;
  }

  public bool IsOpen() {
    return isOpen;
  }

  protected virtual void OnEnable() {
    isOpen = true;
  }
  protected virtual void OnDisable() {
    isOpen = false;
  }

  public float GetTabWidth() {
    return _tabWidth;
  }
  public float GetTabHeight() {
    return _tabHeight;
  }

  /// <summary>
  /// Returns the height of the dialog title bar
  /// </summary>
  public float GetTitleBarHeight() {
    return dialogTitleHandle.rectTransform.sizeDelta.y;
  }

  /// <summary>
  /// Adds a new tab to the dialog, when clicked it will call the given callback
  /// </summary>
  public void AddDialogTab(string tabName, Action onClick) {
    dialogTabs.Add(AddTabButton(tabName, onClick));
  }

  /// <summary>
  /// Add the tab button to the right of the existing tabs
  /// </summary>
  private GameObject AddTabButton(string tabName, Action onClick) {
    GameObject tabButton = new GameObject("TabButton", typeof(RectTransform));

    // Pass false in the second parameter to keep local scale at (1,1,1)
    tabButton.transform.SetParent(transform, false);

    // Optional: Force localScale to one, if you want to be explicit
    // tabButton.transform.localScale = Vector3.one;

    // RectTransform setup
    RectTransform rTransform = tabButton.GetComponent<RectTransform>();
    rTransform.anchorMin = new Vector2(0, 1);
    rTransform.anchorMax = new Vector2(0, 1);
    rTransform.pivot = new Vector2(0, 1);
    rTransform.sizeDelta = new Vector2(_tabWidth, _tabHeight);

    // Calculate anchoredPosition based on how many tabs exist
    rTransform.anchoredPosition = new Vector2(_tabWidth * dialogTabs.Count, -(GetTitleBarHeight()));

    // Add the onClick callback to the button
    Button button = tabButton.AddComponent<Button>();
    button.onClick.AddListener(() => onClick());

    // Add the image to the button and link it to the tab
    button.targetGraphic = tabButton.AddComponent<Image>();

    // Create the child text object
    AddTabText(tabName, tabButton);

    return tabButton;
  }

  /// <summary>
  /// Add text as a child of the tab's button object
  /// </summary>
  private void AddTabText(string tabName, GameObject tabButton) {
    GameObject tabText = new GameObject("TabText", typeof(RectTransform));

    // Again, pass false to avoid messing up the new child's scale
    tabText.transform.SetParent(tabButton.transform, false);

    // Optional: Force localScale to one, if you want to be explicit
    // tabText.transform.localScale = Vector3.one;

    // RectTransform setup
    RectTransform textRectTransform = tabText.GetComponent<RectTransform>();
    textRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
    textRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
    textRectTransform.pivot = new Vector2(0.5f, 0.5f);
    textRectTransform.sizeDelta = new Vector2(_tabWidth, _tabHeight);
    textRectTransform.anchoredPosition = new Vector2(0, 0);

    // Create the TextMeshProUGUI component
    TextMeshProUGUI buttonText = tabText.AddComponent<TextMeshProUGUI>();
    buttonText.text = tabName;
    buttonText.font = UIManager.Instance.GlobalFont;
    buttonText.fontSize = 6;
    buttonText.color = Color.black;
    buttonText.alignment = TextAlignmentOptions.Center;
    buttonText.verticalAlignment = VerticalAlignmentOptions.Middle;
  }

  public virtual UISelectableEntry CreateSelectableEntry() {
    // Create a new entry object with content handle as parent
    GameObject go = Instantiate(Resources.Load<GameObject>("Prefabs/EmptyObject"), contentHandle);
    go.name = "UISelectableEntry";
    UISelectableEntry entry = go.AddComponent<UISelectableEntry>();
    entry.SetParent(this);
    // add to cleanup pool so we can clear later in clear dialog entries
    cleanupPool.Add(entry);
    return entry;
  }

  public void ClearDialogEntries() {
    if (cleanupPool == null)
      return;
    foreach (UISelectableEntry entry in cleanupPool) {
      GameObject.Destroy(entry.gameObject);
    }
    cleanupPool.Clear();
    if (entries != null)
      entries.Clear();
  }

  /// <summary>
  /// Clears, sets, and prints the dialog entries in the order they were added
  /// </summary>
  public virtual void SetDialogEntries(List<UISelectableEntry> entries) {
    this.entries = entries;
    // calculate total height of the content
    float heightHead = -1 * GetTabHeight();
    int count = 0;
    foreach (UISelectableEntry entry in this.entries) {
      (heightHead, count) = RecursiveContentPrint(entry, 1, heightHead, count);
    }
    contentHandle.sizeDelta =
        new Vector2(contentHandle.sizeDelta.x, count * entryHeight + Mathf.Abs(heightHead));
  }

  public virtual void SetDialogEntries(UISelectableEntry entry) {
    SetDialogEntries(new List<UISelectableEntry>() { entry });
  }

  private (float, int)
      RecursiveContentPrint(UISelectableEntry entry, int depth, float heightHead, int count) {
    RectTransform rTransform = entry.GetComponent<RectTransform>();
    rTransform.anchorMin = new Vector2(0, 1);
    rTransform.anchorMax = new Vector2(1, 1);
    rTransform.pivot = new Vector2(0.5f, 1f);

    rTransform.anchoredPosition = new Vector2(0, heightHead);  // positioning from top
    rTransform.sizeDelta = new Vector2(0, entryHeight);
    float padding = 5f;
    rTransform.SetRight(padding);
    rTransform.SetLeft(padding);
    // actually indent the text
    entry.GetTextTransform(0).anchoredPosition = new Vector2(entryIndentWidth * depth, 0);
    heightHead -= entryHeight;
    ++count;
    // Print the children
    if (entry.GetChildEntries() != null) {
      foreach (UISelectableEntry child in entry.GetChildEntries()) {
        (heightHead, count) = RecursiveContentPrint(child, depth + 1, heightHead, count);
      }
    }
    return (heightHead, count);
  }

  // Update is called once per frame
  void Update() {}
}
