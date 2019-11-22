using System.Collections.Generic;
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

         foreach (var fieldDef in data.dataFields) {
            Field f = Instantiate(inputFieldPref, transform);
            f.setFieldProperties(fieldDef.type);
            f.fieldName.text = fieldDef.name + ":";

            f.ValueChanged += (value) => valueChanged(fieldDef.name, value);

            fields.Add(fieldDef.name, f);
         }

         foreach (var fieldDef in data.selectDataFields) {
            Field f = Instantiate(dropdownFieldPref, transform);
            f.setFieldProperties(fieldDef.options);
            f.fieldName.text = fieldDef.name + ":";

            f.ValueChanged += (value) => valueChanged(fieldDef.name, value);

            fields.Add(fieldDef.name, f);
         }
      }

      private void setData (PlacedPrefab placedPrefab) {
         foreach (var f in fields) {
            f.Value.setValue(placedPrefab.getData(f.Key));
         }
      }

      private void valueChanged (string key, string value) {
         drawBoard.setSelectedPrefabData(key, value);
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