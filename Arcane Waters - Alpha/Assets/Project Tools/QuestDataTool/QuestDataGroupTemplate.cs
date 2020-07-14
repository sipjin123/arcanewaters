using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class QuestDataGroupTemplate : GenericEntryTemplate {
   #region Public Variable

   // The xml id of the data
   public int xmlId;

   // Determines if this template is active in the database
   public Toggle isActiveToggle;

   #endregion

   private void OnEnable () {
      setNameRestriction(nameText.text);
   }
}
