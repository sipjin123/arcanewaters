using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

[System.Serializable]
public class CannonballEffector {
   #region Public Variables

   // What effect this effector will cause
   public Type effectorType;

   // How long the effect will last for
   public float effectDuration;

   // How strong the impact of the effect will be
   public float effectStrength;

   // How far this effect will reach
   public float effectRange;

   // How many times this effector has triggered
   public int triggerCount = 0;

   public enum Type
   {
      None = 0,
      Fire = 1,
      Electric = 2,
      Ice = 3,
      Explosion = 4,
      Bouncing = 5,
      Poison = 6
   }

   #endregion

   public CannonballEffector () { }

   public CannonballEffector (Type type, float strength, float duration = 0.0f, float range = 0.0f) {
      effectorType = type;
      effectDuration = duration;
      effectStrength = strength;
      effectRange = range;
   }

   #region Private Variables
      
   #endregion
}
