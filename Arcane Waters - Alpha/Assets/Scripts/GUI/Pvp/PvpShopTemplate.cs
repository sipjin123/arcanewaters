using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using static PvpShopItem;

public class PvpShopTemplate : MonoBehaviour {
   #region Public Variables

   // Id of the item
   public int itemId;

   // Name of the item
   public Text nameText;

   // Cost of the item
   public Text currencyText;

   // Icon of the item
   public Image itemIcon;

   // The type of shop item
   public PvpShopItemType shopItemType;

   // The data cache of the item
   public PvpShopItem itemData;

   // Buy Button
   public Button buyButton;

   #endregion

   public void setupData (PvpShopItem data) {
      itemData = data;
      itemId = itemData.itemId;
      nameText.text = itemData.itemName;
      currencyText.text = itemData.itemCost.ToString();
      itemIcon.sprite = ImageManager.getSprite(itemData.spritePath);
      shopItemType = itemData.shopItemType;
   }

   #region Private Variables

   #endregion
}
