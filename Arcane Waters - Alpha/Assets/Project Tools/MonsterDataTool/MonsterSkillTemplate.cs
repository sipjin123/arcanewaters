using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using Random = UnityEngine.Random;

public class MonsterSkillTemplate : MonoBehaviour
{
   #region Public Variables

   // The unique skill id of this template
   public int skillID;

   // Reference to the monster panel
   public MonsterDataPanel monsterDataPanel;

   // Reference to the ability scene
   public AbilityDataScene abilityDataScene;

   // Audio source to play the sample clips
   public AudioSource audioSource;

   // Content Dropdown feature
   public Button toggleSkillButton;
   public GameObject dropDownIndicator;
   public GameObject[] skillContents;
   public Image previewSelectionIcon;
   public Button deleteSkillButton;

   // Quick Access to Ability Tool
   public Button openAbilityButton;

   // Primary Key variables to determine the ability
   public Text skillName;
   public AbilityType abilityTypeEnum;
   public Sprite buffSprite, attackSprite, stanceSprite;
   public Image abilityTypeIcon;
   public Text skillLabel;
   public Text templateNumber;

   // Ability Stances
   public List<Battler.Stance> stanceList = new List<Battler.Stance>();
   public GameObject stanceTemplate;
   public GameObject stanceTemplateParent;
   public Button addStanceButton;
   public List<StanceTemplate> stanceSlidierList = new List<StanceTemplate>();

   // Generic stats
   public InputField levelRequirement;
   public InputField itemID;
   public InputField itemDescription;
   public InputField itemName;
   public Slider elements;
   public Slider battleItemType;
   public Slider weaponClass;
   public Slider abilityType;
   public Text elementsText;
   public Text battleItemTypeText;
   public Text weaponClassText;
   public Text abilityTypeText;
   public InputField abilityCooldown;
   public InputField apChange;
   public InputField abilityCost;
   public InputField fxTimerPerFrame;
   public InputField hitFxTimerPerFrame;
   public Slider abilityActionType;
   public Text abilityActionTypeText;
   public Image itemIcon;
   public Text itemIconPath;
   public Button selectSkillIconButton;

   // Attack related stats
   public InputField baseDamage;
   public InputField projectileSpeed;
   public InputField projectileScale;
   public Image projectileSprite;
   public Text projectileSpritePath;
   public Button projectileSpriteButton;
   public Toggle hasShake;
   public Toggle hasKnockup;
   public Toggle canBeBlocked;
   public Toggle hasKnockBack;
   public GameObject attackStatHolder;

   // Buff related stats
   public InputField buffDuration;
   public Slider buffType;
   public Text buffTypeText;
   public Slider buffActionType;
   public Text buffActionTypeText;
   public Slider bonusStatType;
   public Text bonusStatText;
   public Text buffIconPath;
   public InputField buffValue;
   public GameObject buffStatHolder;
   public Button selectBuffIconButton;
   public Image buffIcon;

   // Sprite Selection feature
   public Text hitSpritePath;
   public Text castSpritePath;
   public Button selectCastSpriteButton, selectHitSpriteButton, closeSpriteSelectionButton;
   public ItemTypeTemplate spriteTemplate;
   public GameObject spriteSelectionParent;
   public GameObject spriteSelectionPanel;
   public Image castSprite, hitSprite;

   // Audio Selection Feature
   public Text hitSoundEffectName;
   public Text castSoundEffectName;
   public Button selectHitAudioButton;
   public Button selectCastAudioButton;
   public Button playHitAudioButton;
   public Button playCastAudioButton;
   public SoundEffect hitSoundEffect, castSoundEffect;

   // The ability cast position
   public Text abilityCastPositionText;
   public Slider abilityCastPosition;

   // Makes use of the custom projectile sprite
   public Toggle userCustomProjectileSprite;
   public GameObject projectileSpriteSelectionHolder;

   // Holds the variables only available to ability types with projectile
   public GameObject[] projectileVariables;

   // Previews Image icon determining skill type
   public Image typeIcon;

   public enum PathType
   {
      HitSprite,
      CastSprite,
      ItemIcon,
      BuffIcon,
      DeathSfx,
      JumpSfx,
      ProjectileSprite
   }

   #endregion

   #region Init

