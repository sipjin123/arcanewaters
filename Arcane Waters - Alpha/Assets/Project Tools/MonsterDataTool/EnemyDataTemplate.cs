using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class EnemyDataTemplate : GenericEntryTemplate {
   #region Public Variables

   // The xml id of this template
   public int xml_id;

   // An object to determine if the sql data is enabled
   public GameObject enabledIndicator;

   #endregion

   public void updateItemDisplay (BattlerData resultItem, bool isActive) {
      string newName = "Undefined";
      try {
         newName = resultItem.enemyName + "\n(" + ((Enemy.Type) resultItem.enemyType).ToString() + ")";
      } catch {
      }

      modifyDisplay(newName, (int) resultItem.enemyType);
      enabledIndicator.SetActive(isActive);
   }

   #region Private Variables

   #endregion
}
