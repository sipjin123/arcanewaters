using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using static EquipmentToolManager;
using UnityEngine.Events;

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
   public static int WEAPON_SPRITE_INDEX = 47;

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
   public UnityEvent changeIconEvent = new UnityEvent();

   // Event to notify if the action type has been modified
   public UnityEvent changeActionTypeEvent = new UnityEvent();

   // Gender type for preview
   public Gender.Type genderType = Gender.Type.Female;

   #endregion

   private void Awake () {
      if (!MasterToolAccountManager.canAlterData()) {
         saveButton.gameObject.SetActive(false);
      }

      createElementalModifierButton.onClick.AddListener(() => createModifierTemplate(ModifierType.Elemental));
      createRarityModifierButton.onClick.AddListener(() => createModifierTemplate(ModifierType.Rarity));

      _weaponClass.maxValue = Enum.GetValues(typeof(Weapon.Class)).Length - 1;
      _weaponClass.onValueChanged.AddListener(_ => {
         _weaponClassText.text = ((Weapon.Class)_).ToString();
      });

      _materialType.maxValue = Enum.GetValues(typeof(MaterialType)).Length - 1;
      _materialType.onValueChanged.AddListener(_ => {
         _materialTypeText.text = ((MaterialType) _).ToString();
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
         } else if (equipmentType == EquipmentType.Helm) {
            HelmStatData newStatData = getHelmStatData();
            if (newStatData != null) {
               equipmentToolManager.saveHelm(newStatData, currentXmlId, _isEnabled.isOn);
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
         } else if (equipmentType == EquipmentType.Helm) {
            genericSelectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.HelmIcon, _icon, _iconPath);
         }
      });

      _equipmentTypeButton.onClick.AddListener(() => {
         if (equipmentType == EquipmentType.Weapon) {
            genericSelectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.WeaponType, _equipmentTypeText, changeIconEvent);
         } else if (equipmentType == EquipmentType.Armor) {
            genericSelectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.ArmorTypeSprites, _equipmentTypeText, changeIconEvent);
         } else if (equipmentType == EquipmentType.Helm) {
            genericSelectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.HelmType, _equipmentTypeText);
         }
      });

      changeIconEvent.AddListener(() => {
         int spriteIndex = 0;
         string path = getInGameSprite(int.Parse(_equipmentTypeText.text));
         Sprite[] sprites = ImageManager.getSprites(path);

         if (equipmentType == EquipmentType.Weapon) {
            int targetWeaponIndex = WEAPON_SPRITE_INDEX;
            if (sprites.Length-1 >= targetWeaponIndex) {
               spriteIndex = targetWeaponIndex;
               _weaponSpriteImage.sprite = sprites[spriteIndex];
            }
         } else {
            try {
               _armorSpriteImage.sprite = sprites[spriteIndex];
            } catch {
               _armorSpriteImage.sprite = sprites[0];
            }
         }
      });

      _actionTypeButton.onClick.AddListener(() => genericSelectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.WeaponActionType, _actionType, changeActionTypeEvent));
      _colorType1Button.onClick.AddListener(() => genericSelectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.Color, _colorType1Text));
      _colorType2Button.onClick.AddListener(() => genericSelectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.Color, _colorType2Text));

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
      } else if (equipmentType == EquipmentType.Helm) {
         _itemTypeValue.text = "Helm Type:";
         _titleTabValue.text = "Helm Stats";
         _toolTipValue.message = "The defense of the helmet";
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

      int spriteIndex = 0;
      string path = getInGameSprite(statData.weaponType);
      Sprite[] sprites = ImageManager.getSprites(path);

      int targetWeaponIndex = WEAPON_SPRITE_INDEX;
      if (sprites.Length - 1 >= targetWeaponIndex) {
         spriteIndex = targetWeaponIndex;
         _weaponSpriteImage.sprite = sprites[spriteIndex];
      }

      _weaponClass.onValueChanged.Invoke(_weaponClass.value);
   }

   public void loadHelmData (HelmStatData statData, int template_id, bool isEnabled) {
      currentXmlId = template_id;
      startingType = (int) statData.helmType;
      equipmentType = EquipmentType.Helm;
      _isEnabled.isOn = isEnabled;

      foreach (GameObject obj in _actionTypeGroup) {
         obj.SetActive(false);
      }

      loadGenericInfo();
      loadEquipmentData(statData);

      _attributePhysical.text = statData.helmBaseDefense.ToString();
      _attributeFire.text = statData.fireResist.ToString();
      _attributeWater.text = statData.waterResist.ToString();
      _attributeAir.text = statData.airResist.ToString();
      _attributeEarth.text = statData.earthResist.ToString();
      _equipmentTypeText.text = statData.helmType.ToString();

      _weaponClass.onValueChanged.Invoke(_weaponClass.value);
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

      _colorType1Text.text = equipmentData.color1.ToString();
      _colorType2Text.text = equipmentData.color2.ToString();

      _materialType.value = (int) equipmentData.materialType;
      _materialTypeText.text = equipmentData.materialType.ToString();
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

   public HelmStatData getHelmStatData () {
      HelmStatData helmStatData = new HelmStatData();
      setEquipmentStatData(helmStatData);

      // Setup special data
      helmStatData.helmBaseDefense = int.Parse(_attributePhysical.text);
      helmStatData.waterResist = int.Parse(_attributeWater.text);
      helmStatData.earthResist = int.Parse(_attributeEarth.text);
      helmStatData.fireResist = int.Parse(_attributeFire.text);
      helmStatData.airResist = int.Parse(_attributeAir.text);
      helmStatData.helmType = (Helm.Type) Enum.Parse(typeof(Helm.Type), _equipmentTypeText.text);

      return helmStatData;
   }

   private void setEquipmentStatData (EquipmentStatData equipmentData) {
      // Setup base data
      equipmentData.equipmentName = _itemName.text;
      equipmentData.equipmentDescription = _itemDescription.text;
      equipmentData.equipmentID = int.Parse(_itemID.text);
      equipmentData.equipmentPrice = int.Parse(_itemPrice.text);
      equipmentData.canBeTrashed = _canBeTrashed.isOn;
      equipmentData.equipmentIconPath = _iconPath.text;

      equipmentData.setAllColors = _declareAllColors.isOn;
      equipmentData.materialType = (MaterialType) _materialType.value;

      equipmentData.color1 = (ColorType) Enum.Parse(typeof(ColorType), _colorType1Text.text);
      equipmentData.color2 = (ColorType) Enum.Parse(typeof(ColorType), _colorType2Text.text);

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
            spritePath = "Assets/Sprites/Weapons/" + genderType + "/" + "weapon_" + equipmentID +"_front";
            break;
         case EquipmentType.Helm:
         case EquipmentType.Armor:
            spritePath = "Assets/Sprites/Armor/" + genderType + "/" + genderType.ToString().ToLower() + "_armor_" + equipmentID;
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

   // Color Type
   [SerializeField]
   private Text _colorType1Text, _colorType2Text;
   [SerializeField]
   private Button _colorType1Button, _colorType2Button;

   // Icon UI
   [SerializeField]
   private Text _iconPath;
   [SerializeField]
   private Button _changeIconPathButton;
   [SerializeField]
   private Image _icon;

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
   private Image _armorSpriteImage, _weaponSpriteImage;
#pragma warning restore 0649
   #endregion
}