   private void EnableListeners () {
      addStanceButton.onClick.AddListener(() => addStance());
      toggleSkillButton.onClick.AddListener(() => {
         foreach (GameObject obj in skillContents) {
            obj.SetActive(!skillContents[0].activeSelf);
         }
         dropDownIndicator.SetActive(!skillContents[0].activeSelf);
      });
      closeSpriteSelectionButton.onClick.AddListener(() => closeIconSelection());
      projectileSpriteButton.onClick.AddListener(() => toggleSpriteSelection(PathType.ProjectileSprite));
      selectBuffIconButton.onClick.AddListener(() => toggleSpriteSelection(PathType.BuffIcon));
      selectCastSpriteButton.onClick.AddListener(() => toggleSpriteSelection(PathType.CastSprite));
      selectHitSpriteButton.onClick.AddListener(() => toggleSpriteSelection(PathType.HitSprite));
      selectSkillIconButton.onClick.AddListener(() => toggleSpriteSelection(PathType.ItemIcon));
      selectHitAudioButton.onClick.AddListener(() => toggleAudioSelection(PathType.HitSprite));
      selectCastAudioButton.onClick.AddListener(() => toggleAudioSelection(PathType.CastSprite));
      playHitAudioButton.onClick.AddListener(() => {
         if (hitSoundEffect != null) {
            audioSource.clip = hitSoundEffect.clip;
            audioSource.volume = hitSoundEffect.calculateValue(SoundEffect.ValueType.VOLUME);
            audioSource.pitch = hitSoundEffect.calculateValue(SoundEffect.ValueType.PITCH);
            audioSource.Play();
         }
      });
      playCastAudioButton.onClick.AddListener(() => {
         if (castSoundEffect != null) {
            audioSource.clip = castSoundEffect.clip;
            audioSource.volume = castSoundEffect.calculateValue(SoundEffect.ValueType.VOLUME);
            audioSource.pitch = castSoundEffect.calculateValue(SoundEffect.ValueType.PITCH);
            audioSource.Play();
         }
      });

      elements.maxValue = Enum.GetValues(typeof(Element)).Length - 1;
      battleItemType.maxValue = Enum.GetValues(typeof(BattleItemType)).Length - 1;
      weaponClass.maxValue = Enum.GetValues(typeof(Weapon.Class)).Length - 1;
      abilityType.maxValue = Enum.GetValues(typeof(AbilityType)).Length - 1;
      abilityActionType.maxValue = Enum.GetValues(typeof(AbilityActionType)).Length - 1;
      buffType.maxValue = Enum.GetValues(typeof(BuffType)).Length - 1;
      buffActionType.maxValue = Enum.GetValues(typeof(BuffActionType)).Length - 1;
      bonusStatType.maxValue = Enum.GetValues(typeof(BonusStatType)).Length - 1;
      abilityCastPosition.maxValue = Enum.GetValues(typeof(BasicAbilityData.AbilityCastPosition)).Length - 1;

      userCustomProjectileSprite.onValueChanged.AddListener(_ => {
         projectileSpriteSelectionHolder.SetActive(_);
      });

      itemName.onValueChanged.AddListener(_ => {
         skillName.text = itemName.text;
      });
      elements.onValueChanged.AddListener(_ => {
         elementsText.text = ((Element) elements.value).ToString() + countSliderValue(elements);
      });
      battleItemType.onValueChanged.AddListener(_ => {
         battleItemTypeText.text = ((BattleItemType) battleItemType.value).ToString() + countSliderValue(battleItemType);
      });
      weaponClass.onValueChanged.AddListener(_ => {
         weaponClassText.text = ((Weapon.Class) weaponClass.value).ToString() + countSliderValue(weaponClass);
      });
      abilityType.onValueChanged.AddListener(_ => {
         abilityTypeText.text = ((AbilityType) abilityType.value).ToString() + countSliderValue(abilityType);
      });
      bonusStatType.onValueChanged.AddListener(_ => {
         bonusStatText.text = ((BonusStatType) bonusStatType.value).ToString() + countSliderValue(bonusStatType);
      });
      abilityCastPosition.onValueChanged.AddListener(_ => {
         abilityCastPositionText.text = ((BasicAbilityData.AbilityCastPosition) _).ToString() + countSliderValue(abilityCastPosition);
      });
      abilityActionType.onValueChanged.AddListener(_ => {
         abilityActionTypeText.text = ((AbilityActionType) abilityActionType.value).ToString() + countSliderValue(abilityActionType);
         if ((AbilityActionType) abilityActionType.value == AbilityActionType.CastToTarget || (AbilityActionType) abilityActionType.value == AbilityActionType.Ranged) {
            foreach (GameObject obj in projectileVariables) {
               obj.SetActive(true);
            }
         } else {
            foreach (GameObject obj in projectileVariables) {
               obj.SetActive(false);
            }
         }
      });
      buffType.onValueChanged.AddListener(_ => {
         buffTypeText.text = ((BuffType) buffType.value).ToString() + countSliderValue(buffType);
      });
      buffActionType.onValueChanged.AddListener(_ => {
         buffActionTypeText.text = ((BuffActionType) buffActionType.value).ToString() + countSliderValue(buffActionType);
      });

      if (!MasterToolAccountManager.canAlterData()) {
         deleteSkillButton.gameObject.SetActive(false);
      }
   }

