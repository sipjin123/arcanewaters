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
      private PrefabDataDefinition data;
      private Image bg;

      private Dictionary<string, Field> fields = new Dictionary<string, Field>();

      private void Awake () {
         cGroup = GetComponent<CanvasGroup>();
         bg = transform.parent.GetComponent<Image>();

         hide();
      }

      private void OnEnable () {
         DrawBoard.SelectedPrefabChanged += prefabChanged;
         DrawBoard.SelectedPrefabDataChanged += setData;
      }

      private void OnDisable () {
         DrawBoard.SelectedPrefabChanged -= prefabChanged;
         DrawBoard.SelectedPrefabDataChanged -= setData;
      }

      private void prefabChanged (PrefabDataDefinition data, PlacedPrefab placedPrefab) {
         this.data = data;

         if (data == null) {
            hide();
            return;
         }

         foreach (var f in fields) {
            Destroy(f.Value.gameObject);
         }

         fields.Clear();

         show();

         setLayout(data);
         setData(placedPrefab);
      }

      private void setLayout (PrefabDataDefinition data) {
         titleText.text = string.IsNullOrWhiteSpace(data.title) ? "Unnamed" : data.title;

         // Custom logic for warps
         if (data.title.CompareTo("Warp") == 0 || data.title.CompareTo("House") == 0) {
            // target map
            Field mf = Instantiate(dropdownFieldPref, transform);
            mf.fieldName.text = "target map";
            mf.ValueChanged += (value) => valueChanged("target map", value);
            mf.toolTipMessage = "Target Map to Warp Into";
            fields.Add("target map", mf);

            // target spawn
            Field sf = Instantiate(dropdownFieldPref, transform);
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

      private void setData (PlacedPrefab placedPrefab) {
         foreach (var f in fields) {
            f.Value.setValue(placedPrefab.getData(f.Key));
         }

         if (titleText.text.CompareTo("Warp") == 0 || data.title.CompareTo("House") == 0) {
            fields["target map"].setFieldProperties(Enumerable.Repeat("", 1).Union(Overlord.instance.mapSpawns.Keys).ToArray());
            fields["target map"].setValue(placedPrefab.getData("target map"));
            string mapValue = fields["target map"].valueDropdown.options[fields["target map"].valueDropdown.value].text;

            if (Overlord.instance.mapSpawns.ContainsKey(mapValue)) {
               fields["target spawn"].setFieldProperties(Enumerable.Repeat("", 1).Union(Overlord.instance.mapSpawns[mapValue]).ToArray());
            } else {
               fields["target spawn"].setFieldProperties(new string[] { "" });
            }

            fields["target spawn"].setValue(placedPrefab.getData("target spawn"));
         }
      }

      private void valueChanged (string key, string value) {
         drawBoard.setSelectedPrefabData(key, value);

         if (titleText.text.CompareTo("Warp") == 0 || data.title.CompareTo("House") == 0) {
            if (key.CompareTo("target map") == 0) {
               if (Overlord.instance.mapSpawns.ContainsKey(value)) {
                  fields["target spawn"].setFieldProperties(Enumerable.Repeat("", 1).Union(Overlord.instance.mapSpawns[value]).ToArray());
               } else {
                  fields["target spawn"].setFieldProperties(new string[] { "" });
               }

               fields["target spawn"].setValue("");
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