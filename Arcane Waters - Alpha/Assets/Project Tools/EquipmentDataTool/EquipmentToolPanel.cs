using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using static EquipmentToolManager;
using UnityEngine.Events;
using System.Linq;

public class EquipmentToolPanel : MonoBehaviour {
   #region Public Variables

   // Holds reference to the tool manager
   public EquipmentToolManager equipmentToolManager;

   // Holds the damage modifier template
   public DamageModifierTemplate damageModifierTemplate;

   // Elemental and Rarity Damage Modifier feature
   public GameObject elementalModifierParent;
   public GameObject rarityModifierParent;
   public List<DamageModifierTemplate> elementalModifierList, rarityModifierList;
   public Button createElementalModifierButton, createRarityModifierButton;

   // The sprite that the weapon will preview
   public static int EQUIPMENT_SPRITE_INDEX = 47;

   public enum ModifierType
   {
      None = 0,
      Elemental = 1,
      Rarity = 2
   }

   // Buttons for saving and canceling
   public Button saveButton, cancelButton;

   // Caches the initial type incase it is changed
   public int startingType;

   // Generic popup selection
   public GenericSelectionPopup genericSelectionPopup;

   // The type of equipment being reviewed
   public EquipmentType equipmentType;

   // The current xml id
   public int currentXmlId;

   // Event to notify if the equipment ID has been modified
   public UnityEvent changeSpriteDisplayEvent = new UnityEvent();

   // Event to notify if the action type has been modified
   public UnityEvent changeActionTypeEvent = new UnityEvent();

   // Gender type for preview
   public Gender.Type genderType = Gender.Type.Female;

   // Holds the projectile sprite icon selection
   public GameObject projectileSpriteHolder;

   #endregion

