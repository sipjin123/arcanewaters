using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using static PvpShopItem;
using static PvpShopPanel;
using UnityEngine.Events;

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

   // The frame indicating the rarity of the item
   public Image rarityFrame;

   // The rarity type
   public Rarity.Type rarityType;

   // If this template is disabled by server
   public GameObject disabledIcon;

   // If this item was selected
   public UnityEvent selectTemplateEvent = new UnityEvent();

   #endregion

   public void setupData (PvpShopItem data) {
      itemData = data;
      itemId = itemData.itemId;
      currencyText.text = itemData.itemCost.ToString();
      shopItemType = itemData.shopItemType;
      rarityType = data.rarityType;

      if (data.isDisabled) {
         disabledIcon.SetActive(true);
      }

      ToolTipComponent toolTip = GetComponentInChildren<ToolTipComponent>();
      rarityFrame.sprite = PvpShopPanel.self.borderSprites[(int) rarityType - 1];
      PvpItemInfo itemInfo = PvpShopPanel.self.getPvpItemInfo(data);

      nameText.text = itemInfo.name;
      itemIcon.sprite = itemInfo.sprite;
      if (toolTip) {
         toolTip.message = itemInfo.description;
      }
   }

   public void selectThisTemplate () {
      PvpItemInfo itemInfo = PvpShopPanel.self.getPvpItemInfo(itemData);
      if (itemInfo != null) {
         PvpShopPanel.self.itemName.text = itemInfo.name;
         PvpShopPanel.self.itemDescription.text = itemInfo.description;
         PvpShopPanel.self.itemIconFrameInfo.sprite = PvpShopPanel.self.borderSprites[(int) rarityType - 1];
         PvpShopPanel.self.itemIconInfo.sprite = itemInfo.sprite;
      }
      selectTemplateEvent.Invoke();
   }

   #region Private Variables

   #endregion
}
