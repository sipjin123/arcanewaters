using UnityEngine;
using UnityEngine.UI;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class HouseMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable
   {
      private SpriteOutline outline;
      private Text text;

      private string targetMap = "";
      private string targetSpawn = "";

      private void Awake () {
         outline = GetComponent<SpriteOutline>();
         text = GetComponentInChildren<Text>();
      }

      public override void createdForPrieview () {
         transform.localScale = new Vector3(6.25f, 6.25f, 1f);
      }

      public override void placedInEditor () {
         transform.localScale = new Vector3(6.25f, 6.25f, 1f);
      }

      public void dataFieldChanged (string key, string value) {
         if (key.CompareTo(DataField.HOUSE_TARGET_MAP_KEY) == 0) {
            targetMap = value;
            rewriteText();
         } else if (key.CompareTo(DataField.HOUSE_TARGET_SPAWN_KEY) == 0) {
            targetSpawn = value;
            rewriteText();
         }
      }

      private void rewriteText () {
         if (text != null)
            text.text = $"WARP TO\nmap: {targetMap}\nspawn: {targetSpawn}";
      }

      public void setHighlight (bool hovered, bool selected) {
         setOutlineHighlight(outline, hovered, selected);
      }
   }
}

