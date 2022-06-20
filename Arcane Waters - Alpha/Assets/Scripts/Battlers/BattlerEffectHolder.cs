using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BattlerEffectHolder : MonoBehaviour {
   #region Public Variables

   // Effect game object holders
   public GameObject burningEffectObj,
      stunEffectObj,
      slowEffectObj;

   #endregion

   public void updateEffect (Status.Type statusType, bool isEnabled) {
      GameObject statusReference = null;
      switch (statusType) {
         case Status.Type.Burning:
            statusReference = burningEffectObj;
            break;
         case Status.Type.Slowed:
            statusReference = slowEffectObj;
            break;
         case Status.Type.Stunned:
            statusReference = stunEffectObj;
            break;
      }


      if (statusReference != burningEffectObj) {
         burningEffectObj.SetActive(false);
      }
      if (statusReference != stunEffectObj) {
         stunEffectObj.SetActive(false);
      }
      if (statusReference != slowEffectObj) {
         slowEffectObj.SetActive(false);
      }

      if (statusReference != null) {
         statusReference.SetActive(isEnabled);
      }
   }

   #region Private Variables

   #endregion
}
