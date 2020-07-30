using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool;
using MapCreationTool.Serialization;

public class MapSignEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable {
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
      Sprite[] mapSignSprites = ImageManager.getSprites(mapSign.mapSignIcon.sprite.texture);
      if (field.k.CompareTo(DataField.MAP_SIGN_TYPE_KEY) == 0) {
         try {
            int mapTypeIndex = int.Parse(field.v);
            mapTypeIndex = Mathf.Clamp(mapTypeIndex, 0, MapSign.MAP_SIGN_START_INDEX);
            mapSign.mapSignPost.sprite = mapSignSprites[mapTypeIndex];
            highlight.sprite = mapSignSprites[mapTypeIndex];
         } catch { }
      }
      if (field.k.CompareTo(DataField.MAP_ICON_KEY) == 0) {
         try {
            int mapIconIndex = int.Parse(field.v);
            mapIconIndex = Mathf.Clamp(mapIconIndex, 0, (mapSignSprites.Length - 1) - MapSign.MAP_SIGN_START_INDEX);
            mapSign.mapSignIcon.sprite = mapSignSprites[MapSign.MAP_SIGN_START_INDEX + mapIconIndex];
         } catch { }
      }
      if (field.k.CompareTo(DataField.MAP_SIGN_LABEL) == 0) {
         try {
            mapSign.signLabel.text = field.v;
         } catch { }
      }
   }

   public void setHighlight (bool hovered, bool selected, bool deleting) {
      setSpriteOutline(highlight, hovered, selected, deleting);
   }
}