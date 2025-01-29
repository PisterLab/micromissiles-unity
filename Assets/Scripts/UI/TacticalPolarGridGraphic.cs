using UnityEngine;
using UnityEngine.UI;
using TMPro;  // Import TextMeshPro namespace
using System.Collections.Generic;

[RequireComponent(typeof(CanvasRenderer))]
public class TacticalPolarGridGraphic : Graphic {
  [SerializeField]
  private Color _gridColor = new Color(0.0f, 1.0f, 0.0f, 0.3f);
  [SerializeField]
  private int _numberOfBearingLines = 36;  // Every 10 degrees
  [SerializeField]
  private int _numberOfRangeRings = 5;
  [SerializeField]
  private float[] _rangeScales = { 100f, 1000f, 10000f, 40000f };  // in meters

  [SerializeField]
  private float _lineWidth = 2f;

  private int _currentRangeIndex = 1;  // Start with 10km range

  // Updated field for TextMeshPro range text labels
  [SerializeField]
  private GameObject _rangeTextPrefab;  // Assign a TextMeshPro prefab in the inspector

  private List<TextMeshProUGUI> _rangeTexts = new List<TextMeshProUGUI>();

  private float _currentScaleFactor = 1f;  // Store the current scale factor

  public float CurrentScaleFactor => _currentScaleFactor;  // Public getter
  protected override void OnEnable() {
    base.OnEnable();
    if (Application.isPlaying) {
      ClearRangeTexts();
      UpdateRangeTexts();  // Create range texts when the component is enabled
    }
  }

  public void CycleRangeUp() {
    _currentRangeIndex = (_currentRangeIndex + 1) % _rangeScales.Length;
    SetVerticesDirty();  // Mark the UI as needing to be redrawn
    UpdateRangeTexts();  // Update range labels
  }

  public void CycleRangeDown() {
    _currentRangeIndex = (_currentRangeIndex - 1 + _rangeScales.Length) % _rangeScales.Length;
    SetVerticesDirty();  // Mark the UI as needing to be redrawn
    UpdateRangeTexts();  // Update range labels
  }

  protected override void OnPopulateMesh(VertexHelper vh) {
    vh.Clear();

    float maxRange = _rangeScales[_currentRangeIndex] / 1000f;  // Convert to km for UI scale

    // Get the rect dimensions
    Rect rect = GetPixelAdjustedRect();
    Vector2 center = rect.center;

    // Adjust scale based on rect size
    _currentScaleFactor = Mathf.Min(rect.width, rect.height) / (2 * maxRange);

    float scaleFactor = _currentScaleFactor;

    // Draw the grid
    DrawRangeRings(vh, center, maxRange, scaleFactor);
    DrawBearingLines(vh, center, maxRange, scaleFactor);

    // Only update positions of existing range texts
    UpdateRangeTextsPositions(center, maxRange, scaleFactor);
  }

  private void DrawRangeRings(VertexHelper vh, Vector2 center, float maxRange, float scaleFactor) {
    // Extend the range rings to 10x the major marker range
    float extendedMaxRange = maxRange * 10;

    for (int i = 1; i <= _numberOfRangeRings * 10; i++) {
      float radius = (i * extendedMaxRange) / (_numberOfRangeRings * 10);
      DrawCircle(vh, center, radius * scaleFactor, 128, 1f);

      // Make every 10th ring thicker to indicate major markers
      if (i % 10 == 0) {
        DrawCircle(vh, center, radius * scaleFactor, 128, 2f);  // Draw again for thickness
      }
    }
  }

  private void DrawBearingLines(VertexHelper vh, Vector2 center, float maxRange,
                                float scaleFactor) {
    // Extend the bearing lines to 10x the major marker range
    float extendedMaxRange = maxRange * 10;

    float angleStep = 360f / _numberOfBearingLines;

    for (int i = 0; i < _numberOfBearingLines; i++) {
      float angle = i * angleStep * Mathf.Deg2Rad;
      Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
      DrawLine(vh, center, center + direction * extendedMaxRange * scaleFactor, _lineWidth);
    }
  }

