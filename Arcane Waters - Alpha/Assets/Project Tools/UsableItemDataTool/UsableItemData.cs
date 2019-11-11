using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class UsableItemData {
   #region Public Variables

   // The type
   public UsableItem.Type type;

   // Custom name of the item
   public string itemName;

   // Info of the data
   public string description;

   // Image path
   public string itemIconPath;

   // Base modifiers
   public int addedHP;
   public int addedAP;
   public int bonusMaxHP;
   public int bonusArmor;
   public int bonusATK;

   // Stat modifiers
   public int bonusINT;
   public int bonusSTR;
   public int bonusSPT;
   public int bonusLUK;
   public int bonusPRE;
   public int bonusVIT;

   // Defense modifiers
   public int bonusResistancePhys;
   public int bonusResistanceFire;
   public int bonusResistanceWater;
   public int bonusResistanceWind;
   public int bonusResistanceEarth;
   public int bonusResistanceAll;

   // Damage modifiers
   public int bonusDamagePhys;
   public int bonusDamageFire;
   public int bonusDamageWater;
   public int bonusDamageWind;
   public int bonusDamageEarth;
   public int bonusDamageAll;

   // Special modifiers
   public int expBonus;
   public int goldBonus;
   public float cooldownReduce;

   #endregion
}