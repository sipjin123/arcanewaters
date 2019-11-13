using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Linq;
using static MonsterSkillTemplate;

public class MonsterDataPanel : MonoBehaviour {
   #region Public Variables

   // Reference to type selection to avoid future bugs caused by selecting sea monster for land monster tool
   public Enemy.Type[] landmonsterTypes = new Enemy.Type[] { Enemy.Type.Lizard, Enemy.Type.GolemBoss, Enemy.Type.Muckspirit, Enemy.Type.Coralbow, Enemy.Type.Entarcher,
      Enemy.Type.Flower, Enemy.Type.Golem, Enemy.Type.Plant, Enemy.Type.Shroom, Enemy.Type.Wisp, Enemy.Type.Treeman
   };

   // Reference to the ability manager
   public MonsterAbilityManager abilityManager;

   // Reference to the ability tool manager
   public AbilityToolManager abilityToolManager;

   // References the tool manager
   public MonsterToolManager monsterToolManager;

   // Reference to the current XML Template being modified
   public EnemyDataTemplate currentXMLTemplate;

   // Button for closing panel
   public Button revertButton;

   // Button for updating the new data
   public Button saveExitButton;

   // Button tabs for categorized input fields
   public Button toggleMainStatsButton, toggleAttackButton, toggleDefenseButton, toggleLootsButton, toggleSkillsButton;

   // Content holders
   public GameObject mainStatsContent, attackContent, defenseContent, dropDownContent, skillContent;

   // Dropdown indicator objects
   public GameObject dropDownIndicatorMain, dropDownIndicatorAttack, dropDownIndicatorDefense, dropDownLoots, dropDownSkills;

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
   public string startingName;

   // The cached icon path
   public string avatarIconPath;

   // The avatar icon of the enemy
   public Image avatarIcon;

   // The button that toggles the avatar selection
   public Button closeAvatarSelectionButton;

   // The button that opens the avatar selection
   public Button openAvatarSelectionButton;

   // Previews the sprite selection
   public Image previewSelectionIcon;

   // The cache list for icon selection
   public Dictionary<string, Sprite> iconSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> skillIconSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> castIconSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> hitIconSpriteList = new Dictionary<string, Sprite>();

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

   // Skills Variables
   public Button addATKSkillButton, addDEFSkillButton, addTemplateSkillButton;
   public Transform skillTemplateParent;
   public GameObject skillTemplatePrefab;
   List<MonsterSkillTemplate> monsterSkillTemplateList = new List<MonsterSkillTemplate>();

   // Audio Related updates
   public Text jumpClipPath;
   public Text deathClipPath;
   public Button selectJumpAudioButton;
   public Button selectDeathAudioButton;
   public Button playJumpAudioButton;
   public Button playDeathAudioButton;
   public AudioClip jumpAudio, deathAudio;

   // Audio source to play the sample clips
   public AudioSource audioSource;

   #endregion

   private void Awake () {
      revertButton.onClick.AddListener(() => {
         monsterToolManager.loadAllDataFiles();
         abilityToolManager.loadAllDataFiles();
         gameObject.SetActive(false);
      });
      addATKSkillButton.onClick.AddListener(() => addSkillTemplate(AbilityType.Standard));
      addDEFSkillButton.onClick.AddListener(() => addSkillTemplate(AbilityType.BuffDebuff));
      addTemplateSkillButton.onClick.AddListener(() => openSkillSelectionPanel());
      addLootButton.onClick.AddListener(() => addLootTemplate());
      toggleLootsButton.onClick.AddListener(() => toggleLoots());
      closeItemPanelButton.onClick.AddListener(() => lootSelectionPanel.SetActive(false));
      openAvatarSelectionButton.onClick.AddListener(() => openImageSelectionPanel());
      closeAvatarSelectionButton.onClick.AddListener(() => selectionPanel.SetActive(false));
      toggleSelectionPanelButton.onClick.AddListener(() => openSelectionPanel());
      saveExitButton.onClick.AddListener(() => saveData());
      toggleMainStatsButton.onClick.AddListener(() => toggleMainStats());
      toggleAttackButton.onClick.AddListener(() => toggleAttackStats());
      toggleDefenseButton.onClick.AddListener(() => toggleDefenseStats());
      toggleSkillsButton.onClick.AddListener(() => toggleSkills());

      selectJumpAudioButton.onClick.AddListener(() => toggleAudioSelection(PathType.JumpSfx));
      selectDeathAudioButton.onClick.AddListener(() => toggleAudioSelection(PathType.DeathSfx));
      playJumpAudioButton.onClick.AddListener(() => {
         if (jumpAudio != null) {
            audioSource.clip = jumpAudio;
            audioSource.Play();
         }
      });
      playDeathAudioButton.onClick.AddListener(() => {
         if (deathAudio != null) {
            audioSource.clip = deathAudio;
            audioSource.Play();
         }
      });
   }

