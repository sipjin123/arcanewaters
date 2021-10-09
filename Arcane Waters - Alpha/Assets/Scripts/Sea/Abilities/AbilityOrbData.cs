using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

[System.Serializable]
public class AbilityOrbData {
   #region Public Variables

   // The type of attack associated with this ability orb
   public Attack.Type attackType;

   // The target user id
   public int targetUserId;

   // If the orb should snap instantly to the target
   public bool snapToTargetInstantly = false;

   #endregion

   #region Private Variables

   #endregion
}
