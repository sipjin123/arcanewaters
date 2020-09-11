using UnityEngine;
using System;
using System.Xml.Serialization;

[Serializable]
[XmlRoot("Item")]
public class WeaponDefinition : EquipmentDefinition
{
   #region Public Variables

   // Enum of actions defined in the game
   public enum ActionType
   {
      None = 0,
      PlantCrop = 1,
      WaterCrop = 2,
      HarvestCrop = 3,
      CustomizeMap = 4,
      Shovel = 5,
      Chop = 6
   }

   // The weapon class type
   public enum Class { Any = 0, Melee = 1, Ranged = 2, Magic = 3 }

   // Type of action the weapon can perform
   public ActionType actionType;

   // The Weapon class
   public Class weaponClass = Class.Any;

   // The damage of the weapon
   public int baseDamage = 0;

   // The elemental damage of the weapons
   public int fireDamage;
   public int waterDamage;
   public int airDamage;
   public int earthDamage;

   #endregion

   #region Private Variables

   #endregion
}