   private void initializeSliderValues () {
      elements.onValueChanged.Invoke(elements.value);
      battleItemType.onValueChanged.Invoke(battleItemType.value);
      weaponClass.onValueChanged.Invoke(weaponClass.value);
      abilityType.onValueChanged.Invoke(abilityType.value);
      abilityActionType.onValueChanged.Invoke(abilityActionType.value);
      buffType.onValueChanged.Invoke(buffType.value);
      buffActionType.onValueChanged.Invoke(buffActionType.value);
      abilityCastPosition.onValueChanged.Invoke(abilityCastPosition.value);
   }

   #endregion

   #region Ability Stances

   private void addStance () {
      GameObject template = Instantiate(stanceTemplate, stanceTemplateParent.transform);
      StanceTemplate stanceTemp = template.GetComponent<StanceTemplate>();
      stanceTemp.Init();
      stanceTemp.deleteButton.onClick.AddListener(() => {
         StanceTemplate currentTemp = stanceSlidierList.Find(_ => _ == stanceTemp);
         stanceSlidierList.Remove(currentTemp);
         Destroy(template);
      });
      stanceTemp.slider.onValueChanged.Invoke(stanceTemp.slider.value);
      stanceSlidierList.Add(stanceTemp);
   }

   private void loadStance (BasicAbilityData ability) {
      stanceSlidierList = new List<StanceTemplate>();
      if (ability.allowedStances != null) {
         foreach (Battler.Stance stance in ability.allowedStances) {
            GameObject template = Instantiate(stanceTemplate, stanceTemplateParent.transform);
            StanceTemplate stanceTemp = template.GetComponent<StanceTemplate>();
            stanceTemp.Init();
            stanceTemp.slider.value = (int) stance;
            stanceTemp.deleteButton.onClick.AddListener(() => {
               StanceTemplate currentTemp = stanceSlidierList.Find(_ => _ == stanceTemp);
               stanceSlidierList.Remove(currentTemp);
               Destroy(template);
            });
            stanceTemp.slider.onValueChanged.Invoke((int) stance);
            stanceSlidierList.Add(stanceTemp);
         }
      }
   }

   #endregion

   #region Retrieve and Load Data

   public void loadAttackData (AttackAbilityData attackData) {
      if (attackData.abilityType == AbilityType.Standard) {
         skillLabel.text = "Attack Ability";
         abilityTypeIcon.sprite = attackSprite;
         typeIcon.sprite = attackSprite;
      } else if (attackData.abilityType == AbilityType.Stance) {
         skillLabel.text = "Stance Ability";
         abilityTypeIcon.sprite = stanceSprite;
         typeIcon.sprite = stanceSprite;
      }

      loadGenericData(attackData);

      baseDamage.text = attackData.baseDamage.ToString();
      abilityActionType.value = (int) attackData.abilityActionType;
      hasShake.isOn = attackData.hasShake;
      hasKnockup.isOn = attackData.hasKnockup;
      canBeBlocked.isOn = attackData.canBeBlocked;
      hasKnockBack.isOn = attackData.hasKnockBack;
      projectileSpeed.text = attackData.projectileSpeed.ToString();
      projectileScale.text = attackData.projectileScale.ToString();
      projectileSpritePath.text = attackData.projectileSpritePath;
      userCustomProjectileSprite.isOn = attackData.useCustomProjectileSprite;
      projectileSpriteSelectionHolder.SetActive(userCustomProjectileSprite.isOn);

      if (attackData.abilityActionType != AbilityActionType.Melee) {
         if (attackData.projectileSpritePath != null) {
            projectileSprite.sprite = ImageManager.getSprite(attackData.projectileSpritePath);
         }
      } else {
         projectileSprite.sprite = ImageManager.self.blankSprite;
      }

      attackStatHolder.SetActive(true);
      buffStatHolder.SetActive(false);

      initializeSliderValues();
   }

