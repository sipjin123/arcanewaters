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
   public Button createWeaponButton, createArmorButton, createHelmButton, exitButton, mainMenuButton;

   // Togglers
   public Toggle weaponToggle, armorToggle, helmToggle;

   #endregion

   private void Awake () {

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

      exitButton.onClick.AddListener(() => {
         Application.Quit();
      });
      mainMenuButton.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.masterScene);
      });
   }

   private void createWeaponTemplate () {
      WeaponStatData weaponData = new WeaponStatData();
      weaponData.equipmentName = "Undefined";
      weaponData.weaponClass = Weapon.Class.Any;

      EquipmentDataTemplate template = Instantiate(weaponTemplatePrefab, weaponTemplateParent.transform);
      template.equipmentType = EquipmentType.Weapon;
      template.nameText.text = weaponData.equipmentName+"("+ weaponData.weaponType + ")";
      template.indexText.text = weaponData.equipmentID.ToString();

      template.editButton.onClick.AddListener(() => {
         equipmentDataPanel.loadWeaponData(weaponData);
         equipmentDataPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         equipmentToolManager.deleteWeapon(weaponData);
         equipmentToolManager.loadXMLData();
      });

      template.duplicateButton.onClick.AddListener(() => {
         equipmentToolManager.duplicateWeapon(weaponData);
         equipmentToolManager.loadXMLData();
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

      EquipmentDataTemplate template = Instantiate(armorTemplatePrefab, armorTemplateParent.transform);
      template.equipmentType = EquipmentType.Armor;
      template.nameText.text = armorData.equipmentName + "(" + armorData.armorType + ")";
      template.indexText.text = armorData.equipmentID.ToString();

      template.editButton.onClick.AddListener(() => {
         equipmentDataPanel.loadArmorData(armorData);
         equipmentDataPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         equipmentToolManager.deleteArmor(armorData);
         equipmentToolManager.loadXMLData();
      });

      template.duplicateButton.onClick.AddListener(() => {
         equipmentToolManager.duplicateArmor(armorData);
         equipmentToolManager.loadXMLData();
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

      EquipmentDataTemplate template = Instantiate(helmTemplatePrefab, helmTemplateParent.transform);
      template.equipmentType = EquipmentType.Helm;
      template.nameText.text = helmData.equipmentName + "(" + helmData.helmType + ")";
      template.indexText.text = helmData.equipmentID.ToString();

      template.editButton.onClick.AddListener(() => {
         equipmentDataPanel.loadHelmData(helmData);
         equipmentDataPanel.gameObject.SetActive(true);
      });

      template.deleteButton.onClick.AddListener(() => {
         Destroy(template.gameObject, .5f);
         equipmentToolManager.deleteHelm(helmData);
         equipmentToolManager.loadXMLData();
      });

      template.duplicateButton.onClick.AddListener(() => {
         equipmentToolManager.duplicateHelm(helmData);
         equipmentToolManager.loadXMLData();
      });

      try {
         Sprite iconSprite = ImageManager.getSprite(helmData.equipmentIconPath);
         template.itemIcon.sprite = iconSprite;
      } catch {
         template.itemIcon.sprite = emptySprite;
      }

      template.gameObject.SetActive(true);
   }

   public void loadHelmData (Dictionary<string, HelmStatData> helmStats) {
      helmTemplateParent.gameObject.DestroyChildren();

      // Create a row for each weapon data element
      foreach (HelmStatData helmData in helmStats.Values) {
         EquipmentDataTemplate template = Instantiate(helmTemplatePrefab, helmTemplateParent.transform);
         template.nameText.text = helmData.equipmentName + "(" + helmData.helmType + ")";
         template.indexText.text = helmData.equipmentID.ToString();

         template.editButton.onClick.AddListener(() => {
            equipmentDataPanel.loadHelmData(helmData);
            equipmentDataPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            equipmentToolManager.deleteHelm(helmData);
            equipmentToolManager.loadXMLData();
         });

         template.duplicateButton.onClick.AddListener(() => {
            equipmentToolManager.duplicateHelm(helmData);
            equipmentToolManager.loadXMLData();
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


   public void loadArmorData (Dictionary<string, ArmorStatData> armorStats) {
      armorTemplateParent.gameObject.DestroyChildren();

      // Create a row for each weapon data element
      foreach (ArmorStatData armorData in armorStats.Values) {
         EquipmentDataTemplate template = Instantiate(armorTemplatePrefab, armorTemplateParent.transform);
         template.nameText.text = armorData.equipmentName + "(" + armorData.armorType + ")";
         template.indexText.text = armorData.equipmentID.ToString();

         template.editButton.onClick.AddListener(() => {
            equipmentDataPanel.loadArmorData(armorData);
            equipmentDataPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            equipmentToolManager.deleteArmor(armorData);
            equipmentToolManager.loadXMLData();
         });

         template.duplicateButton.onClick.AddListener(() => {
            equipmentToolManager.duplicateArmor(armorData);
            equipmentToolManager.loadXMLData();
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

   public void loadWeaponData (Dictionary<string, WeaponStatData> weaponStats) {
      weaponTemplateParent.gameObject.DestroyChildren();

      // Create a row for each weapon data element
      foreach (WeaponStatData weaponData in weaponStats.Values) {
         EquipmentDataTemplate template = Instantiate(weaponTemplatePrefab, weaponTemplateParent.transform);
         template.nameText.text = weaponData.equipmentName + "(" + weaponData.weaponType + ")";
         template.indexText.text = weaponData.equipmentID.ToString();

         template.editButton.onClick.AddListener(() => {
            equipmentDataPanel.loadWeaponData(weaponData);
            equipmentDataPanel.gameObject.SetActive(true);
         });

         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            equipmentToolManager.deleteWeapon(weaponData);
            equipmentToolManager.loadXMLData();
         });

         template.duplicateButton.onClick.AddListener(() => {
            equipmentToolManager.duplicateWeapon(weaponData);
            equipmentToolManager.loadXMLData();
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
