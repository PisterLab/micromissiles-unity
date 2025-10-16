using UnityEngine;
using TMPro;

public class UIEventMarker : MonoBehaviour {
  // Start is called once before the first execution of Update after the MonoBehaviour is created
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

  // Update is called once per frame
  void LateUpdate() {
    transform.LookAt(Camera.main.transform);
  }
}
