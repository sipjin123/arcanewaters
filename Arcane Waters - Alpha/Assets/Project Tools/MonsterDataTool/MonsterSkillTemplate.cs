using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using Random = UnityEngine.Random;

public class MonsterSkillTemplate : MonoBehaviour {
   #region Public Variables

   // Reference to the monster panel
   public MonsterDataPanel monsterDataPanel;

   // Audio source to play the sample clips
   public AudioSource audioSource;

   // Content Dropdown feature
   public Button toggleSkillButton;
   public GameObject dropDownIndicator;
   public GameObject[] skillContents;
   public Image previewSelectionIcon;
   public Button deleteSkillButton;

   // Primary Key variables to determine the ability
   public Text skillName;
   public AbilityType abilityTypeEnum;
   public Sprite buffSprite, attackSprite;
   public Image abilityTypeIcon;
   public Text skillLabel;

   // Ability Stances
   public List<BattlerBehaviour.Stance> stanceList = new List<BattlerBehaviour.Stance>();
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
   public Slider abilityActionType;
   public Text abilityActionTypeText;
   public Image itemIcon;
   public Text itemIconPath;
   public Button selectSkillIconButton;

   // Attack related stats
   public InputField baseDamage;
   public Toggle hasShake;
   public Toggle hasKnockup;
   public Toggle canBeBlocked;
   public GameObject attackStatHolder;

   // Buff related stats
   public InputField buffDuration;
   public Slider buffType;
   public Text buffTypeText;
   public Slider buffActionType;
   public Text buffActionTypeText;
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
   public Text hitClipPath;
   public Text castClipPath;
   public Button selectHitAudioButton;
   public Button selectCastAudioButton;
   public Button playHitAudioButton;
   public Button playCastAudioButton;
   public AudioClip hitAudio, castAudio;

   public enum PathType
   {
      HitSprite,
      CastSprite,
      ItemIcon,
      BuffIcon,
      DeathSfx,
      JumpSfx
   }

   #endregion

