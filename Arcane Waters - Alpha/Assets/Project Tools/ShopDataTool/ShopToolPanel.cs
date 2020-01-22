using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;
using System;

public class ShopToolPanel : MonoBehaviour
{
   #region Public Variables

   // Buttons for saving and canceling
   public Button saveButton, cancelButton;

   // Caches the initial type incase it is changed
   public string startingName;

   // Shop Prefab
   public ShopDataItemTemplate shopItemPrefab;

   // Popup selection reference
   public GenericSelectionPopup genericPopup;

   // Tool Reference
   public ShopDataToolManager toolManager;

   public const string MIN_PRICE = "1";
   public const string MAX_PRICE = "100";

   public enum ShopCategory
   {
      None = 0,
      Weapon = 1,
      Armor = 2,
      Crop = 3,
      Ship = 4
   }

   // Event for crop selection update
   public UnityEvent updateCropTypeEvent = new UnityEvent();

   // Event for ship selection update
   public UnityEvent updateShipTypeEvent = new UnityEvent();

   // Event for item selection update
   public UnityEvent updateItemTypeEvent = new UnityEvent();

   #endregion

   private void Awake () {
      if (!MasterToolAccountManager.canAlterData()) {
         saveButton.gameObject.SetActive(false);
      }

      saveButton.onClick.AddListener(() => {
         ShopData newShipData = getShopData();

         if (newShipData.shopName != startingName) {
            toolManager.deleteDataFile(new ShopData { shopName = startingName });
         }

         if (newShipData != null) {
            toolManager.saveXMLData(newShipData);
            gameObject.SetActive(false);
         }
      });
      cancelButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
         toolManager.loadXMLData();
      });

      _addWeaponButton.onClick.AddListener(() => {
         createTemplate(ShopCategory.Weapon);
      });
      _addArmorButton.onClick.AddListener(() => {
         createTemplate(ShopCategory.Armor);
      });
      _addShipButton.onClick.AddListener(() => {
         createTemplate(ShopCategory.Ship);
      });
      _addCropButton.onClick.AddListener(() => {
         createTemplate(ShopCategory.Crop);
      });

      _selectShopIcon.onClick.AddListener(() => {
         genericPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.ShopIcon, _shopIcon, _shopIconPath);
      });
   }

   private void createTemplate (ShopCategory category, ShopItemData itemData = null) {
      ShopDataItemTemplate shopItemTemp = Instantiate(shopItemPrefab.gameObject, _itemParent).GetComponent<ShopDataItemTemplate>();
      if (itemData == null) {
         shopItemTemp.itemCostMin.text = MIN_PRICE;
         shopItemTemp.itemCostMax.text = MAX_PRICE;
         shopItemTemp.itemName.text = "Undefined";
         shopItemTemp.itemIDType.text = "0";
         shopItemTemp.itemIDCategory.text = "0";
         shopItemTemp.chanceToDrop.text = "100";
         shopItemTemp.quantityMin.text = "1";
         shopItemTemp.quantityMax.text = "2";
         shopItemTemp.setIcon(category);
      } else {
         shopItemTemp.itemCostMin.text = itemData.shopItemCostMin.ToString();
         shopItemTemp.itemCostMax.text = itemData.shopItemCostMax.ToString();
         shopItemTemp.itemName.text = itemData.itemName.ToString();
         shopItemTemp.itemIDType.text = itemData.shopItemTypeIndex.ToString();
         shopItemTemp.itemIDCategory.text = itemData.shopItemCategoryIndex.ToString();
         shopItemTemp.iconPath.text = itemData.itemIconPath;
         shopItemTemp.chanceToDrop.text = itemData.dropChance.ToString();
         shopItemTemp.quantityMin.text = itemData.shopItemCountMin.ToString();
         shopItemTemp.quantityMax.text = itemData.shopItemCountMax.ToString();

         if (itemData.itemIconPath != "") {
            shopItemTemp.itemImage.sprite = ImageManager.getSprite(itemData.itemIconPath);
         } else {
            shopItemTemp.itemImage.sprite = toolManager.shopScene.emptySprite;
         }
         shopItemTemp.setIcon(itemData.shopItemCategory);
      }

      switch (category) {
         case ShopCategory.Weapon:
            shopItemTemp.itemIDCategory.text = ((int)Item.Category.Weapon).ToString();
            shopItemTemp.itemSelection.onClick.AddListener(() => {
               genericPopup.callItemTypeSelectionPopup(Item.Category.Weapon, shopItemTemp.itemName, shopItemTemp.itemIDType, shopItemTemp.itemImage, updateItemTypeEvent, shopItemTemp.iconPath);
            });
            break;
         case ShopCategory.Armor:
            shopItemTemp.itemIDCategory.text = ((int) Item.Category.Armor).ToString();
            shopItemTemp.itemSelection.onClick.AddListener(() => {
               genericPopup.callItemTypeSelectionPopup(Item.Category.Armor, shopItemTemp.itemName, shopItemTemp.itemIDType, shopItemTemp.itemImage, updateItemTypeEvent, shopItemTemp.iconPath);
            });
            break;
         case ShopCategory.Crop:
            shopItemTemp.itemSelection.onClick.AddListener(() => {
               genericPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.CropType, shopItemTemp.itemName, updateCropTypeEvent);

               updateCropTypeEvent.RemoveAllListeners();
               updateCropTypeEvent.AddListener(() => {
                  shopItemTemp.itemIDType.text = ((int) Enum.Parse(typeof(Crop.Type), shopItemTemp.itemName.text)).ToString();
               });
            });
            break;
         case ShopCategory.Ship:
            shopItemTemp.itemSelection.onClick.AddListener(() => {
               genericPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.ShipType, shopItemTemp.itemName, updateShipTypeEvent);

               updateShipTypeEvent.RemoveAllListeners();
               updateShipTypeEvent.AddListener(() => {
                  shopItemTemp.itemIDType.text = ((int) Enum.Parse(typeof(Ship.Type), shopItemTemp.itemName.text)).ToString();
               });
            });
            break;
      }
      shopItemTemp.deleteItem.onClick.AddListener(() => {
         Destroy(shopItemTemp.gameObject);
      });
   }

   public void loadData (ShopData data) {
      _shopName.text = data.shopName;
      _shopDescription.text = data.shopGreetingText;
      _areaKiller.text = data.areaAttachment;
      startingName = data.shopName;

      _shopIconPath.text = data.shopIconPath;
      if (data.shopIconPath != "") {
         _shopIcon.sprite = ImageManager.getSprite(data.shopIconPath);
      } else {
         _shopIcon.sprite = toolManager.shopScene.emptySprite;
      }

      _itemParent.gameObject.DestroyChildren();
      foreach (ShopItemData itemData in data.shopItems) {
         createTemplate(itemData.shopItemCategory, itemData);
      }
   }

   private ShopData getShopData () {
      ShopData newShopData = new ShopData();
      newShopData.shopName = _shopName.text;
      newShopData.shopGreetingText = _shopDescription.text;
      newShopData.shopIconPath = _shopIconPath.text;
      newShopData.areaAttachment = _areaKiller.text;
      List<ShopItemData> itemDataList = new List<ShopItemData>();

      foreach (Transform child in _itemParent) {
         ShopDataItemTemplate itemTemp = child.GetComponent<ShopDataItemTemplate>();
         ShopItemData newItemData = new ShopItemData {
            itemName = itemTemp.itemName.text,
            shopItemCategory = itemTemp.currentCategory,
            shopItemTypeIndex = int.Parse(itemTemp.itemIDType.text),
            shopItemCategoryIndex = int.Parse(itemTemp.itemIDCategory.text),
            itemIconPath = itemTemp.iconPath.text,
            shopItemCostMin = int.Parse(itemTemp.itemCostMin.text),
            shopItemCostMax = int.Parse(itemTemp.itemCostMax.text),
            dropChance = float.Parse(itemTemp.chanceToDrop.text),
            shopItemCountMin = int.Parse(itemTemp.quantityMin.text),
            shopItemCountMax = int.Parse(itemTemp.quantityMax.text),
         };
         itemDataList.Add(newItemData);
      }
      newShopData.shopItems = itemDataList.ToArray();

      return newShopData;
   }

   #region Private Variables
#pragma warning disable 0649
   // Icon selection
   [SerializeField]
   private Image _shopIcon;
   [SerializeField]
   private Text _shopIconPath;
   [SerializeField]
   private Button _selectShopIcon;

   // Name and description
   [SerializeField]
   private InputField _shopName, _shopDescription, _areaKiller;

   // Add weapon templates
   [SerializeField]
   private Button _addWeaponButton, _addArmorButton, _addShipButton, _addCropButton;

   // Parent Transform
   [SerializeField]
   private Transform _itemParent;
#pragma warning restore 0649
   #endregion
}
