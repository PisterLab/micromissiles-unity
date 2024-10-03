using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotStatusDialog : UIDialog {
  // Start is called before the first frame update
  public override void Start() {
    base.Start();

    UISelectableEntry missiles = CreateSelectableEntry();
    missiles.SetTextContent(new List<string>(new string[] { "Interceptors" }));
    missiles.SetIsSelectable(false);

    UISelectableEntry submunitions = CreateSelectableEntry();
    submunitions.SetTextContent(new List<string>(new string[] { "Submunitions" }));
    submunitions.SetIsSelectable(false);

    UISelectableEntry targets = CreateSelectableEntry();
    targets.SetTextContent(new List<string>(new string[] { "Threats" }));
    targets.SetIsSelectable(false);

    SetDialogEntries(
        new List<UISelectableEntry>(new UISelectableEntry[] { missiles, submunitions, targets }));

    AddDialogTab("All", () => {});
  }

  // Update is called once per frame
  void Update() {}
}
