using UnityEngine;
using UnityEngine.UI;

public class CraftingMaterialRow : MonoBehaviour
{
   #region Public Variables

   public Image icon;
   public Text itemName;

   public Item itemData;
   public Item ItemData { get { return itemData; } }

   public Image selectionIndicator;
   public Button button;
   public Button Button { get { return button; } }

   #endregion Public Variables

   public void InitData (Item item) {
      itemData = item;
      itemName.text = item.getName();
      icon.sprite = ImageManager.getSprite(item.getIconPath());
   }

   public void SelectItem () {
      selectionIndicator.enabled = true;
   }

   public void DeselectItem () {
      selectionIndicator.enabled = false;
   }
}