  private void DrawCircle(VertexHelper vh, Vector2 center, float radius, int segments,
                          float widthMultiplier) {
    float angleStep = 360f / segments;
    Vector2 prevPoint = center + new Vector2(radius, 0);

    for (int i = 1; i <= segments; i++) {
      float angle = i * angleStep * Mathf.Deg2Rad;
      Vector2 newPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
      DrawLine(vh, prevPoint, newPoint, _lineWidth * widthMultiplier);
      prevPoint = newPoint;
    }
  }

  private void DrawLine(VertexHelper vh, Vector2 start, Vector2 end, float thickness) {
    // Calculate the total scale factor from Canvas and RectTransform
    float totalScale = canvas.scaleFactor * transform.lossyScale.x;

    // Thickness adjusted for total scale to maintain constant pixel width
    float adjustedThickness = thickness / totalScale;

    Vector2 direction = (end - start).normalized;
    Vector2 perpendicular = new Vector2(-direction.y, direction.x) * adjustedThickness * 0.5f;

    Vector2 v0 = start - perpendicular;
    Vector2 v1 = start + perpendicular;
    Vector2 v2 = end + perpendicular;
    Vector2 v3 = end - perpendicular;

    int index = vh.currentVertCount;

    UIVertex vertex = UIVertex.simpleVert;
    vertex.color = _gridColor;

    vertex.position = v0;
    vh.AddVert(vertex);

    vertex.position = v1;
    vh.AddVert(vertex);

    vertex.position = v2;
    vh.AddVert(vertex);

    vertex.position = v3;
    vh.AddVert(vertex);

    vh.AddTriangle(index, index + 1, index + 2);
    vh.AddTriangle(index + 2, index + 3, index);
  }

  private void ClearRangeTexts() {
    foreach (var text in _rangeTexts) {
      if (text != null) {
        DestroyImmediate(text.gameObject);
      }
    }
    _rangeTexts.Clear();
  }

  // New method to update range text labels
  private void UpdateRangeTexts() {
    ClearRangeTexts();

    // Only create new labels if they don't exist
    if (_rangeTexts.Count == 0) {
      for (int i = 1; i <= _numberOfRangeRings; i++) {
        GameObject textObj = Instantiate(_rangeTextPrefab, transform);
        TextMeshProUGUI textComponent = textObj.GetComponent<TextMeshProUGUI>();
        textComponent.color = _gridColor;
        textComponent.transform.localScale = Vector3.one;
        _rangeTexts.Add(textComponent);
      }
    }

    // Update the text values
    for (int i = 0; i < _rangeTexts.Count; i++) {
      _rangeTexts[i].text = GetRangeLabelText(i + 1);
    }
  }

  // New method to position and adjust range text labels
  private void UpdateRangeTextsPositions(Vector2 center, float maxRange, float scaleFactor) {
    if (_rangeTexts.Count == 0) {
      return;
    }

    for (int i = 1; i <= _numberOfRangeRings; i++) {
      float radius = (i * maxRange) / _numberOfRangeRings;
      float adjustedRadius = radius * scaleFactor;

      // Position to the right (0 degrees)
      Vector2 position = center + new Vector2(adjustedRadius, 0);

      TextMeshProUGUI textComponent = _rangeTexts[i - 1];
      if (textComponent != null) {
        textComponent.rectTransform.anchoredPosition = position;
        // Adjust text scale inversely to the grid's scaleFactor to maintain constant size
        // Assuming uniform scaling for both x and y
        float inverseScale = 4f / scaleFactor;
        // textComponent.transform.localScale = new Vector3(inverseScale, inverseScale, 1f);
      } else {
        // Remove the object if it doesn't have a TextMeshProUGUI component
        _rangeTexts.Clear();
        return;
      }
    }
  }

  // Helper method to get the range label text
  private string GetRangeLabelText(int ringIndex) {
    float maxRange = _rangeScales[_currentRangeIndex];
    float rangeValue = (ringIndex * maxRange) / _numberOfRangeRings;

    if (rangeValue >= 1000f) {
      return $"{rangeValue / 1000f:F0} km";
    } else {
      return $"{rangeValue:F0} m";
    }
  }

  protected override void OnDestroy() {
    base.OnDestroy();
    ClearRangeTexts();
  }
}
