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
         Tools.AnythingChanged += checkToolChanged;
      }

      private void OnDisable () {
         Tools.AnythingChanged -= checkToolChanged;
      }

      private void checkToolChanged () {
         if (active && Tools.toolType != toolType) {
            active = false;
            unregisterUIEvents();
            cancelAction();
         } else if (!active && Tools.toolType == toolType) {
            active = true;
            registerUIEvents();
         }
      }

      protected virtual void registerUIEvents () { }

      protected virtual void unregisterUIEvents () { }

      protected virtual void cancelAction () { }
   }
}
