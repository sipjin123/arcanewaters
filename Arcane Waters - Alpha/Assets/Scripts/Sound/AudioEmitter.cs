using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool;
using MapCreationTool.Serialization;

public class AudioEmitter : MapEditorPrefab, IPrefabDataListener, IHighlightable {
   #region Public Variables

   // Components
   private SpriteRenderer ren;
   private SpriteOutline outline;

   #endregion

   private void Awake () {
      ren = GetComponentInChildren<SpriteRenderer>();
      outline = GetComponentInChildren<SpriteOutline>();
   }

   public void dataFieldChanged (DataField field) {
      throw new System.NotImplementedException();
   }

   public void setHighlight (bool hovered, bool selected, bool deleting) {
      setOutlineHighlight(outline, hovered, selected, deleting);
   }

   #region Private Variables

   #endregion
}
