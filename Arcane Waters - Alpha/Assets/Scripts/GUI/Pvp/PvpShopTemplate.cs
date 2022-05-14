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
   public Text currencyTextEnabled;
   public Text currencyTextDisabled;

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

   // The cost of this item
   public int itemCost;

   #endregion

   public void setupData (PvpShopItem data) {
      itemData = data;
      itemId = itemData.itemId;
      currencyTextEnabled.text = itemData.itemCost.ToString();
      currencyTextDisabled.text = itemData.itemCost.ToString();
      itemCost = itemData.itemCost;
      shopItemType = itemData.shopItemType;
      rarityType = data.rarityType;

      if (data.isDisabled) {
         enableBlocker(true);
      } else {
         enableBlocker(false);
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

   public void enableBlocker (bool isEnabled) {
      disabledIcon.SetActive(isEnabled);
      currencyTextEnabled.gameObject.SetActive(!isEnabled);
      currencyTextDisabled.gameObject.SetActive(isEnabled);
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
            PvpShopPanel.self.shipSupplyText.text = serverDeclaredData.maxFood.ToString();
            PvpShopPanel.self.shipDefenseText.text = serverDeclaredData.health.ToString();

            int index = 0;
            foreach (Text abilityText in PvpShopPanel.self.abilityTexts) {
               if (index < serverDeclaredData.shipAbilities.ShipAbilities.Length) {
                  int abilityIndex = serverDeclaredData.shipAbilities.ShipAbilities[index];
                  ShipAbilityData shipData = ShipAbilityManager.self.getAbility(abilityIndex);
                  if (shipData != null) {
                     abilityText.text = shipData.abilityName;
                  } else {
                     D.debug("Cant find Ship ability for Ability: " + abilityIndex);
                  }
                  index++;
               }
            }

            index = 0;
            foreach (Image abilityIcon in PvpShopPanel.self.abilityIcons) {
               if (index < serverDeclaredData.shipAbilities.ShipAbilities.Length) {
                  int abilityIndex = serverDeclaredData.shipAbilities.ShipAbilities[index];
                  ShipAbilityData shipData = ShipAbilityManager.self.getAbility(abilityIndex);
                  if (shipData != null) {
                     abilityIcon.sprite = ImageManager.getSprite(shipData.skillIconPath);
                     PvpShopPanel.self.abilityTooltipIcons[index].sprite = ImageManager.getSprite(shipData.skillIconPath);
                  } else {
                     D.debug("Cant find Ship ability for Ability: " + abilityIndex);
                  }
                  index++;
               }
            }

            index = 0;
            foreach (Text toolTip in PvpShopPanel.self.toolTipText) {
               if (index < serverDeclaredData.shipAbilities.ShipAbilities.Length) {
                  int abilityIndex = serverDeclaredData.shipAbilities.ShipAbilities[index];
                  ShipAbilityData shipData = ShipAbilityManager.self.getAbility(abilityIndex);
                  if (shipData != null) {
                     toolTip.text = shipData.abilityDescription;
                  } else {
                     D.debug("Cant find Ship ability for Ability: " + abilityIndex);
                  }
                  index++;
               }
            }
         }
      }
   }

   #region Private Variables

   #endregion
}
