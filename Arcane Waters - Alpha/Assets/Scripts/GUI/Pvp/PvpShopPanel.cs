using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using static PvpShopItem;

public class PvpShopPanel : ClientMonoBehaviour {
   #region Public Variables

   // Prefab spawning components
   public PvpShopTemplate shopTemplatePrefab;
   public Transform shopTemplateHolder;

   // Text ui to be shown in the panel
   public TextMeshProUGUI displayName;
   public TextMeshProUGUI description;

   // Self
   public static PvpShopPanel self;

   // The current silver of the user
   public int userSilver;

   // Text displaying the users silver
   public TextMeshProUGUI userSilverText;

   // The loading blocker
   public GameObject loadingPanel;

   // The popup results
   public GameObject popUpResult;
   public Text popUpText;
   public Image popUpIcon;

   // The shop id
   public int shopId;

   // Shows the entire panel
   public GameObject entirePanel;

   // The shop button
   public GameObject shopButton;

   // The name of the item
   public TextMeshProUGUI itemName;

   // The description of the item
   public TextMeshProUGUI itemDescription;

   // The icon of the item when selected
   public Image itemIconInfo;

   // The icon of the item when selected
   public Image itemIconFrameInfo;

   // The border sprites of the rarity
   public Sprite[] borderSprites;

   public class PvpItemInfo {
      public string name, description;
      public Sprite sprite;
   }

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
      borderSprites = Resources.LoadAll<Sprite>(Powerup.BORDER_SPRITES_LOCATION);
   }

   public void enableShopButton (bool isActive) {
      shopButton.SetActive(isActive);
   }

   public void onShopButtonPressed () {
      Global.player.rpc.Cmd_RequestPvpShopData(shopId);
   }

   public void showEntirePanel () {
      popUpResult.SetActive(false);
      loadingPanel.SetActive(false);
      entirePanel.SetActive(true);
   }

   public void hideEntirePanel () {
      entirePanel.SetActive(false);
   }

   public void populateShop (List<PvpShopItem> pvpItemDataList) {
      shopTemplateHolder.gameObject.DestroyChildren();

      foreach (PvpShopItem shopItemData in pvpItemDataList) {
         PvpShopTemplate shopTemplate = Instantiate(shopTemplatePrefab, shopTemplateHolder);
         shopTemplate.setupData(shopItemData);
         shopTemplate.buyButton.onClick.AddListener(() => {
            if (Global.player != null && Global.player is PlayerShipEntity) {
               PlayerShipEntity playerShip = (PlayerShipEntity) Global.player;
               playerShip.rpc.Cmd_BuyPvpItem(shopItemData.itemId, shopId);
               loadingPanel.SetActive(true);
            }
         });
         if (userSilver < shopItemData.itemCost) {
            shopTemplate.buyButton.interactable = false;
         }
         if (shopItemData.isDisabled) {
            shopTemplate.buyButton.interactable = false;
         }
      }
   }

   public void receivePurchaseResult (PvpShopItem shopItem) {
      popUpResult.SetActive(true);
      PvpItemInfo itemInfo = getPvpItemInfo(shopItem);
      popUpIcon.sprite = itemInfo.sprite;
      popUpText.text = itemInfo.name;
   }

   public void closePopup () {
      popUpResult.SetActive(false);
      loadingPanel.SetActive(false);
   }

   public PvpItemInfo getPvpItemInfo (PvpShopItem itemData) {
      PvpItemInfo newItemInfo = new PvpItemInfo();
      if (itemData.itemId > 0) {
         switch (itemData.shopItemType) {
            case PvpShopItemType.Powerup:
               PowerupData powerupData = PowerupManager.self.getPowerupData((Powerup.Type) itemData.itemId);
               if (powerupData != null) {
                  newItemInfo.sprite = powerupData.spriteIcon;
                  newItemInfo.name = powerupData.powerupName;
                  newItemInfo.description = powerupData.description;
               }
               break;
            case PvpShopItemType.Ship:
               ShipData shipData = ShipDataManager.self.getShipData(itemData.itemId);
               if (shipData != null) {
                  newItemInfo.sprite = ImageManager.getSprite(shipData.spritePath);
                  newItemInfo.name = shipData.shipName;
                  newItemInfo.description = shipData.shipName;
               }
               break;
            case PvpShopItemType.Ability:
               ShipAbilityData shipAbilityData = ShipAbilityManager.self.getAbility(itemData.itemId);
               if (shipAbilityData != null) {
                  newItemInfo.sprite = ImageManager.getSprite(shipAbilityData.skillIconPath);
                  newItemInfo.name = shipAbilityData.abilityName;
                  newItemInfo.description = shipAbilityData.abilityDescription;
               }
               break;
            case PvpShopItemType.Stats:
               break;
         }
      }

      return newItemInfo;
   }

   #region Private Variables

   #endregion
}
