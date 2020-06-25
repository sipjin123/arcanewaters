using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Security.Cryptography;
using UnityEngine.Events;

public class TreasureDropsToolScene : MonoBehaviour {
   #region Public Variables

   // The biome templates
   public Transform biomeTypeParent;
   public TreasureDropsTemplate biomeTypePrefab;

   // The panel containing all the items in the biome
   public GameObject itemPreviewPanel;

   // Populate content panel
   public Text biomeText;
   public Transform itemTemplateHolder;
   public TreasureDropsItemTemplate treasureItemTemplate;

   // The reference to the popup selector
   public GenericSelectionPopup genericSelectionPopup;

   // Add items
   public Button addWeapon, addArmor, addHat, addCraftingIngredient;

   // Event that notifies the script that an item is selected
   public UnityEvent changeItemTypeEvent = new UnityEvent();

   // Cached UI Data
   public Text cachedItemName, cachedItemIndex;
   public Image cachedItemIcon;

   // Bottom panel buttons
   public Button saveButton;
   public Button cancelButton;

   // The current biome selected
   public Biome.Type selectedBiome;

   #endregion

   private void Start () {
      saveButton.onClick.AddListener(() => saveData());
      cancelButton.onClick.AddListener(() => {
         itemPreviewPanel.SetActive(false);
      });

      itemPreviewPanel.SetActive(false);
      addWeapon.onClick.AddListener(() => {
         Item.Category category = Item.Category.Weapon;

         changeItemTypeEvent.RemoveAllListeners();
         changeItemTypeEvent.AddListener(() => {
            TreasureDropsItemTemplate template = Instantiate(treasureItemTemplate.gameObject, itemTemplateHolder).GetComponent<TreasureDropsItemTemplate>();
            int weaponType = int.Parse(cachedItemIndex.text);
            WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(weaponType);
            processItemTemplate(template, category, weaponType, WeaponStatData.serializeWeaponStatData(weaponData), weaponData.equipmentIconPath);
         });
         genericSelectionPopup.callItemTypeSelectionPopup(category, cachedItemName, cachedItemIndex, cachedItemIcon, changeItemTypeEvent);
      });

      addArmor.onClick.AddListener(() => {
         Item.Category category = Item.Category.Armor;

         changeItemTypeEvent.RemoveAllListeners();
         changeItemTypeEvent.AddListener(() => {
            TreasureDropsItemTemplate template = Instantiate(treasureItemTemplate.gameObject, itemTemplateHolder).GetComponent<TreasureDropsItemTemplate>();
            int armorType = int.Parse(cachedItemIndex.text);
            ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(armorType);
            processItemTemplate(template, category, armorType, ArmorStatData.serializeArmorStatData(armorData), armorData.equipmentIconPath);
         });
         genericSelectionPopup.callItemTypeSelectionPopup(category, cachedItemName, cachedItemIndex, cachedItemIcon, changeItemTypeEvent);
      });

      addHat.onClick.AddListener(() => {
         Item.Category category = Item.Category.Hats;

         changeItemTypeEvent.RemoveAllListeners();
         changeItemTypeEvent.AddListener(() => {
            TreasureDropsItemTemplate template = Instantiate(treasureItemTemplate.gameObject, itemTemplateHolder).GetComponent<TreasureDropsItemTemplate>();
            int hatType = int.Parse(cachedItemIndex.text);
            HatStatData hatData = EquipmentXMLManager.self.getHatData(hatType);
            processItemTemplate(template, category, hatType, HatStatData.serializeHatStatData(hatData), hatData.equipmentIconPath);
         });
         genericSelectionPopup.callItemTypeSelectionPopup(category, cachedItemName, cachedItemIndex, cachedItemIcon, changeItemTypeEvent);
      });

      addCraftingIngredient.onClick.AddListener(() => {
         Item.Category category = Item.Category.CraftingIngredients;

         changeItemTypeEvent.RemoveAllListeners();
         changeItemTypeEvent.AddListener(() => {
            TreasureDropsItemTemplate template = Instantiate(treasureItemTemplate.gameObject, itemTemplateHolder).GetComponent<TreasureDropsItemTemplate>();
            int ingredientType = int.Parse(cachedItemIndex.text);
            processItemTemplate(template, category, ingredientType, "");
         });
         genericSelectionPopup.callItemTypeSelectionPopup(category, cachedItemName, cachedItemIndex, cachedItemIcon, changeItemTypeEvent);
      });
   }

