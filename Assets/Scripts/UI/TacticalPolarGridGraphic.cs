using UnityEngine;
using UnityEngine.UI;

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

  public void CycleRange() {
    _currentRangeIndex = (_currentRangeIndex + 1) % _rangeScales.Length;
    SetVerticesDirty();  // Mark the UI as needing to be redrawn
  }

  protected override void OnPopulateMesh(VertexHelper vh) {
    vh.Clear();

    float maxRange = _rangeScales[_currentRangeIndex] / 1000f;  // Convert to km for UI scale

    // Get the rect dimensions
    Rect rect = GetPixelAdjustedRect();
    Vector2 center = rect.center;

    // Adjust scale based on rect size
    float scaleFactor = Mathf.Min(rect.width, rect.height) / (2 * maxRange);

    // Draw the grid
    DrawRangeRings(vh, center, maxRange, scaleFactor);
    DrawBearingLines(vh, center, maxRange, scaleFactor);
  }

  private void DrawRangeRings(VertexHelper vh, Vector2 center, float maxRange, float scaleFactor) {
    for (int i = 1; i <= _numberOfRangeRings; i++) {
      float radius = (i * maxRange) / _numberOfRangeRings;
      DrawCircle(vh, center, radius * scaleFactor, 128);
    }
  }

  private void DrawBearingLines(VertexHelper vh, Vector2 center, float maxRange,
                                float scaleFactor) {
    float angleStep = 360f / _numberOfBearingLines;

    for (int i = 0; i < _numberOfBearingLines; i++) {
      float angle = i * angleStep * Mathf.Deg2Rad;
      Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
      DrawLine(vh, center, center + direction * maxRange * scaleFactor, _lineWidth);
    }
  }

  private void DrawCircle(VertexHelper vh, Vector2 center, float radius, int segments) {
    float angleStep = 360f / segments;
    Vector2 prevPoint = center + new Vector2(radius, 0);

    for (int i = 1; i <= segments; i++) {
      float angle = i * angleStep * Mathf.Deg2Rad;
      Vector2 newPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
      DrawLine(vh, prevPoint, newPoint, _lineWidth);
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
}
