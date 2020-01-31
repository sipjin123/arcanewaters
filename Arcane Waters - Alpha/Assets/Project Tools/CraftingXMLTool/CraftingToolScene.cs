using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;

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
      string itemName = "Undefined";

      if (!toolManager.ifExists(itemName)) {
         CraftableItemTemplate template = GenericEntryTemplate.createGenericTemplate(craftableItemTemplate.gameObject, toolManager, craftableItemParent).GetComponent<CraftableItemTemplate>();
         template.editButton.onClick.AddListener(() => {
            craftingPanel.currentXMLTemplate = template;
            craftingPanel.gameObject.SetActive(true);
            craftingPanel.setData(requirementData);
         });
         template.deleteButton.onClick.AddListener(() => {
            toolManager.deleteCraftingDataFile(requirementData);
         });
         template.itemIcon.sprite = Util.getRawSpriteIcon(requirementData.resultItem.category, requirementData.resultItem.itemTypeId);

         template.gameObject.SetActive(true);
         toolManager.saveDataToFile(requirementData, false);
      }
   }

   public void refreshXML() {
      toolManager.loadAllDataFiles();
   }

   public void updatePanelWithCraftingIngredients (Dictionary<string, CraftableItemRequirements> _craftingData) {
      // Clear all the rows
      craftableItemParent.gameObject.DestroyChildren();

      // Create a row for each crafting ingredient
      foreach (CraftableItemRequirements craftingRequirement in _craftingData.Values) {
         CraftableItemTemplate template = GenericEntryTemplate.createGenericTemplate(craftableItemTemplate.gameObject, toolManager, craftableItemParent).GetComponent<CraftableItemTemplate>();
         template.updateItemDisplay(craftingRequirement.resultItem);
         template.editButton.onClick.AddListener(() =>
         {
            craftingPanel.currentXMLTemplate = template;
            craftingPanel.gameObject.SetActive(true);
            craftingPanel.setData(craftingRequirement);
         });
         template.deleteButton.onClick.AddListener(() => {
            Destroy(template.gameObject, .5f);
            toolManager.deleteCraftingDataFile(craftingRequirement);
         });
         template.itemIcon.sprite = Util.getRawSpriteIcon(craftingRequirement.resultItem.category, craftingRequirement.resultItem.itemTypeId);

         template.gameObject.SetActive(true);
      }
   }

   #region Private Variables

   #endregion
}
