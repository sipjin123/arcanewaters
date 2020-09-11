using System;
using System.Xml.Serialization;

[Serializable]
[XmlRoot("Item")]
public class ArmorDefinition : EquipmentDefinition
{
   #region Public Variables

   // The defense of the armor
   public int baseDefense;

   // The elemental defense of the armor
   public int fireResist;
   public int waterResist;
   public int airResist;
   public int earthResist;

   #endregion

   #region Private Variables

   #endregion
}
