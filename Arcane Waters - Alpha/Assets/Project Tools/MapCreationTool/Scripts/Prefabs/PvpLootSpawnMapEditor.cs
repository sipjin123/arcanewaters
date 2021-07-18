using MapCreationTool;
using MapCreationTool.Serialization;
using UnityEngine;

public class PvpLootSpawnMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable {
   #region Public Variables

   // Components
   private SpriteOutline outline;

   #endregion

   private void Awake () {
      outline = GetComponentInChildren<SpriteOutline>();
   }

   public void dataFieldChanged (DataField field) {

   }

   public void setHighlight (bool hovered, bool selected, bool deleting) {
      setOutlineHighlight(outline, hovered, selected, deleting);
   }

   #region Private Variables

   #endregion
}