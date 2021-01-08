﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Attack : MonoBehaviour {
   #region Public Variables

   // The types of attack
   public enum Type {  
      None = 0, Cannon = 1, Ice = 2, Air = 3, Tentacle = 4, 
      Venom = 5, Boulder = 6, Shock_Ball = 7, Tentacle_Range = 8, 
      Mini_Boulder = 9, Fire = 10, Heal = 11, SpeedBoost = 12,
      DamageAmplify = 13, SpawnStoneBlocker = 14,  
   }

   public enum ImpactMagnitude
   {
      None = 0,
      Weak = 1,
      Normal = 2,
      Strong = 3
   }

   #endregion

   public static float getDamageModifier (Type attackType) {
      // Some attacks modify the amount of damage that's done

      ShipAbilityData shipData = ShipAbilityManager.self.getAbility(attackType);
      if (shipData == null) {
         switch (attackType) {
            case Type.Ice:
               return .20f;
            case Type.Tentacle:
               return .10f;
            case Type.Venom:
               return .10f;
            case Type.Boulder:
               return .10f;
            case Type.Mini_Boulder:
               return .10f;
            case Type.Tentacle_Range:
               return .10f;
            case Type.Shock_Ball:
               return .10f;
            default:
               return 1.0f;
         }
      } else {
         return 1.0f;
      }
   }

   public static float getSpeedModifier (Type attackType) {
      // Used for altering the speed of the projectile
      switch (attackType) {
         case Type.Boulder:
            return 1.50f;
         case Type.Shock_Ball:
            return 1.25f;
         case Type.Cannon:
            return 2.275f;
         default:
            return 1.0f;
      }
   }

   #region Private Variables

   #endregion
}
