using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaMonsterDataTemplate : GenericEntryTemplate
{
   #region Public Variables

   // The xml id of this template
   public int xml_id;

   // An object to determine if the sql data is enabled
   public GameObject enabledIndicator;

   #endregion

   public void updateItemDisplay (SeaMonsterEntityData resultItem, bool isActive) {
      string newName = resultItem.monsterName + " (" + ((Enemy.Type) resultItem.seaMonsterType).ToString() + ")";

      modifyDisplay(newName, (int) resultItem.seaMonsterType);
      enabledIndicator.SetActive(isActive);
   }

   #region Private Variables

   #endregion
}
