using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.SceneManagement;

public class AbilityDataScene : MonoBehaviour
{
   #region Public Variables

   // Reference to the tool manager
   public AbilityToolManager abilityManager;

   // Parent holder of the ability templates
   public Transform abilityTemplateParent;

   // Ability template
   public AbilityDataTemplate abilityTemplate;

   // Button that generates a new ability template
   public Button createTemplateButton;

   // Button that saves the data
   public Button saveButton;

   // Button that cancels data setup
   public Button cancelButton;

   // Opens the main tool
   public Button openMainTool;

   // Skills Variables
   public Button addATKSkillButton, addDEFSkillButton;
   public Transform skillTemplateParent;
   public GameObject skillTemplatePrefab;
   public List<MonsterSkillTemplate> skillTemplateList = new List<MonsterSkillTemplate>();
   public GameObject addSkillBar;

   // Determines if the sprites have been initialized
   public bool hasBeenInitialized;

   // Empty Sprite reference
   public Sprite emptySprite;

   // Holds the panel of the abilities
   public GameObject abilityPanel;

   // The cache list for icon selection
   public Dictionary<string, Sprite> iconSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> castIconSpriteList = new Dictionary<string, Sprite>();
   public Dictionary<string, Sprite> hitIconSpriteList = new Dictionary<string, Sprite>();

   #endregion

