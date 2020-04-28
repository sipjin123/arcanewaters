using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using MapCreationTool;

public class SecretsEntranceMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable {
   #region Public Variables

   // The sprite outline for highlighting
   public SpriteOutline spriteOutline; 

   // The text component for the area to warp to
   public Text warpText;

   #endregion

   private void Awake () {
      _secretEntrance = GetComponent<SecretEntrance>();
      _secretEntrance.transform.localScale = new Vector3(2, 2, 2);
      _text = GetComponentInChildren<Text>();
      _text.text = "Secrets ID: -";
   }

   public void dataFieldChanged (DataField field) {
      if (field.k.CompareTo(DataField.SECRETS_TYPE_ID) == 0) {
         _text.text = "Secrets ID: " + field.v;
      } else if (field.k.CompareTo(DataField.WARP_TARGET_MAP_KEY) == 0) {
         if (field.tryGetIntValue(out int mapId)) {
            if (!Overlord.remoteMaps.maps.ContainsKey(mapId)) {
               _targetMap = "Unrecognized";
            } else {
               _targetMap = Overlord.remoteMaps.maps[mapId].name;
            }
         } else {
            _targetMap = "Unrecognized";
         }
         updateText();
      } else if (field.k.CompareTo(DataField.WARP_TARGET_SPAWN_KEY) == 0) {
         _targetSpawn = field.v;
         updateText();
      } else if (field.k.CompareTo(DataField.SECRETS_START_SPRITE) == 0) {
         _secretEntrance.mainSprite = ImageManager.getSprite(field.v);
         _secretEntrance.spriteRenderer.sprite = _secretEntrance.mainSprite;
         spriteOutline.Regenerate();
         spriteOutline.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;
      } else if (field.k.CompareTo(DataField.SECRETS_INTERACT_SPRITE) == 0) {
         _secretEntrance.subSprite = ImageManager.getSprites(field.v)[0];
         _secretEntrance.subSpriteRenderer.sprite = _secretEntrance.subSprite;
      }
   }

   public void setHighlight (bool hovered, bool selected, bool deleting) {
      setOutlineHighlight(spriteOutline, hovered, selected, deleting);
   }

   private void updateText () {
      warpText.text = $"{ _targetMap }\n{ _targetSpawn }";
   }


   #region Private Variables

   // Our components
   private SecretEntrance _secretEntrance;
   private Text _text;

   // The target area for warping
   private string _targetMap = "";
   private string _targetSpawn = "";

   #endregion
}
