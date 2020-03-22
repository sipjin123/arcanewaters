using System.Collections.Generic;
using UnityEngine;

namespace MapCreationTool
{
   public class Tool : MonoBehaviour
   {
      protected bool active = false;
      protected virtual ToolType toolType => ToolType.Brush;

      private void Start () {
         if (Tools.toolType == toolType) {
            active = true;
            registerUIEvents();
         }
      }

      private void OnEnable () {
         Tools.ToolChanged += onToolChanged;
      }

      private void OnDisable () {
         Tools.ToolChanged -= onToolChanged;
      }

      private void onToolChanged (ToolType from, ToolType to) {
         if (active && from == toolType) {
            active = false;
            unregisterUIEvents();
            cancelAction();
         } else if (!active && to == toolType) {
            active = true;
            registerUIEvents();
         }
      }

      protected virtual void registerUIEvents () {

      }

      protected virtual void unregisterUIEvents () {

      }

      protected virtual void cancelAction () {

      }
   }
}
