using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Attack : MonoBehaviour {
   #region Public Variables

   // The types of attack
   public enum Type {  None = 0, Cannon = 1, Ice = 2, Air = 3, Tentacle = 4, Venom = 5, Boulder = 6, Shock_Ball = 7 }

   #endregion

   public static float getDamageModifier (Type attackType) {
      // Some attacks modify the amount of damage that's done
      switch (attackType) {
         case Type.Ice:
            return .20f;
         case Type.Tentacle:
            return .10f;
         case Type.Venom:
            return .10f;
         case Type.Boulder:
            return .10f;
         case Type.Shock_Ball:
            return .10f;
         default:
            return 1.0f;
      }
   }

   #region Private Variables

   #endregion
}
