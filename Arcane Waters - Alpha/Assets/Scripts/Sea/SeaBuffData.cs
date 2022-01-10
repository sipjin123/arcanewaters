using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

[System.Serializable]
public class SeaBuffData {
   #region Public Variables

   // At what time this buff effect begins
   public double buffStartTime;

   // At what time this buff effect ends
   public double buffEndTime;

   // What type of buff this is - what value it will be buffing
   public SeaBuff.Type buffType;

   // How strong this buff is
   public float buffMagnitude;

   // The last time the buff took effect
   public double lastBuffTick;

   // The xml id reference
   public int buffAbilityIdReference;

   #endregion

   public SeaBuffData () { }

   public SeaBuffData (double buffStartTime, double buffEndTime, SeaBuff.Type buffType, float buffMagnitude, int buffAbilityXmlId) {
      this.buffStartTime = buffStartTime;
      this.buffEndTime = buffEndTime;
      this.buffType = buffType;
      this.buffMagnitude = buffMagnitude;
      this.buffAbilityIdReference = buffAbilityXmlId;
   }

   public static Attack.Type getAttackType (SeaBuff.Type buffType) {
      switch (buffType) {
         case SeaBuff.Type.SpeedBoost:
            return Attack.Type.SpeedBoost;
         case SeaBuff.Type.DamageAmplify:
            return Attack.Type.DamageAmplify;
         default:
            return Attack.Type.None;
      }
   }

   public static SeaBuff.Type getBuffType (Attack.Type attackType ) {
      switch (attackType) {
         case Attack.Type.SpeedBoost:
            return SeaBuff.Type.SpeedBoost;
         case Attack.Type.DamageAmplify:
            return SeaBuff.Type.DamageAmplify;
         default:
            return SeaBuff.Type.None;
      }
   }

   #region Private Variables
      
   #endregion
}

namespace SeaBuff
{
   public enum Type
   {
      None = 0,
      SpeedBoost = 1,
      DamageAmplify = 2,
      Heal = 3
   }

   public enum Category
   {
      None = 0,
      Buff = 1,
      Debuff = 2,
   }
}