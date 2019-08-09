using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AdventureItemRow : MonoBehaviour {
   #region Public Variables

   // The icon
   public Image iconImage;

   // The text
   public Text itemName;

   // The number available in stock
   public Text stockCountText;

   // The gold amount
   public Text goldAmount;

   // The Button
   public Button buyButton;

   // The Item associated with this row
   public Item item;

   // The tooltip on the image
   public Tooltipped tooltip;
      
   #endregion

   public void setRowForItem (Item item) {
      this.item = item;

      string path = item.getIconPath();
      iconImage.sprite = ImageManager.getSprite(path);
      itemName.text = item.getName();
      itemName.color = Rarity.getColor(item.getRarity());
      stockCountText.text = Item.getColoredStockCount(item.count);
      goldAmount.text = item.getSellPrice() + "";

      // Recolor
      ColorKey colorKey = item.getColorKey();
      iconImage.GetComponent<RecoloredSprite>().recolor(colorKey, item.color1, item.color2);

      // Sets the tooltip when hovering the image
      tooltip.text = item.getTooltip();

      // Associate a new function with the confirmation button
      buyButton.onClick.RemoveAllListeners();
      buyButton.onClick.AddListener(() => AdventureShopScreen.self.buyButtonPressed(item.id));
   }

   #region Private Variables
      
   #endregion
}
