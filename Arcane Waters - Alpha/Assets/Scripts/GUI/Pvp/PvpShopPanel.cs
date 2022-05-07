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

   // The text display of the shop
   public GameObject seaShopLabel, landShopLabel;

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

   // The ship ability display
   public GameObject shipAbilityPanel;

   // The ability templates that display the info
   public Image[] abilityIcons;
   public Image[] abilityTooltipIcons;
   public Text[] abilityTexts;
   public Text[] toolTipText;
   public GameObject[] abilityHoverable;
   public GameObject[] abilityDetailHolder;
   
   // The icon of the item when selected
   public Image itemIconFrameInfo;

   // The border sprites of the rarity
   public Sprite[] borderSprites;

   // The category icons on the left side of the panel
   public RectTransform shipCategoryObj, powerupCategoryObj, abilityCategoryObj, itemCategoryObj, landPowerupCategoryObj;
   public RectTransform shipCategoryObjHover, powerupCategoryObjHover, abilityCategoryObjHover, itemCategoryObjHover, landPowerupCategoryObjHover;
   public RectTransform shipCategoryObjSelect, powerupCategoryObjSelect, abilityCategoryObjSelect, itemCategoryObjSelect, landPowerupCategoryObjSelect;

   // The coordinates of the category icons depending on their button state
   public const int IDLE_POS = -55;
   public const int HOVER_POS = -75;

   // Reference to the horizontal category x axis
   public RectTransform horizontalCategoryReference;

   // List of pvp ship icons and their sprite reference which is unique to this panel
   public List<PvpShipIconPair> pvpShipIconList = new List<PvpShipIconPair>();
   public List<PvpShipIconPair> pvpShipIconDisabledList = new List<PvpShipIconPair>();

   // The ship stats
   public Text shipAttackText, shipSpeedText, shipRangeText, shipDefenseText, shipCargoText, shipSupplyText, shipSailorsText;

   // The stat tab of the ship
   public GameObject shipStatTab;

   // The current category
   public PvpShopItemType selectedCategory;

   // If the shop is for sea
   public bool isSeaShop;

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
      landPowerupCategoryObj.GetComponent<Button>().onClick.AddListener(() => {
         onSelectLandCategory();
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

      EventTrigger landPowerupEventTrigger = landPowerupCategoryObj.GetComponent<EventTrigger>();
      setupHovering(landPowerupEventTrigger, PvpShopItemType.LandPowerup);

      foreach (GameObject newHoverableObj in abilityDetailHolder) {
         newHoverableObj.SetActive(false);
      }
      int index = 0;
      foreach (GameObject hoverableObj in abilityHoverable) {
         EventTrigger eventTrigger = hoverableObj.GetComponent<EventTrigger>();
         Utilities.addPointerListener(eventTrigger, EventTriggerType.PointerEnter, (e) => selectIndex(hoverableObj));
         Utilities.addPointerListener(eventTrigger, EventTriggerType.PointerExit, (e) => selectIndex(null));
         index++;
      }
   }

   private void selectIndex (GameObject newObj) {
      foreach (GameObject newHoverableObj in abilityDetailHolder) {
         newHoverableObj.SetActive(false);
      }

      if (newObj != null) {
         int currentIndex = newObj.GetComponent<PvpShopAbility>().abilityIndex;
         if (currentIndex < abilityDetailHolder.Length) {
            abilityDetailHolder[currentIndex].SetActive(true);
         }
      }
   }

   private void Start () {
      // Update shop panel templates to remove gray out if silver is sufficient
      PvpStatusPanel.self.silverAddedEvent.AddListener(_ => updatedShopTemplates(_));

      clearDisplay();
   }

   public void clearDisplay () {
      if (Util.isBatch()) {
         return;
      }
      shipAttackText.text = "";
      shipSpeedText.text = "";
      shipRangeText.text = "";
      shipCargoText.text = "";
      shipSupplyText.text = "";
      shipSailorsText.text = "";
      shipDefenseText.text = "";

      foreach (Text abilityText in abilityTexts) {
         if (abilityText != null) {
            abilityText.text = "";
         }
      }
      foreach (Image abilityIcon in abilityIcons) {
         if (abilityIcon != null) {
            abilityIcon.sprite = ImageManager.self.blankSprite;
         }
      }
   }

   public void updatedShopTemplates (int silverValue) {
      bool isShipDisplaying = shipTemplateHolder.childCount > 0;
      Transform parentHolder = isShipDisplaying ? shipTemplateHolder : shopTemplateHolder;
      foreach (Transform child in parentHolder) {
         PvpShopTemplate shopTemplate = child.GetComponent<PvpShopTemplate>();
         bool canAffordItem = silverValue >= shopTemplate.itemCost;
         if (shopTemplate.shopItemType != PvpShopItemType.LandPowerup) {
            if (canAffordItem) {
               shopTemplate.enableBlocker(false);
               shopTemplate.buyButton.interactable = true;
            } else {
               shopTemplate.enableBlocker(true);
               shopTemplate.buyButton.interactable = false;
            }
         }

         if (isShipDisplaying) {
            shopTemplate.itemData.isDisabled = !canAffordItem;
            PvpItemInfo itemInfo = getPvpItemInfo(shopTemplate.itemData);
            if (itemInfo != null) {
               shopTemplate.itemIcon.sprite = itemInfo.sprite;
            }
            shopTemplate.buyButton.GetComponent<Image>().enabled = canAffordItem;
         }
      }
      userSilverText.text = silverValue.ToString();
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
      InputManager.self.inputMaster?.UIControl.Enable();
      popUpResult.SetActive(false);
      loadingPanel.SetActive(false);
      entirePanel.SetActive(true);
   }

   public void hideEntirePanel () {
      InputManager.self.inputMaster?.UIControl.Disable();
      entirePanel.SetActive(false);
   }

   private void clearInfoPanel () {
      itemName.text = "";
      itemDescription.text = "";
      itemIconFrameInfo.sprite = ImageManager.self.blankSprite;
      itemIconInfo.sprite = ImageManager.self.blankSprite;
   }

   public void populateShop (List<PvpShopItem> pvpItemDataList, List<PvpShopItemType> shopItemTypes) {
      clearInfoPanel();
      loadingPanel.SetActive(false);
      shopTemplateHolder.gameObject.DestroyChildren();
      shipTemplateHolder.gameObject.DestroyChildren();

      // Override selected category 
      if (pvpItemDataList.Count > 0) {
         selectedCategory = pvpItemDataList[0].shopItemType;
         selectPvpItemCategory(selectedCategory);
      }

      // Enable stat panel if shop list has ship item
      if (selectedCategory == PvpShopItemType.Ship) {
         shopTemplateScroller.gameObject.SetActive(false);
         shipTemplateScroller.gameObject.SetActive(true);
         shipStatTab.SetActive(true);
         shipAbilityPanel.SetActive(true);
      } else {
         shopTemplateScroller.gameObject.SetActive(true);
         shipTemplateScroller.gameObject.SetActive(false);
         shipStatTab.SetActive(false);
         shipAbilityPanel.SetActive(false);
      }

      // Disable categories that dont exist in this shop
      shipCategoryObj.parent.gameObject.SetActive(shopItemTypes.Exists(_ => _ == PvpShopItemType.Ship));
      powerupCategoryObj.parent.gameObject.SetActive(shopItemTypes.Exists(_ => _ == PvpShopItemType.Powerup));
      abilityCategoryObj.parent.gameObject.SetActive(shopItemTypes.Exists(_ => _ == PvpShopItemType.Ability));
      itemCategoryObj.parent.gameObject.SetActive(shopItemTypes.Exists(_ => _ == PvpShopItemType.Item));
      landPowerupCategoryObj.parent.gameObject.SetActive(shopItemTypes.Exists(_ => _ == PvpShopItemType.LandPowerup));

      foreach (PvpShopItem shopItemData in pvpItemDataList) {
         PvpShopTemplate shopTemplate = selectedCategory == PvpShopItemType.Ship ? 
            Instantiate(shipTemplatePrefab, shipTemplateHolder) : Instantiate(shopTemplatePrefab, shopTemplateHolder);
         shopTemplate.setupData(shopItemData);
         shopTemplate.selectTemplateEvent.AddListener(() => {
            if (Global.player != null && shopTemplate.buyButton.IsInteractable() && shopTemplate.itemCost <= userSilver) {
               Global.player.rpc.Cmd_BuyPvpItem(shopItemData.itemId, shopId, (int) shopItemData.shopItemType);
               loadingPanel.SetActive(true);
               shopTemplate.selectedObj.SetActive(true);
            }
         });

         if (userSilver < shopItemData.itemCost) {
            shopItemData.isDisabled = true;
            shopTemplate.buyButton.GetComponent<Image>().enabled = false;
         } else {
            shopTemplate.buyButton.GetComponent<Image>().enabled = true;
         }
         if (shopItemData.isDisabled) {
            shopTemplate.buyButton.interactable = false;
            shopTemplate.enableBlocker(true);
         }

         if (selectedCategory == PvpShopItemType.Ship) {
            PvpItemInfo itemInfo = getPvpItemInfo(shopItemData);
            if (itemInfo != null) {
               shopTemplate.itemIcon.sprite = itemInfo.sprite;
            }
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
      if (Global.player && !isSeaShop) {
         loadingPanel.SetActive(true);
         Global.player.rpc.Cmd_RequestPvpShopData(shopId, (int) PvpShopItemType.LandPowerup);
      }

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
                  if (itemData.isDisabled) {
                     PvpShipIconPair shipSpritePair = pvpShipIconDisabledList.Find(_ => _.shipType == shipData.shipType);
                     if (shipSpritePair != null) {
                        newItemInfo.sprite = shipSpritePair.shipSprite;
                     }
                  } else {
                     PvpShipIconPair shipSpritePair = pvpShipIconList.Find(_ => _.shipType == shipData.shipType);
                     if (shipSpritePair != null) {
                        newItemInfo.sprite = shipSpritePair.shipSprite;
                     }
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
            case PvpShopItemType.LandPowerup:
               LandPowerupType landPowerupType = (LandPowerupType) itemData.itemId;
               if (LandPowerupManager.self.landPowerupInfo.ContainsKey(landPowerupType)) {
                  LandPowerupInfo landPowerupInfo = LandPowerupManager.self.landPowerupInfo[landPowerupType];
                  if (landPowerupInfo != null) {
                     if (landPowerupInfo.spriteRef != null) {
                        newItemInfo.sprite = landPowerupInfo.spriteRef;
                     } else {
                        if (landPowerupInfo.iconPath.Length > 1) {
                           newItemInfo.sprite = ImageManager.getSprite(landPowerupInfo.iconPath);
                        }
                     }
                     newItemInfo.name = landPowerupInfo.powerupName;
                     newItemInfo.description = landPowerupInfo.powerupInfo;
                  }
               } else {
                  D.debug("Does not contain land powerup type {" + landPowerupType + "}");
               }
               break;
            case PvpShopItemType.Item:
               switch ((PvpConsumableItem) itemData.itemId) {
                  case PvpConsumableItem.RepairTool:
                     newItemInfo.name = "Repair Rush";
                     newItemInfo.description = "Repairs 40% of ship health.";
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

   public void clearPanel () {
      shopButton.SetActive(false);
      hideEntirePanel();
   }

   public void onShopButtonPressed (bool isSeaShop, int shopId) {
      this.shopId = shopId;
      this.isSeaShop = isSeaShop;

      landShopLabel.SetActive(!isSeaShop);
      seaShopLabel.SetActive(isSeaShop);
      if (isSeaShop) {
         onSelectShipCategory();
      } else {
         onSelectLandCategory();
      }

      shipCategoryObj.gameObject.SetActive(isSeaShop);
      powerupCategoryObj.gameObject.SetActive(isSeaShop);
      abilityCategoryObj.gameObject.SetActive(isSeaShop);
      itemCategoryObj.gameObject.SetActive(isSeaShop);
      landPowerupCategoryObj.gameObject.SetActive(!isSeaShop);
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

   public void onSelectLandCategory () {
      Global.player.rpc.Cmd_RequestPvpShopData(shopId, (int) PvpShopItemType.LandPowerup);
      loadingPanel.SetActive(true);
      selectedCategory = PvpShopItemType.LandPowerup;
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
      landPowerupCategoryObjHover.gameObject.SetActive(false);

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
         case PvpShopItemType.LandPowerup:
            landPowerupCategoryObjHover.gameObject.SetActive(true);
            break;
      }
   }

   private void selectPvpItemCategory (PvpShopItemType itemType) {
      powerupCategoryObjSelect.gameObject.SetActive(false);
      shipCategoryObjSelect.gameObject.SetActive(false);
      abilityCategoryObjSelect.gameObject.SetActive(false); 
      itemCategoryObjSelect.gameObject.SetActive(false);
      landPowerupCategoryObjSelect.gameObject.SetActive(false);

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
         case PvpShopItemType.LandPowerup:
            landPowerupCategoryObjSelect.gameObject.SetActive(true);
            break;
      }
   }

   public virtual void OnPointerClick (PointerEventData eventData) {
      if (eventData.rawPointerPress == this.gameObject) {
         // If the mouse is over the input field zone, select it through the panel black background
         if (!Panel.tryFocusChatInputField()) {
            // If the black background outside is clicked, hide the panel
            hideEntirePanel();
         }
      }
   }

   public bool isActive () {
      return shopButton.activeInHierarchy || entirePanel.activeInHierarchy;
   }

   #region Private Variables

   #endregion
}
