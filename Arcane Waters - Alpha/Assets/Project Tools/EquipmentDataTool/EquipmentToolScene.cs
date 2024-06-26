﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;
using static EquipmentToolManager;

public class EquipmentToolScene : MonoBehaviour {
   #region Public Variables

   // Holds the prefab for templates
   public EquipmentDataTemplate weaponTemplatePrefab, armorTemplatePrefab, helmTemplatePrefab;

   // Holds the tool manager reference
   public EquipmentToolManager equipmentToolManager;

   // The parent holding the selection template
   public GameObject weaponTemplateParent, armorTemplateParent, helmTemplateParent;

   // Holds the data panel
   public EquipmentToolPanel equipmentDataPanel;

   // Holds the empty sprite for null values
   public Sprite emptySprite;

   // Main menu Buttons
   public Button createWeaponButton, createArmorButton, createHelmButton, mainMenuButton;

   // Togglers
   public Toggle weaponToggle, armorToggle, helmToggle;

   #endregion

   private void Awake () {
      if (!MasterToolAccountManager.canAlterData()) {
         createWeaponButton.gameObject.SetActive(false);
         createArmorButton.gameObject.SetActive(false);
         createHelmButton.gameObject.SetActive(false);
      }

      weaponToggle.onValueChanged.AddListener(_ => {
         weaponTemplateParent.SetActive(_);
      });
      armorToggle.onValueChanged.AddListener(_ => {
         armorTemplateParent.SetActive(_);
      });
      helmToggle.onValueChanged.AddListener(_ => {
         helmTemplateParent.SetActive(_);
      });

      equipmentDataPanel.gameObject.SetActive(false);
      createWeaponButton.onClick.AddListener(() => {
         createWeaponTemplate();
      });
      createArmorButton.onClick.AddListener(() => {
         createArmorTemplate();
      });
      createHelmButton.onClick.AddListener(() => {
         createHatTemplate();
      });

      mainMenuButton.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.masterScene);
      });
   }

   private void createWeaponTemplate () {
      WeaponStatData weaponData = new WeaponStatData();
      weaponData.equipmentName = "Undefined";
      weaponData.weaponClass = Weapon.Class.Any;

      EquipmentDataTemplate template = GenericEntryTemplate.createGenericTemplate(weaponTemplatePrefab.gameObject, equipmentToolManager, weaponTemplateParent.transform).GetComponent<EquipmentDataTemplate>();
      template.setData(weaponData.equipmentName, EquipmentType.Weapon, -1);

      template.editButton.onClick.AddListener(() => {
         equipmentDataPanel.loadWeaponData(weaponData, template.xmlId, false);
         equipmentDataPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         equipmentToolManager.deleteWeapon(template.xmlId);
      });

      template.duplicateButton.onClick.AddListener(() => {
         equipmentToolManager.duplicateWeapon(weaponData);
      });

      try {
         Sprite iconSprite = ImageManager.getSprite(weaponData.equipmentIconPath);
         template.itemIcon.sprite = iconSprite;
      } catch {
         template.itemIcon.sprite = emptySprite;
      }

      template.setWarning();
      template.gameObject.SetActive(true);
   }

   private void createArmorTemplate () {
      ArmorStatData armorData = new ArmorStatData();
      armorData.equipmentName = "Undefined";

      EquipmentDataTemplate template = GenericEntryTemplate.createGenericTemplate(armorTemplatePrefab.gameObject, equipmentToolManager, armorTemplateParent.transform).GetComponent<EquipmentDataTemplate>();
      template.setData(armorData.equipmentName, EquipmentType.Armor, -1);

      template.editButton.onClick.AddListener(() => {
         equipmentDataPanel.loadArmorData(armorData, template.xmlId, false);
         equipmentDataPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         equipmentToolManager.deleteArmor(template.xmlId);
      });

      template.duplicateButton.onClick.AddListener(() => {
         equipmentToolManager.duplicateArmor(armorData);
      });

      try {
         Sprite iconSprite = ImageManager.getSprite(armorData.equipmentIconPath);
         template.itemIcon.sprite = iconSprite;
      } catch {
         template.itemIcon.sprite = emptySprite;
      }

      template.setWarning();
      template.gameObject.SetActive(true);
   }

   private void createHatTemplate () {
      HatStatData hatData = new HatStatData();
      hatData.equipmentName = "Undefined";

      EquipmentDataTemplate template = GenericEntryTemplate.createGenericTemplate(helmTemplatePrefab.gameObject, equipmentToolManager, helmTemplateParent.transform).GetComponent<EquipmentDataTemplate>();
      template.setData(hatData.equipmentName, EquipmentType.Hat, -1);

      template.editButton.onClick.AddListener(() => {
         equipmentDataPanel.loadHatData(hatData, template.xmlId, false);
         equipmentDataPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         equipmentToolManager.deleteHat(template.xmlId);
      });

      template.duplicateButton.onClick.AddListener(() => {
         equipmentToolManager.duplicateHat(hatData);
      });

      try {
         Sprite iconSprite = ImageManager.getSprite(hatData.equipmentIconPath);
         template.itemIcon.sprite = iconSprite;
      } catch {
         template.itemIcon.sprite = emptySprite;
      }

      template.setWarning();
      template.gameObject.SetActive(true);
   }

   public void loadHatData (List<HatXmlContent> hatStats) {
      helmTemplateParent.gameObject.DestroyChildren();

      // Create a row for each weapon data element
      foreach (HatXmlContent xmlData in hatStats) {
         HatStatData hatData = xmlData.hatStatData;
         EquipmentDataTemplate template = GenericEntryTemplate.createGenericTemplate(helmTemplatePrefab.gameObject, equipmentToolManager, helmTemplateParent.transform).GetComponent<EquipmentDataTemplate>();
         template.setData(hatData.equipmentName, EquipmentType.Hat, xmlData.xml_id);
         template.isEnabledIndicator.SetActive(xmlData.isEnabled);
         template.spriteID.text = ((int)hatData.hatType).ToString();

         template.editButton.onClick.AddListener(() => {
            equipmentDataPanel.loadHatData(hatData, template.xmlId, xmlData.isEnabled);
            equipmentDataPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            equipmentToolManager.deleteHat(template.xmlId);
         });

         template.duplicateButton.onClick.AddListener(() => {
            equipmentToolManager.duplicateHat(hatData);
         });

         try {
            Sprite iconSprite = ImageManager.getSprite(hatData.equipmentIconPath);
            template.itemIcon.sprite = iconSprite;
         } catch {
            template.itemIcon.sprite = emptySprite;
         }

         if (!Util.hasValidEntryName(template.nameText.text)) {
            template.setWarning();
         }
         template.gameObject.SetActive(true);

         template.indexText.gameObject.SetActive(MasterToolAccountManager.canAlterData());
         template.spriteID.gameObject.SetActive(MasterToolAccountManager.canAlterData());
      }
   }

   public void loadArmorData (List<ArmorXMLContent> armorStats) {
      armorTemplateParent.gameObject.DestroyChildren();

      // Create a row for each weapon data element
      foreach (ArmorXMLContent xmlData in armorStats) {
         ArmorStatData armorData = xmlData.armorStatData;
         EquipmentDataTemplate template = GenericEntryTemplate.createGenericTemplate(armorTemplatePrefab.gameObject, equipmentToolManager, armorTemplateParent.transform).GetComponent<EquipmentDataTemplate>();
         template.setData(armorData.equipmentName, EquipmentType.Armor, xmlData.xml_id);
         template.isEnabledIndicator.SetActive(xmlData.isEnabled);
         template.spriteID.text = armorData.armorType.ToString();

         template.editButton.onClick.AddListener(() => {
            equipmentDataPanel.loadArmorData(armorData, template.xmlId, xmlData.isEnabled);
            equipmentDataPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            equipmentToolManager.deleteArmor(template.xmlId);
         });

         template.duplicateButton.onClick.AddListener(() => {
            equipmentToolManager.duplicateArmor(armorData);
         });

         try {
            Sprite iconSprite = ImageManager.getSprite(armorData.equipmentIconPath);
            template.itemIcon.sprite = iconSprite;
         } catch {
            template.itemIcon.sprite = emptySprite;
         }

         if (!Util.hasValidEntryName(template.nameText.text)) {
            template.setWarning();
         }
         template.gameObject.SetActive(true);

         template.indexText.gameObject.SetActive(MasterToolAccountManager.canAlterData());
         template.spriteID.gameObject.SetActive(MasterToolAccountManager.canAlterData());
      }
   }

   public void loadWeaponData (List<WeaponXMLContent> weaponStats) {
      weaponTemplateParent.gameObject.DestroyChildren();

      // Create a row for each weapon data element
      foreach (WeaponXMLContent xmlData in weaponStats) {
         WeaponStatData weaponData = xmlData.weaponStatData;
         EquipmentDataTemplate template = GenericEntryTemplate.createGenericTemplate(weaponTemplatePrefab.gameObject, equipmentToolManager, weaponTemplateParent.transform).GetComponent<EquipmentDataTemplate>();
         template.setData(weaponData.equipmentName, EquipmentType.Weapon, xmlData.xml_id);
         template.isEnabledIndicator.SetActive(xmlData.isEnabled);
         template.spriteID.text = weaponData.weaponType.ToString();

         template.editButton.onClick.AddListener(() => {
            equipmentDataPanel.loadWeaponData(weaponData, template.xmlId, xmlData.isEnabled);
            equipmentDataPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            equipmentToolManager.deleteWeapon(template.xmlId);
         });

         template.duplicateButton.onClick.AddListener(() => {
            equipmentToolManager.duplicateWeapon(weaponData);
         });

         try {
            Sprite iconSprite = ImageManager.getSprite(weaponData.equipmentIconPath);
            template.itemIcon.sprite = iconSprite;
         } catch {
            template.itemIcon.sprite = emptySprite;
         }

         if (!Util.hasValidEntryName(template.nameText.text)) {
            template.setWarning();
         }
         template.gameObject.SetActive(true);

         template.indexText.gameObject.SetActive(MasterToolAccountManager.canAlterData());
         template.spriteID.gameObject.SetActive(MasterToolAccountManager.canAlterData());
      }
   }
}
