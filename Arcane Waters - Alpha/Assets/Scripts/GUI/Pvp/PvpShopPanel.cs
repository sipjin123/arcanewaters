using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using static PvpShopItem;
using UnityEngine.EventSystems;
using MapCreationTool;

public class PvpShopPanel : ClientMonoBehaviour, IPointerClickHandler {
   #region Public Variables

   // Prefab spawning components
   public PvpShopTemplate shopTemplatePrefab, shipTemplatePrefab;
   public Transform shopTemplateHolder, shipTemplateHolder;
   public Transform shopTemplateScroller, shipTemplateScroller;

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
   public RectTransform shipCategoryObj, powerupCategoryObj, abilityCategoryObj, itemCategoryObj;
   public RectTransform shipCategoryObjHover, powerupCategoryObjHover, abilityCategoryObjHover, itemCategoryObjHover;
   public RectTransform shipCategoryObjSelect, powerupCategoryObjSelect, abilityCategoryObjSelect, itemCategoryObjSelect;

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

   // The current category
   public PvpShopItemType selectedCategory;

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
      itemCategoryObj.GetComponent<Button>().onClick.AddListener(() => {
         onSelectItemCategory();
      });

      // Setup the hover functionalities
      EventTrigger shipEventTrigger = shipCategoryObj.GetComponent<EventTrigger>();
      setupHovering(shipEventTrigger, PvpShopItemType.Ship);

      EventTrigger abilityEventTrigger = abilityCategoryObj.GetComponent<EventTrigger>();
      setupHovering(abilityEventTrigger, PvpShopItemType.Ability);

      EventTrigger powerupEventTrigger = powerupCategoryObj.GetComponent<EventTrigger>();
      setupHovering(powerupEventTrigger, PvpShopItemType.Powerup);

      EventTrigger itemEventTrigger = itemCategoryObj.GetComponent<EventTrigger>();
      setupHovering(itemEventTrigger, PvpShopItemType.Item);

