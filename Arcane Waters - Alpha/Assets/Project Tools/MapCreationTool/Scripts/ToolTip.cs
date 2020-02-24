using UnityEngine;

namespace MapCreationTool
{
   public class ToolTip : MonoBehaviour
   {
      public string message = "";

      protected virtual void pointerEnter () {
         ToolTipManager.showToolTip(this);
      }

      protected virtual void pointerExit () {
         ToolTipManager.hideToolTip(this);
      }
   }
}