   private void EnableListeners () {
      addStanceButton.onClick.AddListener(() => addStance());
      toggleSkillButton.onClick.AddListener(() => {
         foreach (GameObject obj in skillContents) {
            obj.SetActive(!skillContents[0].activeSelf);
         }
         dropDownIndicator.SetActive(!skillContents[0].activeSelf);
      });
      selectBuffIconButton.onClick.AddListener(() => toggleSpriteSelection(PathType.BuffIcon));
      selectCastSpriteButton.onClick.AddListener(() => toggleSpriteSelection(PathType.CastSprite));
      selectHitSpriteButton.onClick.AddListener(() => toggleSpriteSelection(PathType.HitSprite));
      selectSkillIconButton.onClick.AddListener(() => toggleSpriteSelection(PathType.ItemIcon)); 
      selectHitAudioButton.onClick.AddListener(() => toggleAudioSelection(PathType.HitSprite));
      selectCastAudioButton.onClick.AddListener(() => toggleAudioSelection(PathType.CastSprite));
      playHitAudioButton.onClick.AddListener(() => {
         if (hitAudio != null) {
            audioSource.clip = hitAudio;
            audioSource.Play();
         }
      });
      playCastAudioButton.onClick.AddListener(() => {
         if (castAudio != null) {
            audioSource.clip = castAudio;
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
      abilityActionType.onValueChanged.AddListener(_ => {
         abilityActionTypeText.text = ((AbilityActionType) abilityActionType.value).ToString() + countSliderValue(abilityActionType);
      });
      buffType.onValueChanged.AddListener(_ => {
         buffTypeText.text = ((BuffType) buffType.value).ToString() + countSliderValue(buffType);
      });
      buffActionType.onValueChanged.AddListener(_ => {
         buffActionTypeText.text = ((BuffActionType) buffActionType.value).ToString() + countSliderValue(buffActionType);
      });
   }

   private void initializeSliderValues() {
      elements.onValueChanged.Invoke(elements.value);
      battleItemType.onValueChanged.Invoke(battleItemType.value);
      weaponClass.onValueChanged.Invoke(weaponClass.value);
      abilityType.onValueChanged.Invoke(abilityType.value);
      abilityActionType.onValueChanged.Invoke(abilityActionType.value);
      buffType.onValueChanged.Invoke(buffType.value);
      buffActionType.onValueChanged.Invoke(buffActionType.value);
   }

   #region Ability Stances

   private void addStance() {
      GameObject template = Instantiate(stanceTemplate, stanceTemplateParent.transform);
      StanceTemplate stanceTemp = template.GetComponent<StanceTemplate>();
      stanceTemp.Init();
      stanceTemp.deleteButton.onClick.AddListener(() => {
         StanceTemplate currentTemp = stanceSlidierList.Find(_=>_ == stanceTemp);
         stanceSlidierList.Remove(currentTemp);
         Destroy(template);
      });
      stanceTemp.slider.onValueChanged.Invoke(stanceTemp.slider.value);
      stanceSlidierList.Add(stanceTemp);
   }

   private void loadStance(BasicAbilityData ability) {
      stanceSlidierList = new List<StanceTemplate>();
      foreach (BattlerBehaviour.Stance stance in ability.allowedStances) {
         GameObject template = Instantiate(stanceTemplate, stanceTemplateParent.transform);
         StanceTemplate stanceTemp = template.GetComponent<StanceTemplate>();
         stanceTemp.Init();
         stanceTemp.slider.value = (int)stance;
         stanceTemp.deleteButton.onClick.AddListener(() => {
            StanceTemplate currentTemp = stanceSlidierList.Find(_ => _ == stanceTemp);
            stanceSlidierList.Remove(currentTemp);
            Destroy(template);
         });
         stanceTemp.slider.onValueChanged.Invoke((int) stance);
         stanceSlidierList.Add(stanceTemp);
      }
   }

   #endregion

   #region Retrieve and Load Data

   public void loadAttackData(AttackAbilityData attackData) {
      skillLabel.text = "Attack Ability";
      abilityTypeIcon.sprite = attackSprite;

      loadGenericData(attackData);

      baseDamage.text = attackData.baseDamage.ToString();
      abilityActionType.value = (int)attackData.abilityActionType;
      hasShake.isOn = attackData.hasShake;
      hasKnockup.isOn = attackData.hasKnockup;
      canBeBlocked.isOn = attackData.canBeBlocked;

      attackStatHolder.SetActive(true);
      buffStatHolder.SetActive(false);

      initializeSliderValues();
   }

   public void loadBuffData (BuffAbilityData buffData) {
      skillLabel.text = "Buff Ability";
      abilityTypeIcon.sprite = buffSprite;

      loadGenericData(buffData);

      buffDuration.text = buffData.duration.ToString();
      buffType.value = (int)buffData.buffType;
      buffActionType.value = (int) buffData.buffActionType;

      if (buffData.iconPath != null) {
         buffIconPath.text = buffData.iconPath;
         buffIcon.sprite = ImageManager.getSprite(buffData.iconPath);
      }
      buffValue.text = buffData.value.ToString();

      buffStatHolder.SetActive(true);
      attackStatHolder.SetActive(false);

      initializeSliderValues();
   }

   private void loadGenericData (BasicAbilityData abilityData) {
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

      if (abilityData.castSpritesPath != null) {
         castSpritePath.text = abilityData.castSpritesPath[0];
         castSprite.sprite = ImageManager.getSprite(abilityData.castSpritesPath[0]);
      }
      if (abilityData.hitSpritesPath != null) {
         hitSpritePath.text = abilityData.hitSpritesPath[0];
         hitSprite.sprite = ImageManager.getSprite(abilityData.hitSpritesPath[0]);
      }
      if (abilityData.itemIconPath != String.Empty) {
         itemIconPath.text = abilityData.itemIconPath;
         itemIcon.sprite = ImageManager.getSprite(abilityData.itemIconPath);
      }

      hitClipPath.text = abilityData.hitAudioClipPath;
      castClipPath.text = abilityData.castAudioClipPath;
      hitAudio = AudioClipManager.self.getAudioClipData(abilityData.hitAudioClipPath).audioClip;
      castAudio = AudioClipManager.self.getAudioClipData(abilityData.castAudioClipPath).audioClip;

      loadStance(abilityData);
   }
   
   public AttackAbilityData getAttackData () {
      AbilityType abilityType = (AbilityType) this.abilityType.value;
      Element element = (Element) this.elements.value;
      BattleItemType battleItemType = (BattleItemType) this.battleItemType.value;
      Weapon.Class weaponClass = (Weapon.Class) this.weaponClass.value;

      BattleItemData battleItemData = BattleItemData.CreateInstance(int.Parse(itemID.text), itemName.text, itemDescription.text, element, hitClipPath.text,
        new string[]{ hitSpritePath.text }, battleItemType, weaponClass, itemIconPath.text, int.Parse(levelRequirement.text));

      stanceList = new List<BattlerBehaviour.Stance>();
      foreach(StanceTemplate templateStance in stanceSlidierList) {
         BattlerBehaviour.Stance stance = (BattlerBehaviour.Stance) System.Enum.Parse(typeof(BattlerBehaviour.Stance), templateStance.label.text);
         stanceList.Add(stance);
      }

      BasicAbilityData basicData = BasicAbilityData.CreateInstance(battleItemData, 
         int.Parse(abilityCost.text), 
         new string[] { castSpritePath.text }, 
         castClipPath.text, stanceList.ToArray(), 
         abilityType, 
         int.Parse(abilityCooldown.text), 
         int.Parse(apChange.text),
         int.Parse(fxTimerPerFrame.text));

      AttackAbilityData attackData = AttackAbilityData.CreateInstance(basicData, hasKnockup.isOn, int.Parse(baseDamage.text), hasShake.isOn, (AbilityActionType) abilityActionType.value, canBeBlocked.isOn);

      return attackData;
   }

   public BuffAbilityData getBuffData () {
      AbilityType abilityType = (AbilityType) this.abilityType.value;
      Element element = (Element) this.elements.value;
      BattleItemType battleItemType = (BattleItemType) this.battleItemType.value;
      Weapon.Class weaponClass = (Weapon.Class) this.weaponClass.value;

      BattleItemData battleItemData = BattleItemData.CreateInstance(int.Parse(itemID.text), itemName.text, itemDescription.text, element, hitClipPath.text,
        new string[] { hitSpritePath.text }, battleItemType, weaponClass, itemIconPath.text, int.Parse(levelRequirement.text));

      stanceList = new List<BattlerBehaviour.Stance>();
      foreach (StanceTemplate templateStance in stanceSlidierList) {
         BattlerBehaviour.Stance stance = (BattlerBehaviour.Stance) System.Enum.Parse(typeof(BattlerBehaviour.Stance), templateStance.label.text);
         stanceList.Add(stance);
      }

      BasicAbilityData basicData = BasicAbilityData.CreateInstance(battleItemData,
         int.Parse(abilityCost.text),
         new string[] { castSpritePath.text },
         castClipPath.text, stanceList.ToArray(),
         abilityType,
         int.Parse(abilityCooldown.text),
         int.Parse(apChange.text),
         int.Parse(fxTimerPerFrame.text));

      BuffType buffType = (BuffType) this.buffType.value;
      BuffActionType buffActionType = (BuffActionType) this.buffActionType.value;
      BuffAbilityData buffData = BuffAbilityData.CreateInstance(basicData, float.Parse(buffDuration.text), buffType, buffActionType, buffIconPath.text, int.Parse(buffValue.text));

      return buffData;
   }

   #endregion

   private void toggleSpriteSelection (PathType pathType) {
      spriteSelectionPanel.SetActive(true);
      spriteSelectionParent.DestroyChildren();

      previewSelectionIcon.sprite = monsterDataPanel.emptySprite;
      foreach (KeyValuePair<string, Sprite> sourceSprite in monsterDataPanel.castIconSpriteList) {
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
            }
            closeSpriteSelectionButton.onClick.Invoke();
         });
      }
   }

   private void toggleAudioSelection (PathType pathType) {
      spriteSelectionPanel.SetActive(true);
      spriteSelectionParent.DestroyChildren();

      foreach (AudioClipManager.AudioClipData sourceClip in AudioClipManager.self.audioDataList) {
         GameObject iconTempObj = Instantiate(spriteTemplate.gameObject, spriteSelectionParent.transform);
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
               case PathType.CastSprite:
                  castClipPath.text = sourceClip.audioPath;
                  castAudio = sourceClip.audioClip;
                  break;
               case PathType.HitSprite:
                  hitClipPath.text = sourceClip.audioPath;
                  hitAudio = sourceClip.audioClip;
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
