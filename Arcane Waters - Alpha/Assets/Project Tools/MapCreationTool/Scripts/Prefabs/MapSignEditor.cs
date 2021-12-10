using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool;
using MapCreationTool.Serialization;

public class MapSignEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable
{
   #region Public Variables

   // The highlight object
   public SpriteRenderer highlight = null;

   // The reference to the actual object
   public MapSign mapSign;

   #endregion

   public override void createdInPalette () {
      // Don't show sign label in palette
      mapSign.directionLabelUI.SetActive(false);
   }

   public void dataFieldChanged (DataField field) {
      try {
         if (field.k.CompareTo(DataField.MAP_SIGN_TYPE_KEY) == 0) {
            mapSign.setPostType(field.intValue);
            highlight.sprite = mapSign.mapSignPost.sprite;
         } else if (field.k.CompareTo(DataField.MAP_ICON_KEY) == 0) {
            mapSign.setIconType(field.intValue);
         } else if (field.k.CompareTo(DataField.MAP_SIGN_LABEL) == 0) {
            mapSign.signLabel.text = field.v;
         }
      } catch { }
   }

   public void setHighlight (bool hovered, bool selected, bool deleting) {
      setSpriteHighlight(highlight, hovered, selected, deleting);
   }
}