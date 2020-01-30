using UnityEngine;
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
         createHelmTemplate();
      });

      mainMenuButton.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.masterScene);
      });
   }

   private void createWeaponTemplate () {
      WeaponStatData weaponData = new WeaponStatData();
      weaponData.equipmentName = "Undefined";
      weaponData.weaponClass = Weapon.Class.Any;

      EquipmentDataTemplate template = GenericEntryTemplate.CreateGenericTemplate(weaponTemplatePrefab.gameObject, equipmentToolManager, weaponTemplateParent.transform).GetComponent<EquipmentDataTemplate>();
      template.setData(weaponData.equipmentName, weaponData.equipmentID, EquipmentType.Weapon, -1);

      template.editButton.onClick.AddListener(() => {
         equipmentDataPanel.loadWeaponData(weaponData, template.xmlId);
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

      template.gameObject.SetActive(true);
   }

   private void createArmorTemplate () {
      ArmorStatData armorData = new ArmorStatData();
      armorData.equipmentName = "Undefined";

      EquipmentDataTemplate template = GenericEntryTemplate.CreateGenericTemplate(armorTemplatePrefab.gameObject, equipmentToolManager, armorTemplateParent.transform).GetComponent<EquipmentDataTemplate>();
      template.setData(armorData.equipmentName, armorData.equipmentID, EquipmentType.Armor, -1);

      template.editButton.onClick.AddListener(() => {
         equipmentDataPanel.loadArmorData(armorData, template.xmlId);
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

      template.gameObject.SetActive(true);
   }

   private void createHelmTemplate () {
      HelmStatData helmData = new HelmStatData();
      helmData.equipmentName = "Undefined";

      EquipmentDataTemplate template = GenericEntryTemplate.CreateGenericTemplate(helmTemplatePrefab.gameObject, equipmentToolManager, helmTemplateParent.transform).GetComponent<EquipmentDataTemplate>();
      template.setData(helmData.equipmentName, helmData.equipmentID, EquipmentType.Helm, -1);

      template.editButton.onClick.AddListener(() => {
         equipmentDataPanel.loadHelmData(helmData, template.xmlId);
         equipmentDataPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         equipmentToolManager.deleteHelm(template.xmlId);
      });

      template.duplicateButton.onClick.AddListener(() => {
         equipmentToolManager.duplicateHelm(helmData);
      });

      try {
         Sprite iconSprite = ImageManager.getSprite(helmData.equipmentIconPath);
         template.itemIcon.sprite = iconSprite;
      } catch {
         template.itemIcon.sprite = emptySprite;
      }

      template.gameObject.SetActive(true);
   }

   public void loadHelmData (List<HelmXMLContent> helmStats) {
      helmTemplateParent.gameObject.DestroyChildren();

      // Create a row for each weapon data element
      foreach (HelmXMLContent xmlData in helmStats) {
         HelmStatData helmData = xmlData.helmStatData;
         EquipmentDataTemplate template = GenericEntryTemplate.CreateGenericTemplate(helmTemplatePrefab.gameObject, equipmentToolManager, helmTemplateParent.transform).GetComponent<EquipmentDataTemplate>();
         template.setData(helmData.equipmentName, helmData.equipmentID, EquipmentType.Helm, xmlData.xml_id);
         template.editButton.onClick.AddListener(() => {
            equipmentDataPanel.loadHelmData(helmData, template.xmlId);
            equipmentDataPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            equipmentToolManager.deleteHelm(template.xmlId);
         });

         template.duplicateButton.onClick.AddListener(() => {
            equipmentToolManager.duplicateHelm(helmData);
         });

         try {
            Sprite iconSprite = ImageManager.getSprite(helmData.equipmentIconPath);
            template.itemIcon.sprite = iconSprite;
         } catch {
            template.itemIcon.sprite = emptySprite;
         }

         template.gameObject.SetActive(true);
      }
   }


   public void loadArmorData (List<ArmorXMLContent> armorStats) {
      armorTemplateParent.gameObject.DestroyChildren();

      // Create a row for each weapon data element
      foreach (ArmorXMLContent xmlData in armorStats) {
         ArmorStatData armorData = xmlData.armorStatData;
         EquipmentDataTemplate template = GenericEntryTemplate.CreateGenericTemplate(armorTemplatePrefab.gameObject, equipmentToolManager, armorTemplateParent.transform).GetComponent<EquipmentDataTemplate>();
         template.setData(armorData.equipmentName, armorData.equipmentID, EquipmentType.Armor, xmlData.xml_id);

         template.editButton.onClick.AddListener(() => {
            equipmentDataPanel.loadArmorData(armorData, template.xmlId);
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

         template.gameObject.SetActive(true);
      }
   }

   public void loadWeaponData (List<WeaponXMLContent> weaponStats) {
      weaponTemplateParent.gameObject.DestroyChildren();

      // Create a row for each weapon data element
      foreach (WeaponXMLContent xmlData in weaponStats) {
         WeaponStatData weaponData = xmlData.weaponStatData;
         EquipmentDataTemplate template = GenericEntryTemplate.CreateGenericTemplate(weaponTemplatePrefab.gameObject, equipmentToolManager, weaponTemplateParent.transform).GetComponent<EquipmentDataTemplate>();
         template.setData(weaponData.equipmentName, weaponData.equipmentID, EquipmentType.Weapon, xmlData.xml_id);

         template.editButton.onClick.AddListener(() => {
            equipmentDataPanel.loadWeaponData(weaponData, template.xmlId);
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

         template.gameObject.SetActive(true);
      }
   }
}
