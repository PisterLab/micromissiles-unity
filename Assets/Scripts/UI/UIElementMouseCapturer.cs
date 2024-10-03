using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIElementMouseCapturer : EventTrigger {
  public override void OnPointerEnter(PointerEventData eventData) {
    InputManager.Instance.mouseActive = false;
    base.OnPointerEnter(eventData);
  }

  public override void OnPointerExit(PointerEventData eventData) {
    InputManager.Instance.mouseActive = true;
    base.OnPointerExit(eventData);
  }

  public void OnDisable() {
    OnPointerExit(null);
  }
}