   private void Start () {
      abilityPanel.SetActive(false);

      cancelButton.onClick.AddListener(() => {
         abilityPanel.SetActive(false);
         loadAllDataFiles();
      });
      openMainTool.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.masterScene);
      });
      addATKSkillButton.onClick.AddListener(() => addSkillTemplate(AbilityType.Standard));
      addDEFSkillButton.onClick.AddListener(() => addSkillTemplate(AbilityType.BuffDebuff));
      createTemplateButton.onClick.AddListener(() => createNewTemplate(new BasicAbilityData()));
      saveButton.onClick.AddListener(() => saveXML());

      if (!hasBeenInitialized) {
         hasBeenInitialized = true;
         string spritePath = "Assets/Sprites/Icons/";
         List<ImageManager.ImageData> spriteIconFiles = ImageManager.getSpritesInDirectory(spritePath);

         foreach (ImageManager.ImageData imgData in spriteIconFiles) {
            Sprite sourceSprite = imgData.sprite;
            iconSpriteList.Add(imgData.imagePath, sourceSprite);
         }

         string hitSpritePath = "Assets/Sprites/Effects/";
         List<ImageManager.ImageData> hitSpriteIconFiles = ImageManager.getSpritesInDirectory(hitSpritePath);

         foreach (ImageManager.ImageData imgData in hitSpriteIconFiles) {
            Sprite sourceSprite = imgData.sprite;
            hitIconSpriteList.Add(imgData.imagePath, sourceSprite);
         }

         string castSpritePath = "Assets/Sprites/Effects/";
         List<ImageManager.ImageData> castSpriteIconFiles = ImageManager.getSpritesInDirectory(castSpritePath);

         foreach (ImageManager.ImageData imgData in castSpriteIconFiles) {
            Sprite sourceSprite = imgData.sprite;
            castIconSpriteList.Add(imgData.imagePath, sourceSprite);
         }
      }
   }

   private void createNewTemplate (BasicAbilityData abilityData) {
      AbilityDataTemplate template = Instantiate(abilityTemplate, abilityTemplateParent);
      template.editButton.onClick.AddListener(() => {
         abilityPanel.SetActive(true);
         skillTemplateList = new List<MonsterSkillTemplate>();
         skillTemplateParent.gameObject.DestroyChildren();
         addSkillBar.SetActive(true);
      });
      template.deleteButton.onClick.AddListener(() => {
         Destroy(template);
      });

      template.gameObject.SetActive(true);
   }

   public void saveXML () {
      abilityPanel.SetActive(false);

      if (skillTemplateList.Count < 1) {
         loadAllDataFiles();
         Debug.LogError("No skill yet");
         return;
      }

      MonsterSkillTemplate skillTemplate = skillTemplateList[0];
      if (skillTemplateList[0].abilityTypeEnum == AbilityType.Standard) {
         abilityManager.saveAbility(skillTemplate.getAttackData(), AbilityToolManager.DirectoryType.AttackAbility);
      } else if (skillTemplateList[0].abilityTypeEnum == AbilityType.BuffDebuff) {
         abilityManager.saveAbility(skillTemplate.getBuffData(), AbilityToolManager.DirectoryType.BuffAbility);
      }
      loadAllDataFiles();
   }

   public void deleteAbility (BasicAbilityData data) {
      switch (data.abilityType) {
         case AbilityType.Standard:
            abilityManager.deleteSkillDataFile(data, AbilityToolManager.DirectoryType.AttackAbility);
            break;
         case AbilityType.BuffDebuff:
            abilityManager.deleteSkillDataFile(data, AbilityToolManager.DirectoryType.BuffAbility);
            break;
         default:
            abilityManager.deleteSkillDataFile(data, AbilityToolManager.DirectoryType.BasicAbility);
            break;
      }
   }

   public void loadAllDataFiles() {
      abilityManager.loadAllDataFiles();
   }

   public void updateWithAbilityData (Dictionary<string, BasicAbilityData> basicAbilityData, Dictionary<string, AttackAbilityData> attackData, Dictionary<string, BuffAbilityData> buffData) {
      // Clear all the rows
      abilityTemplateParent.gameObject.DestroyChildren();
      skillTemplateList = new List<MonsterSkillTemplate>();
      foreach (BasicAbilityData abilityData in basicAbilityData.Values) {
         AbilityDataTemplate template = Instantiate(abilityTemplate, abilityTemplateParent);
         template.editButton.onClick.AddListener(() => {
            loadGenericData(abilityData);
            abilityPanel.SetActive(true);
         });
         template.deleteButton.onClick.AddListener(() => {
            deleteAbility(new BasicAbilityData { itemName = template.actualName, abilityType = AbilityType.Undefined });
            loadAllDataFiles();
            Destroy(template.gameObject, .5f);
         });

         finalizeTemplate(template, abilityData);
      }

      foreach (AttackAbilityData abilityData in attackData.Values) {
         AbilityDataTemplate template = Instantiate(abilityTemplate, abilityTemplateParent);
         template.editButton.onClick.AddListener(() => {
            loadAttackData(abilityData);
            abilityPanel.SetActive(true);
         });
         template.deleteButton.onClick.AddListener(() => {
            deleteAbility(new BasicAbilityData { itemName = template.actualName, abilityType = AbilityType.Standard });
            loadAllDataFiles();
            Destroy(template.gameObject, .5f);
         });

         finalizeTemplate(template, abilityData);
      }

      foreach (BuffAbilityData abilityData in buffData.Values) {
         AbilityDataTemplate template = Instantiate(abilityTemplate, abilityTemplateParent);
         template.editButton.onClick.AddListener(() => {
            loadBuffData(abilityData);
            abilityPanel.SetActive(true);
         });
         template.deleteButton.onClick.AddListener(() => {
            deleteAbility(new BasicAbilityData { itemName = template.actualName, abilityType = AbilityType.BuffDebuff });
            loadAllDataFiles();
            Destroy(template.gameObject, .5f);
         });

         finalizeTemplate(template, abilityData);
      }
   }

   private void loadGenericData (BasicAbilityData ability) {
      skillTemplateParent.gameObject.DestroyChildren();
      BattleItemData battleItemData = BattleItemData.CreateInstance(ability.itemID, ability.itemName, ability.itemDescription, ability.elementType, ability.hitAudioClipPath, ability.hitSpritesPath, ability.battleItemType, ability.classRequirement, ability.itemIconPath, ability.levelRequirement);
      BasicAbilityData basicData = BasicAbilityData.CreateInstance(battleItemData, ability.abilityCost, ability.castSpritesPath, ability.castAudioClipPath, ability.allowedStances, ability.abilityType, ability.abilityCooldown, ability.apChange, ability.FXTimePerFrame);

      GameObject template = Instantiate(skillTemplatePrefab, skillTemplateParent);
      MonsterSkillTemplate skillTemplate = template.GetComponent<MonsterSkillTemplate>();
      skillTemplate.deleteSkillButton.onClick.AddListener(() => {
         MonsterSkillTemplate toRemoveSkillTemp = skillTemplateList.Find(_ => _ == skillTemplate);
         skillTemplateList.Remove(toRemoveSkillTemp);
         Destroy(template);
      });
      skillTemplate.abilityTypeEnum = AbilityType.Undefined;
      skillTemplate.loadGenericData(ability);
      skillTemplateList.Add(skillTemplate);
   }

   private void loadAttackData (AttackAbilityData ability) {
      skillTemplateParent.gameObject.DestroyChildren();
      BattleItemData battleItemData = BattleItemData.CreateInstance(ability.itemID, ability.itemName, ability.itemDescription, ability.elementType, ability.hitAudioClipPath, ability.hitSpritesPath, ability.battleItemType, ability.classRequirement, ability.itemIconPath, ability.levelRequirement);
      BasicAbilityData basicData = BasicAbilityData.CreateInstance(battleItemData, ability.abilityCost, ability.castSpritesPath, ability.castAudioClipPath, ability.allowedStances, ability.abilityType, ability.abilityCooldown, ability.apChange, ability.FXTimePerFrame);
      AttackAbilityData attackAbility = AttackAbilityData.CreateInstance(basicData, ability.hasKnockup, ability.baseDamage, ability.hasShake, ability.abilityActionType, ability.canBeBlocked);
      finalizeAttackTemplate(attackAbility);
   }

   private void loadBuffData (BuffAbilityData ability) {
      skillTemplateParent.gameObject.DestroyChildren();
      BattleItemData battleItemData = BattleItemData.CreateInstance(ability.itemID, ability.itemName, ability.itemDescription, ability.elementType, ability.hitAudioClipPath, ability.hitSpritesPath, ability.battleItemType, ability.classRequirement, ability.itemIconPath, ability.levelRequirement);
      BasicAbilityData basicData = BasicAbilityData.CreateInstance(battleItemData, ability.abilityCost, ability.castSpritesPath, ability.castAudioClipPath, ability.allowedStances, ability.abilityType, ability.abilityCooldown, ability.apChange, ability.FXTimePerFrame);
      BuffAbilityData buffAbility = BuffAbilityData.CreateInstance(basicData, ability.duration, ability.buffType, ability.buffActionType, ability.iconPath, ability.value);
      finalizeBuffTemplate(buffAbility);
   }

   private void addSkillTemplate (AbilityType type) {
      switch (type) {
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
         MonsterSkillTemplate toRemoveSkillTemp = skillTemplateList.Find(_ => _ == skillTemplate);
         skillTemplateList.Remove(toRemoveSkillTemp);
         Destroy(template);
      });
      skillTemplate.abilityTypeEnum = AbilityType.Standard;
      skillTemplate.loadAttackData(attackAbility);
      skillTemplateList.Add(skillTemplate);
      addSkillBar.SetActive(false);
   }

   private void finalizeBuffTemplate (BuffAbilityData buffAbility) {
      GameObject template = Instantiate(skillTemplatePrefab, skillTemplateParent);
      MonsterSkillTemplate skillTemplate = template.GetComponent<MonsterSkillTemplate>();
      skillTemplate.deleteSkillButton.onClick.AddListener(() => {
         MonsterSkillTemplate toRemoveSkillTemp = skillTemplateList.Find(_ => _ == skillTemplate);
         skillTemplateList.Remove(toRemoveSkillTemp);
         Destroy(template);
      });
      skillTemplate.abilityTypeEnum = AbilityType.BuffDebuff;
      skillTemplate.loadBuffData(buffAbility);
      skillTemplateList.Add(skillTemplate);
      addSkillBar.SetActive(false);
   }

   private void finalizeTemplate (AbilityDataTemplate template, BasicAbilityData abilityData) {
      try {
         template.itemIcon.sprite = ImageManager.getSprite(abilityData.itemIconPath);
      } catch {
         template.itemIcon.sprite = emptySprite;
      }
      template.updateItemDisplay(abilityData);
      template.gameObject.SetActive(true);
   }
}
