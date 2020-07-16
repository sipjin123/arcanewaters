using UnityEngine;
using UnityEngine.UI;

namespace ItemDefinitionTool
{
   public class ItemDefinitionListEntry : MonoBehaviour
   {
      #region Public Variables

      // Toggle which shows if item definition is enabled
      public Toggle enabledToggle;

      // Image which shows the icon of the item definition
      public Image iconImage;

      // Label at the top of the entry for ID and category
      public Text topLabel;

      // Label at the bottom of the entry for the name
      public Text bottomLabel;

      // Buttons for various actions with the item definition
      public Button duplicateButton;
      public Button editButton;
      public Button deleteButton;

      #endregion

      public void set (ItemDefinition definition) {
         enabledToggle.isOn = definition.enabled;

         Sprite sprite = ImageManager.getSprite(definition.iconPath);
         if (sprite == null) {
            sprite = ImageManager.self.blankSprite;
         }
         iconImage.sprite = sprite;

         topLabel.text = $"ID: { definition.id }, Category: { definition.category }";
         bottomLabel.text = definition.name;

         duplicateButton.onClick.AddListener(() => ItemDefinitionToolManager.self.duplicateDefinition(definition.id));
         editButton.onClick.AddListener(() => ItemDefinitionToolManager.self.editDefinition(definition.id));
         deleteButton.onClick.AddListener(() => ItemDefinitionToolManager.self.deleteDefinition(definition.id));
      }

      #region Private Variables

      #endregion
   }
}
