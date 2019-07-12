using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class DresserArrow : ClientMonoBehaviour {
   #region Public Variables

   // Our child container object
   public GameObject childContainer;

   #endregion

   private void Update () {
      bool needsToGetDressed = false;

      if (Global.player is PlayerBodyEntity) {
         PlayerBodyEntity body = (PlayerBodyEntity) Global.player;
         needsToGetDressed = !body.armorManager.hasArmor();
      }

      childContainer.SetActive(needsToGetDressed);
   }

   #region Private Variables

   #endregion
}
