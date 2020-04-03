using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShopToolTemplate : GenericEntryTemplate {
   #region Public Variable

   // The xml id of the data
   public int xmlId;

   #endregion

   private void OnEnable () {
      setNameRestriction(nameText.text);
   }
}
