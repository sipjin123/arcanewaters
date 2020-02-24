using UnityEngine;

namespace MapCreationTool
{
   public class ToolTipManager : MonoBehaviour
   {
      public static ToolTip currentToolTip { get; private set; }

      public static void showToolTip (ToolTip toolTip) {
         currentToolTip = toolTip;
      }

      public static void hideToolTip (ToolTip toolTip) {
         if (currentToolTip == toolTip) {
            currentToolTip = null;
         }
      }

      public static string currentMessage
      {
         get
         {
            return currentToolTip == null ? "" : currentToolTip.message;
         }
      }
   }
}

