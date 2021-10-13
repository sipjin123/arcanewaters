using MapCreationTool;
using MapCreationTool.Serialization;
using UnityEngine;

public class PvpLootSpawnMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable {
   #region Public Variables

   // Components
   public SpriteOutline outline;

   #endregion

   private void Awake () {
      outline = GetComponentInChildren<SpriteOutline>();
   }

   private void Start () {
      if (transform.parent.gameObject.name == "prefabs") {
         Vector3 currScale = transform.localScale;
         transform.localScale = currScale * 1.5f;
      }
   }

   public void dataFieldChanged (DataField field) {
   }

   public void setHighlight (bool hovered, bool selected, bool deleting) {
      setOutlineHighlight(outline, hovered, selected, deleting);
   }

   #region Private Variables

   #endregion
}