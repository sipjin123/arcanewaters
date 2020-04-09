using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using MapCreationTool;

public class SecretsEntranceMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable {
   #region Public Variables

   [SerializeField]
   private SpriteRenderer highlight = null;

   private SecretEntrance secretsNode;
   private Text text;
   public Text warpText;

   private string targetMap = "";
   private string targetSpawn = "";

   #endregion

   private void Awake () {
      secretsNode = GetComponent<SecretEntrance>();
      secretsNode.transform.localScale = new Vector3(2, 2, 2);
      text = GetComponentInChildren<Text>();
      text.text = "Secrets ID: -";
   }

   public void dataFieldChanged (DataField field) {
      if (field.k.CompareTo(DataField.SECRETS_TYPE_ID) == 0) {
         text.text = "Secrets ID: " + field.v;
      } else if (field.k.CompareTo(DataField.WARP_TARGET_MAP_KEY) == 0) {
         if (field.tryGetIntValue(out int mapId)) {
            if (!Overlord.remoteMaps.maps.ContainsKey(mapId)) {
               targetMap = "Unrecognized";
            } else {
               targetMap = Overlord.remoteMaps.maps[mapId].name;
            }
         } else {
            targetMap = "Unrecognized";
         }
         updateText();
      } else if (field.k.CompareTo(DataField.WARP_TARGET_SPAWN_KEY) == 0) {
         targetSpawn = field.v;
         updateText();
      }
   }

   public void setHighlight (bool hovered, bool selected, bool deleting) {
      setSpriteOutline(highlight, hovered, selected, deleting);
   }

   private void updateText () {
      warpText.text = $"{ targetMap }\n{ targetSpawn }";
   }
}
