using UnityEngine;
using UnityEngine.UI;

public class KeybindingsSection : MonoBehaviour {
   #region Public Variables
   // Name of the action
   public Text sectionLabel;

   public KeybindingsSection initialize (string title) {
      sectionLabel.text = title;
      return this;
   }
   #endregion

   #region Private Variables

   #endregion
}
