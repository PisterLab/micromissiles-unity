using UnityEngine;

public static class SpriteManager {
  private const string _symbolPathFormat = "APP6-D_Symbology/{0}";

  public static Sprite LoadSymbolSprite(string symbolName) {
    string path = string.Format(_symbolPathFormat, symbolName);
    Sprite sprite = Resources.Load<Sprite>(path);
    if (sprite == null) {
      Debug.LogWarning($"Failed to load sprite at path: {path}.");
    }
    return sprite;
  }
}