   private void saveData () {
      List<TreasureDropsData> newDropsDataList = new List<TreasureDropsData>();
      foreach (Transform children in itemTemplateHolder) {
         TreasureDropsItemTemplate treasureItemTemplate = children.GetComponent<TreasureDropsItemTemplate>();
         if (treasureItemTemplate.item.category != 0 && treasureItemTemplate.item.itemTypeId != 0) {
            TreasureDropsData newDropsData = new TreasureDropsData {
               item = treasureItemTemplate.item,
               spawnChance = float.Parse(treasureItemTemplate.dropChance.text),
               spawnInSecretChest = treasureItemTemplate.spawnOnSecrets.isOn
            };
            newDropsDataList.Add(newDropsData);
         }
      }
      TreasureDropsToolManager.instance.saveDataFile(selectedBiome, newDropsDataList);
      itemPreviewPanel.SetActive(false);
   }

   private void processItemTemplate (TreasureDropsItemTemplate template, Item.Category category, int itemType, string rawData, string iconPath = "") {
      // Create Item
      template.item = new Item {
         category = category,
         itemTypeId = itemType,
         itemName = cachedItemName.text,
         data = rawData,
         iconPath = iconPath
      };
      
      if (iconPath == "") {
         template.item.iconPath = template.item.getCastItem().getIconPath();
      }

      // Item Info
      template.itemName.text = cachedItemName.text;
      template.itemIndex.text = itemTemplateHolder.childCount.ToString();
      template.itemIcon.sprite = cachedItemIcon.sprite;
      template.itemType.text = category == Item.Category.CraftingIngredients ? "Material" : template.item.category.ToString();

      template.gameObject.SetActive(true);
      template.destroyButton.onClick.AddListener(() => {
         Destroy(template.gameObject);
         StartCoroutine(CO_SortContents());
      });
   }

   public void cacheDatabaseContents (Dictionary<Biome.Type, List<TreasureDropsData>> databaseContents) {
      biomeTypeParent.gameObject.DestroyChildren();
      foreach (Biome.Type biomeType in Enum.GetValues(typeof(Biome.Type))) {
         if (biomeType != Biome.Type.None) {
            TreasureDropsTemplate template = Instantiate(biomeTypePrefab.gameObject, biomeTypeParent).GetComponent<TreasureDropsTemplate>();
            template.biomeTypeText.text = biomeType.ToString();
            template.selectButton.onClick.AddListener(() => loadItemsInbiome(biomeType));
            template.gameObject.SetActive(true);
         }
      }
   }

   private void loadItemsInbiome (Biome.Type biomeType) {
      itemTemplateHolder.gameObject.DestroyChildren();
      selectedBiome = biomeType;
      biomeText.text = biomeType.ToString();
      itemPreviewPanel.SetActive(true);

      int i = 0;
      foreach (TreasureDropsData treasureData in TreasureDropsToolManager.instance.treasureDropsCollection[biomeType]) {
         TreasureDropsItemTemplate template = Instantiate(treasureItemTemplate.gameObject, itemTemplateHolder).GetComponent<TreasureDropsItemTemplate>();

         // Item info
         template.item = treasureData.item;
         template.itemName.text = treasureData.item.itemName;
         template.itemIcon.sprite = ImageManager.getSprite(treasureData.item.iconPath);
         template.itemType.text = template.item.category == Item.Category.CraftingIngredients ? "Material" : template.item.category.ToString();

         // Spawning parameters
         template.dropChance.text = treasureData.spawnChance.ToString();
         template.spawnOnSecrets.isOn = treasureData.spawnInSecretChest;

         template.gameObject.SetActive(true);
         template.destroyButton.onClick.AddListener(() => {
            Destroy(template.gameObject);
            StartCoroutine(CO_SortContents());
         });
         i++;
         template.itemIndex.text = i.ToString();
      }
   }

   private IEnumerator CO_SortContents () {
      yield return new WaitForSeconds(1);
      int i = 0;
      foreach (Transform child in itemTemplateHolder) {
         TreasureDropsItemTemplate treasureItemTemplate = child.GetComponent<TreasureDropsItemTemplate>();
         i++;
         treasureItemTemplate.itemIndex.text = i.ToString();
      }
   }

   #region Private Variables

   #endregion
}
