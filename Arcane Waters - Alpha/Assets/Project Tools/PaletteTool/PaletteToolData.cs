using System;
#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

[Serializable]
public class PaletteToolData
{
   #region Public Variables

   // The name of the palette
   public string paletteName;

   // Left side color - source colors
   public string[] srcColor;

   // Right side color - destination colors
   public string[] dstColor;

   // Type of palette e.g. hair/eyes/weapon
   public int paletteType;

   // The subcategory for referencing
   public string subcategory;

   // The tag id for referencing
   public int tagId;

   #endregion

   public PaletteToolData () { }

   public PaletteToolData (string paletteName, string[] srcColor, string[] dstColor, int paletteType) {
      this.paletteName = paletteName;
      this.srcColor = srcColor;
      this.dstColor = dstColor;
      this.paletteType = paletteType;
   }

   public bool isPrimary () {
      if (string.IsNullOrWhiteSpace(subcategory)) {
         return false;
      }

      return subcategory.ToLower().Contains("primary");
   }

   public bool isSecondary () {
      if (string.IsNullOrWhiteSpace(subcategory)) {
         return false;
      }

      return subcategory.ToLower().Contains("secondary");
   }

   public bool isAccent () {
      if (string.IsNullOrWhiteSpace(subcategory)) {
         return false;
      }

      return subcategory.ToLower().Contains("accent");
   }

   #region Private Variables

   #endregion
}

[Serializable]
public class RawPaletteToolData {
   // The database Id
   public int xmlId;

   // The xml content
   public string xmlData;

   // The subcategory for referencing
   public string subcategory;

   // The tag id for referencing
   public int tagId;
}