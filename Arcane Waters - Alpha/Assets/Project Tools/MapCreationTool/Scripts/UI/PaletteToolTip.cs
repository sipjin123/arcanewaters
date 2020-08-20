namespace MapCreationTool
{
   public class PaletteToolTip : ToolTip
   {
      public void setToolTip (string message) {
         this.message = message;
         pointerEnter();
      }

      public void hideToolTip () {
         pointerExit();
      }
   }
}
