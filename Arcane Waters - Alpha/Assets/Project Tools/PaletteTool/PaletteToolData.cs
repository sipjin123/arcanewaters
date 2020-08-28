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

   #endregion

   public PaletteToolData () { }

   public PaletteToolData (string paletteName, string[] srcColor, string[] dstColor, int paletteType) {
      this.paletteName = paletteName;
      this.srcColor = srcColor;
      this.dstColor = dstColor;
      this.paletteType = paletteType;
   }
 
   #region Private Variables

   #endregion
}
