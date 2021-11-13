using System;

[Serializable]
public class PaletteRecolorData
{
   #region Public Variables

   // SQL id
   public int xmlId;

   // The name of the palette
   public string paletteName;

   // The display name of the palette
   public string displayName;

   // The palette's description
   public string description;

   // Source colors references
   public int[] sourcesIds;

   // Destination colors
   public string[] destinationColors;

   // The palette's type, e.g. armor/weapon
   public PaletteType paletteType;

   // The palette's tag references
   public int[] tagsIds;

   #endregion

   #region Private Variables

   #endregion

   public enum PaletteType
   {
      None = 0,
      Armor = 1,
      Weapon = 2,
      Hat = 3,
      Hair = 4,
      Eyes = 5,
      Ship = 6,
      Guild = 7,
      Flag = 8,
      SeaStructure = 9
   }


   // The tags SQL ids
   public enum Tags
   {
      None = 0
   }
}
