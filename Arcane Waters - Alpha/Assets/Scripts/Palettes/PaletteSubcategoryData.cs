using System;

[Serializable]
public class PaletteSubcategoryData
{
   #region Public Variables

   // SQL id
   public int xmlId;

   // The name of this subcategory
   public string name;

   // The palette type of this subcategory
   public PaletteRecolorData.PaletteType type;

   // Source colors, in hex format
   public string[] colorsHex;

   #endregion

   #region Private Variables

   #endregion
}
