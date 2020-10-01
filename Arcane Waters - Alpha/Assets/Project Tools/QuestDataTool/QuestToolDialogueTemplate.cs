using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.Analytics;
using UnityEngine.EventSystems;
using MapCreationTool;
using EventTrigger = UnityEngine.EventSystems.EventTrigger;

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
   public Dropdown rewardDropdown, requirementDropDown, abilityRewardDropdown;

   // The text containing the ability id
   public Text abilityIdText;

   // The index of the ability dropdown rewards
   public List<int> abilityDropDownRewardIndex;

   // Job requirement ui
   public Dropdown jobTypeDropdown;
   public InputField jobLevelField;

   // The move bar obj
   public GameObject moveBar;

   // Adding dialogue in between nodes
   public Button addDialogueBelow, addDialogueAbove;

   private void Awake () {
      // Add event trigger for dragging
      EventTrigger eventTrigger = moveBar.GetComponent<EventTrigger>();
      Utilities.addPointerListener(eventTrigger, EventTriggerType.PointerDown, (e) => onKeyDown());
      Utilities.addPointerListener(eventTrigger, EventTriggerType.PointerEnter, (e) => onKeyHover());
      
      abilityDropDownRewardIndex = new List<int>();
      List<Dropdown.OptionData> abilityOptionsList = new List<Dropdown.OptionData>();

      // Add default data
      abilityOptionsList.Add(new Dropdown.OptionData { text = "None" });
      abilityDropDownRewardIndex.Add(-1);

      foreach (BasicAbilityData ability in QuestDataToolManager.instance.basicAbilityList) {
         abilityOptionsList.Add(new Dropdown.OptionData { text = ability.itemName.ToString() });
         abilityDropDownRewardIndex.Add(ability.itemID);
      }
      abilityRewardDropdown.options = abilityOptionsList;
      abilityRewardDropdown.onValueChanged.AddListener(_ => {
         abilityIdText.text = abilityDropDownRewardIndex[_].ToString();
      });

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

      addDialogueBelow.onClick.AddListener(() => {
         int newIndex = transform.GetSiblingIndex() + 1;
         QuestDataToolPanel.self.createDialogueTemplate(new QuestDialogueNode(), newIndex);
         QuestDataToolPanel.self.recalibrateDialogueIds();
      });
      addDialogueAbove.onClick.AddListener(() => {
         int newIndex = transform.GetSiblingIndex() - 1;
         if (newIndex < 0) {
            newIndex = 0;
         }
         QuestDataToolPanel.self.createDialogueTemplate(new QuestDialogueNode(), newIndex);
         QuestDataToolPanel.self.recalibrateDialogueIds();
      });
   }

   public void setDialogueData (QuestDialogueNode dialogueData) {
      npcDialogue.text = dialogueData.npcDialogue;
      playerDialogue.text = dialogueData.playerDialogue;
      dialogueIdText.text = dialogueData.dialogueIdIndex.ToString();
      friendshipRewardPts.text = dialogueData.friendshipRewardPts.ToString();
      jobLevelField.text = dialogueData.jobLevelRequirement.ToString();
      jobTypeDropdown.value = dialogueData.jobTypeRequirement;
      abilityIdText.text = dialogueData.abilityIdReward.ToString();
      if (dialogueData.abilityIdReward > 0) {
         BasicAbilityData fetchedAbilityData = QuestDataToolManager.instance.basicAbilityList.Find(_ => _.itemID == dialogueData.abilityIdReward);
         if (fetchedAbilityData != null) {
            abilityRewardDropdown.value = abilityDropDownRewardIndex.FindIndex(_ => _ == dialogueData.abilityIdReward);
         }
      }
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

   public void onKeyDown () {
      if (QuestDataToolPanel.self.selectedDialogueTemplate != this) {
         QuestDataToolPanel.self.selectDialogueNode(this);
      }
   }

   public void onKeyHover () {
      QuestDataToolPanel.self.hoverDialogueNode(this);
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

      newDialogue.abilityIdReward = int.Parse(abilityIdText.text);

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
      itemTemplate.itemCount.text = item.count.ToString();
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
         case Item.Category.Blueprint:
            CraftableItemRequirements craftingItem = QuestDataToolManager.instance.craftingDataList.Find(_ => _.xmlId == item.itemTypeId);

            string iconPath = "";
            string itemName = "";
            if (craftingItem.resultItem.category == Item.Category.Weapon) {
               WeaponStatData fetchedData = EquipmentXMLManager.self.getWeaponData(craftingItem.resultItem.itemTypeId);
               iconPath = fetchedData.equipmentIconPath;
               itemName = fetchedData.equipmentName;
            }
            if (craftingItem.resultItem.category == Item.Category.Armor) {
               ArmorStatData fetchedData = EquipmentXMLManager.self.getArmorData(craftingItem.resultItem.itemTypeId);
               iconPath = fetchedData.equipmentIconPath;
               itemName = fetchedData.equipmentName;
            }
            if (craftingItem.resultItem.category == Item.Category.Hats) {
               HatStatData fetchedData = EquipmentXMLManager.self.getHatData(craftingItem.resultItem.itemTypeId);
               iconPath = fetchedData.equipmentIconPath;
               itemName = fetchedData.equipmentName;
            }

            itemTemplate.itemName.text = itemName;
            itemTemplate.itemIcon.sprite = ImageManager.getSprite(iconPath);
            break;
      }
      itemTemplate.deleteButton.onClick.AddListener(() => {
         Destroy(itemTemplate.gameObject);
      });
   }
}
