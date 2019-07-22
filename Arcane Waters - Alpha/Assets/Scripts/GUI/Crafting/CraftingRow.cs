using UnityEngine;
using UnityEngine.UI;

public class CraftingRow : MonoBehaviour
{
   #region Public Variables

   // Cached Item Data
   public Item item;
   public Button button;

   // To determine if ingredients are enough
   public Image checkIcon;

   // Icon of the current item
   public Image icon;

   // Highlight indicator if item is selected
   public Image selectionIcon;

   // Name of the item
   public Text nameText;

   // Quantity of the item
   public Text quantityText;

   // Quantity of the items the player has to craft the item
   public Text requirementText;

   // Flag to check if this template is empty
   public bool hasData;

   // Sprite to be setup when there is no data
   public Sprite emptySprite;

   #endregion

   public void injectItem (Item itemvar, int playerIngredientQuantity ,int quantity, bool isEnough) {
      hasData = true;
      item = itemvar;
      icon.sprite = ImageManager.getSprite(item.getIconPath());

      nameText.text = item.getName();
      quantityText.text = quantity.ToString();
      requirementText.text = playerIngredientQuantity + "/" + quantity;

      requirementText.enabled = !isEnough;
      checkIcon.enabled = isEnough;
   }

   public void purgeData () {
      hasData = false;
      item = null;
      icon.sprite = emptySprite;

      quantityText.text = "";
      nameText.text = "";
      requirementText.text = "";

      requirementText.enabled = false;
      checkIcon.enabled = false;
   }

   public void selectItem () {
      selectionIcon.enabled = true;
   }

   public void unselectItem () {
      selectionIcon.enabled = false;
   }
}