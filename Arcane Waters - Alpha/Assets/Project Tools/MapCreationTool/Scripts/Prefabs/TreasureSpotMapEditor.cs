using UnityEngine;
using UnityEngine.UI;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class TreasureSpotMapEditor : MapEditorPrefab, IPrefabDataListener
   {
      [SerializeField]
      private Sprite landSprite = null;
      [SerializeField]
      private Sprite seaSprite = null;

      private Text text;

      private void Awake () {
         text = GetComponentInChildren<Text>();

         if (Tools.editorType == EditorType.Sea) {
            GetComponent<SpriteRenderer>().sprite = seaSprite;
         } else {
            GetComponent<SpriteRenderer>().sprite = landSprite;
         }
      }

      public void dataFieldChanged (string key, string value) {
         if (key.CompareTo(DataField.TREASURE_SPOT_SPAWN_CHANCE_KEY) == 0) {
            if (float.TryParse(value, out float chance)) {
               text.text = Mathf.Round(100f * Mathf.Clamp(chance, 0, 1)) + "%";
            }
         }
      }
   }
}