   public void loadBuffData (BuffAbilityData buffData) {
      skillLabel.text = "Buff Ability";
      abilityTypeIcon.sprite = buffSprite;
      typeIcon.sprite = buffSprite;

      loadGenericData(buffData);

      buffDuration.text = buffData.duration.ToString();
      buffType.value = (int) buffData.buffType;
      buffActionType.value = (int) buffData.buffActionType;
      bonusStatType.value = (int) buffData.bonusStatType;

      if (buffData.iconPath != null) {
         buffIconPath.text = buffData.iconPath;
         buffIcon.sprite = ImageManager.getSprite(buffData.iconPath);
      }
      buffValue.text = buffData.value.ToString();

      buffStatHolder.SetActive(true);
      attackStatHolder.SetActive(false);

      initializeSliderValues();
   }

   public void loadGenericData (BasicAbilityData abilityData) {
      EnableListeners();
      skillName.text = abilityData.itemName;
      levelRequirement.text = abilityData.levelRequirement.ToString();
      itemID.text = abilityData.itemID.ToString();
      itemDescription.text = abilityData.itemDescription;
      itemName.text = abilityData.itemName;

      elements.value = (int) abilityData.elementType;
      battleItemType.value = (int) abilityData.battleItemType;
      weaponClass.value = (int) abilityData.classRequirement;
      abilityType.value = (int) abilityData.abilityType;

      abilityCooldown.text = abilityData.abilityCooldown.ToString();
      apChange.text = abilityData.apChange.ToString();
      abilityCost.text = abilityData.abilityCost.ToString();
      fxTimerPerFrame.text = abilityData.FXTimePerFrame.ToString();
      hitFxTimerPerFrame.text = abilityData.hitFXTimePerFrame.ToString();

      abilityCastPositionText.text = abilityData.abilityCastPosition.ToString();
      abilityCastPosition.value = (int) abilityData.abilityCastPosition;

      if (abilityData.castSpritesPath != null) {
         castSpritePath.text = abilityData.castSpritesPath[0];
         castSprite.sprite = ImageManager.getSprite(abilityData.castSpritesPath[0]);
      }
      if (abilityData.hitSpritesPath != null) {
         hitSpritePath.text = abilityData.hitSpritesPath[0];
         hitSprite.sprite = ImageManager.getSprite(abilityData.hitSpritesPath[0]);
      }
      if (abilityData.itemIconPath != null) {
         itemIconPath.text = abilityData.itemIconPath;
         itemIcon.sprite = ImageManager.getSprite(abilityData.itemIconPath);
      }

      hitSoundEffect = SoundEffectManager.self.getSoundEffect(abilityData.hitSoundEffectId);
      castSoundEffect = SoundEffectManager.self.getSoundEffect(abilityData.castSoundEffectId);

      hitSoundEffectName.text = "";
      castSoundEffectName.text = "";

      if (hitSoundEffect != null) {
         hitSoundEffectName.text = hitSoundEffect.name;
      }
      if (castSoundEffect != null) {
         castSoundEffectName.text = castSoundEffect.name;
      }

      loadStance(abilityData);
   }

