using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Linq;

public class MonsterDataPanel : MonoBehaviour {
   #region Public Variables

   // References the tool manager
   public MonsterToolManager monsterToolManager;

   // Reference to the current XML Template being modified
   public EnemyDataTemplate currentXMLTemplate;

   // Button for closing panel
   public Button revertButton;

   // Button for updating the new data
   public Button saveExitButton;

   // Button tabs for categorized input fields
   public Button toggleMainStatsButton, toggleAttackButton, toggleDefenseButton;

   // Content holders
   public GameObject mainStatsContent, attackContent, defenseContent;

   // Dropdown indicator objects
   public GameObject dropDownIndicatorMain, dropDownIndicatorAttack, dropDownIndicatorDefense;

   // Holds the item type selection template
   public ItemTypeTemplate monsterTypeTemplate;

   // Holds the parent of the monster types
   public GameObject monsterTypeParent;

   // The selection panel for monster types
   public GameObject selectionPanel;

   // The type of monster this unit is
   public Text monsterTypeText;

   // Button to trigger seleciton panel
   public Button toggleSelectionPanelButton;

   // Caches the initial type incase it is changed
   public Enemy.Type startingType;

   // The cache list for avatar icon selection
   public Dictionary<string, Sprite> iconSpriteList = new Dictionary<string, Sprite>();

   // The cached icon path
   public string avatarIconPath;

   // The avatar icon of the enemy
   public Image avatarIcon;

   // The button that toggles the avatar selection
   public Button closeAvatarSelectionButton;

   // The button that opens the avatar selection
   public Button openAvatarSelectionButton;

   // Item Loot Variables
   public GameObject lootSelectionPanel;
   public GameObject lootTemplateParent;
   public MonsterLootRow lootTemplate, currentLootTemplate;
   public List<MonsterLootRow> monsterLootList = new List<MonsterLootRow>();

   public GameObject itemCategoryParent, itemTypeParent;
   public ItemCategoryTemplate itemCategoryTemplate;
   public ItemTypeTemplate itemTypeTemplate;

   public Button confirmItemButton, addLootButton, closeItemPanelButton;
   public Item.Category selectedCategory;
   public int itemTypeIDSelected;
   public MonsterLootRow monsterLootRowDefault;
   public InputField rewardItemMin, rewardItemMax;
   public Sprite emptySprite;

   #endregion

   private void Awake () {
      revertButton.onClick.AddListener(() => {
         monsterToolManager.loadAllDataFiles();
         gameObject.SetActive(false);
      });
      addLootButton.onClick.AddListener(() => addLootTemplate());
      closeItemPanelButton.onClick.AddListener(() => lootSelectionPanel.SetActive(false));
      openAvatarSelectionButton.onClick.AddListener(() => openImageSelectionPanel());
      closeAvatarSelectionButton.onClick.AddListener(() => selectionPanel.SetActive(false));
      toggleSelectionPanelButton.onClick.AddListener(() => openSelectionPanel());
      saveExitButton.onClick.AddListener(() => saveData());
      toggleMainStatsButton.onClick.AddListener(() => toggleMainStats());
      toggleAttackButton.onClick.AddListener(() => toggleAttackStats());
      toggleDefenseButton.onClick.AddListener(() => toggleDefenseStats());
   }

