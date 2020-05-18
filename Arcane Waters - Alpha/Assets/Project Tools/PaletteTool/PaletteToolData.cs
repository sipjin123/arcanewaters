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

   // Number of pixels to compare (size = pow(x, 2); size >= 16)
   public int size;

   // Left side color - source colors
   public string[] srcColor;

   // Right side color - destination colors
   public string[] dstColor;

   // Type of palette e.g. hair/eyes/weapon
   public int paletteType;

   #endregion

   public PaletteToolData () { }

   public PaletteToolData (string paletteName, int size, string[] srcColor, string[] dstColor, int paletteType) {
      this.paletteName = paletteName;
      this.size = size;
      this.srcColor = srcColor;
      this.dstColor = dstColor;
      this.paletteType = paletteType;
   }

   public static PaletteToolData CreateAchievementData (PaletteToolData copy) {
      PaletteToolData newData = new PaletteToolData();
      newData.paletteName = copy.paletteName;
      newData.size = copy.size;
      newData.srcColor = copy.srcColor;
      newData.dstColor = copy.dstColor;
      newData.paletteType = copy.paletteType;
      return newData;
   }
 
   #region Private Variables

   #endregion
}
