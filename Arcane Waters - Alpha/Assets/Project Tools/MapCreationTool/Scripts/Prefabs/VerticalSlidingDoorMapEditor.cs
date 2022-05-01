using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class VerticalSlidingDoorMapEditor : MapEditorPrefab, IPrefabDataListener
   {
      #region Public Variables

      // The original component
      public SlidingDoor slidingDoor;

      #endregion

      public override void placedInEditor () {
         _placedInEditor = true;
      }

      public override void createdForPreview () {
         _createdForPreview = true;
      }

      public void dataFieldChanged (DataField field) {
         if (_placedInEditor || _createdForPreview) {
            if (field.k.Equals(DataField.SLIDING_DOOR_WIDTH_KEY)) {
               if (field.tryGetIntValue(out int val)) {
                  slidingDoor.setWidth(val);
               }
            }
         }
      }

      #region Private Variables

      // Were we placed in editor
      private bool _placedInEditor = false;

      // Were we created for preview
      private bool _createdForPreview = false;

      #endregion
   }
}
