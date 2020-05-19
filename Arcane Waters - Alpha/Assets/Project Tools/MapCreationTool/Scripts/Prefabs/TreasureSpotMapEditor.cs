using UnityEngine;
using UnityEngine.UI;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class TreasureSpotMapEditor : MapEditorPrefab, IPrefabDataListener
   {
      // The reference sprites
      [SerializeField]
      private Sprite landSprite = null;
      [SerializeField]
      private Sprite seaSprite = null;

      // The UI text
      private Text text;

      // If this treasure chest uses a custom sprite for secret purposes
      private bool useCustomSprite = false;

      private void Awake () {
         text = GetComponentInChildren<Text>();

         if (Tools.editorType == EditorType.Sea) {
            GetComponent<SpriteRenderer>().sprite = seaSprite;
         } else {
            GetComponent<SpriteRenderer>().sprite = landSprite;
         }
      }

      public void dataFieldChanged (DataField field) {
         if (field.k.CompareTo(DataField.TREASURE_SPOT_SPAWN_CHANCE_KEY) == 0) {
            if (field.tryGetFloatValue(out float chance)) {
               text.text = Mathf.Round(100f * Mathf.Clamp(chance, 0, 1)) + "%";
            }
         }
         if (field.k.CompareTo(DataField.TREASURE_USE_CUSTOM_TYPE_KEY) == 0) {
            try {
               if (field.tryGetIntValue(out int boolResult)) {
                  useCustomSprite = boolResult == 1 ? true : false;
               }
            } catch {
               useCustomSprite = false;
            }
         }
         if (field.k.CompareTo(DataField.TREASURE_SPRITE_TYPE_KEY) == 0) {
            if (useCustomSprite) {
               GetComponent<SpriteRenderer>().sprite = ImageManager.getSprites(field.v)[0];
            }
         }
      }
   }
}
