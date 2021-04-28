using System;
using UnityEngine;

[Serializable]
public class PowerupData {
   #region Public Variables

   // The name for this powerup, to be displayed in the tooltip
   public string powerupName;

   // What type of powerup this is
   public Powerup.Type powerupType;

   // The description for this powerup, to be displayed in the tooltip
   public string description;

   // The sprite icon
   public Sprite spriteIcon;

   // The boost that each rarity of this powerup will give, normalised (1 = 100%)
   public float[] rarityBoostFactors = { 0.0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };

   #endregion

   public PowerupData () { }

   #region Private Variables

   #endregion
}
