using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using MapCreationTool;
using System.Linq;
using System;

public class SecretsEntranceMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable {
   #region Public Variables

   // The sprite outline for highlighting
   public SpriteOutline spriteOutline; 

   // The text component for the area to warp to
   public Text warpText;
   public Text secretTypeText;

   // The scale when editing this obj
   public static int EDITOR_SCALE = 6;
   public static int EDITOR_SCALE_TOOL = 3;

   // List of secret prefabs
   public List<SecretsPrefabCollection> secretPrefabs;

   // Holds the spawnable secret obj
   public Transform secretObjHolder;

   // The secret type
   public SecretType secretType;

   #endregion

   private void Awake () {
      secretTypeText = GetComponentInChildren<Text>();
      secretTypeText.text = "Secrets ID: -";
   }

   private void Start () {
      if (gameObject.name.Contains("(Clone)")) {
         transform.localScale = new Vector3(EDITOR_SCALE_TOOL, EDITOR_SCALE_TOOL, EDITOR_SCALE_TOOL);
      } else {
         transform.localScale = new Vector3(EDITOR_SCALE, EDITOR_SCALE, EDITOR_SCALE);
      }
   }

   public void dataFieldChanged (DataField field) {
      if (field.k.CompareTo(DataField.SECRETS_TYPE_ID) == 0) {
         secretObjHolder.gameObject.DestroyChildren();
         secretTypeText.text = "Secrets ID: " + field.v;
         secretType = (SecretType) Enum.Parse(typeof(SecretType), field.v);
         if (secretType != SecretType.None) {
            GameObject secretObjVariant = Instantiate(secretPrefabs.Find(_ => _.secretType == secretType).secretPrefabVariant, secretObjHolder);
            secretObjVariant.transform.localPosition = Vector3.zero;
         }
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
      } 
   }

   public void setHighlight (bool hovered, bool selected, bool deleting) {
      setOutlineHighlight(spriteOutline, hovered, selected, deleting);
   }

   private void updateText () {
      warpText.text = $"{ _targetMap }\n{ _targetSpawn }";
   }


   #region Private Variables

   // The target area for warping
   private string _targetMap = "";
   private string _targetSpawn = "";

   #endregion
}

[Serializable]
public class SecretsPrefabCollection {
   // The secret type
   public SecretType secretType;

   // The prefab to use depending on secret type
   public GameObject secretPrefabVariant;
}