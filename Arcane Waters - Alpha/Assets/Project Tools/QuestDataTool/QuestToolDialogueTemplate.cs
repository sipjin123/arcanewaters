using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class QuestToolDialogueTemplate : MonoBehaviour {
   // The item ui template of the item requirement
   public GenericItemUITemplate itemUITemplate;
   
   // Add item buttons
   public Button addItemRequirementButton, addItemRewardButton;

   // Deletes the template
   public Button deleteButton;

   // Basic dialogue data
   public InputField npcDialogue;
   public InputField playerDialogue;
   public InputField friendshipRewardPts;

   // The id of the dialogue
   public Text dialogueIdText;

   // Parent holding the item requirement/reward
   public Transform questRewardParent, questRequirementParent;

   // Reference to the generic selection
   public GenericSelectionPopup genericSelectionPopup;

   // Dropdown selection of the item to create
   public Dropdown rewardDropdown, requirementDropDown;

   // Job requirement ui
   public Dropdown jobTypeDropdown;
   public InputField jobLevelField;

   private void Awake () {
      List<Dropdown.OptionData> optionsList = new List<Dropdown.OptionData>();
      foreach (Jobs.Type category in Enum.GetValues(typeof(Jobs.Type))) {
         optionsList.Add(new Dropdown.OptionData { text = category.ToString() });
      }
      jobTypeDropdown.options = optionsList;

      addItemRequirementButton.onClick.AddListener(() => {
         GenericItemUITemplate itemTemplate = Instantiate(itemUITemplate.gameObject, questRequirementParent).GetComponent<GenericItemUITemplate>();
         itemTemplate.itemButton.onClick.AddListener(() => {
            string categoryText = requirementDropDown.options[requirementDropDown.value].text;
            Item.Category category = (Item.Category) Enum.Parse(typeof(Item.Category), categoryText);
            itemTemplate.itemCategory.text = ((int) category).ToString();
            genericSelectionPopup.callItemTypeSelectionPopup(category, itemTemplate.itemName, itemTemplate.itemId, itemTemplate.itemIcon);
         });
         itemTemplate.deleteButton.onClick.AddListener(() => {
            Destroy(itemTemplate.gameObject);
         });
      });
      addItemRewardButton.onClick.AddListener(() => {
         GenericItemUITemplate itemTemplate = Instantiate(itemUITemplate.gameObject, questRewardParent).GetComponent<GenericItemUITemplate>();
         itemTemplate.itemButton.onClick.AddListener(() => {
            string categoryText = rewardDropdown.options[rewardDropdown.value].text;
            Item.Category category = (Item.Category) Enum.Parse(typeof(Item.Category), categoryText);
            itemTemplate.itemCategory.text = ((int) category).ToString();
            genericSelectionPopup.callItemTypeSelectionPopup(category, itemTemplate.itemName, itemTemplate.itemId, itemTemplate.itemIcon);
         });
         itemTemplate.deleteButton.onClick.AddListener(() => {
            Destroy(itemTemplate.gameObject);
         });
      });
   }

   public void setDialogueData (QuestDialogueNode dialogueData) {
      npcDialogue.text = dialogueData.npcDialogue;
      playerDialogue.text = dialogueData.playerDialogue;
      dialogueIdText.text = dialogueData.dialogueIdIndex.ToString();
      friendshipRewardPts.text = dialogueData.friendshipRewardPts.ToString();
      jobLevelField.text = dialogueData.jobLevelRequirement.ToString();
      jobTypeDropdown.value = dialogueData.jobTypeRequirement;


      if (dialogueData.itemRewards != null) {
         foreach (Item item in dialogueData.itemRewards) {
            loadItemTemplate(item, questRewardParent);
         }
      }

      if (dialogueData.itemRequirements != null) {
         foreach (Item item in dialogueData.itemRequirements) {
            loadItemTemplate(item, questRequirementParent);
         }
      }
   }

   public QuestDialogueNode getData () {
      QuestDialogueNode newDialogue = new QuestDialogueNode();
      newDialogue.npcDialogue = npcDialogue.text;
      newDialogue.playerDialogue = playerDialogue.text;
   
      newDialogue.itemRewards = convertItemUIToList(questRewardParent).ToArray();
      newDialogue.itemRequirements = convertItemUIToList(questRequirementParent).ToArray();
      newDialogue.friendshipRewardPts = int.Parse(friendshipRewardPts.text);
      newDialogue.dialogueIdIndex = int.Parse(dialogueIdText.text);

      newDialogue.jobLevelRequirement = int.Parse(jobLevelField.text);
      newDialogue.jobTypeRequirement = (int) Enum.Parse(typeof(Jobs.Type), jobTypeDropdown.options[jobTypeDropdown.value].text);

      return newDialogue;
   }

   private List<Item> convertItemUIToList (Transform parentObj) {
      List<Item> itemRewardList = new List<Item>();
      foreach (Transform itemTemplateObj in parentObj) {
         GenericItemUITemplate itemTemplate = itemTemplateObj.GetComponent<GenericItemUITemplate>();
         Item newItem = new Item();
         newItem.category = (Item.Category) int.Parse(itemTemplate.itemCategory.text);
         newItem.itemTypeId = int.Parse(itemTemplate.itemId.text);
         newItem.itemName = itemTemplate.itemName.text;
         newItem.count = int.Parse(itemTemplate.itemCount.text);

         itemRewardList.Add(newItem);
      }

      return itemRewardList;
   }

   private void loadItemTemplate (Item item, Transform parent) {
      GenericItemUITemplate itemTemplate = Instantiate(itemUITemplate.gameObject, parent).GetComponent<GenericItemUITemplate>();
      itemTemplate.itemCategory.text = ((int) item.category).ToString();
      itemTemplate.itemId.text = item.itemTypeId.ToString();
      switch (item.category) {
         case Item.Category.Weapon:
            WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(item.itemTypeId);
            itemTemplate.itemName.text = weaponData.equipmentName;
            itemTemplate.itemIcon.sprite = ImageManager.getSprite(weaponData.equipmentIconPath);
            break;
         case Item.Category.Hats:
            HatStatData hatData = EquipmentXMLManager.self.getHatData(item.itemTypeId);
            itemTemplate.itemName.text = hatData.equipmentName;
            itemTemplate.itemIcon.sprite = ImageManager.getSprite(hatData.equipmentIconPath);
            break;
         case Item.Category.Armor:
            ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(item.itemTypeId);
            itemTemplate.itemName.text = armorData.equipmentName;
            itemTemplate.itemIcon.sprite = ImageManager.getSprite(armorData.equipmentIconPath);
            break;
         case Item.Category.CraftingIngredients:
            CraftingIngredients.Type ingredientType = (CraftingIngredients.Type) item.itemTypeId;
            itemTemplate.itemName.text = CraftingIngredients.getName(ingredientType);
            itemTemplate.itemIcon.sprite = ImageManager.getSprite(CraftingIngredients.getIconPath(ingredientType));
            break;
      }
      itemTemplate.deleteButton.onClick.AddListener(() => {
         Destroy(itemTemplate.gameObject);
      });
   }
}