   private void openSkillSelectionPanel () {
      selectionPanel.SetActive(true);
      monsterTypeParent.DestroyChildren();

      previewSelectionIcon.sprite = emptySprite;
      foreach (AttackAbilityData ability in abilityManager.attackAbilityList) {
         GameObject iconTempObj = Instantiate(monsterTypeTemplate.gameObject, monsterTypeParent.transform);
         ItemTypeTemplate iconTemp = iconTempObj.GetComponent<ItemTypeTemplate>();
         iconTemp.spriteIcon.sprite = ImageManager.getSprite(ability.itemIconPath);
         iconTemp.itemTypeText.text = ability.itemName;
         iconTemp.previewButton.onClick.AddListener(() => {
            previewSelectionIcon.sprite = ImageManager.getSprite(ability.itemIconPath);
         });
         iconTemp.selectButton.onClick.AddListener(() => {
            loadSkill(new AttackAbilityData[] { ability }, null);
            closeAvatarSelectionButton.onClick.Invoke();
         });
      }

      foreach (BuffAbilityData ability in abilityManager.buffAbilityList) {
         GameObject iconTempObj = Instantiate(monsterTypeTemplate.gameObject, monsterTypeParent.transform);
         ItemTypeTemplate iconTemp = iconTempObj.GetComponent<ItemTypeTemplate>();
         iconTemp.spriteIcon.sprite = ImageManager.getSprite(ability.itemIconPath);
         iconTemp.itemTypeText.text = ability.itemName;
         iconTemp.previewButton.onClick.AddListener(() => {
            previewSelectionIcon.sprite = ImageManager.getSprite(ability.itemIconPath);
         });
         iconTemp.selectButton.onClick.AddListener(() => {
            loadSkill(null, new BuffAbilityData[] { ability });
            closeAvatarSelectionButton.onClick.Invoke();
         });
      }
   }

   private void toggleAudioSelection (PathType pathType) {
      selectionPanel.SetActive(true);
      monsterTypeParent.DestroyChildren();

      foreach (AudioClipManager.AudioClipData sourceClip in AudioClipManager.self.audioDataList) {
         GameObject iconTempObj = Instantiate(monsterTypeTemplate.gameObject, monsterTypeParent.transform);
         ItemTypeTemplate iconTemp = iconTempObj.GetComponent<ItemTypeTemplate>();
         iconTemp.itemTypeText.text = sourceClip.audioName;

         iconTemp.previewButton.onClick.AddListener(() => {
            if (sourceClip.audioClip != null) {
               audioSource.clip = sourceClip.audioClip;
               audioSource.Play();
            }
         });

         iconTemp.selectButton.onClick.AddListener(() => {
            switch (pathType) {
               case PathType.DeathSfx:
                  deathClipPath.text = sourceClip.audioPath;
                  deathAudio = sourceClip.audioClip;
                  break;
               case PathType.JumpSfx:
                  jumpClipPath.text = sourceClip.audioPath;
                  jumpAudio = sourceClip.audioClip;
                  break;
            }
            closeAvatarSelectionButton.onClick.Invoke();
         });
      }
   }

   #region Save and Load Data