      shipAttackText.text = "";
      shipSpeedText.text = "";
      shipRangeText.text = "";
      shipCargoText.text = "";
      shipSupplyText.text = "";
      shipSailorsText.text = "";
      shipDefenseText.text = "";
   }

   private void setupHovering (EventTrigger eventTrigger, PvpShopItemType pvpShopItem) {
      Utilities.addPointerListener(eventTrigger, EventTriggerType.PointerEnter, (e) => {
         hoverPvpItemCategory(pvpShopItem);
      });
      Utilities.addPointerListener(eventTrigger, EventTriggerType.PointerExit, (e) => {
         hoverPvpItemCategory(PvpShopItemType.None);
      });
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

   private void clearInfoPanel () {
      itemName.text = "";
      itemDescription.text = "";
      itemIconFrameInfo.sprite = ImageManager.self.blankSprite;
      itemIconInfo.sprite = ImageManager.self.blankSprite;
   }

   public void populateShop (List<PvpShopItem> pvpItemDataList) {
      clearInfoPanel();
      loadingPanel.SetActive(false);
      shopTemplateHolder.gameObject.DestroyChildren();
      shipTemplateHolder.gameObject.DestroyChildren();

      // Enable stat panel if shop list has ship item
      if (selectedCategory == PvpShopItemType.Ship) {
         shopTemplateScroller.gameObject.SetActive(false);
         shipTemplateScroller.gameObject.SetActive(true);
         shipStatTab.SetActive(true);
      } else {
         shopTemplateScroller.gameObject.SetActive(true);
         shipTemplateScroller.gameObject.SetActive(false);
         shipStatTab.SetActive(false);
      }

      foreach (PvpShopItem shopItemData in pvpItemDataList) {
         PvpShopTemplate shopTemplate = selectedCategory == PvpShopItemType.Ship ? 
            Instantiate(shipTemplatePrefab, shipTemplateHolder) : Instantiate(shopTemplatePrefab, shopTemplateHolder);
         shopTemplate.setupData(shopItemData);
         shopTemplate.selectTemplateEvent.AddListener(() => {
            if (Global.player != null && Global.player is PlayerShipEntity && shopTemplate.buyButton.IsInteractable()) {
               PlayerShipEntity playerShip = (PlayerShipEntity) Global.player;
               playerShip.rpc.Cmd_BuyPvpItem(shopItemData.itemId, shopId, (int) shopItemData.shopItemType);
               loadingPanel.SetActive(true);
               shopTemplate.selectedObj.SetActive(true);
            }
         });

         if (userSilver < shopItemData.itemCost) {
            shopTemplate.buyButton.interactable = false;
            shopTemplate.disabledIcon.SetActive(true);
         }
         if (shopItemData.isDisabled) {
            shopTemplate.buyButton.interactable = false;
            shopTemplate.disabledIcon.SetActive(true);
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
            case PvpShopItemType.Item:
               switch ((PvpConsumableItem) itemData.itemId) {
                  case PvpConsumableItem.RepairTool:
                     newItemInfo.name = "Repair Tool";
                     newItemInfo.description = "Repairs your ship for 100 hp";
                     newItemInfo.sprite = ImageManager.getSprite(itemData.spritePath);
                     break;
                  default:
                     newItemInfo.name = "Unsupported";
                     newItemInfo.description = "";
                     break;
               }
               break;
         }
      }

      return newItemInfo;
   }

   public void onShopButtonPressed () {
      onSelectShipCategory();
   }

   public void onSelectShipCategory () {
      Global.player.rpc.Cmd_RequestPvpShopData(shopId, (int) PvpShopItemType.Ship);
      loadingPanel.SetActive(true);
      selectedCategory = PvpShopItemType.Ship;
      selectPvpItemCategory(selectedCategory);
   }

   public void onSelectAbilityCategory () {
      Global.player.rpc.Cmd_RequestPvpShopData(shopId, (int) PvpShopItemType.Ability);
      loadingPanel.SetActive(true);
      selectedCategory = PvpShopItemType.Ability;
      selectPvpItemCategory(selectedCategory);
   }

   public void onSelectItemCategory () {
      Global.player.rpc.Cmd_RequestPvpShopData(shopId, (int) PvpShopItemType.Item);
      loadingPanel.SetActive(true);
      selectedCategory = PvpShopItemType.Item;
      selectPvpItemCategory(selectedCategory);
   }

   public void onSelectPowerupCategory () {
      Global.player.rpc.Cmd_RequestPvpShopData(shopId, (int) PvpShopItemType.Powerup);
      loadingPanel.SetActive(true);
      selectedCategory = PvpShopItemType.Powerup;
      selectPvpItemCategory(selectedCategory);
   }

   private void hoverPvpItemCategory (PvpShopItemType itemType) {
      powerupCategoryObjHover.gameObject.SetActive(false);
      shipCategoryObjHover.gameObject.SetActive(false);
      abilityCategoryObjHover.gameObject.SetActive(false);
      itemCategoryObjHover.gameObject.SetActive(false);

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
         case PvpShopItemType.Item:
            itemCategoryObjHover.gameObject.SetActive(true);
            break;
      }
   }

   private void selectPvpItemCategory (PvpShopItemType itemType) {
      powerupCategoryObjSelect.gameObject.SetActive(false);
      shipCategoryObjSelect.gameObject.SetActive(false);
      abilityCategoryObjSelect.gameObject.SetActive(false); 
      itemCategoryObjSelect.gameObject.SetActive(false);

      switch (itemType) {
         case PvpShopItemType.Ability:
            abilityCategoryObjSelect.gameObject.SetActive(true);
            break;
         case PvpShopItemType.Ship:
            shipCategoryObjSelect.gameObject.SetActive(true);
            break;
         case PvpShopItemType.Powerup:
            powerupCategoryObjSelect.gameObject.SetActive(true);
            break;
         case PvpShopItemType.Item:
            itemCategoryObjSelect.gameObject.SetActive(true);
            break;
      }
   }

   public virtual void OnPointerClick (PointerEventData eventData) {
      // If the black background outside is clicked, hide the panel
      if (eventData.rawPointerPress == entirePanel.gameObject) {
         hideEntirePanel();
      }
   }

   #region Private Variables

   #endregion
}