   public AttackAbilityData getAttackData () {
      AbilityType abilityType = AbilityType.Standard;
      Element element = (Element) this.elements.value;
      BattleItemType battleItemType = (BattleItemType) this.battleItemType.value;
      Weapon.Class weaponClass = (Weapon.Class) this.weaponClass.value;

      int hitSoundEffectId = -1;
      if (hitSoundEffect != null) {
         hitSoundEffectId = hitSoundEffect.id;
      }
      BattleItemData battleItemData = BattleItemData.CreateInstance(int.Parse(itemID.text), itemName.text, itemDescription.text, element, hitSoundEffectId,
        new string[] { hitSpritePath.text }, battleItemType, weaponClass, itemIconPath.text, int.Parse(levelRequirement.text));

      stanceList = new List<Battler.Stance>();
      foreach (StanceTemplate templateStance in stanceSlidierList) {
         Battler.Stance stance = (Battler.Stance) System.Enum.Parse(typeof(Battler.Stance), templateStance.label.text);
         stanceList.Add(stance);
      }

      int castSoundEffectId = -1;
      if (castSoundEffect != null) {
         castSoundEffectId = castSoundEffect.id;
      }
      BasicAbilityData basicData = BasicAbilityData.CreateInstance(battleItemData,
         int.Parse(abilityCost.text),
         new string[] { castSpritePath.text },
         castSoundEffectId, stanceList.ToArray(),
         abilityType,
         int.Parse(abilityCooldown.text),
         int.Parse(apChange.text),
         float.Parse(fxTimerPerFrame.text),
         (BasicAbilityData.AbilityCastPosition) abilityCastPosition.value,
         float.Parse(hitFxTimerPerFrame.text));

      AttackAbilityData attackData = AttackAbilityData.CreateInstance(basicData,
         hasKnockup.isOn,
         int.Parse(baseDamage.text),
         hasShake.isOn, (AbilityActionType) abilityActionType.value,
         canBeBlocked.isOn, hasKnockBack.isOn,
         float.Parse(projectileSpeed.text),
         projectileSpritePath.text,
         float.Parse(projectileScale.text),
         userCustomProjectileSprite.isOn);
      attackData.abilityCastPosition = (BasicAbilityData.AbilityCastPosition) abilityCastPosition.value;

      return attackData;
   }

   public BuffAbilityData getBuffData () {
      AbilityType abilityType = AbilityType.BuffDebuff;
      Element element = (Element) this.elements.value;
      BattleItemType battleItemType = (BattleItemType) this.battleItemType.value;
      Weapon.Class weaponClass = (Weapon.Class) this.weaponClass.value;

      int hitSoundEffectId = -1;
      if (hitSoundEffect != null) {
         hitSoundEffectId = hitSoundEffect.id;
      }
      BattleItemData battleItemData = BattleItemData.CreateInstance(int.Parse(itemID.text), itemName.text, itemDescription.text, element, hitSoundEffectId,
        new string[] { hitSpritePath.text }, battleItemType, weaponClass, itemIconPath.text, int.Parse(levelRequirement.text));

      stanceList = new List<Battler.Stance>();
      foreach (StanceTemplate templateStance in stanceSlidierList) {
         Battler.Stance stance = (Battler.Stance) System.Enum.Parse(typeof(Battler.Stance), templateStance.label.text);
         stanceList.Add(stance);
      }

      int castSoundEffectId = -1;
      if (castSoundEffect != null) {
         castSoundEffectId = castSoundEffect.id;
      }
      BasicAbilityData basicData = BasicAbilityData.CreateInstance(battleItemData,
         int.Parse(abilityCost.text),
         new string[] { castSpritePath.text },
         castSoundEffectId, stanceList.ToArray(),
         abilityType,
         int.Parse(abilityCooldown.text),
         int.Parse(apChange.text),
         float.Parse(fxTimerPerFrame.text),
         (BasicAbilityData.AbilityCastPosition) abilityCastPosition.value,
         float.Parse(hitFxTimerPerFrame.text));

      BuffType buffType = (BuffType) this.buffType.value;
      BuffActionType buffActionType = (BuffActionType) this.buffActionType.value;
      BonusStatType bonusStatType = (BonusStatType) this.bonusStatType.value;
      BuffAbilityData buffData = BuffAbilityData.CreateInstance(basicData, float.Parse(buffDuration.text), buffType, buffActionType, buffIconPath.text, int.Parse(buffValue.text), bonusStatType);
      buffData.abilityCastPosition = (BasicAbilityData.AbilityCastPosition) abilityCastPosition.value;

      return buffData;
   }

   #endregion

