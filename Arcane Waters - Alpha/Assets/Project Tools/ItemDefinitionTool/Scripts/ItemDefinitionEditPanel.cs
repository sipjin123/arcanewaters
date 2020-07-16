using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

namespace ItemDefinitionTool
{
   public class ItemDefinitionEditPanel : MonoBehaviour
   {
      #region Public Variables

      // Singleton instance
      public static ItemDefinitionEditPanel self;

      // Canvas group of the panel
      public CanvasGroup canvasGroup;

      // The title label of the panel, showing currently editable item
      public Text titleLabel;

      [Header("Controls for generic attributes")]
      // Sprite selector for the icon image
      public SpriteSelector iconSelector;

      // Label which shows the id of the item
      public Text idLabel;

      // Label which shows the creator id of the item
      public Text creatorIdLabel;

      // Toggle for changing enabled/disabled state of the item
      public Toggle enabledToggle;

      // Dropdown for changing category of an item
      public Dropdown categoryDropdown;

      // Input for changing the name of an item
      public InputField nameInput;

      // Input for changing the description of an item
      public InputField descriptionInput;

      // Button for saving item definition
      public Button saveAndExitButton;

      #endregion

      private void Awake () {
         self = this;
         hide();

         // Set the category options
         categoryDropdown.options.Clear();
         foreach (ItemDefinition.Category category in Enum.GetValues(typeof(ItemDefinition.Category))) {
            categoryDropdown.options.Add(new Dropdown.OptionData { text = category.ToString() });
         }

         if (MasterToolAccountManager.self != null && !MasterToolAccountManager.canAlterData()) {
            saveAndExitButton.gameObject.SetActive(false);
         }
      }

      public void show () {
         canvasGroup.alpha = 1;
         canvasGroup.interactable = true;
         canvasGroup.blocksRaycasts = true;

         // Check if we are creating a new item or editing an existing one
         if (ItemDefinitionToolManager.selectedItemDefinition.id == -1) {
            titleLabel.text = "New Item Definition";
         } else {
            titleLabel.text = $"{ ItemDefinitionToolManager.selectedItemDefinition.id } - { ItemDefinitionToolManager.selectedItemDefinition.name }";
         }

         setGenericAttributeControlValues(ItemDefinitionToolManager.selectedItemDefinition);
      }

      private void setGenericAttributeControlValues (ItemDefinition model) {
         iconSelector.setValueWithoutNotify(model.iconPath);

         idLabel.text = $"ID - { model.id }";
         creatorIdLabel.text = $"Creator ID - { model.creatorUserId }";

         enabledToggle.SetIsOnWithoutNotify(model.enabled);

         categoryDropdown.SetValueWithoutNotify(categoryDropdown.options.FindIndex(c => c.text.Equals(model.category.ToString())));

         nameInput.SetTextWithoutNotify(model.name);
         descriptionInput.SetTextWithoutNotify(model.description);
      }

      public void saveAndExit () {
         // Apply attributes from the controls
         ItemDefinitionToolManager.selectedItemDefinition.name = nameInput.text;
         ItemDefinitionToolManager.selectedItemDefinition.description = descriptionInput.text;
         ItemDefinitionToolManager.selectedItemDefinition.iconPath = iconSelector.value;
         ItemDefinitionToolManager.selectedItemDefinition.enabled = enabledToggle.isOn;
         ItemDefinitionToolManager.selectedItemDefinition.category =
            (ItemDefinition.Category) Enum.Parse(typeof(ItemDefinition.Category), categoryDropdown.options[categoryDropdown.value].text);

         hide();
         ItemDefinitionToolManager.self.saveSelectedDefinition();
      }

      public void hide () {
         canvasGroup.alpha = 0;
         canvasGroup.interactable = false;
         canvasGroup.blocksRaycasts = false;
      }

      #region Private Variables

      #endregion
   }
}
