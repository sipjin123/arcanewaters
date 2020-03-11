using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaMonsterDataTemplate : GenericEntryTemplate
{
   #region Public Variables

   // The xml id of this template
   public int xmlId;

   // An object to determine if the sql data is enabled
   public GameObject enabledIndicator;

   #endregion

   public void updateItemDisplay (SeaMonsterEntityData resultItem, bool isActive) {
      string newName = resultItem.monsterName + " (" + ((Enemy.Type) resultItem.seaMonsterType).ToString() + ")";

      updateDisplay(newName, (int) resultItem.seaMonsterType);
      setIdRestriction(xmlId);
      enabledIndicator.SetActive(isActive);
   }

   #region Private Variables

   #endregion
}
