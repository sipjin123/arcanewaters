using System.Collections.Generic;
using System.Linq;
using MapCreationTool.Serialization;
using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class SelectedPrefabPanel : MonoBehaviour
   {
      [SerializeField]
      private Text titleText = null;
      [SerializeField]
      private Field inputFieldPref = null;
      [SerializeField]
      private Field dropdownFieldPref = null;
      [SerializeField]
      private DrawBoard drawBoard = null;

      private CanvasGroup cGroup;

      private Dictionary<string, Field> fields = new Dictionary<string, Field>();

      private Selection selection;
      private PlacedPrefab prefab;
      private PrefabDataDefinition dataDef;
      private bool hasWarpLogic = false;

      private void Awake () {
         cGroup = GetComponent<CanvasGroup>();

         hide();
      }

      private void OnEnable () {
         DrawBoard.SelectionChanged += selectionChanged;
         DrawBoard.PrefabDataChanged += prefabDataChanged;
      }

      private void OnDisable () {
         DrawBoard.SelectionChanged -= selectionChanged;
         DrawBoard.PrefabDataChanged -= prefabDataChanged;
      }

      private void selectionChanged (Selection selection) {
         if (selection.empty) {
            hide();
            return;
         }

         this.selection = selection;
         prefab = null;

         if (selection.tiles.Count > 0 && selection.prefabs.Count > 0) {
            titleText.text = "Multiple Objects";
         } else if (selection.tiles.Count > 0) {
            titleText.text = "Tiles";
         } else if (selection.prefabs.Count > 1) {
            titleText.text = "Multiple Prefabs";
         } else {
            titleText.text = "Prefab";
            prefab = selection.prefabs.First();
         }

         foreach (var f in fields) {
            Destroy(f.Value.gameObject);
         }

         fields.Clear();

         show();

         if (prefab != null) {
            dataDef = prefab.placedInstance.GetComponent<PrefabDataDefinition>();
            if (dataDef != null) {
               hasWarpLogic = dataDef.title.CompareTo("Warp") == 0 || dataDef.title.CompareTo("House") == 0 || dataDef.title.CompareTo("Secret") == 0;
               setLayout(dataDef, prefab);
               setData(prefab, dataDef);
            }
         }
      }

      private void prefabDataChanged (PlacedPrefab prefab) {
         if (prefab == this.prefab && dataDef != null) {
            setData(prefab, dataDef);
         }
      }

      private void setLayout (PrefabDataDefinition data, PlacedPrefab prefab) {
         titleText.text = data.title + " " + prefab.getData(DataField.PLACED_PREFAB_ID);
         // Custom logic for warps
         if (hasWarpLogic) {
            // target map
            Field mf = Instantiate(dropdownFieldPref, transform);
            mf.setFieldProperties(Overlord.remoteMaps.formMapsSelectOptions());
            mf.fieldName.text = "target map";
            mf.ValueChanged += (value) => valueChanged("target map", value);
            mf.toolTipMessage = "Target Map to Warp Into";
            fields.Add("target map", mf);

            // TODO: Remove this after spawn feature functionality is confirmed
            // facing position
            /*
            try {
               Field facingDir = Instantiate(dropdownFieldPref, transform);
               facingDir.setFieldProperties(Overlord.remoteMaps.formMapsSelectOptions());
               facingDir.fieldName.text = "target map";
               facingDir.ValueChanged += (value) => valueChanged("target map", value);
               facingDir.toolTipMessage = "Target Map to Warp Into";
               fields.Add("target map", facingDir);
            } catch {

            }*/

            // target spawn
            Field sf = Instantiate(dropdownFieldPref, transform);
            sf.setFieldProperties(Overlord.remoteSpawns.formSpawnsSelectOptions(-1));
            sf.fieldName.text = "target spawn";
            sf.ValueChanged += (value) => valueChanged("target spawn", value);
            sf.toolTipMessage = "In the Selected 'target map', Which Spawn to Warp into";
            fields.Add("target spawn", sf);
         }

         foreach (var fieldDef in data.dataFields) {
            Field f = Instantiate(inputFieldPref, transform);
            f.setFieldProperties(fieldDef.type);
            f.fieldName.text = fieldDef.name + ":";
            f.toolTipMessage = fieldDef.toolTip;

            f.ValueChanged += (value) => valueChanged(fieldDef.name, value);

            fields.Add(fieldDef.name, f);
         }

         foreach (var fieldDef in data.selectDataFields) {
            Field f = Instantiate(dropdownFieldPref, transform);
            f.setFieldProperties(fieldDef.options);
            f.fieldName.text = fieldDef.name + ":";
            f.toolTipMessage = fieldDef.toolTip;

            f.ValueChanged += (value) => valueChanged(fieldDef.name, value);

            fields.Add(fieldDef.name, f);
         }
      }

      private void setData (PlacedPrefab placedPrefab, PrefabDataDefinition data) {
         foreach (var f in fields) {
            if (f.Key == DataField.SHIP_DATA_KEY) {
               // Overwrite default ship value into the first entry upon selecting object in the drawing board
               if (placedPrefab.getData(f.Key) == "None") {
                  D.debug("Value set to" + " : " + SeaMonsterEntityData.DEFAULT_SHIP_ID);
                  f.Value.setValue(SeaMonsterEntityData.DEFAULT_SHIP_ID.ToString());
               } else {
                  f.Value.setValue(placedPrefab.getData(f.Key));
               }
            } else {
               f.Value.setValue(placedPrefab.getData(f.Key));
            }
         }

         if (hasWarpLogic) {
            fields["target map"].setFieldProperties(Overlord.remoteMaps.formMapsSelectOptions());
            fields["target map"].setValue(placedPrefab.getData("target map"));
            string mapValue = fields["target map"].value;

            if (int.TryParse(mapValue, out int mapId)) {
               fields["target spawn"].setFieldProperties(Overlord.remoteSpawns.formSpawnsSelectOptions(mapId));
            } else {
               fields["target spawn"].setFieldProperties(new SelectOption("-1", "").toArray());
            }

            fields["target spawn"].setValue(placedPrefab.getData("target spawn"));
         }
      }

      private void valueChanged (string key, string value) {
         if (dataDef == null) {
            return;
         }

         drawBoard.setPrefabData(prefab, key, value);

         if (hasWarpLogic) {
            if (key.CompareTo("target map") == 0) {
               if (int.TryParse(value, out int mapId)) {
                  fields["target spawn"].setFieldProperties(Overlord.remoteSpawns.formSpawnsSelectOptions(mapId));
               } else {
                  fields["target spawn"].setFieldProperties(new SelectOption("-1", "").toArray());
               }

               fields["target spawn"].setValue("-1");
            }
         }
      }

      public void show () {
         cGroup.alpha = 1;
         cGroup.blocksRaycasts = true;
         cGroup.interactable = true;
      }

      public void hide () {
         cGroup.alpha = 0;
         cGroup.blocksRaycasts = false;
         cGroup.interactable = false;
      }
   }
}