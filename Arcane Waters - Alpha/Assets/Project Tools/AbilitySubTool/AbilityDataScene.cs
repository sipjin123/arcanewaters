using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.SceneManagement;
using System.Linq;

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

   // Quick Access to monster tool
   public Button[] openMonsterTool;

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
   public Dictionary<string, Sprite> projectileSpriteList = new Dictionary<string, Sprite>();

   // List of id's that have been spawned
   public List<int> idList;

   // Error Panel
   public GameObject errorPanel;
   public Button closeErrorPanel;

   #endregion

   private void Start () {
      abilityPanel.SetActive(false);

      closeErrorPanel.onClick.AddListener(() => {
         errorPanel.SetActive(false);
      });

      if (!MasterToolAccountManager.canAlterData()) {
         saveButton.gameObject.SetActive(false);
         createTemplateButton.gameObject.SetActive(false);
      }

      cancelButton.onClick.AddListener(() => {
         abilityPanel.SetActive(false);
         abilityManager.loadXML();
      });
      openMainTool.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.masterScene);
      });
      foreach (Button button in openMonsterTool) {
         button.onClick.AddListener(() => {
            SceneManager.LoadScene(MasterToolScene.monsterScene);
         });
      }
      addATKSkillButton.onClick.AddListener(() => addSkillTemplate(AbilityType.Standard));
      addDEFSkillButton.onClick.AddListener(() => addSkillTemplate(AbilityType.BuffDebuff));
      createTemplateButton.onClick.AddListener(() => createNewTemplate(new BasicAbilityData { itemID = -1, itemName = MasterToolScene.UNDEFINED }));
      saveButton.onClick.AddListener(() => saveXML());

      if (!hasBeenInitialized) {
         hasBeenInitialized = true;
         string spritePath = "Assets/Sprites/Icons/Abilities/";
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

         string projectileSpritePath = "Assets/Sprites/Projectiles/";
         List<ImageManager.ImageData> projecitleSpriteIconFiles = ImageManager.getSpritesInDirectory(projectileSpritePath);

         foreach (ImageManager.ImageData imgData in projecitleSpriteIconFiles) {
            Sprite sourceSprite = imgData.sprite;
            projectileSpriteList.Add(imgData.imagePath, sourceSprite);
         }
      }
   }

   private void createNewTemplate (BasicAbilityData abilityData) {
      AbilityDataTemplate template = GenericEntryTemplate.createGenericTemplate(abilityTemplate.gameObject, abilityManager, abilityTemplateParent.transform).GetComponent<AbilityDataTemplate>();
      template.setWarning();
      template.editButton.onClick.AddListener(() => {
         _startingName = abilityData.itemName;
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
      if (skillTemplateList.Count < 1) {
         abilityManager.loadXML();
         Debug.LogError("No skill yet");
         return;
      }

      MonsterSkillTemplate skillTemplate = skillTemplateList[0];
      if (!idList.Exists(_ => _ == skillTemplate.skillID)) {
         if (skillTemplateList[0].abilityTypeEnum == AbilityType.Standard || skillTemplateList[0].abilityTypeEnum == AbilityType.Stance) {
            AttackAbilityData attackData = skillTemplate.getAttackData();
            abilityManager.saveAbility(attackData);
         } else if (skillTemplateList[0].abilityTypeEnum == AbilityType.BuffDebuff) {
            BuffAbilityData buffData = skillTemplate.getBuffData();
            abilityManager.saveAbility(buffData);
         }
            
         abilityPanel.SetActive(false);
      } else {
         errorPanel.SetActive(true);
      }
   }

   public void deleteAbility (int skillId) {
      abilityManager.deleteSkillDataFile(skillId);
   }

   public void updateWithAbilityData (Dictionary<int, BasicAbilityData> basicAbilityData, Dictionary<int, AttackAbilityData> attackData, Dictionary<int, BuffAbilityData> buffData) {
      // Clear all the rows
      abilityTemplateParent.gameObject.DestroyChildren();
      skillTemplateList = new List<MonsterSkillTemplate>();
      idList = new List<int>();

      List<AttackAbilityData> sortedAttackData = attackData.Values.ToList().OrderBy(w => w.itemID).ToList();
      List<BuffAbilityData> sortedBuffData = buffData.Values.ToList().OrderBy(w => w.itemID).ToList();
      List<BasicAbilityData> sortedBasicData = basicAbilityData.Values.ToList().OrderBy(w => w.itemID).ToList();

      foreach (AttackAbilityData abilityData in sortedAttackData) {
         if (abilityData.abilityType != AbilityType.Stance) {
            AbilityDataTemplate template = GenericEntryTemplate.createGenericTemplate(abilityTemplate.gameObject, abilityManager, abilityTemplateParent.transform).GetComponent<AbilityDataTemplate>();
            template.editButton.onClick.AddListener(() => {
               _startingName = abilityData.itemName;
               loadAttackData(abilityData);
               abilityPanel.SetActive(true);
            });

            template.deleteButton.onClick.AddListener(() => {
               deleteAbility(abilityData.itemID);
               Destroy(template.gameObject, .5f);
            });

            template.duplicateButton.onClick.AddListener(() => {
               abilityManager.duplicateFile(abilityData);
            });

            finalizeTemplate(template, abilityData);
         }
      }

      foreach (BuffAbilityData abilityData in sortedBuffData) {
         AbilityDataTemplate template = GenericEntryTemplate.createGenericTemplate(abilityTemplate.gameObject, abilityManager, abilityTemplateParent.transform).GetComponent<AbilityDataTemplate>();
         template.editButton.onClick.AddListener(() => {
            _startingName = abilityData.itemName;
            loadBuffData(abilityData);
            abilityPanel.SetActive(true);
         });
         template.deleteButton.onClick.AddListener(() => {
            deleteAbility(abilityData.itemID);
            Destroy(template.gameObject, .5f);
         });
         template.duplicateButton.onClick.AddListener(() => {
            abilityManager.duplicateFile(abilityData);
         });

         finalizeTemplate(template, abilityData);
      }

      foreach (BasicAbilityData abilityData in sortedBasicData) {
         if (abilityData.abilityType == AbilityType.Stance) {
            AbilityDataTemplate template = GenericEntryTemplate.createGenericTemplate(abilityTemplate.gameObject, abilityManager, abilityTemplateParent.transform).GetComponent<AbilityDataTemplate>();
            template.editButton.onClick.AddListener(() => {
               _startingName = abilityData.itemName;
               loadGenericData(abilityData);
               abilityPanel.SetActive(true);
            });
            template.deleteButton.onClick.AddListener(() => {
               deleteAbility(abilityData.itemID);
               Destroy(template.gameObject, .5f);
            });
            template.duplicateButton.onClick.AddListener(() => {
               abilityManager.duplicateFile(abilityData);
            });

            finalizeTemplate(template, abilityData);
         }
      }
   }

   private void loadGenericData (BasicAbilityData ability) {
      skillTemplateParent.gameObject.DestroyChildren();
      BattleItemData battleItemData = BattleItemData.CreateInstance(ability.itemID, ability.itemName, ability.itemDescription, ability.elementType, ability.hitSoundEffectId, ability.hitSpritesPath, ability.battleItemType, ability.classRequirement, ability.itemIconPath, ability.levelRequirement);
      BasicAbilityData basicData = BasicAbilityData.CreateInstance(battleItemData, ability.abilityCost, ability.castSpritesPath, ability.castSoundEffectId, ability.allowedStances, ability.abilityType, ability.abilityCooldown, ability.apChange, ability.FXTimePerFrame);

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
      BattleItemData battleItemData = BattleItemData.CreateInstance(ability.itemID, ability.itemName, ability.itemDescription, ability.elementType, ability.hitSoundEffectId, ability.hitSpritesPath, ability.battleItemType, ability.classRequirement, ability.itemIconPath, ability.levelRequirement);
      BasicAbilityData basicData = BasicAbilityData.CreateInstance(battleItemData, ability.abilityCost, ability.castSpritesPath, ability.castSoundEffectId, ability.allowedStances, ability.abilityType, ability.abilityCooldown, ability.apChange, ability.FXTimePerFrame);
      AttackAbilityData attackAbility = AttackAbilityData.CreateInstance(basicData, ability.hasKnockup, ability.baseDamage, ability.hasShake, ability.abilityActionType, ability.canBeBlocked, ability.hasKnockBack, ability.projectileSpeed, ability.projectileSpritePath, ability.projectileScale);
      finalizeAttackTemplate(attackAbility);
   }

   private void loadBuffData (BuffAbilityData ability) {
      skillTemplateParent.gameObject.DestroyChildren();
      BattleItemData battleItemData = BattleItemData.CreateInstance(ability.itemID, ability.itemName, ability.itemDescription, ability.elementType, ability.hitSoundEffectId, ability.hitSpritesPath, ability.battleItemType, ability.classRequirement, ability.itemIconPath, ability.levelRequirement);
      BasicAbilityData basicData = BasicAbilityData.CreateInstance(battleItemData, ability.abilityCost, ability.castSpritesPath, ability.castSoundEffectId, ability.allowedStances, ability.abilityType, ability.abilityCooldown, ability.apChange, ability.FXTimePerFrame);
      BuffAbilityData buffAbility = BuffAbilityData.CreateInstance(basicData, ability.duration, ability.buffType, ability.buffActionType, ability.iconPath, ability.value, ability.bonusStatType);
      finalizeBuffTemplate(buffAbility);
   }

   private void addSkillTemplate (AbilityType type) {
      switch (type) {
         case AbilityType.Standard: {
               // Basic data set
               BattleItemData battleItemData = BattleItemData.CreateInstance(-1, "Name", "Desc", Element.ALL, -1, null, BattleItemType.UNDEFINED, Weapon.Class.Any, String.Empty, 1);
               BasicAbilityData basicData = BasicAbilityData.CreateInstance(battleItemData, 1, null, -1, new Battler.Stance[] { }, AbilityType.Standard, 1, 1, 1);
               AttackAbilityData attackData = AttackAbilityData.CreateInstance(basicData, false, 0, false, AbilityActionType.UNDEFINED, false, false, 2, null, 1);
               finalizeAttackTemplate(attackData);
            }
            break;
         case AbilityType.BuffDebuff: {
               // Basic data set
               BattleItemData battleItemData = BattleItemData.CreateInstance(-1, "Name", "Desc", Element.ALL, -1, null, BattleItemType.UNDEFINED, Weapon.Class.Any, String.Empty, 1);
               BasicAbilityData basicData = BasicAbilityData.CreateInstance(battleItemData, 1, null, -1, new Battler.Stance[] { }, AbilityType.BuffDebuff, 1, 1, 1);
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

      idList.Add(abilityData.itemID);
      template.updateItemDisplay(abilityData);

      if (!Util.hasValidEntryName(template.nameText.text)) {
         template.setWarning();
      }

      template.editButton.onClick.AddListener(() => {
         idList.Remove(abilityData.itemID);
      });
      template.gameObject.SetActive(true);
   }

   #region Private Variables

   // Initial name before editing was done
   private string _startingName;

   #endregion
}
