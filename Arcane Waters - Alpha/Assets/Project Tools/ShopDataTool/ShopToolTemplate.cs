using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShopToolTemplate : GenericEntryTemplate {
   #region Public Variable

   #endregion

   private void OnEnable () {
      setRestrictions(nameText.text);
   }
}
