public class StoreDyeBox : StoreItemBox
{
   #region Public Variables

   // The clothing dye information
   public DyeData dye;

   // Palette information
   public PaletteToolData palette;

   // Reference to the banner that displays the type of the dye (primary, secondary or accent)
   public StoreDyeTypeBanner banner;

   #endregion

   public void setupBanner() {
      // Show the banner
      banner.toggle(palette != null);

      // Change the sprite
      if (palette == null) {
         return;
      }

      if (palette.isPrimary()) {
         banner.setAsPrimary();
      } else if (palette.isSecondary()) {
         banner.setAsSecondary();
      } else if (palette.isAccent()) {
         banner.setAsAccent();
      }
   }

   #region Private Variables

   #endregion
}

