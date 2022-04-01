using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Security.Cryptography;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

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
   public Button addWeapon, addArmor, addHat, addCraftingIngredient, addBlueprint;

   // Event that notifies the script that an item is selected
   public UnityEvent changeItemTypeEvent = new UnityEvent();

   // Cached UI Data
   public Text cachedItemName, cachedItemIndex;
   public Image cachedItemIcon;

   // Bottom panel buttons
   public Button saveButton;
   public Button cancelButton;
   public Button mainMenuButton;
   public Button createGroupButton;
   public Button createGroupBiomeButton;

   // The current biome selected
   public Biome.Type selectedBiome;

   // The current xmlId
   public int selectedXmlId;

   // The loot group name
   public InputField lootGroupName;

   // Biome selection UI
   public Button biomeTypeButton;
   public Text biomeTypeText;

   // The event triggered after the biome is selected
   public UnityEvent biomeSelectedEvent;

   // Reference to the warning panel
   public GameObject warningPanel;
   public Button closeWarningButton;

   #endregion

   private void Start () {
      saveButton.onClick.AddListener(() => saveData());
      cancelButton.onClick.AddListener(() => {
         itemPreviewPanel.SetActive(false);
      });

      closeWarningButton.onClick.AddListener(() => {
         warningPanel.SetActive(false);
      });
      mainMenuButton.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.masterScene);
      });

      itemPreviewPanel.SetActive(false);

      biomeTypeButton.onClick.AddListener(() => {
         biomeSelectedEvent.AddListener(() => {
            selectedBiome = (Biome.Type) Enum.Parse(typeof(Biome.Type), biomeTypeText.text);
         });
         genericSelectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.BiomeType, biomeTypeText, biomeSelectedEvent);
      });

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
            ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(armorType);
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

      addBlueprint.onClick.AddListener(() => {
         Item.Category category = Item.Category.Blueprint;

         changeItemTypeEvent.RemoveAllListeners();
         changeItemTypeEvent.AddListener(() => {
            TreasureDropsItemTemplate template = Instantiate(treasureItemTemplate.gameObject, itemTemplateHolder).GetComponent<TreasureDropsItemTemplate>();
            int ingredientType = int.Parse(cachedItemIndex.text);

            string rawData = "";
            CraftableItemRequirements craftingRequirements = TreasureDropsToolManager.instance.craftingDataList.Find(_ => _.xmlId == ingredientType);
            processItemTemplate(template, category, ingredientType, rawData);
         });
         genericSelectionPopup.callItemTypeSelectionPopup(category, cachedItemName, cachedItemIndex, cachedItemIcon, changeItemTypeEvent);
      });

      createGroupButton.onClick.AddListener(() => {
         selectedBiome = Biome.Type.None;
         lootGroupName.text = "";

         loadItemsFromGroup(-1);
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

      if (newDropsDataList.Count > 0) {
         LootGroupData newLootGroupData = new LootGroupData {
            treasureDropsCollection = newDropsDataList,
            lootGroupName = lootGroupName.text,
            xmlId = selectedXmlId,
            biomeType = selectedBiome,
         };

         TreasureDropsToolManager.instance.saveDataFile(selectedXmlId, selectedBiome, newLootGroupData);
         itemPreviewPanel.SetActive(false);
      } else {
         warningPanel.SetActive(true);
      }
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
      template.itemName.text = template.item.category == Item.Category.Blueprint ? cachedItemName.text + " BP" : cachedItemName.text;
      template.itemIndex.text = itemTemplateHolder.childCount.ToString();
      if (template.item.category != Item.Category.Blueprint) {
         template.itemIcon.sprite = cachedItemIcon.sprite;
      } else {
         string iconPathNew = EquipmentXMLManager.self.getItemIconPath(template.item);
         template.itemIcon.sprite = ImageManager.getSprite(iconPathNew);
      }

      template.itemType.text = category == Item.Category.CraftingIngredients ? "Material" : template.item.category.ToString();

      template.gameObject.SetActive(true);
      template.destroyButton.onClick.AddListener(() => {
         Destroy(template.gameObject);
         StartCoroutine(CO_SortContents());
      });
   }

   public void cacheDatabaseContents (Dictionary<int, LootGroupData> lootGroups) {
      biomeTypeParent.gameObject.DestroyChildren();
      foreach (KeyValuePair<int, LootGroupData> lootGrpData in lootGroups) {
         TreasureDropsTemplate template = Instantiate(biomeTypePrefab.gameObject, biomeTypeParent).GetComponent<TreasureDropsTemplate>();
         template.lootGroupName.text = lootGrpData.Value.lootGroupName;
         template.biomeTypeText.text = lootGrpData.Value.biomeType == Biome.Type.None ? "" : lootGrpData.Value.biomeType.ToString();
         template.setImage(lootGrpData.Value.biomeType != Biome.Type.None);
         template.selectButton.onClick.AddListener(() => {
            lootGroupName.text = lootGrpData.Value.lootGroupName;
            loadItemsFromGroup(lootGrpData.Key);
         });
         template.duplicateButton.onClick.AddListener(() => {
            TreasureDropsToolManager.instance.duplicateData(lootGrpData.Value);
         });
         template.gameObject.SetActive(true);
      }
   }

   private void loadItemsFromGroup (int lootGroupId) {
      selectedXmlId = lootGroupId;
      itemTemplateHolder.gameObject.DestroyChildren();
      itemPreviewPanel.SetActive(true);

      int i = 0;
      if (lootGroupId < 0) {
         selectedBiome = Biome.Type.None;
         biomeText.text = Biome.Type.None.ToString();
      } else {
         selectedBiome = TreasureDropsToolManager.instance.treasureDropsCollection[lootGroupId].biomeType;
         biomeText.text = selectedBiome.ToString();
         foreach (TreasureDropsData treasureData in TreasureDropsToolManager.instance.treasureDropsCollection[lootGroupId].treasureDropsCollection) {
            TreasureDropsItemTemplate template = Instantiate(treasureItemTemplate.gameObject, itemTemplateHolder).GetComponent<TreasureDropsItemTemplate>();

            // Item info
            template.item = treasureData.item;
            template.itemName.text = template.item.category == Item.Category.Blueprint ? treasureData.item.itemName + " BP" : treasureData.item.itemName;
            if (template.item.category != Item.Category.Blueprint) {
               template.itemIcon.sprite = ImageManager.getSprite(treasureData.item.iconPath);
            } else {
               string iconPath = EquipmentXMLManager.self.getItemIconPath(treasureData.item);
               template.itemIcon.sprite = ImageManager.getSprite(iconPath);
            }
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
