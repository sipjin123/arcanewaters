using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class ToolSelect : MonoBehaviour
   {
      public event Action<ToolType> OnValueChanged;

      [SerializeField]
      private ToolType value = ToolType.Brush;
      [SerializeField]
      private ToolTypeButton[] buttons = new ToolTypeButton[0];

      private Dictionary<ToolType, Button> typeButtons = new Dictionary<ToolType, Button>();

      private void Awake () {
         updateButtonDictionary();
      }

      private void updateButtonDictionary () {
         typeButtons.Clear();
         foreach (ToolTypeButton entry in buttons) {
            if (entry.button == null) {
               continue;
            }
            typeButtons.Add(entry.toolType, entry.button);
            entry.button.onClick.RemoveAllListeners();
            entry.button.onClick.AddListener(() => onButtonClick(entry.toolType));
         }
      }

      [ExecuteInEditMode]
      private void OnValidate () {
         updateButtonDictionary();
         foreach (ToolTypeButton entry in buttons) {
            if (entry.button == null)
               continue;
            entry.button.interactable = true;
         }
         setValueNoNotify(value);
      }

      public void setValueNoNotify (ToolType value) {
         if (typeButtons.TryGetValue(this.value, out Button button)) {
            button.interactable = true;
         }

         this.value = value;

         if (typeButtons.TryGetValue(this.value, out Button button2)) {
            button2.interactable = false;
         }
      }

      public void setValue (ToolType value) {
         setValueNoNotify(value);
         OnValueChanged?.Invoke(value);
      }

      private void onButtonClick (ToolType value) {
         setValue(value);
      }

      [Serializable]
      private class ToolTypeButton
      {
         public ToolType toolType = ToolType.Brush;
         public Button button = null;
      }
   }
}

