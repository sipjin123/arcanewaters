using UnityEngine;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class SpiderWebMapEditor : MapEditorPrefab, IPrefabDataListener
   {
      public const float width = 2;

      // A multiplier used to convert between editor and in-game scaling. (In-game * factor = editor scaling, Editor / factor = in-game scaling).
      public static float EDITOR_SCALING_FACTOR = 0.16f;

      // height of the effector, used for the launch
      public float destinationX, destinationY;

      // Visual elements for indicating current launch height
      public SpriteRenderer heightLine;
      public SpriteRenderer heightBox;

      public void dataFieldChanged (DataField field) {
         if (field.k.CompareTo(DataField.SPIDER_WEB_X_KEY) == 0) {
            if (field.tryGetFloatValue(out float value)) {
               destinationX = value;
            }
         }

         if (field.k.CompareTo(DataField.SPIDER_WEB_Y_KEY) == 0) {
            if (field.tryGetFloatValue(out float value)) {
               destinationY = value;
            }
         }

         heightBox.transform.localPosition = new Vector3(destinationX * EDITOR_SCALING_FACTOR, destinationY * EDITOR_SCALING_FACTOR, 0);
         // heightLine.transform.localPosition = new Vector3(destinationX * 0.16f, destinationY * 0.08f, 0);
         // heightLine.transform.localScale = new Vector3(destinationX, destinationY * 0.16f, heightLine.transform.localScale.z);
      }

      public override void createdInPalette () {
         heightBox.enabled = false;
         // heightLine.enabled = false;
      }

      public override void createdForPreview () {
         heightBox.enabled = false;
         // heightLine.enabled = false;
      }

      public override void placedInEditor () {
         heightBox.enabled = true;
         // heightLine.enabled = true;
      }
   }
}