   public void loadData(BattlerData newBattleData) {
      startingName = newBattleData.enemyName;
      monsterTypeText.text = ((Enemy.Type) newBattleData.enemyType).ToString();

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

      _preContactLength.text = newBattleData.preContactLength.ToString();
      _preMagicLength.text = newBattleData.preMagicLength.ToString();

      deathClipPath.text = newBattleData.deathSoundPath;
      jumpClipPath.text = newBattleData.attackJumpSoundPath;
      jumpAudio = AudioClipManager.self.getAudioClipData(newBattleData.attackJumpSoundPath).audioClip;
      deathAudio = AudioClipManager.self.getAudioClipData(newBattleData.deathSoundPath).audioClip;

      _name.text = newBattleData.enemyName;

      loadLootTemplates(newBattleData);
      loadSkillTemplates(newBattleData);
   }

   private BattlerData getBattlerData () {
      BattlerData newBattData = new BattlerData();

      newBattData.enemyType = (Enemy.Type) Enum.Parse(typeof(Enemy.Type), monsterTypeText.text);
      newBattData.imagePath = avatarIconPath;

      newBattData.baseHealth = int.Parse(_baseHealth.text);
      newBattData.baseDefense = int.Parse(_baseDefense.text);
      newBattData.baseDamage = int.Parse(_baseDamage.text);
      newBattData.baseGoldReward = int.Parse(_baseGoldReward.text);
      newBattData.baseXPReward = int.Parse(_baseXPReward.text);
      newBattData.damagePerLevel = int.Parse(_damagePerLevel.text);
      newBattData.defensePerLevel = int.Parse(_defensePerLevel.text);
      newBattData.healthPerlevel = int.Parse(_healthPerlevel.text);

      newBattData.physicalDefenseMultiplier = int.Parse(_physicalDefenseMultiplier.text);
      newBattData.allDefenseMultiplier = int.Parse(_allDefenseMultiplier.text);
      newBattData.fireDefenseMultiplier = int.Parse(_fireDefenseMultiplier.text);
      newBattData.earthDefenseMultiplier = int.Parse(_earthDefenseMultiplier.text);
      newBattData.waterDefenseMultiplier = int.Parse(_waterDefenseMultiplier.text);
      newBattData.airDefenseMultiplier = int.Parse(_airDefenseMultiplier.text);

      newBattData.physicalAttackMultiplier = int.Parse(_physicalAttackMultiplier.text);
      newBattData.allAttackMultiplier = int.Parse(_allAttackMultiplier.text);
      newBattData.fireAttackMultiplier = int.Parse(_fireAttackMultiplier.text);
      newBattData.earthAttackMultiplier = int.Parse(_earthAttackMultiplier.text);
      newBattData.waterAttackMultiplier = int.Parse(_waterAttackMultiplier.text);
      newBattData.airAttackMultiplier = int.Parse(_airAttackMultiplier.text);

      newBattData.preContactLength = int.Parse(_preContactLength.text);
      newBattData.preMagicLength = int.Parse(_preMagicLength.text);

      newBattData.deathSoundPath = deathClipPath.text;
      newBattData.attackJumpSoundPath = jumpClipPath.text;

      newBattData.enemyName = _name.text;

      List<BasicAbilityData> basicAbilityList = new List<BasicAbilityData>();
      List<AttackAbilityData> attackAbilityList = new List<AttackAbilityData>();
      List<BuffAbilityData> buffAbilityList = new List<BuffAbilityData>();
      foreach (MonsterSkillTemplate skillTemplate in monsterSkillTemplateList) {
         if(skillTemplate.abilityTypeEnum == AbilityType.Standard) {
            attackAbilityList.Add(skillTemplate.getAttackData());
            basicAbilityList.Add(skillTemplate.getAttackData());
         } else if (skillTemplate.abilityTypeEnum == AbilityType.BuffDebuff) {
            buffAbilityList.Add(skillTemplate.getBuffData());
            basicAbilityList.Add(skillTemplate.getBuffData());
         }
      }
      newBattData.battlerAbilities = new AbilityDataRecord {
         basicAbilityDataList = basicAbilityList.ToArray(),
         attackAbilityDataList = attackAbilityList.ToArray(),
         buffAbilityDataList = buffAbilityList.ToArray()
      };

      return newBattData;
   }

