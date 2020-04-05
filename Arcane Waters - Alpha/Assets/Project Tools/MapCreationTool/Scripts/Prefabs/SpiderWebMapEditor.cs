using UnityEngine;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class SpiderWebMapEditor : MapEditorPrefab, IPrefabDataListener
   {
      public const float width = 2;

      // height of the effector, used for the launch
      public float height;

      private RectTransform rect;

      private void Awake () {
         rect = GetComponentInChildren<Canvas>(true).GetComponent<RectTransform>();
      }

      public void dataFieldChanged (DataField field) {
         if (field.k.CompareTo(DataField.SPIDER_WEB_HEIGHT_KEY) == 0) {
            if (field.tryGetFloatValue(out float h)) {
               height = h;
            }
         }

         rect.sizeDelta = new Vector2(rect.sizeDelta.x, height * 100f);
      }

      public override void createdInPalette () {
         GetComponentInChildren<Canvas>(true).enabled = false;
      }

      public override void createdForPreview () {
         GetComponentInChildren<Canvas>(true).enabled = false;
      }

      public override void placedInEditor () {
         GetComponentInChildren<Canvas>(true).enabled = true;
      }
   }

}
