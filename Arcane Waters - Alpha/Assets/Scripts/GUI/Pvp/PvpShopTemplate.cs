using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using static PvpShopData;

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
   public PvpShopData shopData;

   // Buy Button
   public Button buyButton;

   #endregion

   public void setupData (PvpShopData data) {
      shopData = data;
      itemId = data.itemId;
      nameText.text = shopData.itemName;
      currencyText.text = shopData.itemCost.ToString();
      itemIcon.sprite = ImageManager.getSprite(shopData.spritePath);
      shopItemType = shopData.shopItemType;
   }

   #region Private Variables

   #endregion
}
