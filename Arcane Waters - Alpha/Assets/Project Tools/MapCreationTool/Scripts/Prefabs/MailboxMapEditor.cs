using MapCreationTool;
using MapCreationTool.Serialization;

public class MailboxMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable {
   #region Public Variables

   // The biome type
   public Biome.Type biomeType;

   #endregion

   public void dataFieldChanged (DataField field) {
   }

   public void setHighlight (bool hovered, bool selected, bool deleting) {
   }

   #region Private Variables

   #endregion
}
