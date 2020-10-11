using UnityEngine;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class SpiderWebMapEditor : MapEditorPrefab, IPrefabDataListener
   {
      public const float width = 2;

      // height of the effector, used for the launch
      public float height;

      // Visual elements for indicating current launch height
      public SpriteRenderer heightLine;
      public SpriteRenderer heightBox;

      public void dataFieldChanged (DataField field) {
         if (field.k.CompareTo(DataField.SPIDER_WEB_HEIGHT_KEY) == 0) {
            if (field.tryGetFloatValue(out float h)) {
               height = h + SpiderWeb.CONSTANT_HEIGHT;
            }
         }

         heightBox.transform.localPosition = new Vector3(heightBox.transform.localPosition.x, height * 0.16f, 0);
         heightLine.transform.localPosition = new Vector3(heightLine.transform.localPosition.x, (height - 0.5f) * 0.08f, 0);
         heightLine.transform.localScale = new Vector3(heightLine.transform.localScale.x, (height - 0.5f) * 0.16f, heightLine.transform.localScale.z);
      }

      public override void createdInPalette () {
         heightBox.enabled = false;
         heightLine.enabled = false;
      }

      public override void createdForPreview () {
         heightBox.enabled = false;
         heightLine.enabled = false;
      }

      public override void placedInEditor () {
         heightBox.enabled = true;
         heightLine.enabled = true;
      }
   }
}