   private void Awake () {
      if (!MasterToolAccountManager.canAlterData()) {
         saveButton.gameObject.SetActive(false);
      }

      createElementalModifierButton.onClick.AddListener(() => createModifierTemplate(ModifierType.Elemental));
      createRarityModifierButton.onClick.AddListener(() => createModifierTemplate(ModifierType.Rarity));

      _weaponClass.maxValue = Enum.GetValues(typeof(Weapon.Class)).Length - 1;
      _weaponClass.onValueChanged.AddListener(_ => {
         _weaponClassText.text = ((Weapon.Class) _).ToString();
         projectileSpriteHolder.SetActive((Weapon.Class) _ != Weapon.Class.Melee);
      });

      _materialType.maxValue = 0;
      _materialType.onValueChanged.AddListener(_ => {
         _materialTypeText.text = "MATERIAL TYPE is no longer used. Please remove this from UI";
      });

      saveButton.onClick.AddListener(() => {
         if (equipmentType == EquipmentType.Weapon) {
            WeaponStatData newStatData = getWeaponStatData();
            if (newStatData != null) {
               equipmentToolManager.saveWeapon(newStatData, currentXmlId, _isEnabled.isOn);
               gameObject.SetActive(false);
            }
         } else if (equipmentType == EquipmentType.Armor) {
            ArmorStatData newStatData = getArmorStatData();
            if (newStatData != null) {
               equipmentToolManager.saveArmor(newStatData, currentXmlId, _isEnabled.isOn);
               gameObject.SetActive(false);
            }
         } else if (equipmentType == EquipmentType.Hat) {
            HatStatData newStatData = getHatStatData();
            if (newStatData != null) {
               equipmentToolManager.saveHat(newStatData, currentXmlId, _isEnabled.isOn);
               gameObject.SetActive(false);
            }
         }
      });

      cancelButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
         equipmentToolManager.loadXMLData();
      });

      _changeIconPathButton.onClick.AddListener(() => {
         if (equipmentType == EquipmentType.Weapon) {
            genericSelectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.WeaponIcon, _icon, _iconPath);
         } else if (equipmentType == EquipmentType.Armor) {
            genericSelectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.ArmorIcon, _icon, _iconPath);
         } else if (equipmentType == EquipmentType.Hat) {
            genericSelectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.HatIcon, _icon, _iconPath);
         }
      });
      
      _changeProjectileIconPathButton.onClick.AddListener(() => {
         genericSelectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.CannonSprites, _projectileIcon, _projectileIconPath);
      });

      _equipmentTypeButton.onClick.AddListener(() => {
         if (equipmentType == EquipmentType.Weapon) {
            genericSelectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.WeaponType, _equipmentTypeText, changeSpriteDisplayEvent);
         } else if (equipmentType == EquipmentType.Armor) {
            genericSelectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.ArmorTypeSprites, _equipmentTypeText, changeSpriteDisplayEvent);
         } else if (equipmentType == EquipmentType.Hat) {
            genericSelectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.HatType, _equipmentTypeText, changeSpriteDisplayEvent);
         }
      });

      changeSpriteDisplayEvent.AddListener(() => {
         int spriteIndex = 0;
         string path = getInGameSprite(int.Parse(_equipmentTypeText.text));
         Sprite[] sprites = ImageManager.getSprites(path);
         if (equipmentType == EquipmentType.Weapon) {
            int targetWeaponIndex = EQUIPMENT_SPRITE_INDEX;
            if (sprites.Length-1 >= targetWeaponIndex) {
               spriteIndex = targetWeaponIndex;
               _weaponSpriteImage.sprite = sprites[spriteIndex];
            }
         } else if (equipmentType == EquipmentType.Armor) {
            if (sprites.Length > 0) {
               try {
                  _armorSpriteImage.sprite = sprites[spriteIndex];
               } catch {
                  _armorSpriteImage.sprite = sprites[0];
               }
            } 
         } else {
            if (sprites.Length > 0) {
               try {
                  _helmSpriteImage.sprite = sprites[spriteIndex];
               } catch {
                  _helmSpriteImage.sprite = sprites[0];
               }
            }
         }
      });

      _actionTypeButton.onClick.AddListener(() => genericSelectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.WeaponActionType, _actionType, changeActionTypeEvent));
      _paletteName1Button.onClick.AddListener(() => genericSelectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.Color, _paletteName1Text));
      _paletteName2Button.onClick.AddListener(() => genericSelectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.Color, _paletteName2Text));

      changeActionTypeEvent.AddListener(() => updateActionValueEquivalent());
      _actionTypeValue.onValueChanged.AddListener(_ => updateActionValueEquivalent());
      updateActionValueEquivalent();
   }

   private void updateActionValueEquivalent () {
      if ((Weapon.ActionType) Enum.Parse(typeof(Weapon.ActionType), _actionType.text) == Weapon.ActionType.PlantCrop) {
         try {
            _actionValueEquivalent.text = ((Crop.Type) int.Parse(_actionTypeValue.text)).ToString();
         } catch {
            _actionValueEquivalent.text = "None";
         }
      } else {
         _actionValueEquivalent.text = "None";
      }
   }

   private void createModifierTemplate (ModifierType modifierType) {
      switch (modifierType) {
         case ModifierType.Elemental: {
               DamageModifierTemplate newTemplate = Instantiate(damageModifierTemplate, elementalModifierParent.transform);
               newTemplate.damageMultiplier.text = "0";
               newTemplate.label.text = "Element";
               if (equipmentType == EquipmentType.Weapon) {
                  newTemplate.modifiers.text = "Damage\nMultiplier";
               } else {
                  newTemplate.modifiers.text = "Defense\nMultiplier";
               }

               newTemplate.typeSlidier.maxValue = Enum.GetValues(typeof(Element)).Length - 1;
               newTemplate.typeSlidier.value = 0;
               newTemplate.typeSlidier.onValueChanged.AddListener(_ => {
                  newTemplate.typeText.text = ((Element) _).ToString();
               });
               newTemplate.typeSlidier.onValueChanged.Invoke(newTemplate.typeSlidier.value);

               newTemplate.deleteButton.onClick.AddListener(() => {
                  rarityModifierList.Remove(newTemplate);
                  Destroy(newTemplate.gameObject);
               });

               elementalModifierList.Add(newTemplate);
            }
            break;
         case ModifierType.Rarity: {
               DamageModifierTemplate newTemplate = Instantiate(damageModifierTemplate, rarityModifierParent.transform);
               newTemplate.damageMultiplier.text = "0";
               newTemplate.label.text = "Rarity";
               if (equipmentType == EquipmentType.Weapon) {
                  newTemplate.modifiers.text = "Damage\nMultiplier";
               } else {
                  newTemplate.modifiers.text = "Defense\nMultiplier";
               }

               newTemplate.typeSlidier.maxValue = Enum.GetValues(typeof(Rarity.Type)).Length - 1;
               newTemplate.typeSlidier.value = 0;
               newTemplate.typeSlidier.onValueChanged.AddListener(_ => {
                  newTemplate.typeText.text = ((Rarity.Type) _).ToString();
               });
               newTemplate.typeSlidier.onValueChanged.Invoke(newTemplate.typeSlidier.value);

               newTemplate.deleteButton.onClick.AddListener(() => {
                  rarityModifierList.Remove(newTemplate);
                  Destroy(newTemplate.gameObject);
               });

               rarityModifierList.Add(newTemplate);
            }
            break;
      }

   }

   #region Loading Data

   private void loadGenericInfo () {
      if (equipmentType == EquipmentType.Weapon) {
         _itemTypeValue.text = "Weapon Sprite ID:";
         _titleTabValue.text = "Weapon Stats";
         _toolTipValue.message = "The damage of the weapon";
         _itemRawValue.text = "Base Damage";
         _damageClassObj.SetActive(true);
      } else if (equipmentType == EquipmentType.Armor) {
         _itemTypeValue.text = "Armor Sprite ID:";
         _titleTabValue.text = "Armor Stats";
         _toolTipValue.message = "The defense of the armor";
         _itemRawValue.text = "Base Defense";
         _damageClassObj.SetActive(false);
      } else if (equipmentType == EquipmentType.Hat) {
         _itemTypeValue.text = "Hat Type:";
         _titleTabValue.text = "Hat Stats";
         _toolTipValue.message = "The defense of the hat";
         _itemRawValue.text = "Base Defense";
         _damageClassObj.SetActive(false);
      }
   }

   public void loadArmorData (ArmorStatData statData, int template_id, bool isEnabled) {
      currentXmlId = template_id;
      startingType = (int) statData.armorType;
      equipmentType = EquipmentType.Armor;
      _isEnabled.isOn = isEnabled;

      foreach (GameObject obj in _actionTypeGroup) {
         obj.SetActive(false);
      }

      loadGenericInfo();
      loadEquipmentData(statData);

      _attributePhysical.text = statData.armorBaseDefense.ToString();
      _attributeFire.text = statData.fireResist.ToString();
      _attributeWater.text = statData.waterResist.ToString();
      _attributeAir.text = statData.airResist.ToString();
      _attributeEarth.text = statData.earthResist.ToString();

      _equipmentTypeText.text = statData.armorType.ToString();

      string path = getInGameSprite(statData.armorType);
      _armorSpriteImage.sprite = ImageManager.getSprite(path);

      _weaponClass.onValueChanged.Invoke(_weaponClass.value);
   }

   public void loadWeaponData (WeaponStatData statData, int template_id, bool isEnabled) {
      currentXmlId = template_id;
      startingType = (int)statData.weaponType;
      equipmentType = EquipmentType.Weapon;
      _isEnabled.isOn = isEnabled;

      foreach (GameObject obj in _actionTypeGroup) {
         obj.SetActive(true);
      }
      _actionType.text = statData.actionType.ToString();
      _actionTypeValue.text = statData.actionTypeValue.ToString();

      loadGenericInfo();
      loadEquipmentData(statData);

      _attributePhysical.text = statData.weaponBaseDamage.ToString();
      _attributeFire.text = statData.weaponDamageFire.ToString();
      _attributeWater.text = statData.weaponDamageWater.ToString();
      _attributeAir.text = statData.weaponDamageAir.ToString();
      _attributeEarth.text = statData.weaponDamageEarth.ToString();

      _equipmentTypeText.text = statData.weaponType.ToString();
      _weaponClass.value = (int) statData.weaponClass;
      _weaponClassText.text = statData.weaponClass.ToString();

      _projectileIconPath.text = statData.projectileSprite;
      if (statData.projectileSprite.Length > 0) {
         Sprite projectileSprite = ImageManager.getSprite(statData.projectileSprite);
         _projectileIcon.sprite = projectileSprite;
      } else {
         _projectileIcon.sprite = ImageManager.self.blankSprite;
      }

      int spriteIndex = 0;
      string path = getInGameSprite(statData.weaponType);
      Sprite[] sprites = ImageManager.getSprites(path);

      int targetWeaponIndex = EQUIPMENT_SPRITE_INDEX;
      if (sprites.Length - 1 >= targetWeaponIndex) {
         spriteIndex = targetWeaponIndex;
         _weaponSpriteImage.sprite = sprites[spriteIndex];
      }

      _weaponClass.onValueChanged.Invoke(_weaponClass.value);
   }

   public void loadHatData (HatStatData statData, int template_id, bool isEnabled) {
      currentXmlId = template_id;
      startingType = (int) statData.hatType;
      equipmentType = EquipmentType.Hat;
      _isEnabled.isOn = isEnabled;

      foreach (GameObject obj in _actionTypeGroup) {
         obj.SetActive(false);
      }

      loadGenericInfo();
      loadEquipmentData(statData);

      _attributePhysical.text = statData.hatBaseDefense.ToString();
      _attributeFire.text = statData.fireResist.ToString();
      _attributeWater.text = statData.waterResist.ToString();
      _attributeAir.text = statData.airResist.ToString();
      _attributeEarth.text = statData.earthResist.ToString();
      _equipmentTypeText.text = statData.hatType.ToString();

      _weaponClass.onValueChanged.Invoke(_weaponClass.value);

      string path = getInGameSprite(statData.hatType);
      _helmSpriteImage.sprite = ImageManager.getSprite(path);
   }

   private void loadEquipmentData (EquipmentStatData equipmentData) {
      elementalModifierList = new List<DamageModifierTemplate>();
      rarityModifierList = new List<DamageModifierTemplate>();
      elementalModifierParent.DestroyChildren();
      rarityModifierParent.DestroyChildren();

      _itemName.text = equipmentData.equipmentName;
      _itemDescription.text = equipmentData.equipmentDescription;
      _itemID.text = currentXmlId.ToString();
      _itemPrice.text = equipmentData.equipmentPrice.ToString();
      _canBeTrashed.isOn = equipmentData.canBeTrashed;
      _iconPath.text = equipmentData.equipmentIconPath;

      _paletteName1Text.text = equipmentData.palettes;
      _paletteName2Text.text = "UNUSED";

      _materialType.value = 0;
      _materialTypeText.text = "MATERIAL TYPE is no longer used. Please remove this from UI";
      _declareAllColors.isOn = equipmentData.setAllColors;

      if (equipmentData.equipmentIconPath != "") {
         _icon.sprite = ImageManager.getSprite(equipmentData.equipmentIconPath);
      } else {
         _icon.sprite = genericSelectionPopup.emptySprite;
      }

      if (equipmentData.elementModifiers != null) {
         foreach (ElementModifier modifier in equipmentData.elementModifiers) {
            DamageModifierTemplate newTemplate = Instantiate(damageModifierTemplate, elementalModifierParent.transform);
            newTemplate.damageMultiplier.text = modifier.multiplier.ToString();
            newTemplate.label.text = "Element";
            if (equipmentType == EquipmentType.Weapon) {
               newTemplate.modifiers.text = "Damage\nMultiplier";
            } else {
               newTemplate.modifiers.text = "Defense\nMultiplier";
            }

            newTemplate.typeSlidier.maxValue = Enum.GetValues(typeof(Element)).Length - 1;
            newTemplate.typeSlidier.value = (int) modifier.elementType;
            newTemplate.typeSlidier.onValueChanged.AddListener(_ => {
               newTemplate.typeText.text = ((Element) _).ToString();
            });
            newTemplate.typeSlidier.onValueChanged.Invoke(newTemplate.typeSlidier.value);

            newTemplate.deleteButton.onClick.AddListener(() => {
               rarityModifierList.Remove(newTemplate);
               Destroy(newTemplate.gameObject);
            });

            elementalModifierList.Add(newTemplate);
         }
      }

      if (equipmentData.rarityModifiers != null) {
         foreach (RarityModifier modifier in equipmentData.rarityModifiers) {
            DamageModifierTemplate newTemplate = Instantiate(damageModifierTemplate, rarityModifierParent.transform);
            newTemplate.damageMultiplier.text = modifier.multiplier.ToString();
            newTemplate.label.text = "Rarity";
            if (equipmentType == EquipmentType.Weapon) {
               newTemplate.modifiers.text = "Damage\nMultiplier";
            } else {
               newTemplate.modifiers.text = "Defense\nMultiplier";
            }

            newTemplate.typeSlidier.maxValue = Enum.GetValues(typeof(Rarity.Type)).Length - 1;
            newTemplate.typeSlidier.value = (int) modifier.rarityType;
            newTemplate.typeSlidier.onValueChanged.AddListener(_ => {
               newTemplate.typeText.text = ((Rarity.Type) _).ToString();
            });
            newTemplate.typeSlidier.onValueChanged.Invoke(newTemplate.typeSlidier.value);

            newTemplate.deleteButton.onClick.AddListener(() => {
               rarityModifierList.Remove(newTemplate);
               Destroy(newTemplate.gameObject);
            });

            rarityModifierList.Add(newTemplate);
         }
      }

      _intelligenceText.text = equipmentData.statsData.intelligence.ToString();
      _vitalityText.text = equipmentData.statsData.vitality.ToString();
      _strengthText.text = equipmentData.statsData.strength.ToString();
      _precisionText.text = equipmentData.statsData.precision.ToString();
      _luckText.text = equipmentData.statsData.luck.ToString();
      _spiritText.text = equipmentData.statsData.spirit.ToString();
   }

   #endregion

   #region Saving Data

   public WeaponStatData getWeaponStatData () {
      WeaponStatData weaponStatData = new WeaponStatData();
      setEquipmentStatData(weaponStatData);

      // Setup special data
      weaponStatData.weaponBaseDamage = int.Parse(_attributePhysical.text);
      weaponStatData.weaponDamageWater = int.Parse(_attributeWater.text);
      weaponStatData.weaponDamageEarth = int.Parse(_attributeEarth.text);
      weaponStatData.weaponDamageFire = int.Parse(_attributeFire.text);
      weaponStatData.weaponDamageAir = int.Parse(_attributeAir.text);
      weaponStatData.weaponType = int.Parse(_equipmentTypeText.text);
      weaponStatData.weaponClass = (Weapon.Class) Enum.Parse(typeof(Weapon.Class), _weaponClassText.text);
      weaponStatData.actionType = (Weapon.ActionType) Enum.Parse(typeof(Weapon.ActionType), _actionType.text);
      weaponStatData.actionTypeValue = int.Parse(_actionTypeValue.text);
      weaponStatData.projectileSprite = _projectileIconPath.text;
      return weaponStatData;
   }

   public ArmorStatData getArmorStatData () {
      ArmorStatData armorStatData = new ArmorStatData();
      setEquipmentStatData(armorStatData);

      // Setup special data
      armorStatData.armorBaseDefense = int.Parse(_attributePhysical.text);
      armorStatData.waterResist = int.Parse(_attributeWater.text);
      armorStatData.earthResist = int.Parse(_attributeEarth.text);
      armorStatData.fireResist = int.Parse(_attributeFire.text);
      armorStatData.airResist = int.Parse(_attributeAir.text);
      armorStatData.armorType = int.Parse(_equipmentTypeText.text);
      
      return armorStatData;
   }

   public HatStatData getHatStatData () {
      HatStatData hatStatData = new HatStatData();
      setEquipmentStatData(hatStatData);

      // Setup special data
      hatStatData.hatBaseDefense = int.Parse(_attributePhysical.text);
      hatStatData.waterResist = int.Parse(_attributeWater.text);
      hatStatData.earthResist = int.Parse(_attributeEarth.text);
      hatStatData.fireResist = int.Parse(_attributeFire.text);
      hatStatData.airResist = int.Parse(_attributeAir.text);
      hatStatData.hatType = int.Parse(_equipmentTypeText.text);

      return hatStatData;
   }

   private void setEquipmentStatData (EquipmentStatData equipmentData) {
      // Setup base data
      equipmentData.equipmentName = _itemName.text;
      equipmentData.equipmentDescription = _itemDescription.text;
      equipmentData.sqlId = int.Parse(_itemID.text);
      equipmentData.equipmentPrice = int.Parse(_itemPrice.text);
      equipmentData.canBeTrashed = _canBeTrashed.isOn;
      equipmentData.equipmentIconPath = _iconPath.text;

      equipmentData.setAllColors = _declareAllColors.isOn;

      equipmentData.palettes = _paletteName1Text.text;

      equipmentData.rarityModifiers = getRarityModifiers();
      equipmentData.elementModifiers = getElementalModifiers();

      equipmentData.statsData.intelligence = int.Parse(_intelligenceText.text);
      equipmentData.statsData.vitality = int.Parse(_vitalityText.text);
      equipmentData.statsData.strength = int.Parse(_strengthText.text);
      equipmentData.statsData.precision = int.Parse(_precisionText.text);
      equipmentData.statsData.luck = int.Parse(_luckText.text);
      equipmentData.statsData.spirit = int.Parse(_spiritText.text);
   }

   private ElementModifier[] getElementalModifiers () {
      // Setup elemental modifiers
      List<ElementModifier> elementalModifiers = new List<ElementModifier>();
      foreach (DamageModifierTemplate equipmentTemplate in elementalModifierList) {
         ElementModifier newModifier = new ElementModifier();
         newModifier.multiplier = float.Parse(equipmentTemplate.damageMultiplier.text);
         newModifier.elementType = (Element) equipmentTemplate.typeSlidier.value;

         elementalModifiers.Add(newModifier);
      }
      return elementalModifiers.ToArray();
   }

   private RarityModifier[] getRarityModifiers () {
      // Setup rarity modifiers
      List<RarityModifier> rarityModifiers = new List<RarityModifier>();
      foreach (DamageModifierTemplate equipmentTemplate in rarityModifierList) {
         RarityModifier newModifier = new RarityModifier();
         newModifier.multiplier = float.Parse(equipmentTemplate.damageMultiplier.text);
         newModifier.rarityType = (Rarity.Type) equipmentTemplate.typeSlidier.value;

         rarityModifiers.Add(newModifier);
      }
      return rarityModifiers.ToArray();
   }

   #endregion

   private string getInGameSprite (int equipmentID) {
      string spritePath = "";
      switch (equipmentType) {
         case EquipmentType.Weapon:
            spritePath = "Sprites/Weapons/" + genderType + "/" + "weapon_" + equipmentID +"_front";
            break;
         case EquipmentType.Hat:
            spritePath = "Sprites/Hats/" + genderType + "/" + genderType.ToString().ToLower() + "_hat_" + equipmentID;
            break;
         case EquipmentType.Armor:
            spritePath = "Sprites/Armor/" + genderType + "/" + genderType.ToString().ToLower() + "_armor_" + equipmentID;
            break;
      }

      return spritePath;
   }

   #region Private Variables
