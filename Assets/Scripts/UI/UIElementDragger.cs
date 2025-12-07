using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UIElementMouseCapturer))]
public class UIElementDragger : EventTrigger {
  public override void OnDrag(PointerEventData eventData) {
    transform.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
  }
}
