using UnityEngine;
using UnityEngine.UI;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class WarpMapEditor : MapEditorPrefab, IPrefabDataListener
   {
      [SerializeField]
      private RectTransform boundsRect = null;
      [SerializeField]
      private Text text = null;
      [SerializeField]
      private SpriteRenderer arrowRen = null;

      private string targetMap = "";
      private string targetSpawn = "";

      private float width = 3f;
      private float height = 3f;

      private Direction arriveFacing = Direction.North;

      public void dataFieldChanged (DataField field) {
         if (field.k.CompareTo(DataField.WARP_TARGET_MAP_KEY) == 0) {
            if (field.tryGetIntValue(out int mapId)) {
               if (!Overlord.remoteMaps.maps.ContainsKey(mapId)) {
                  targetMap = "Unrecognized";
               } else {
                  targetMap = Overlord.remoteMaps.maps[mapId].name;
               }
            } else {
               targetMap = "Unrecognized";
            }
            updateText();
         } else if (field.k.CompareTo(DataField.WARP_TARGET_SPAWN_KEY) == 0) {
            targetSpawn = field.v;
            updateText();
         } else if (field.k.CompareTo(DataField.WARP_ARRIVE_FACING_KEY) == 0) {
            if (field.tryGetDirectionValue(out Direction dir)) {
               arriveFacing = dir;
            }
         }

         Sprite sprite = getArrowSprite(Tools.editorType, arriveFacing);
         if (sprite != null) {
            arrowRen.sprite = sprite;
         }
      }

      public static Sprite getArrowSprite (EditorType editorType, Direction arriveFacing) {
         string dir = arriveFacing.ToString().ToLower();
         string color = "gold";
         if (editorType == EditorType.Sea) {
            color = "blue";
         }

         string spriteName = $"warp_{color}_{dir}";

         return ImageManager.getSprite("Map/Warp Arrows/" + spriteName);
      }

      public override void createdInPalette () {
         boundsRect.gameObject.SetActive(false);
      }

      public override void createdForPreview () {
         boundsRect.gameObject.SetActive(false);
      }

      public override void placedInEditor () {
         boundsRect.gameObject.SetActive(false);
      }

      public override void setHovered (bool hovered) {
         base.setHovered(hovered);
         boundsRect.gameObject.SetActive(hovered || selected);
      }

      private void updateBoundsSize () {
         boundsRect.sizeDelta = new Vector2(width * 100, height * 100);
      }

      private void updateText () {
         text.text = $"{ targetMap }\n{ targetSpawn }";
      }
   }
}