   public void loadData(MonsterRawData newBattleData) {
      startingType = ((Enemy.Type)newBattleData.battlerID);
      monsterTypeText.text = startingType.ToString();

      try {
         avatarIcon.sprite = ImageManager.getSprite(newBattleData.imagePath);
      } catch {
         avatarIcon.sprite = emptySprite;
      }
      avatarIconPath = newBattleData.imagePath;
      
      _baseHealth.text = newBattleData.baseHealth.ToString();
      _baseDefense.text = newBattleData.baseDefense.ToString();
      _baseDamage.text = newBattleData.baseDamage.ToString();
      _baseGoldReward.text = newBattleData.baseGoldReward.ToString();
      _baseXPReward.text = newBattleData.baseXPReward.ToString();

      _damagePerLevel.text = newBattleData.damagePerLevel.ToString();
      _defensePerLevel.text = newBattleData.defensePerLevel.ToString();
      _healthPerlevel.text = newBattleData.healthPerlevel.ToString();

      _physicalDefenseMultiplier.text = newBattleData.physicalDefenseMultiplier.ToString();
      _fireDefenseMultiplier.text = newBattleData.fireDefenseMultiplier.ToString();
      _earthDefenseMultiplier.text = newBattleData.earthDefenseMultiplier.ToString();
      _airDefenseMultiplier.text = newBattleData.airDefenseMultiplier.ToString();
      _waterDefenseMultiplier.text = newBattleData.waterDefenseMultiplier.ToString();
      _allDefenseMultiplier.text = newBattleData.allDefenseMultiplier.ToString();

      _physicalAttackMultiplier.text = newBattleData.physicalAttackMultiplier.ToString();
      _fireAttackMultiplier.text = newBattleData.fireAttackMultiplier.ToString();
      _earthAttackMultiplier.text = newBattleData.earthAttackMultiplier.ToString();
      _airAttackMultiplier.text = newBattleData.airAttackMultiplier.ToString();
      _waterAttackMultiplier.text = newBattleData.waterAttackMultiplier.ToString();
      _allAttackMultiplier.text = newBattleData.allAttackMultiplier.ToString();

      loadLootTemplates(newBattleData);
   }

   private MonsterRawData getMonsterRawData () {
      MonsterRawData newBattData = new MonsterRawData();

      newBattData.battlerID = (Enemy.Type) Enum.Parse(typeof(Enemy.Type), monsterTypeText.text);
      newBattData.imagePath = avatarIconPath;

      newBattData.baseHealth = int.Parse(_baseHealth.text);
      newBattData.baseDefense = int.Parse(_baseDefense.text);
      newBattData.baseDamage = int.Parse(_baseDamage.text);
      newBattData.baseGoldReward = int.Parse(_baseGoldReward.text);
      newBattData.baseXPReward = int.Parse(_baseXPReward.text);
      newBattData.damagePerLevel = int.Parse(_damagePerLevel.text);
      newBattData.defensePerLevel = int.Parse(_defensePerLevel.text);
      newBattData.healthPerlevel = int.Parse(_healthPerlevel.text);

      newBattData.allDefenseMultiplier = int.Parse(_allDefenseMultiplier.text);
      newBattData.fireDefenseMultiplier = int.Parse(_fireDefenseMultiplier.text);
      newBattData.earthDefenseMultiplier = int.Parse(_earthDefenseMultiplier.text);
      newBattData.waterDefenseMultiplier = int.Parse(_waterDefenseMultiplier.text);
      newBattData.airDefenseMultiplier = int.Parse(_airDefenseMultiplier.text);

      newBattData.allAttackMultiplier = int.Parse(_allAttackMultiplier.text);
      newBattData.fireAttackMultiplier = int.Parse(_fireAttackMultiplier.text);
      newBattData.earthAttackMultiplier = int.Parse(_earthAttackMultiplier.text);
      newBattData.waterAttackMultiplier = int.Parse(_waterAttackMultiplier.text);
      newBattData.airAttackMultiplier = int.Parse(_airAttackMultiplier.text);

      return newBattData;
   }

   public void saveData() {
      MonsterRawData rawData = getMonsterRawData();
      if (rawData.battlerID != startingType) {
         deleteOldData(new MonsterRawData { battlerID = startingType });
      }

      rawData.battlerLootData = getRawLootData();
      monsterToolManager.saveDataToFile(rawData);
      monsterToolManager.loadAllDataFiles();
      gameObject.SetActive(false);
   }

   private RawGenericLootData getRawLootData() {
      RawGenericLootData rawData = new RawGenericLootData();
      List<LootInfo> tempLootInfo = new List<LootInfo>();

      foreach (MonsterLootRow lootRow in monsterLootList) {
         LootInfo newLootInfo = new LootInfo();
         newLootInfo.lootType = (CraftingIngredients.Type) lootRow.currentType;
         newLootInfo.quantity = int.Parse(lootRow.itemCount.text);
         newLootInfo.chanceRatio = (float) lootRow.chanceRatio.value;
         tempLootInfo.Add(newLootInfo);
      }
      rawData.defaultLoot = (CraftingIngredients.Type) monsterLootRowDefault.currentType;
      rawData.lootList = tempLootInfo.ToArray();
      rawData.minQuantity = int.Parse(rewardItemMin.text);
      rawData.maxQuantity = int.Parse(rewardItemMax.text);

      return rawData;
   }

