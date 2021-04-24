using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

[System.Serializable]
public class Powerup {
   #region Public Variables

   public enum Type
   {
      None = 0,
      TreasureDropUp = 1,
      SpeedUp = 2,
      ElectricShots = 3,
      FireShots = 4,
      IceShots = 5,
      MultiShots = 6,
      ExplosiveShots = 7,
      BouncingShots = 8,
      DamageReduction = 9,
      IncreasedHealth = 10
   }

   // What type of powerup this is
   public Powerup.Type powerupType;

   // What rarity this powerup is
   public Rarity.Type powerupRarity;

   #endregion

   public Powerup () { }
   
   public Powerup (Powerup.Type type, Rarity.Type rarity) {
      powerupType = type;
      powerupRarity = rarity;
   }

   #region Private Variables

   #endregion
}
