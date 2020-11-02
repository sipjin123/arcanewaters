using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Linq;
using static MonsterSkillTemplate;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class MonsterDataPanel : MonoBehaviour
{
   #region Public Variables

   // The generic popup reference
   public GenericSelectionPopup genericPopup;

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

   // The skill count of this unit
   public int skillIndex;

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
   public Dictionary<string, Sprite> projectileSpriteList = new Dictionary<string, Sprite>();

   // Item Loot Variables
   public GameObject lootSelectionPanel;
   public GameObject lootTemplateParent;
   public GameObject itemCategoryParent, itemTypeParent;
   public ItemCategoryTemplate itemCategoryTemplate;
   public ItemTypeTemplate itemTypeTemplate;
   public Button confirmItemButton, addLootButton, closeItemPanelButton;
   public Item.Category selectedCategory;
   public int itemTypeIDSelected;
   public string itemDataSelected;
   public Sprite emptySprite;

   // Skills Variables
   public Button addATKSkillButton, addDEFSkillButton, addTemplateSkillButton;
   public Transform skillTemplateParent;
   public GameObject skillTemplatePrefab;
   List<MonsterSkillTemplate> monsterSkillTemplateList = new List<MonsterSkillTemplate>();

   // Audio Related updates
   public Text jumpSoundEffectName;
   public Text deathSoundEffectName;
   public Button selectJumpAudioButton;
   public Button selectDeathAudioButton;
   public Button playJumpAudioButton;
   public Button playDeathAudioButton;
   public SoundEffect jumpSoundEffect, deathSoundEffect;

   // Audio source to play the sample clips
   public AudioSource audioSource;

   // Reference to current xml id
   public int currentXmlId;

   // Toggler to determine if this sql data is active in the database
   public Toggle xml_toggler;

   // Toggler to determine if this enemy is a boss
   public Toggle isBossToggler;

   // Toggler to determine if this enemy is a support
   public Toggle isSupportType;

   // Error Panel
   public GameObject errorPanel;
   public Button closeErrorPanel;

   // Anim group altering
   public Slider animGroupSlider;
   public Text animGroupText;

   // Variables that will be hidden if the enemy is a boss type
   public GameObject[] nonBossTypeVariables;

   // The in game texture
   public SpriteRenderer inGameSprite;

   // Reference to the shadow
   public Transform shadowTransform;

   // The offset and scale inputfield of the shadow transform
   public InputField shadowXOffset, shadowYOffset, shadowScale;

   // Loot group UI requirements
   public Button lootGroupButton;
   public Text lootGroupText, lootGroupIndexText;
   int lootGroupIdSelected = 0;
   public Transform lootGroupParent;
   public GameObject lootGroupPrefab;
   public UnityEvent lootGroupSelected = new UnityEvent();

   #endregion

   private void Awake () {
      if (!MasterToolAccountManager.canAlterData()) {
         saveExitButton.gameObject.SetActive(false);
      }

      lootGroupButton.onClick.AddListener(() => {
         lootGroupSelected.AddListener(() => {
            lootGroupIdSelected = int.Parse(lootGroupIndexText.text);
            loadLootGroupById(lootGroupIdSelected);
         });
         genericPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.LootGroupsLandMonsters, lootGroupIndexText, lootGroupSelected);
      });
      
      revertButton.onClick.AddListener(() => {
         monsterToolManager.loadAllDataFiles();
         abilityToolManager.loadXML();
         gameObject.SetActive(false);
      });
      addATKSkillButton.onClick.AddListener(() => addSkillTemplate(AbilityType.Standard));
      addDEFSkillButton.onClick.AddListener(() => addSkillTemplate(AbilityType.BuffDebuff));
      addTemplateSkillButton.onClick.AddListener(() => openSkillSelectionPanel());
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

      shadowScale.onValueChanged.AddListener(_ => {
         try {
            float floatVal = float.Parse(_);
            shadowTransform.localScale = new Vector2(floatVal, floatVal);
         } catch {

         }
      });

      shadowXOffset.onValueChanged.AddListener(_ => {
         try {
            float floatVal = float.Parse(_);
            shadowTransform.localPosition = new Vector3(floatVal, shadowTransform.localPosition.y, 0.1f);
         } catch {

         }
      });

      shadowYOffset.onValueChanged.AddListener(_ => {
         try {
            float floatVal = float.Parse(_);
            shadowTransform.localPosition = new Vector3(shadowTransform.localPosition.x, floatVal, 0.1f);
         } catch {

         }
      });

      animGroupSlider.maxValue = Enum.GetValues(typeof(Anim.Group)).Length;
      animGroupSlider.onValueChanged.AddListener(_ => {
         animGroupText.text = ((Anim.Group) _).ToString();
      });
      animGroupSlider.value = 0;

      selectJumpAudioButton.onClick.AddListener(() => toggleAudioSelection(PathType.JumpSfx));
      selectDeathAudioButton.onClick.AddListener(() => toggleAudioSelection(PathType.DeathSfx));
      playJumpAudioButton.onClick.AddListener(() => {
         if (jumpSoundEffect != null) {
            audioSource.clip = jumpSoundEffect.clip;
            jumpSoundEffect.calibrateSource(audioSource);
            audioSource.loop = false;
            audioSource.Play();
         }
      });
      playDeathAudioButton.onClick.AddListener(() => {
         if (deathSoundEffect != null) {
            audioSource.clip = deathSoundEffect.clip;
            deathSoundEffect.calibrateSource(audioSource);
            audioSource.loop = false;
            audioSource.Play();
         }
      });
      closeErrorPanel.onClick.AddListener(() => {
         errorPanel.SetActive(false);
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

      foreach (SoundEffect effect in SoundEffectManager.self.getAllSoundEffects()) {
         GameObject iconTempObj = Instantiate(monsterTypeTemplate.gameObject, monsterTypeParent.transform);
         ItemTypeTemplate iconTemp = iconTempObj.GetComponent<ItemTypeTemplate>();
         iconTemp.itemTypeText.text = effect.name;

         iconTemp.previewButton.onClick.AddListener(() => {
            audioSource.clip = effect.clip;
            effect.calibrateSource(audioSource);
            audioSource.loop = false;
            audioSource.Play();
         });

         iconTemp.selectButton.onClick.AddListener(() => {
            switch (pathType) {
               case PathType.DeathSfx:
                  deathSoundEffect = effect;
                  if (deathSoundEffect != null) {
                     deathSoundEffectName.text = effect.name;
                  }
                  break;
               case PathType.JumpSfx:
                  jumpSoundEffect = effect;
                  if (jumpSoundEffect != null) {
                     jumpSoundEffectName.text = effect.name;
                  }
                  break;
            }
            closeAvatarSelectionButton.onClick.Invoke();
         });
      }
   }

   #region Save and Load Data

   public void loadData (BattlerData newBattleData, int xml_id, bool isActive) {
      xml_toggler.isOn = isActive;
      isBossToggler.isOn = newBattleData.isBossType;
      isSupportType.isOn = newBattleData.isSupportType;
      currentXmlId = xml_id;
      startingName = newBattleData.enemyName;
      monsterTypeText.text = ((Enemy.Type) newBattleData.enemyType).ToString();
      animGroupText.text = ((Anim.Group) newBattleData.animGroup).ToString();
      animGroupSlider.value = (int) newBattleData.animGroup;

      try {
         avatarIcon.sprite = ImageManager.getSprite(newBattleData.imagePath);
         inGameSprite.sprite = avatarIcon.sprite;
      } catch {
         avatarIcon.sprite = emptySprite;
         inGameSprite.sprite = avatarIcon.sprite;
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

      _physicalDefenseMultiplier.text = newBattleData.baseDefenseMultiplierSet.physicalDefenseMultiplier.ToString();
      _fireDefenseMultiplier.text = newBattleData.baseDefenseMultiplierSet.fireDefenseMultiplier.ToString();
      _earthDefenseMultiplier.text = newBattleData.baseDefenseMultiplierSet.earthDefenseMultiplier.ToString();
      _airDefenseMultiplier.text = newBattleData.baseDefenseMultiplierSet.airDefenseMultiplier.ToString();
      _waterDefenseMultiplier.text = newBattleData.baseDefenseMultiplierSet.waterDefenseMultiplier.ToString();
      _allDefenseMultiplier.text = newBattleData.baseDefenseMultiplierSet.allDefenseMultiplier.ToString();

      _physicalAttackMultiplier.text = newBattleData.baseDamageMultiplierSet.physicalAttackMultiplier.ToString();
      _fireAttackMultiplier.text = newBattleData.baseDamageMultiplierSet.fireAttackMultiplier.ToString();
      _earthAttackMultiplier.text = newBattleData.baseDamageMultiplierSet.earthAttackMultiplier.ToString();
      _airAttackMultiplier.text = newBattleData.baseDamageMultiplierSet.airAttackMultiplier.ToString();
      _waterAttackMultiplier.text = newBattleData.baseDamageMultiplierSet.waterAttackMultiplier.ToString();
      _allAttackMultiplier.text = newBattleData.baseDamageMultiplierSet.allAttackMultiplier.ToString();

      _physicalDefenseMultiplierPerLevel.text = newBattleData.perLevelDefenseMultiplierSet.physicalDefenseMultiplierPerLevel.ToString();
      _fireDefenseMultiplierPerLevel.text = newBattleData.perLevelDefenseMultiplierSet.fireDefenseMultiplierPerLevel.ToString();
      _earthDefenseMultiplierPerLevel.text = newBattleData.perLevelDefenseMultiplierSet.earthDefenseMultiplierPerLevel.ToString();
      _airDefenseMultiplierPerLevel.text = newBattleData.perLevelDefenseMultiplierSet.airDefenseMultiplierPerLevel.ToString();
      _waterDefenseMultiplierPerLevel.text = newBattleData.perLevelDefenseMultiplierSet.waterDefenseMultiplierPerLevel.ToString();
      _allDefenseMultiplierPerLevel.text = newBattleData.perLevelDefenseMultiplierSet.allDefenseMultiplierPerLevel.ToString();

      _physicalAttackMultiplierPerLevel.text = newBattleData.perLevelDamageMultiplierSet.physicalAttackMultiplierPerLevel.ToString();
      _fireAttackMultiplierPerLevel.text = newBattleData.perLevelDamageMultiplierSet.fireAttackMultiplierPerLevel.ToString();
      _earthAttackMultiplierPerLevel.text = newBattleData.perLevelDamageMultiplierSet.earthAttackMultiplierPerLevel.ToString();
      _airAttackMultiplierPerLevel.text = newBattleData.perLevelDamageMultiplierSet.airAttackMultiplierPerLevel.ToString();
      _waterAttackMultiplierPerLevel.text = newBattleData.perLevelDamageMultiplierSet.waterAttackMultiplierPerLevel.ToString();
      _allAttackMultiplierPerLevel.text = newBattleData.perLevelDamageMultiplierSet.allAttackMultiplierPerLevel.ToString();
      shadowScale.text = newBattleData.shadowScale.ToString("f2");
      shadowXOffset.text = newBattleData.shadowOffset.x.ToString("f2");
      shadowYOffset.text = newBattleData.shadowOffset.y.ToString("f2");

      _preContactLength.text = newBattleData.preContactLength.ToString();
      _preMagicLength.text = newBattleData.preMagicLength.ToString();

      deathSoundEffect = SoundEffectManager.self.getSoundEffect(newBattleData.deathSoundEffectId);
      jumpSoundEffect = SoundEffectManager.self.getSoundEffect(newBattleData.jumpSoundEffectId);

      lootGroupIdSelected = newBattleData.lootGroupId;
      if (lootGroupIdSelected > 0) {
         loadLootGroupById(lootGroupIdSelected);
      } else {
         lootGroupText.text = "";
         lootGroupParent.gameObject.DestroyChildren();
      }

      jumpSoundEffectName.text = "";
      if (jumpSoundEffect != null) {
         jumpSoundEffectName.text = jumpSoundEffect.name;
      }

      deathSoundEffectName.text = "";
      if (deathSoundEffect != null) {
         deathSoundEffectName.text = deathSoundEffect.name;
      }

      _name.text = newBattleData.enemyName;

      _disableOnDeath.isOn = newBattleData.disableOnDeath;

      loadSkillTemplates(newBattleData);
   }

   private BattlerData getBattlerData () {
      BattlerData newBattData = new BattlerData();

      newBattData.enemyType = (Enemy.Type) Enum.Parse(typeof(Enemy.Type), monsterTypeText.text);
      newBattData.animGroup = (Anim.Group) Enum.Parse(typeof(Anim.Group), animGroupText.text);
      newBattData.imagePath = avatarIconPath;
      newBattData.isBossType = isBossToggler.isOn;
      newBattData.isSupportType = isSupportType.isOn;

      newBattData.baseHealth = int.Parse(_baseHealth.text);
      newBattData.baseDefense = int.Parse(_baseDefense.text);
      newBattData.baseDamage = int.Parse(_baseDamage.text);
      newBattData.baseGoldReward = int.Parse(_baseGoldReward.text);
      newBattData.baseXPReward = int.Parse(_baseXPReward.text);
      newBattData.damagePerLevel = int.Parse(_damagePerLevel.text);
      newBattData.defensePerLevel = int.Parse(_defensePerLevel.text);
      newBattData.healthPerlevel = int.Parse(_healthPerlevel.text);

      newBattData.baseDefenseMultiplierSet.physicalDefenseMultiplier = float.Parse(_physicalDefenseMultiplier.text);
      newBattData.baseDefenseMultiplierSet.allDefenseMultiplier = float.Parse(_allDefenseMultiplier.text);
      newBattData.baseDefenseMultiplierSet.fireDefenseMultiplier = float.Parse(_fireDefenseMultiplier.text);
      newBattData.baseDefenseMultiplierSet.earthDefenseMultiplier = float.Parse(_earthDefenseMultiplier.text);
      newBattData.baseDefenseMultiplierSet.waterDefenseMultiplier = float.Parse(_waterDefenseMultiplier.text);
      newBattData.baseDefenseMultiplierSet.airDefenseMultiplier = float.Parse(_airDefenseMultiplier.text);

      newBattData.baseDamageMultiplierSet.physicalAttackMultiplier = float.Parse(_physicalAttackMultiplier.text);
      newBattData.baseDamageMultiplierSet.allAttackMultiplier = float.Parse(_allAttackMultiplier.text);
      newBattData.baseDamageMultiplierSet.fireAttackMultiplier = float.Parse(_fireAttackMultiplier.text);
      newBattData.baseDamageMultiplierSet.earthAttackMultiplier = float.Parse(_earthAttackMultiplier.text);
      newBattData.baseDamageMultiplierSet.waterAttackMultiplier = float.Parse(_waterAttackMultiplier.text);
      newBattData.baseDamageMultiplierSet.airAttackMultiplier = float.Parse(_airAttackMultiplier.text);

      newBattData.perLevelDefenseMultiplierSet.physicalDefenseMultiplierPerLevel = float.Parse(_physicalDefenseMultiplierPerLevel.text);
      newBattData.perLevelDefenseMultiplierSet.allDefenseMultiplierPerLevel = float.Parse(_allDefenseMultiplierPerLevel.text);
      newBattData.perLevelDefenseMultiplierSet.fireDefenseMultiplierPerLevel = float.Parse(_fireDefenseMultiplierPerLevel.text);
      newBattData.perLevelDefenseMultiplierSet.earthDefenseMultiplierPerLevel = float.Parse(_earthDefenseMultiplierPerLevel.text);
      newBattData.perLevelDefenseMultiplierSet.waterDefenseMultiplierPerLevel = float.Parse(_waterDefenseMultiplierPerLevel.text);
      newBattData.perLevelDefenseMultiplierSet.airDefenseMultiplierPerLevel = float.Parse(_airDefenseMultiplierPerLevel.text);

      newBattData.perLevelDamageMultiplierSet.physicalAttackMultiplierPerLevel = float.Parse(_physicalAttackMultiplierPerLevel.text);
      newBattData.perLevelDamageMultiplierSet.allAttackMultiplierPerLevel = float.Parse(_allAttackMultiplierPerLevel.text);
      newBattData.perLevelDamageMultiplierSet.fireAttackMultiplierPerLevel = float.Parse(_fireAttackMultiplierPerLevel.text);
      newBattData.perLevelDamageMultiplierSet.earthAttackMultiplierPerLevel = float.Parse(_earthAttackMultiplierPerLevel.text);
      newBattData.perLevelDamageMultiplierSet.waterAttackMultiplierPerLevel = float.Parse(_waterAttackMultiplierPerLevel.text);
      newBattData.perLevelDamageMultiplierSet.airAttackMultiplierPerLevel = float.Parse(_airAttackMultiplierPerLevel.text);

      newBattData.preContactLength = int.Parse(_preContactLength.text);
      newBattData.preMagicLength = int.Parse(_preMagicLength.text);
      newBattData.shadowOffset = new Vector3 (float.Parse(shadowXOffset.text), float.Parse(shadowYOffset.text), 0.1f);
      newBattData.shadowScale = float.Parse(shadowScale.text);

      newBattData.deathSoundEffectId = deathSoundEffect.id;
      newBattData.jumpSoundEffectId = jumpSoundEffect.id;

      newBattData.enemyName = _name.text;
      newBattData.lootGroupId = lootGroupIdSelected;

      List<int> basicAbilityList = new List<int>();
      List<int> attackAbilityList = new List<int>();
      List<int> buffAbilityList = new List<int>();
      foreach (MonsterSkillTemplate skillTemplate in monsterSkillTemplateList) {
         if (skillTemplate.abilityTypeEnum == AbilityType.Standard) {
            attackAbilityList.Add(skillTemplate.getAttackData().itemID);
            basicAbilityList.Add(skillTemplate.getAttackData().itemID);
         } else if (skillTemplate.abilityTypeEnum == AbilityType.BuffDebuff) {
            buffAbilityList.Add(skillTemplate.getBuffData().itemID);
            basicAbilityList.Add(skillTemplate.getBuffData().itemID);
         }
      }
      newBattData.battlerAbilities = new AbilityDataRecord {
         basicAbilityDataList = basicAbilityList.ToArray(),
         attackAbilityDataList = attackAbilityList.ToArray(),
         buffAbilityDataList = buffAbilityList.ToArray()
      };

      newBattData.disableOnDeath = _disableOnDeath.isOn;

      return newBattData;
   }

   public void saveData () {
      BattlerData battlerData = getBattlerData();

      if (isValidData(battlerData)) {
         monsterToolManager.saveDataToFile(battlerData, currentXmlId, xml_toggler.isOn);
         gameObject.SetActive(false);
      } else {
         errorPanel.SetActive(true);
      }
   }

   private bool isValidData (BattlerData battleData) {
      if (battleData.imagePath != string.Empty
         && battleData.enemyName != string.Empty
         && battleData.enemyType != Enemy.Type.None) {
         return true;
      }

      return false;
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
            inGameSprite.sprite = avatarIcon.sprite;
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

   private void loadLootGroupById (int groupId) {
      LootGroupData lootGroupData = monsterToolManager.lootGroupDataCollection[groupId];
      lootGroupText.text = lootGroupData.lootGroupName;

      lootGroupParent.gameObject.DestroyChildren();
      foreach (TreasureDropsData lootGroupItemData in lootGroupData.treasureDropsCollection) {
         TreasureDropsItemTemplate itemTemplate = Instantiate(lootGroupPrefab, lootGroupParent).GetComponent<TreasureDropsItemTemplate>();
         Item cachedItem = lootGroupItemData.item;

         itemTemplate.dropChance.text = lootGroupItemData.spawnChance.ToString();
         itemTemplate.item = cachedItem;
         itemTemplate.itemIcon.sprite = ImageManager.getSprite(cachedItem.iconPath);
         itemTemplate.itemName.text = cachedItem.itemName;
         itemTemplate.itemType.text = cachedItem.category == Item.Category.CraftingIngredients ? "Material" : cachedItem.category.ToString();
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

   #region Skill Feature

   private void loadSkillTemplates (BattlerData battlerData) {
      skillIndex = 0;
      skillTemplateParent.gameObject.DestroyChildren();
      monsterSkillTemplateList = new List<MonsterSkillTemplate>();
      AbilityDataRecord dataRecord = battlerData.battlerAbilities;

      if (dataRecord != null) {
         List<AttackAbilityData> attackAbilityData = new List<AttackAbilityData>();
         List<BuffAbilityData> buffAbilityData = new List<BuffAbilityData>();
         foreach (int abilityId in dataRecord.attackAbilityDataList) {
            AttackAbilityData attackData = monsterToolManager.attackAbilityList.Find(_ => _.itemID == abilityId);
            if (attackData != null) {
               attackAbilityData.Add(attackData);
            } else {
               D.editorLog("Attack is null: " + abilityId, Color.red);
            }
         }
         foreach (int abilityId in dataRecord.buffAbilityDataList) {
            BuffAbilityData buffData = monsterToolManager.buffAbilityList.Find(_ => _.itemID == abilityId);
            if (buffData != null) {
               buffAbilityData.Add(buffData);
            } else {
               D.editorLog("Buff is null: " + abilityId, Color.red);
            }
         }
         loadSkill(attackAbilityData.ToArray(), buffAbilityData.ToArray());
      }
   }

   private void loadSkill (AttackAbilityData[] attackAbilityDataList, BuffAbilityData[] buffAbilityDataList) {
      if (attackAbilityDataList != null) {
         foreach (AttackAbilityData referenceAbility in attackAbilityDataList) {
            AttackAbilityData ability = monsterToolManager.attackAbilityList.Find(_ => _.itemName == referenceAbility.itemName);
            BattleItemData battleItemData = BattleItemData.CreateInstance(ability.itemID, ability.itemName, ability.itemDescription, ability.elementType, ability.hitSoundEffectId, ability.hitSpritesPath, ability.battleItemType, ability.classRequirement, ability.itemIconPath, ability.levelRequirement);
            BasicAbilityData basicData = BasicAbilityData.CreateInstance(battleItemData, ability.abilityCost, ability.castSpritesPath, ability.castSoundEffectId, ability.allowedStances, ability.abilityType, ability.abilityCooldown, ability.apChange, ability.FXTimePerFrame, ability.abilityCastPosition, ability.hitFXTimePerFrame);
            AttackAbilityData attackAbility = AttackAbilityData.CreateInstance(basicData, ability.hasKnockup, ability.baseDamage, ability.hasShake, ability.abilityActionType, ability.canBeBlocked, ability.hasKnockBack, ability.projectileSpeed, ability.projectileSpritePath, ability.projectileScale, ability.useCustomProjectileSprite);
            finalizeAttackTemplate(attackAbility);
         }
      }
      if (buffAbilityDataList != null) {
         foreach (BuffAbilityData referenceAbility in buffAbilityDataList) {
            BuffAbilityData ability = monsterToolManager.buffAbilityList.Find(_ => _.itemName == referenceAbility.itemName);
            if (ability != null) {
               BattleItemData battleItemData = BattleItemData.CreateInstance(ability.itemID, ability.itemName, ability.itemDescription, ability.elementType, ability.hitSoundEffectId, ability.hitSpritesPath, ability.battleItemType, ability.classRequirement, ability.itemIconPath, ability.levelRequirement);
               BasicAbilityData basicData = BasicAbilityData.CreateInstance(battleItemData, ability.abilityCost, ability.castSpritesPath, ability.castSoundEffectId, ability.allowedStances, ability.abilityType, ability.abilityCooldown, ability.apChange, ability.FXTimePerFrame, ability.abilityCastPosition, ability.hitFXTimePerFrame);
               BuffAbilityData buffAbility = BuffAbilityData.CreateInstance(basicData, ability.duration, ability.buffType, ability.buffActionType, ability.iconPath, ability.value, ability.bonusStatType);
               finalizeBuffTemplate(buffAbility);
            }
         }
      }
   }

   private void addSkillTemplate (AbilityType type) {
      switch (type) {
         case AbilityType.Standard: {
               // Basic data set
               BattleItemData battleItemData = BattleItemData.CreateInstance(1, "Name", "Desc", Element.ALL, -1, null, BattleItemType.UNDEFINED, Weapon.Class.Any, String.Empty, 1);
               BasicAbilityData basicData = BasicAbilityData.CreateInstance(battleItemData, 1, null, -1, new Battler.Stance[] { }, AbilityType.Standard, 1, 1, 1, BasicAbilityData.AbilityCastPosition.Self, .1f);
               AttackAbilityData attackData = AttackAbilityData.CreateInstance(basicData, false, 0, false, AbilityActionType.UNDEFINED, false, false, 2, null, 1, true);
               finalizeAttackTemplate(attackData);
            }
            break;
         case AbilityType.BuffDebuff: {
               // Basic data set
               BattleItemData battleItemData = BattleItemData.CreateInstance(1, "Name", "Desc", Element.ALL, -1, null, BattleItemType.UNDEFINED, Weapon.Class.Any, String.Empty, 1);
               BasicAbilityData basicData = BasicAbilityData.CreateInstance(battleItemData, 1, null, -1, new Battler.Stance[] { }, AbilityType.BuffDebuff, 1, 1, 1, BasicAbilityData.AbilityCastPosition.Self, .1f);
               BuffAbilityData buffData = BuffAbilityData.CreateInstance(basicData, 1, BuffType.UNDEFINED, BuffActionType.UNDEFINED, string.Empty, 0, BonusStatType.None);
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
      skillTemplate.openAbilityButton.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.abilityScene);
      });

      skillTemplate.templateNumber.text = skillIndex.ToString();
      skillTemplate.abilityTypeEnum = AbilityType.Standard;
      skillTemplate.loadAttackData(attackAbility);
      monsterSkillTemplateList.Add(skillTemplate);
      skillIndex++;
   }

   private void finalizeBuffTemplate (BuffAbilityData buffAbility) {
      GameObject template = Instantiate(skillTemplatePrefab, skillTemplateParent);
      MonsterSkillTemplate skillTemplate = template.GetComponent<MonsterSkillTemplate>();
      skillTemplate.deleteSkillButton.onClick.AddListener(() => {
         MonsterSkillTemplate monsterTemp = monsterSkillTemplateList.Find(_ => _ == skillTemplate);
         monsterSkillTemplateList.Remove(monsterTemp);
         Destroy(template);
      });
      skillTemplate.openAbilityButton.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.abilityScene);
      });

      skillTemplate.templateNumber.text = skillIndex.ToString();
      skillTemplate.abilityTypeEnum = AbilityType.BuffDebuff;
      skillTemplate.loadBuffData(buffAbility);
      monsterSkillTemplateList.Add(skillTemplate);
      skillIndex++;
   }

   #endregion

   #region Private Variables

#pragma warning disable 0649 
   // Main parameters
   [SerializeField] private InputField _name;

   // Toggle variables
   [SerializeField] private Toggle _disableOnDeath;

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

   // Element defense multiplier values PerLevel
   [SerializeField] private InputField _physicalDefenseMultiplierPerLevel;
   [SerializeField] private InputField _fireDefenseMultiplierPerLevel;
   [SerializeField] private InputField _earthDefenseMultiplierPerLevel;
   [SerializeField] private InputField _airDefenseMultiplierPerLevel;
   [SerializeField] private InputField _waterDefenseMultiplierPerLevel;
   [SerializeField] private InputField _allDefenseMultiplierPerLevel;

   // Element attack multiplier values PerLevel
   [SerializeField] private InputField _physicalAttackMultiplierPerLevel;
   [SerializeField] private InputField _fireAttackMultiplierPerLevel;
   [SerializeField] private InputField _earthAttackMultiplierPerLevel;
   [SerializeField] private InputField _airAttackMultiplierPerLevel;
   [SerializeField] private InputField _waterAttackMultiplierPerLevel;
   [SerializeField] private InputField _allAttackMultiplierPerLevel;
#pragma warning restore 0649 

   #endregion
}
