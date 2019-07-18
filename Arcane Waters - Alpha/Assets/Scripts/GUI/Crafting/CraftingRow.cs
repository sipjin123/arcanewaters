using UnityEngine;
using UnityEngine.UI;

public class CraftingRow : MonoBehaviour
{
   #region Public Variables
   // Cached Item Data
   public Item item;
   public Button button;

   // Icon of the current item
   public Image icon;
   // Highlight indicator if item is selected
   public Image selectionIcon;

   // Name of the item
   public Text nameText;

   // Flag to check if this template is empty
   public bool hasData;

   // Sprite to be setup when there is no data
   public Sprite emptySprite;
   #endregion

   public void injectItem (Item itemvar) {
      hasData = true;
      item = itemvar;
      nameText.text = item.getName();
      icon.sprite = ImageManager.getSprite(item.getIconPath());
   }

   public void purgeData () {
      hasData = false;
      item = null;
      nameText.text = "";
      icon.sprite = emptySprite;
   }

   public void selectItem () {
      selectionIcon.enabled = true;
   }

   public void unselectItem () {
      selectionIcon.enabled = false;
   }
}