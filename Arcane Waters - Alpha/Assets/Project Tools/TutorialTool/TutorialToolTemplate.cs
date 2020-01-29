using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TutorialToolTemplate : GenericEntryTemplate {
   #region Public Variables

   #endregion

   private void OnEnable () {
      setRestrictions(nameText.text);
   }

   #region Private Variables

   #endregion
}