   public void saveData() {
      BattlerData rawData = getBattlerData();
      if (rawData.enemyName != startingName) {
         deleteOldData(new BattlerData { enemyName = startingName });
      }

      rawData.battlerLootData = getRawLootData();

      foreach (AttackAbilityData attackAbility in rawData.battlerAbilities.attackAbilityDataList) {
         abilityToolManager.saveAbility(attackAbility, AbilityToolManager.DirectoryType.AttackAbility);
      }
      foreach (BuffAbilityData buffAbility in rawData.battlerAbilities.buffAbilityDataList) {
         abilityToolManager.saveAbility(buffAbility, AbilityToolManager.DirectoryType.BuffAbility);
      }

      monsterToolManager.saveDataToFile(rawData);
      abilityToolManager.loadAllDataFiles();
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

   private void deleteOldData(BattlerData rawData) {
      monsterToolManager.deleteMonsterDataFile(rawData);
   }

   #endregion

   #region Stats Feature

   private void openImageSelectionPanel () {
      selectionPanel.SetActive(true);
      monsterTypeParent.DestroyChildren();

      previewSelectionIcon.sprite = emptySprite;
      foreach (KeyValuePair<string, Sprite> sourceSprite in iconSpriteList) {
         GameObject iconTempObj = Instantiate(monsterTypeTemplate.gameObject, monsterTypeParent.transform);
         ItemTypeTemplate iconTemp = iconTempObj.GetComponent<ItemTypeTemplate>();
         iconTemp.spriteIcon.sprite = sourceSprite.Value;
         iconTemp.itemTypeText.text = sourceSprite.Value.name;
         iconTemp.previewButton.onClick.AddListener(() => {
            previewSelectionIcon.sprite = sourceSprite.Value;
         });
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

      foreach (Enemy.Type category in landmonsterTypes) {
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

   private void toggleLoots () {
      dropDownContent.SetActive(!dropDownContent.activeSelf);
      dropDownLoots.SetActive(!dropDownContent.activeSelf);
   }

   private void toggleMainStats () {
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

   private void toggleSkills () {
      skillContent.SetActive(!skillContent.activeSelf);
      dropDownSkills.SetActive(!skillContent.activeSelf);
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

   public void loadLootTemplates(BattlerData rawData) {
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

   #region Skill Feature

   private void loadSkillTemplates(BattlerData battlerData) {
      skillTemplateParent.gameObject.DestroyChildren();
      monsterSkillTemplateList = new List<MonsterSkillTemplate>();
      AbilityDataRecord dataRecord = battlerData.battlerAbilities;

      if (dataRecord != null) {
         loadSkill(dataRecord.attackAbilityDataList, dataRecord.buffAbilityDataList);
      }
   }

   private void loadSkill (AttackAbilityData[] attackAbilityDataList, BuffAbilityData[] buffAbilityDataList) {
      if (attackAbilityDataList != null) {
         foreach (AttackAbilityData ability in attackAbilityDataList) {
            BattleItemData battleItemData = BattleItemData.CreateInstance(ability.itemID, ability.itemName, ability.itemDescription, ability.elementType, ability.hitAudioClipPath, ability.hitSpritesPath, ability.battleItemType, ability.classRequirement, ability.itemIconPath, ability.levelRequirement);
            BasicAbilityData basicData = BasicAbilityData.CreateInstance(battleItemData, ability.abilityCost, ability.castSpritesPath, ability.castAudioClipPath, ability.allowedStances, ability.abilityType, ability.abilityCooldown, ability.apChange, ability.FXTimePerFrame);
            AttackAbilityData attackAbility = AttackAbilityData.CreateInstance(basicData, ability.hasKnockup, ability.baseDamage, ability.hasShake, ability.abilityActionType, ability.canBeBlocked);
            finalizeAttackTemplate(attackAbility);
         }
      }
      if (buffAbilityDataList != null) {
         foreach (BuffAbilityData ability in buffAbilityDataList) {
            BattleItemData battleItemData = BattleItemData.CreateInstance(ability.itemID, ability.itemName, ability.itemDescription, ability.elementType, ability.hitAudioClipPath, ability.hitSpritesPath, ability.battleItemType, ability.classRequirement, ability.itemIconPath, ability.levelRequirement);
            BasicAbilityData basicData = BasicAbilityData.CreateInstance(battleItemData, ability.abilityCost, ability.castSpritesPath, ability.castAudioClipPath, ability.allowedStances, ability.abilityType, ability.abilityCooldown, ability.apChange, ability.FXTimePerFrame);
            BuffAbilityData buffAbility = BuffAbilityData.CreateInstance(basicData, ability.duration, ability.buffType, ability.buffActionType, ability.iconPath, ability.value);
            finalizeBuffTemplate(buffAbility);
         }
      }
   }

   private void addSkillTemplate(AbilityType type) {
      switch(type) {
         case AbilityType.Standard: {
               // Basic data set
               BattleItemData battleItemData = BattleItemData.CreateInstance(1, "Name", "Desc", Element.ALL, null, null, BattleItemType.UNDEFINED, Weapon.Class.Any, String.Empty, 1);
               BasicAbilityData basicData = BasicAbilityData.CreateInstance(battleItemData, 1, null, "", new BattlerBehaviour.Stance[] { }, AbilityType.Standard, 1, 1, 1);
               AttackAbilityData attackData = AttackAbilityData.CreateInstance(basicData, false, 0, false, AbilityActionType.UNDEFINED, false);
               finalizeAttackTemplate(attackData);
            }
            break;
         case AbilityType.BuffDebuff: {
               // Basic data set
               BattleItemData battleItemData = BattleItemData.CreateInstance(1, "Name", "Desc", Element.ALL, null, null, BattleItemType.UNDEFINED, Weapon.Class.Any, String.Empty, 1);
               BasicAbilityData basicData = BasicAbilityData.CreateInstance(battleItemData, 1, null, "", new BattlerBehaviour.Stance[] { }, AbilityType.BuffDebuff, 1, 1, 1);
               BuffAbilityData buffData = BuffAbilityData.CreateInstance(basicData, 1, BuffType.UNDEFINED, BuffActionType.UNDEFINED, string.Empty, 0);
               finalizeBuffTemplate(buffData);
            }
            break;
      }
   }

   private void finalizeAttackTemplate (AttackAbilityData attackAbility) {
      GameObject template = Instantiate(skillTemplatePrefab, skillTemplateParent);
      MonsterSkillTemplate skillTemplate = template.GetComponent<MonsterSkillTemplate>();
      skillTemplate.deleteSkillButton.onClick.AddListener(() => {
         MonsterSkillTemplate monsterTemp = monsterSkillTemplateList.Find(_ => _ == skillTemplate);
         monsterSkillTemplateList.Remove(monsterTemp);
         Destroy(template);
      });
      skillTemplate.abilityTypeEnum = AbilityType.Standard;
      skillTemplate.loadAttackData(attackAbility);
      monsterSkillTemplateList.Add(skillTemplate);
   }

   private void finalizeBuffTemplate (BuffAbilityData buffAbility) {
      GameObject template = Instantiate(skillTemplatePrefab, skillTemplateParent);
      MonsterSkillTemplate skillTemplate = template.GetComponent<MonsterSkillTemplate>();
      skillTemplate.deleteSkillButton.onClick.AddListener(() => {
         MonsterSkillTemplate monsterTemp = monsterSkillTemplateList.Find(_ => _ == skillTemplate);
         monsterSkillTemplateList.Remove(monsterTemp);
         Destroy(template);
      });
      skillTemplate.abilityTypeEnum = AbilityType.BuffDebuff;
      skillTemplate.loadBuffData(buffAbility);
      monsterSkillTemplateList.Add(skillTemplate);
   }

   #endregion

   #region Private Variables

#pragma warning disable 0649 
   // Main parameters
   [SerializeField] private InputField _name;

   // Base battler parameters
   [SerializeField] private InputField _preContactLength;
   [SerializeField] private InputField _preMagicLength;

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
#pragma warning restore 0649 

   #endregion
}