#pragma warning disable 0649
   // Enables/Disables Weapon class depending on equipment type
   [SerializeField]
   private GameObject _damageClassObj;

   // Modifies the tool tip if damage or defense
   [SerializeField]
   private ToolTipComponent _toolTipValue;

   // Labels
   [SerializeField]
   private Text _itemTypeValue, _titleTabValue, _itemRawValue;

   // Name of the item
   [SerializeField]
   private InputField _itemName;

   // Details of the item
   [SerializeField]
   private InputField _itemDescription;

   // Id of the item
   [SerializeField]
   private Text _itemID;

   // Price of the item in the shop
   [SerializeField]
   private InputField _itemPrice;

   // Determines if the entry is enabled from the sql table
   [SerializeField]
   private Toggle _isEnabled;

   // Determines if the item can be trashed
   [SerializeField]
   private Toggle _canBeTrashed;

   // Determines if the equipment should set all colors
   [SerializeField]
   private Toggle _declareAllColors;

   // Input field variables for float and int values
   [SerializeField]
   private InputField _attributePhysical, _attributeFire, _attributeWater, _attributeAir, _attributeEarth;

   // Sliders
   [SerializeField]
   private Slider _weaponClass;
   [SerializeField]
   private Text _weaponClassText;

   // Material Type Slider
   [SerializeField]
   private Slider _materialType;
   [SerializeField]
   private Text _materialTypeText;

   // Type selection UI
   [SerializeField]
   private Text _equipmentTypeText;
   [SerializeField]
   private Button _equipmentTypeButton;

   // Palette names
   [SerializeField]
   private Text _paletteName1Text, _paletteName2Text;
   [SerializeField]
   private Button _paletteName1Button, _paletteName2Button;

   // Icon UI
   [SerializeField]
   private Text _iconPath;
   [SerializeField]
   private Button _changeIconPathButton;
   [SerializeField]
   private Image _icon;

   // Projectile Icon UI
   [SerializeField]
   private Text _projectileIconPath;
   [SerializeField]
   private Button _changeProjectileIconPathButton;
   [SerializeField]
   private Image _projectileIcon;

   // Action Type
   [SerializeField]
   private InputField _actionTypeValue;
   [SerializeField]
   private Text _actionValueEquivalent;
   [SerializeField]
   private Text _actionType;
   [SerializeField]
   private Button _actionTypeButton;
   [SerializeField]
   private GameObject[] _actionTypeGroup;

   // Stats UI
   [SerializeField]
   private InputField _intelligenceText;
   [SerializeField]
   private InputField _vitalityText;
   [SerializeField]
   private InputField _precisionText;
   [SerializeField]
   private InputField _strengthText;
   [SerializeField]
   private InputField _spiritText;
   [SerializeField]
   private InputField _luckText;

   // The preview of how the item looks like
   [SerializeField]
   private Image _armorSpriteImage, _weaponSpriteImage, _helmSpriteImage;
#pragma warning restore 0649
   #endregion
}