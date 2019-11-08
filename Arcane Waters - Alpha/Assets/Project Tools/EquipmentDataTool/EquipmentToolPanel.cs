﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using static EquipmentToolManager;

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
   public enum ModifierType
   {
      None = 0,
      Elemental = 1,
      Rarity = 2
   }

   // Buttons for saving and canceling
   public Button saveButton, cancelButton;

   // Caches the initial type incase it is changed
   public string startingName;

   // Generic popup selection
   public GenericSelectionPopup genericSelectionPopup;

   // The type of equipment being reviewed
   public EquipmentType equipmentType;

   #endregion

   private void Awake () {
      createElementalModifierButton.onClick.AddListener(() => createModifierTemplate(ModifierType.Elemental));
      createRarityModifierButton.onClick.AddListener(() => createModifierTemplate(ModifierType.Rarity));

      _weaponClass.maxValue = Enum.GetValues(typeof(Weapon.Class)).Length - 1;
      _weaponClass.onValueChanged.AddListener(_ => {
         _weaponClassText.text = ((Weapon.Class)_).ToString();
      });

      saveButton.onClick.AddListener(() => {
         if (equipmentType == EquipmentType.Weapon) {
            WeaponStatData newStatData = getWeaponStatData();
            if (newStatData != null) {
               if (newStatData.equipmentName != startingName) {
                  equipmentToolManager.deleteWeapon(new WeaponStatData { equipmentName = startingName });
               }
               equipmentToolManager.saveWeapon(newStatData);
               gameObject.SetActive(false);
               equipmentToolManager.loadXMLData();
            }
         } else if (equipmentType == EquipmentType.Armor) {
            ArmorStatData newStatData = getArmorStatData();
            if (newStatData != null) {
               if (newStatData.equipmentName != startingName) {
                  equipmentToolManager.deleteArmor(new ArmorStatData { equipmentName = startingName });
               }
               equipmentToolManager.saveArmor(newStatData);
               gameObject.SetActive(false);
               equipmentToolManager.loadXMLData();
            }
         } else if (equipmentType == EquipmentType.Helm) {
            HelmStatData newStatData = getHelmStatData();
            if (newStatData != null) {
               if (newStatData.equipmentName != startingName) {
                  equipmentToolManager.deleteHelm(new HelmStatData { equipmentName = startingName });
               }
               equipmentToolManager.saveHelm(newStatData);
               gameObject.SetActive(false);
               equipmentToolManager.loadXMLData();
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
      _weaponTypeButton.onClick.AddListener(() => {
         if (equipmentType == EquipmentType.Weapon) {
            genericSelectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.WeaponType, _weaponTypeText);
         } else if (equipmentType == EquipmentType.Armor) {
            genericSelectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.ArmorType, _weaponTypeText);
         } else if (equipmentType == EquipmentType.Helm) {
            genericSelectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.HelmType, _weaponTypeText);
         }
      });
      _colorType1Button.onClick.AddListener(() => genericSelectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.Color, _colorType1Text));
      _colorType2Button.onClick.AddListener(() => genericSelectionPopup.callTextSelectionPopup(GenericSelectionPopup.selectionType.Color, _colorType2Text));
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
         _itemTypeValue.text = "Weapon Type:";
         _titleTabValue.text = "Weapon Stats";
         _toolTipValue.message = "The damage of the weapon";
         _itemRawValue.text = "Base Damage";
         _damageClassObj.SetActive(true);
      } else if (equipmentType == EquipmentType.Armor) {
         _itemTypeValue.text = "Armor Type:";
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

   public void loadArmorData (ArmorStatData statData) {
      equipmentType = EquipmentType.Armor;

      loadGenericInfo();
      loadEquipmentData(statData);

      _weaponDamage.text = statData.armorBaseDefense.ToString();
      _weaponTypeText.text = statData.armorType.ToString();

      _weaponClass.onValueChanged.Invoke(_weaponClass.value);
   }

   public void loadWeaponData (WeaponStatData statData) {
      equipmentType = EquipmentType.Weapon;

      loadGenericInfo();
      loadEquipmentData(statData);

      _weaponDamage.text = statData.weaponBaseDamage.ToString();
      _weaponTypeText.text = statData.weaponType.ToString();
      _weaponClass.value = (int) statData.weaponClass;
      _weaponClassText.text = statData.weaponClass.ToString();

      _weaponClass.onValueChanged.Invoke(_weaponClass.value);
   }

   public void loadHelmData (HelmStatData statData) {
      equipmentType = EquipmentType.Helm;

      loadGenericInfo();
      loadEquipmentData(statData);

      _weaponDamage.text = statData.helmBaseDefense.ToString();
      _weaponTypeText.text = statData.helmType.ToString();

      _weaponClass.onValueChanged.Invoke(_weaponClass.value);
   }

   private void loadEquipmentData (EquipmentStatData equipmentData) {
      elementalModifierList = new List<DamageModifierTemplate>();
      rarityModifierList = new List<DamageModifierTemplate>();
      elementalModifierParent.DestroyChildren();
      rarityModifierParent.DestroyChildren();

      _itemName.text = equipmentData.equipmentName;
      _itemDescription.text = equipmentData.equipmentDescription;
      _itemID.text = equipmentData.equipmentID.ToString();
      _itemPrice.text = equipmentData.equipmentPrice.ToString();
      _canBeTrashed.isOn = equipmentData.canBeTrashed;
      _iconPath.text = equipmentData.equipmentIconPath;

      _colorType1Text.text = equipmentData.color1.ToString();
      _colorType2Text.text = equipmentData.color2.ToString();

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
      weaponStatData.weaponBaseDamage = int.Parse(_weaponDamage.text);
      weaponStatData.weaponType = (Weapon.Type) Enum.Parse(typeof(Weapon.Type), _weaponTypeText.text);
      weaponStatData.weaponClass = (Weapon.Class) Enum.Parse(typeof(Weapon.Class), _weaponClassText.text);
      
      return weaponStatData;
   }

   public ArmorStatData getArmorStatData () {
      ArmorStatData armorStatData = new ArmorStatData();
      setEquipmentStatData(armorStatData);

      // Setup special data
      armorStatData.armorBaseDefense = int.Parse(_weaponDamage.text);
      armorStatData.armorType = (Armor.Type) Enum.Parse(typeof(Armor.Type), _weaponTypeText.text);
      
      return armorStatData;
   }

   public HelmStatData getHelmStatData () {
      HelmStatData helmStatData = new HelmStatData();
      setEquipmentStatData(helmStatData);

      // Setup special data
      helmStatData.helmBaseDefense = int.Parse(_weaponDamage.text);
      helmStatData.helmType = (Helm.Type) Enum.Parse(typeof(Helm.Type), _weaponTypeText.text);

      return helmStatData;
   }

   private void setEquipmentStatData (EquipmentStatData equpmentData) {
      // Setup base data
      equpmentData.equipmentName = _itemName.text;
      equpmentData.equipmentDescription = _itemDescription.text;
      equpmentData.equipmentID = int.Parse(_itemID.text);
      equpmentData.equipmentPrice = int.Parse(_itemPrice.text);
      equpmentData.canBeTrashed = _canBeTrashed.isOn;
      equpmentData.equipmentIconPath = _iconPath.text;

      equpmentData.color1 = (ColorType) Enum.Parse(typeof(ColorType), _colorType1Text.text);
      equpmentData.color2 = (ColorType) Enum.Parse(typeof(ColorType), _colorType2Text.text);

      equpmentData.rarityModifiers = getRarityModifiers();
      equpmentData.elementModifiers = getElementalModifiers();

      equpmentData.statsData.intelligence = int.Parse(_intelligenceText.text);
      equpmentData.statsData.vitality = int.Parse(_vitalityText.text);
      equpmentData.statsData.strength = int.Parse(_strengthText.text);
      equpmentData.statsData.precision = int.Parse(_precisionText.text);
      equpmentData.statsData.luck = int.Parse(_luckText.text);
      equpmentData.statsData.spirit = int.Parse(_spiritText.text);
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

   #region Private Variables

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
   private InputField _itemID;

   // Price of the item in the shop
   [SerializeField]
   private InputField _itemPrice;

   // Determines if the item can be trashed
   [SerializeField]
   private Toggle _canBeTrashed;

   // Input field variables for float and int values
   [SerializeField]
   private InputField _weaponDamage;

   // Sliders
   [SerializeField]
   private Slider _weaponClass;
   [SerializeField]
   private Text _weaponClassText;

   // Type selection UI
   [SerializeField]
   private Text _weaponTypeText;
   [SerializeField]
   private Button _weaponTypeButton;

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

   #endregion
}