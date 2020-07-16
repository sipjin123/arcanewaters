using UnityEngine;
using System;
using System.Xml.Serialization;

[Serializable]
[XmlRoot("Item")]
public class WeaponDefinition : ItemDefinition
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

   // Type of action the weapon can perform
   public ActionType actionType;

   #endregion

   #region Private Variables

   #endregion
}
