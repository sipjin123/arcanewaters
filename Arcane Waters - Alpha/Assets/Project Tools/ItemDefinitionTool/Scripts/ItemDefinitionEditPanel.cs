using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using System.Linq;

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

         // Gather all type specific attribute sections
         _typeSpecificAttributes = GetComponentsInChildren<TypeSpecificAttributes>(true);
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

         // Update which attribute sections should be shown
         updateShownAttributeSections();

         // For every shown attribute section, set values control values
         foreach (TypeSpecificAttributes section in _typeSpecificAttributes) {
            if (section.gameObject.activeSelf) {
               section.setValuesWithoutNotify(ItemDefinitionToolManager.selectedItemDefinition);
            }
         }
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

      public void categoryDropdownChanged () {
         ItemDefinition.Category newCat = (ItemDefinition.Category) Enum.Parse(typeof(ItemDefinition.Category), categoryDropdown.options[categoryDropdown.value].text);

         // If category is the same, do nothing
         if (ItemDefinitionToolManager.selectedItemDefinition.category == newCat) return;

         // Create item definition, use class based on category
         int id = ItemDefinitionToolManager.selectedItemDefinition.id;
         int userId = ItemDefinitionToolManager.selectedItemDefinition.creatorUserId;

         ItemDefinitionToolManager.selectedItemDefinition = ItemDefinition.create(newCat);
         ItemDefinitionToolManager.selectedItemDefinition.id = id;
         ItemDefinitionToolManager.selectedItemDefinition.creatorUserId = userId;
         applyBaseValues(ItemDefinitionToolManager.selectedItemDefinition);

         updateShownAttributeSections();

         // For every shown attribute section, set values control values
         foreach (TypeSpecificAttributes section in _typeSpecificAttributes) {
            if (section.gameObject.activeSelf) {
               section.setValuesWithoutNotify(ItemDefinitionToolManager.selectedItemDefinition);
            }
         }
      }

      private void updateShownAttributeSections () {
         HashSet<Type> targetTypes = new HashSet<Type>(getAllBaseTypes(ItemDefinitionToolManager.selectedItemDefinition.GetType()));
         foreach (TypeSpecificAttributes section in _typeSpecificAttributes) {
            section.gameObject.SetActive(targetTypes.Contains(section.targetType));
         }
      }

      public void saveAndExit () {
         // Apply attributes from the controls
         applyBaseValues(ItemDefinitionToolManager.selectedItemDefinition);

         // Apply type specific attributes
         foreach (TypeSpecificAttributes attributes in _typeSpecificAttributes) {
            if (attributes.gameObject.activeSelf) {
               attributes.applyAttributeValues(ItemDefinitionToolManager.selectedItemDefinition);
            }
         }

         hide();
         ItemDefinitionToolManager.self.saveSelectedDefinition();
      }

      private void applyBaseValues (ItemDefinition target) {
         target.name = nameInput.text;
         target.description = descriptionInput.text;
         target.iconPath = iconSelector.value;
         target.enabled = enabledToggle.isOn;
         target.category = (ItemDefinition.Category) Enum.Parse(typeof(ItemDefinition.Category), categoryDropdown.options[categoryDropdown.value].text);
      }

      public void hide () {
         canvasGroup.alpha = 0;
         canvasGroup.interactable = false;
         canvasGroup.blocksRaycasts = false;
      }

      private IEnumerable<Type> getAllBaseTypes (Type type) {
         // Recursively get all of all parent types
         while (type != null) {
            yield return type;
            type = type.BaseType;
         }
      }

      #region Private Variables

      // Attribute sections that target specific item definition types
      private TypeSpecificAttributes[] _typeSpecificAttributes;

      #endregion
   }
}
