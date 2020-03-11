using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class EnemyDataTemplate : GenericEntryTemplate {
   #region Public Variables

   // The xml id of this template
   public int xmlId;

   // An object to determine if the sql data is enabled
   public GameObject enabledIndicator;

   #endregion

   public void updateItemDisplay (BattlerData resultItem, bool isActive, int xmlId) {
      string newName = "Undefined";
      this.xmlId = xmlId;
      newName = resultItem.enemyName + "\n(" + ((Enemy.Type) resultItem.enemyType).ToString() + ")";

      updateDisplay(newName, (int) resultItem.enemyType);
      setIdRestriction(xmlId);
      enabledIndicator.SetActive(isActive);
   }

   #region Private Variables

   #endregion
}
