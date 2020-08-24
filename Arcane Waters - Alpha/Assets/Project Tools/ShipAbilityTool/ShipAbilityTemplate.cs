using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipAbilityTemplate : GenericEntryTemplate
{
   #region Public Variables

   // The database id
   public int xmlId;

   // Determines if this template is active in the database
   public GameObject isActiveIndicator;

   #endregion

   private void OnEnable () {
      setNameRestriction(nameText.text);
   }

   #region Private Variables

   #endregion
}
