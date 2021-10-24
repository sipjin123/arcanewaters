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

   public enum Expiry {
      None = 0,
      OnWarp = 1,
      Timed = 2
   }

   // What type of powerup this is
   public Powerup.Type powerupType;

   // What rarity this powerup is
   public Rarity.Type powerupRarity;

   // The type of expiry
   public Expiry expiry;

   // Where the icon sprites for the powerups are located
   public static string ICON_SPRITES_LOCATION = "Sprites/Powerups/PowerUpIcons";

   // Where the border sprites for the powerups are located
   public static string BORDER_SPRITES_LOCATION = "Sprites/Powerups/PowerUpBorders";

   // How long the powerup will take place
   public float powerupDuration = -1;

   #endregion

   public Powerup () { }
   
   public Powerup (Powerup.Type type, Rarity.Type rarity, Expiry expiry) {
      powerupType = type;
      powerupRarity = rarity;
      this.expiry = expiry;
   }

   #region Private Variables

   #endregion
}
