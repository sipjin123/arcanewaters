using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class HouseMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable {
      [SerializeField]
      private GameObject lockVisual = null;

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
         if (key.CompareTo("target map") == 0) {
            targetMap = value;
            rewriteText();
         } else if (key.CompareTo("target spawn") == 0) {
            targetSpawn = value;
            rewriteText();
         } else if(key.CompareTo("locked") == 0) {
            lockVisual.SetActive(bool.Parse(value));
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

