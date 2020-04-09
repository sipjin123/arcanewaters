using System.Collections.Generic;
using System.Linq;
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
      private Image bg;

      private Dictionary<string, Field> fields = new Dictionary<string, Field>();

      private Selection selection;
      private PlacedPrefab prefab;
      private PrefabDataDefinition dataDef;

      private void Awake () {
         cGroup = GetComponent<CanvasGroup>();
         bg = transform.parent.GetComponent<Image>();

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
               setLayout(dataDef);
               setData(prefab, dataDef);
            }
         }
      }

      private void prefabDataChanged (PlacedPrefab prefab) {
         if (prefab == this.prefab && dataDef != null) {
            setData(prefab, dataDef);
         }
      }

      private void setLayout (PrefabDataDefinition data) {
         titleText.text = data.title;
         // Custom logic for warps
         if (data.title.CompareTo("Warp") == 0 || data.title.CompareTo("House") == 0 || data.title.CompareTo("Secret") == 0) {
            // target map
            Field mf = Instantiate(dropdownFieldPref, transform);
            mf.setFieldProperties(Overlord.remoteMaps.formMapsSelectOptions());
            mf.fieldName.text = "target map";
            mf.ValueChanged += (value) => valueChanged("target map", value);
            mf.toolTipMessage = "Target Map to Warp Into";
            fields.Add("target map", mf);

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
            f.Value.setValue(placedPrefab.getData(f.Key));
         }

         if (titleText.text.CompareTo("Warp") == 0 || data.title.CompareTo("House") == 0 || data.title.CompareTo("Secret") == 0) {
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

         if (titleText.text.CompareTo("Warp") == 0 || dataDef.title.CompareTo("House") == 0) {
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
         bg.enabled = true;
      }

      public void hide () {
         cGroup.alpha = 0;
         cGroup.blocksRaycasts = false;
         cGroup.interactable = false;
         bg.enabled = false;
      }
   }
}