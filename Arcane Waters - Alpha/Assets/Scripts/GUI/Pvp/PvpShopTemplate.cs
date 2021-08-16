﻿using UnityEngine;
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

   // The highlighted object indicator
   public GameObject highlightObj;

   // The selected object indicator
   public GameObject selectedObj;

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

   public void onHoverEnter () {
      highlightObj.SetActive(true);
      displayData();
   }

   public void onHoverExit () {
      highlightObj.SetActive(false);
   }

   public void selectThisTemplate () {
      displayData();
      selectedObj.SetActive(true);
      selectTemplateEvent.Invoke();
   }

   private void displayData () {
      PvpShopPanel.self.clearSelectedObj();
      PvpItemInfo itemInfo = PvpShopPanel.self.getPvpItemInfo(itemData);
      if (itemInfo != null) {
         PvpShopPanel.self.itemName.text = itemInfo.name;
         PvpShopPanel.self.itemDescription.text = itemInfo.description;
         PvpShopPanel.self.itemIconFrameInfo.sprite = PvpShopPanel.self.borderSprites[(int) rarityType - 1];
         PvpShopPanel.self.itemIconInfo.sprite = itemInfo.sprite;

         if (itemData.shopItemType == PvpShopItemType.Ship && itemData.itemData.Contains(EquipmentXMLManager.VALID_XML_FORMAT)) {
            ShipInfo serverDeclaredData = Util.xmlLoad<ShipInfo>(itemData.itemData);
            PvpShopPanel.self.shipAttackText.text = (serverDeclaredData.damage * 100).ToString("f1") + "%";
            PvpShopPanel.self.shipSpeedText.text = serverDeclaredData.speed.ToString();
            PvpShopPanel.self.shipRangeText.text = serverDeclaredData.attackRange.ToString();
            PvpShopPanel.self.shipCargoText.text = serverDeclaredData.cargoMax.ToString();
            PvpShopPanel.self.shipSupplyText.text = serverDeclaredData.supplies.ToString();
            PvpShopPanel.self.shipSailorsText.text = serverDeclaredData.sailors.ToString();
            PvpShopPanel.self.shipDefenseText.text = serverDeclaredData.health.ToString();
         }
      }
   }

   #region Private Variables

   #endregion
}