   private void toggleSpriteSelection (PathType pathType) {
      spriteSelectionPanel.SetActive(true);
      spriteSelectionParent.DestroyChildren();

      Dictionary<string, Sprite> iconSpriteList = new Dictionary<string, Sprite>();
      if (monsterDataPanel != null) {
         previewSelectionIcon.sprite = monsterDataPanel.emptySprite;
         switch (pathType) {
            case PathType.ItemIcon:
               iconSpriteList = monsterDataPanel.skillIconSpriteList;
               break;
            case PathType.CastSprite:
               iconSpriteList = monsterDataPanel.castIconSpriteList;
               break;
            case PathType.BuffIcon:
               iconSpriteList = monsterDataPanel.iconSpriteList;
               break;
            case PathType.HitSprite:
               iconSpriteList = monsterDataPanel.hitIconSpriteList;
               break;
            case PathType.ProjectileSprite:
               iconSpriteList = monsterDataPanel.projectileSpriteList;
               break;
         }
      }
      if (abilityDataScene != null) {
         previewSelectionIcon.sprite = abilityDataScene.emptySprite;
         switch (pathType) {
            case PathType.ItemIcon:
               iconSpriteList = abilityDataScene.iconSpriteList;
               break;
            case PathType.CastSprite:
               iconSpriteList = abilityDataScene.castIconSpriteList;
               break;
            case PathType.BuffIcon:
               iconSpriteList = abilityDataScene.iconSpriteList;
               break;
            case PathType.HitSprite:
               iconSpriteList = abilityDataScene.hitIconSpriteList;
               break;
            case PathType.ProjectileSprite:
               iconSpriteList = abilityDataScene.projectileSpriteList;
               break;
         }
      }

      foreach (KeyValuePair<string, Sprite> sourceSprite in iconSpriteList) {
         GameObject iconTempObj = Instantiate(spriteTemplate.gameObject, spriteSelectionParent.transform);
         ItemTypeTemplate iconTemp = iconTempObj.GetComponent<ItemTypeTemplate>();
         iconTemp.spriteIcon.sprite = sourceSprite.Value;
         iconTemp.itemTypeText.text = sourceSprite.Value.name;
         iconTemp.previewButton.onClick.AddListener(() => {
            previewSelectionIcon.sprite = sourceSprite.Value;
         });
         iconTemp.selectButton.onClick.AddListener(() => {
            switch (pathType) {
               case PathType.CastSprite:
                  castSprite.sprite = sourceSprite.Value;
                  castSpritePath.text = sourceSprite.Key;
                  break;
               case PathType.HitSprite:
                  hitSprite.sprite = sourceSprite.Value;
                  hitSpritePath.text = sourceSprite.Key;
                  break;
               case PathType.ItemIcon:
                  itemIcon.sprite = sourceSprite.Value;
                  itemIconPath.text = sourceSprite.Key;
                  break;
               case PathType.BuffIcon:
                  buffIcon.sprite = sourceSprite.Value;
                  buffIconPath.text = sourceSprite.Key;
                  break;
               case PathType.ProjectileSprite:
                  projectileSprite.sprite = sourceSprite.Value;
                  projectileSpritePath.text = sourceSprite.Key;
                  break;
            }
            closeSpriteSelectionButton.onClick.Invoke();
         });
      }
   }

   private void toggleAudioSelection (PathType pathType) {
      spriteSelectionPanel.SetActive(true);
      spriteSelectionParent.DestroyChildren();

      foreach (SoundEffect effect in SoundEffectManager.self.getAllSoundEffects()) {
         GameObject iconTempObj = Instantiate(spriteTemplate.gameObject, spriteSelectionParent.transform);
         ItemTypeTemplate iconTemp = iconTempObj.GetComponent<ItemTypeTemplate>();
         iconTemp.itemTypeText.text = effect.name;

         iconTemp.previewButton.onClick.AddListener(() => {
            audioSource.clip = effect.clip;
            audioSource.volume = effect.calculateValue(SoundEffect.ValueType.VOLUME);
            audioSource.pitch = effect.calculateValue(SoundEffect.ValueType.PITCH);
            audioSource.Play();
         });

         iconTemp.selectButton.onClick.AddListener(() => {
            switch (pathType) {
               case PathType.CastSprite:
                  castSoundEffect = effect;
                  if (castSoundEffect != null) {
                     castSoundEffectName.text = castSoundEffect.name;
                  }
                  break;
               case PathType.HitSprite:
                  hitSoundEffect = effect;
                  if (hitSoundEffect != null) {
                     hitSoundEffectName.text = hitSoundEffect.name;
                  }
                  break;
            }
            closeSpriteSelectionButton.onClick.Invoke();
         });
      }
   }

   private void closeIconSelection () {
      spriteSelectionPanel.SetActive(false);
   }

   private string countSliderValue (Slider slider) {
      return " ( " + slider.value + " / " + slider.maxValue + " )";
   }
}