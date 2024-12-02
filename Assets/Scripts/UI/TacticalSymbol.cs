using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class TacticalSymbol : MonoBehaviour {
  [SerializeField]
  private GameObject _directionArrow;
  [SerializeField]
  private TextMeshProUGUI _uniqueDesignatorText;
  [SerializeField]
  private TextMeshProUGUI _iffText;
  [SerializeField]
  private TextMeshProUGUI _typeText;
  [SerializeField]
  private TextMeshProUGUI _speedAltText;
  [SerializeField]
  private TextMeshProUGUI _additionalInfoText;

  private SpriteManager _spriteManager;

  // Start is called once before the first execution of Update after the MonoBehaviour is created
  void Awake() {
    _spriteManager = new SpriteManager();
    _uniqueDesignatorText.text = "";
    _iffText.text = "";
    _typeText.text = "";
    _speedAltText.text = "";
    _additionalInfoText.text = "";
  }

  public void SetDirectionArrowRotation(float rotationDegrees) {
    if (_directionArrow != null) {
      _directionArrow.GetComponent<RectTransform>().rotation =
          Quaternion.Euler(0, 0, rotationDegrees);
    }
  }

  public void DisableDirectionArrow() {
    if (_directionArrow != null) {
      _directionArrow.SetActive(false);
    }
    else {
      Debug.LogWarning("Direction arrow not found on TacticalSymbol" + name);
    }
  }

  public void SetSprite(string spriteName) {
    spriteName = spriteName.ToUpper();
    // Update main symbol image
    Image symbolImage = GetComponent<Image>();
    if (symbolImage != null) {
      Sprite symbolSprite = _spriteManager.LoadSymbolSprite(spriteName);
      if (symbolSprite != null) {
        symbolImage.sprite = symbolSprite;
      }
    }
  }

  public void SetUniqueDesignator(string text) {
    SetText(_uniqueDesignatorText, text);
  }

  public void SetIFF(string text) {
    SetText(_iffText, text);
  }

  public void SetType(string text) {
    SetText(_typeText, text);
  }

  public void SetSpeedAlt(string text) {
    SetText(_speedAltText, text);
  }

  public void SetAdditionalInfo(string text) {
    SetText(_additionalInfoText, text);
  }

  private void SetText(TextMeshProUGUI textComponent, string text) {
    if (textComponent != null) {
      textComponent.text = text.ToUpper();
    }
  }

  // Update is called once per frame
  void Update() {}
}

public class SpriteManager {
  private const string SymbolPathFormat = "APP6-D_Symbology/{0}";

  public Sprite LoadSymbolSprite(string symbolName) {
    string path = string.Format(SymbolPathFormat, symbolName);
    Sprite sprite = Resources.Load<Sprite>(path);
    if (sprite == null) {
      Debug.LogWarning($"Failed to load sprite at path: {path}");
    }
    return sprite;
  }
}
