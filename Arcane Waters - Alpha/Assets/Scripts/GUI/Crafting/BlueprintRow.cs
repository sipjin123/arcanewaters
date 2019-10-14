using UnityEngine;
using UnityEngine.UI;

public class BlueprintRow : MonoBehaviour
{
   #region Public Variables

   // The icon of the material
   public Image icon;

   // The name of the material
   public Text itemName;

   // The cached data of the item
   public Blueprint itemData;

   // An image to indicate the highlighted item
   public Image selectionIndicator;

   // To send notification that this item was selected
   public Button button;

   #endregion Public Variables

   public void initData (Blueprint item, Sprite newicon) {
      itemData = item;
      itemName.text = item.getName();
      icon.sprite = newicon;
   }

   public void selectItem () {
      selectionIndicator.enabled = true;
   }

   public void deselectItem () {
      selectionIndicator.enabled = false;
   }
}