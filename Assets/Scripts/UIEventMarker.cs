using UnityEngine;
using TMPro;

public class UIEventMarker : MonoBehaviour {
  private TextMeshProUGUI _text;

  void Awake() {
    _text = GetComponentInChildren<TextMeshProUGUI>();
    if (_text == null) {
      Debug.LogError("No TextMeshProUGUI component found in children.");
    }
  }

  public void SetEventHit() {
    _text.text = "x";
    _text.color = Color.green;
  }

  public void SetEventMiss() {
    _text.text = "o";
    _text.color = Color.red;
  }

  void LateUpdate() {
    transform.LookAt(Camera.main.transform);
  }
}
