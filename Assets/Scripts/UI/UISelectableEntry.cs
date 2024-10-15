using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class UISelectableEntry : EventTrigger {
  private List<UISelectableEntry> children;
  private List<string> textContent;

  private UIDialog parentDialog;

  private RectTransform rectTransform;

  private CanvasRenderer canvasRenderer;

  private Image image;

  private TextMeshProUGUI textHandle;

  private bool isSelectable = true;

  private static Color baseColor = new Color32(31, 31, 45, 140);

  private Action<object> OnClickCallback;
  private object callbackArgument;

  public void Awake() {
    rectTransform = gameObject.AddComponent<RectTransform>();
    textHandle = Instantiate(Resources.Load<GameObject>("Prefabs/EmptyObject"), rectTransform)
                     .AddComponent<TextMeshProUGUI>();
    textHandle.gameObject.name = "UISelectableEntry::Text";
    textHandle.fontSize = 8;
    textHandle.font = UIManager.Instance.Font;
    textHandle.alignment = TextAlignmentOptions.MidlineLeft;
    textHandle.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f);
    textHandle.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.5f);
    textHandle.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
    textHandle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
    textHandle.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 0);

    image = gameObject.AddComponent<Image>();
    image.type = Image.Type.Sliced;
    image.color = baseColor;
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
    if (isSelectable)
      image.enabled = true;
    else
      image.enabled = false;
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
    textHandle.text = string.Join("\t\t\t", textContent);
  }

  public RectTransform GetTextTransform() {
    return textHandle.GetComponent<RectTransform>();
  }
}
