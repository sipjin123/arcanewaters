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
      burningEffectObj.SetActive(false);
      stunEffectObj.SetActive(false);
      slowEffectObj.SetActive(false);

      switch (statusType) {
         case Status.Type.Burning:
            burningEffectObj.SetActive(isEnabled);
            break;
         case Status.Type.Slowed:
            slowEffectObj.SetActive(isEnabled);
            break;
         case Status.Type.Stunned:
            stunEffectObj.SetActive(isEnabled);
            break;
      }
   }

   #region Private Variables
      
   #endregion
}
