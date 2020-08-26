using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;
using static CraftingToolManager;
using System.Linq;

public class CraftingToolScene : MonoBehaviour {
   #region Public Variables

   // Reference to the tool manager
   public CraftingToolManager toolManager;

   // Reference to crafting ingredient panel
   public CraftingIngredientPanel craftingPanel;

   // Parent holder of the craftable item templates
   public Transform craftableItemParent;

   // Craftable item template that holds the info of the items that can be crafted
   public CraftableItemTemplate craftableItemTemplate;

   // Button that generates a new crafting template
   public Button createTemplateButton;

   // Refreshes the file that is loaded in XML
   public Button refreshButton;

   // Opens the main tool
   public Button openMainTool;

   #endregion

   private void Awake () {
      openMainTool.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.masterScene);
      });
      craftingPanel.gameObject.SetActive(false);
      craftingPanel.popUpSelectionPanel.gameObject.SetActive(false);
      refreshButton.onClick.AddListener(() => refreshXML());
      createTemplateButton.onClick.AddListener(() => createNewTemplate(new CraftableItemRequirements()));

      if (!MasterToolAccountManager.canAlterData()) {
         createTemplateButton.gameObject.SetActive(false);
      }
   }

   private void createNewTemplate(CraftableItemRequirements requirementData) {
      requirementData.resultItem = new Item { category = Item.Category.None, itemTypeId = 0, count = 0 };

      CraftableItemTemplate template = GenericEntryTemplate.createGenericTemplate(craftableItemTemplate.gameObject, toolManager, craftableItemParent).GetComponent<CraftableItemTemplate>();
      template.xmlID = -1;
      template.editButton.onClick.AddListener(() => {
         craftingPanel.currentXMLTemplate = template;
         craftingPanel.gameObject.SetActive(true);
         craftingPanel.setData(requirementData, -1);
      });
      template.deleteButton.onClick.AddListener(() => {
         toolManager.deleteCraftingDataFile(template.xmlID);
      });

      if (requirementData.resultItem.category == Item.Category.Hats) {
         string hatSprite = EquipmentXMLManager.self.getHatData(requirementData.resultItem.itemTypeId).equipmentIconPath;
         template.itemIcon.sprite = ImageManager.getSprite(hatSprite);
      } else {
         template.itemIcon.sprite = Util.getRawSpriteIcon(requirementData.resultItem.category, requirementData.resultItem.itemTypeId);
      }

      template.setWarning();
      template.gameObject.SetActive(true);
      toolManager.saveDataToFile(requirementData, template.xmlID);
   }

   public void refreshXML() {
      toolManager.loadAllDataFiles();
   }

   public void updatePanelWithCraftingIngredients (List<CraftableRequirementXML> craftableList) {
      // Clear all the rows
      craftableItemParent.gameObject.DestroyChildren();

      createTemplates(craftableList.FindAll(_ => _.requirements.resultItem.category == Item.Category.Weapon).OrderBy(_=>_.requirements.resultItem.itemName).ToList());
      createTemplates(craftableList.FindAll(_ => _.requirements.resultItem.category == Item.Category.Armor).OrderBy(_ => _.requirements.resultItem.itemName).ToList());
      createTemplates(craftableList.FindAll(_ => _.requirements.resultItem.category == Item.Category.Hats).OrderBy(_ => _.requirements.resultItem.itemName).ToList());
      createTemplates(craftableList.FindAll(_ => _.requirements.resultItem.category == Item.Category.CraftingIngredients).OrderBy(_ => _.requirements.resultItem.itemName).ToList());
      createTemplates(craftableList.FindAll(_ => _.requirements.resultItem.category == Item.Category.None));
   }

   private void createTemplates (List<CraftableRequirementXML> craftableList) {
      // Create a row for each crafting ingredient
      foreach (CraftableRequirementXML xmlContent in craftableList) {
         CraftableItemTemplate template = GenericEntryTemplate.createGenericTemplate(craftableItemTemplate.gameObject, toolManager, craftableItemParent).GetComponent<CraftableItemTemplate>();
         template.updateItemDisplay(xmlContent.requirements.resultItem);
         template.xmlID = xmlContent.xmlID;
         template.editButton.onClick.AddListener(() => {
            craftingPanel.currentXMLTemplate = template;
            craftingPanel.gameObject.SetActive(true);
            craftingPanel.setData(xmlContent.requirements, xmlContent.xmlID);
         });
         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            toolManager.deleteCraftingDataFile(xmlContent.xmlID);
         });

         if (xmlContent.requirements.resultItem.category == Item.Category.None) {
            template.setWarning();
         }

         updateThisIcon(template.itemIcon, xmlContent.requirements.resultItem.category, xmlContent.requirements.resultItem.itemTypeId);

         template.gameObject.SetActive(true);
      }
   }

   public static void updateThisIcon (Image imageIcon, Item.Category category, int resultType) {
      switch (category) {
         case Item.Category.Weapon:
            string imagePath = EquipmentXMLManager.self.getWeaponData(resultType).equipmentIconPath;
            imageIcon.sprite = ImageManager.getSprite(imagePath);
            break;
         case Item.Category.Armor:
            imagePath = EquipmentXMLManager.self.getArmorData(resultType).equipmentIconPath;
            imageIcon.sprite = ImageManager.getSprite(imagePath);
            break;
         case Item.Category.Hats:
            imagePath = EquipmentXMLManager.self.getHatData(resultType).equipmentIconPath;
            imageIcon.sprite = ImageManager.getSprite(imagePath);
            break;
         default:
            imageIcon.sprite = Util.getRawSpriteIcon(category, resultType);
            break;
      }
   }

   #region Private Variables

   #endregion
}