   private void deleteOldData(MonsterRawData rawData) {
      monsterToolManager.deleteMonsterDataFile(rawData);
   }

   #region Stats Feature

   private void openImageSelectionPanel () {
      selectionPanel.SetActive(true);
      monsterTypeParent.DestroyChildren();

      foreach (KeyValuePair<string, Sprite> sourceSprite in iconSpriteList) {
         GameObject iconTempObj = Instantiate(monsterTypeTemplate.gameObject, monsterTypeParent.transform);
         ItemTypeTemplate iconTemp = iconTempObj.GetComponent<ItemTypeTemplate>();
         iconTemp.spriteIcon.sprite = sourceSprite.Value;
         iconTemp.itemTypeText.text = sourceSprite.Value.name;
         iconTemp.selectButton.onClick.AddListener(() => {
            avatarIconPath = sourceSprite.Key;
            avatarIcon.sprite = sourceSprite.Value;
            closeAvatarSelectionButton.onClick.Invoke();
         });
      }
   }

   private void openSelectionPanel () {
      selectionPanel.SetActive(true);
      monsterTypeParent.DestroyChildren();

      foreach (Enemy.Type category in Enum.GetValues(typeof(Enemy.Type))) {
         GameObject template = Instantiate(monsterTypeTemplate.gameObject, monsterTypeParent.transform);
         ItemTypeTemplate itemTemp = template.GetComponent<ItemTypeTemplate>();
         itemTemp.itemTypeText.text = category.ToString();
         itemTemp.itemIndexText.text = ((int) category).ToString();

         itemTemp.selectButton.onClick.AddListener(() => {
            monsterTypeText.text = category.ToString();
            selectionPanel.SetActive(false);
         });
         template.SetActive(true);
      }
   }

   private void toggleMainStats() {
      mainStatsContent.SetActive(!mainStatsContent.activeSelf);
      dropDownIndicatorMain.SetActive(!mainStatsContent.activeSelf);
   }

   private void toggleAttackStats () {
      attackContent.SetActive(!attackContent.activeSelf);
      dropDownIndicatorAttack.SetActive(!attackContent.activeSelf);
   }

   private void toggleDefenseStats () {
      defenseContent.SetActive(!defenseContent.activeSelf);
      dropDownIndicatorDefense.SetActive(!defenseContent.activeSelf);
   }

   #endregion

   #region Loots Feature

   public void addLootTemplate() {
      GameObject lootTemp = Instantiate(lootTemplate.gameObject, lootTemplateParent.transform);
      MonsterLootRow row = lootTemp.GetComponent<MonsterLootRow>();
      row.currentCategory = Item.Category.None;
      row.initializeSetup();
      row.updateDisplayName();
      lootTemp.SetActive(true);
      monsterLootList.Add(row);
   }

   public void loadLootTemplates(MonsterRawData rawData) {
      lootTemplateParent.DestroyChildren();
      monsterLootList = new List<MonsterLootRow>();

      if (rawData.battlerLootData != null) {
         foreach (LootInfo lootInfo in rawData.battlerLootData.lootList) {
            GameObject lootTemp = Instantiate(lootTemplate.gameObject, lootTemplateParent.transform);
            MonsterLootRow row = lootTemp.GetComponent<MonsterLootRow>();
            row.currentCategory = Item.Category.CraftingIngredients;
            row.currentType = (int) lootInfo.lootType;
            row.itemCount.text = lootInfo.quantity.ToString();
            row.chanceRatio.value = lootInfo.chanceRatio;

            row.initializeSetup();
            row.updateDisplayName();
            lootTemp.SetActive(true);
            monsterLootList.Add(row);
         }
      }
      if (rawData.battlerLootData != null) {
         monsterLootRowDefault.currentCategory = Item.Category.CraftingIngredients;
         monsterLootRowDefault.currentType = (int) rawData.battlerLootData.defaultLoot;
         rewardItemMin.text = rawData.battlerLootData.minQuantity.ToString();
         rewardItemMax.text = rawData.battlerLootData.maxQuantity.ToString();
      } else {
         monsterLootRowDefault.currentCategory = Item.Category.CraftingIngredients;
         monsterLootRowDefault.currentType = 0;
      }
      monsterLootRowDefault.initializeSetup();
      monsterLootRowDefault.updateDisplayName();
   }

