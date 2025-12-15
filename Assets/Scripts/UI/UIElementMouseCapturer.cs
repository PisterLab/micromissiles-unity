using UnityEngine;
using UnityEngine.EventSystems;

public class UIElementMouseCapturer : EventTrigger {
  public override void OnPointerEnter(PointerEventData eventData) {
    InputManager.Instance.MouseActive = false;
    base.OnPointerEnter(eventData);
  }

  public override void OnPointerExit(PointerEventData eventData) {
    InputManager.Instance.MouseActive = true;
    base.OnPointerExit(eventData);
  }

  public void OnDisable() {
    OnPointerExit(null);
  }
}
