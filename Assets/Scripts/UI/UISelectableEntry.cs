using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;
public class UISelectableEntry : EventTrigger {
  private List<UISelectableEntry> children = null!;
  private List<string> textContent = null!;

  private UIDialog parentDialog = null!;

  private RectTransform rectTransform = null!;

  private Image image = null!;

  // Replace the single TextMeshProUGUI with a list to hold multiple columns
  private List<TextMeshProUGUI> textHandles = null!;

  private bool isSelectable = true;

  private static Color baseColor = new Color32(31, 31, 45, 140);

  private Action<object> OnClickCallback = null!;
  private object callbackArgument = null!;

  public void Awake() {
    rectTransform = gameObject.AddComponent<RectTransform>();

    image = gameObject.AddComponent<Image>();
    image.type = Image.Type.Sliced;
    image.color = baseColor;

    // Initialize the list for text handles
    textHandles = new List<TextMeshProUGUI>();
  }

  public void SetClickCallback(Action<object> callback, object argument) {
    OnClickCallback = callback;
    callbackArgument = argument;
  }

  public override void OnPointerEnter(PointerEventData eventData) {
    if (isSelectable)
      image.color = baseColor + new Color32(20, 20, 20, 40);
    base.OnPointerEnter(eventData);
  }

  public override void OnPointerDown(PointerEventData eventData) {
    if (isSelectable && OnClickCallback != null) {
      OnClickCallback(callbackArgument);
      image.color = baseColor + new Color32(40, 40, 40, 40);
    }
    base.OnPointerClick(eventData);
  }

  public override void OnPointerExit(PointerEventData eventData) {
    if (isSelectable)
      image.color = baseColor;
    base.OnPointerExit(eventData);
  }

  public void SetIsSelectable(bool isSelectable) {
    image.enabled = isSelectable;
    this.isSelectable = isSelectable;
  }

  public bool GetIsSelectable() {
    return isSelectable;
  }

  public void AddChildEntry(UISelectableEntry child) {
    if (children == null) {
      children = new List<UISelectableEntry>();
    }
    children.Add(child);
  }

  public void SetParent(UIDialog parentDialog) {
    this.parentDialog = parentDialog;
  }

  public void SetChildEntries(List<UISelectableEntry> children) {
    this.children = children;
  }

  // Get the children of this entry
  public List<UISelectableEntry> GetChildEntries() {
    return children;
  }

  public void SetTextContent(List<string> textContent) {
    this.textContent = textContent;

    // Clear existing text handles
    foreach (var textHandle in textHandles) {
      Destroy(textHandle.gameObject);
    }
    textHandles.Clear();

    int columnCount = textContent.Count;
    float totalWidth = rectTransform.rect.width;
    float columnWidth = totalWidth / columnCount;

    for (int i = 0; i < columnCount; ++i) {
      // Create a new TextMeshProUGUI for each column
      var textHandle = Instantiate(Resources.Load<GameObject>("Prefabs/EmptyObject"), rectTransform)
                           .AddComponent<TextMeshProUGUI>();
      textHandle.gameObject.name = $"UISelectableEntry::Text_{i}";
      textHandle.fontSize = 6;
      textHandle.font = UIManager.Instance.GlobalFont;
      textHandle.alignment = TextAlignmentOptions.MidlineLeft;

      // Set the RectTransform for proper alignment
      var textRect = textHandle.GetComponent<RectTransform>();
      textRect.anchorMin = new Vector2((float)i / columnCount, 0);
      textRect.anchorMax = new Vector2((float)(i + 1) / columnCount, 1);
      textRect.offsetMin = Vector2.zero;
      textRect.offsetMax = Vector2.zero;

      // Set the text content
      textHandle.text = textContent[i];

      // Add to the list of text handles
      textHandles.Add(textHandle);
    }
  }

  public RectTransform GetTextTransform(int index) {
    if (index >= 0 && index < textHandles.Count) {
      return textHandles[index].GetComponent<RectTransform>();
    }
    return null;
  }
}