   private void updateMainItemDisplay() {
      lootSelectionPanel.SetActive(false);
      currentLootTemplate.itemTypeString.text = itemTypeIDSelected.ToString();
      currentLootTemplate.itemCategoryString.text = selectedCategory.ToString();
   }

   public void popupChoices () {
      lootSelectionPanel.SetActive(true);
      confirmItemButton.onClick.RemoveAllListeners();
      confirmItemButton.onClick.AddListener(() => updateMainItemDisplay());
      itemCategoryParent.gameObject.DestroyChildren();

      foreach (Item.Category category in Enum.GetValues(typeof(Item.Category))) {
         if (category == Item.Category.CraftingIngredients) {
            GameObject template = Instantiate(itemCategoryTemplate.gameObject, itemCategoryParent.transform);
            ItemCategoryTemplate categoryTemp = template.GetComponent<ItemCategoryTemplate>();
            categoryTemp.itemCategoryText.text = category.ToString();
            categoryTemp.itemIndexText.text = ((int) category).ToString();
            categoryTemp.itemCategory = category;

            categoryTemp.selectButton.onClick.AddListener(() => {
               selectedCategory = category;
               updateTypeOptions();
            });
            template.SetActive(true);
         }
      }
      updateTypeOptions();
   }

   private void updateTypeOptions () {
      // Dynamically handles the type of item
      Type itemType = Util.getItemType(selectedCategory);
      itemTypeParent.gameObject.DestroyChildren();

      Dictionary<int, string> itemNameList = new Dictionary<int, string>();
      if (itemType != null) {
         foreach (object item in Enum.GetValues(itemType)) {
            int newVal = (int) item;
            itemNameList.Add(newVal, item.ToString());
         }

         var sortedList = itemNameList.OrderBy(r => r.Value);
         foreach (var item in sortedList) {
            GameObject template = Instantiate(itemTypeTemplate.gameObject, itemTypeParent.transform);
            ItemTypeTemplate itemTemp = template.GetComponent<ItemTypeTemplate>();
            itemTemp.itemTypeText.text = item.Value.ToString();
            itemTemp.itemIndexText.text = "" + item.Key;
            itemTemp.spriteIcon.sprite = Util.getRawSpriteIcon(selectedCategory, item.Key);

            itemTemp.selectButton.onClick.AddListener(() => {
               itemTypeIDSelected = (int) item.Key;
               confirmItemButton.onClick.Invoke();
            });
         }
      }
   }

   #endregion

   #region Private Variables

   // Main parameters
   [SerializeField] private InputField _name;

   // Base battler parameters
   [SerializeField] private InputField _baseHealth;
   [SerializeField] private InputField _baseDefense;
   [SerializeField] private InputField _baseDamage;
   [SerializeField] private InputField _baseGoldReward;
   [SerializeField] private InputField _baseXPReward;

   // Increments in stats per level.
   [SerializeField] private InputField _damagePerLevel;
   [SerializeField] private InputField _defensePerLevel;
   [SerializeField] private InputField _healthPerlevel;

   // Element defense multiplier values
   [SerializeField] private InputField _physicalDefenseMultiplier;
   [SerializeField] private InputField _fireDefenseMultiplier;
   [SerializeField] private InputField _earthDefenseMultiplier;
   [SerializeField] private InputField _airDefenseMultiplier;
   [SerializeField] private InputField _waterDefenseMultiplier;
   [SerializeField] private InputField _allDefenseMultiplier;

   // Element attack multiplier values
   [SerializeField] private InputField _physicalAttackMultiplier;
   [SerializeField] private InputField _fireAttackMultiplier;
   [SerializeField] private InputField _earthAttackMultiplier;
   [SerializeField] private InputField _airAttackMultiplier;
   [SerializeField] private InputField _waterAttackMultiplier;
   [SerializeField] private InputField _allAttackMultiplier;

   #endregion
}
