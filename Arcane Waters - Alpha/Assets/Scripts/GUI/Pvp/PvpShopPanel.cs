using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using static PvpShopItem;
using UnityEngine.EventSystems;
using MapCreationTool;

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

   // The category icons on the left side of the panel
   public RectTransform shipCategoryObj, powerupCategoryObj, abilityCategoryObj;
   public RectTransform shipCategoryObjHover, powerupCategoryObjHover, abilityCategoryObjHover;

   // The coordinates of the category icons depending on their button state
   public const int IDLE_POS = -55;
   public const int HOVER_POS = -75;

   // Reference to the horizontal category x axis
   public RectTransform horizontalCategoryReference;

   // List of pvp ship icons and their sprite reference which is unique to this panel
   public List<PvpShipIconPair> pvpShipIconList = new List<PvpShipIconPair>();

   // The ship stats
   public Text shipAttackText, shipSpeedText, shipRangeText, shipDefenseText, shipCargoText, shipSupplyText, shipSailorsText;

   // The stat tab of the ship
   public GameObject shipStatTab;

   public class PvpItemInfo {
      public string name, description;
      public Sprite sprite;
   }

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
      borderSprites = Resources.LoadAll<Sprite>(Powerup.BORDER_SPRITES_LOCATION);

      // Setup the button click functionalities
      shipCategoryObj.GetComponent<Button>().onClick.AddListener(() => {
         onSelectShipCategory();
      });
      abilityCategoryObj.GetComponent<Button>().onClick.AddListener(() => {
         onSelectAbilityCategory();
      });
      powerupCategoryObj.GetComponent<Button>().onClick.AddListener(() => {
         onSelectPowerupCategory();
      });

      // Setup the hover functionalities
      EventTrigger shipEventTrigger = shipCategoryObj.GetComponent<EventTrigger>();
      Utilities.addPointerListener(shipEventTrigger, EventTriggerType.PointerEnter, (e) => {
         hoverPvpItemCategory(PvpShopItemType.Ship);
      });
      Utilities.addPointerListener(shipEventTrigger, EventTriggerType.PointerExit, (e) => {
         hoverPvpItemCategory(PvpShopItemType.None);
      });

      EventTrigger abilityEventTrigger = abilityCategoryObj.GetComponent<EventTrigger>();
      Utilities.addPointerListener(abilityEventTrigger, EventTriggerType.PointerEnter, (e) => {
         hoverPvpItemCategory(PvpShopItemType.Ability);
      });
      Utilities.addPointerListener(abilityEventTrigger, EventTriggerType.PointerExit, (e) => {
         hoverPvpItemCategory(PvpShopItemType.None);
      });

      EventTrigger powerupEventTrigger = powerupCategoryObj.GetComponent<EventTrigger>();
      Utilities.addPointerListener(powerupEventTrigger, EventTriggerType.PointerEnter, (e) => {
         hoverPvpItemCategory(PvpShopItemType.Powerup);
      });
      Utilities.addPointerListener(powerupEventTrigger, EventTriggerType.PointerExit, (e) => {
         hoverPvpItemCategory(PvpShopItemType.None);
      });

      shipAttackText.text = "";
      shipSpeedText.text = "";
      shipRangeText.text = "";
      shipCargoText.text = "";
      shipSupplyText.text = "";
      shipSailorsText.text = "";
      shipDefenseText.text = "";
   }

   public void clearSelectedObj () {
      foreach (Transform child in shopTemplateHolder.transform) {
         child.GetComponent<PvpShopTemplate>().selectedObj.SetActive(false);
      }
   }

   public void enableShopButton (bool isActive) {
      shopButton.SetActive(isActive);
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
      loadingPanel.SetActive(false);
      shopTemplateHolder.gameObject.DestroyChildren();

      // Enable stat panel if shop list has ship item
      if (pvpItemDataList.FindAll(_ => _.shopItemType == PvpShopItemType.Ship).Count > 0) {
         shipStatTab.SetActive(true);
      } else {
         shipStatTab.SetActive(false);
      }

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
                  PvpShipIconPair shipSpritePair = pvpShipIconList.Find(_ => _.shipType == shipData.shipType);
                  if (shipSpritePair != null) {
                     newItemInfo.sprite = shipSpritePair.shipSprite;
                  }
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

   public void onShopButtonPressed () {
      onSelectPowerupCategory();
   }

   public void onSelectShipCategory () {
      Global.player.rpc.Cmd_RequestPvpShopData(shopId, (int) PvpShopItemType.Ship);
      loadingPanel.SetActive(true);
      hoverPvpItemCategory(PvpShopItemType.Ship);
   }

   public void onSelectAbilityCategory () {
      Global.player.rpc.Cmd_RequestPvpShopData(shopId, (int) PvpShopItemType.Ability);
      loadingPanel.SetActive(true);
      hoverPvpItemCategory(PvpShopItemType.Ability);
   }

   public void onSelectPowerupCategory () {
      Global.player.rpc.Cmd_RequestPvpShopData(shopId, (int) PvpShopItemType.Powerup);
      loadingPanel.SetActive(true);
      hoverPvpItemCategory(PvpShopItemType.Powerup);
   }

   private void hoverPvpItemCategory (PvpShopItemType itemType) {
      powerupCategoryObjHover.gameObject.SetActive(false);
      shipCategoryObjHover.gameObject.SetActive(false);
      abilityCategoryObjHover.gameObject.SetActive(false);

      switch (itemType) {
         case PvpShopItemType.Ability:
            abilityCategoryObjHover.gameObject.SetActive(true);
            break;
         case PvpShopItemType.Ship:
            shipCategoryObjHover.gameObject.SetActive(true);
            break;
         case PvpShopItemType.Powerup:
            powerupCategoryObjHover.gameObject.SetActive(true);
            break;
      }
   }

   #region Private Variables

   #endregion
}
