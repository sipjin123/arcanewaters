using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.Animations;

public class QuestToolDialogueTemplate : MonoBehaviour {
   // The item ui template of the item requirement
   public GenericItemUITemplate itemUITemplate;
   
   // Add item buttons
   public Button addItemRequirementButton, addItemRewardButton;

   // Basic dialogue data
   public InputField npcDialogue;
   public InputField playerDialogue;
   public InputField friendshipRewardPts;

   // Parent holding the item requirement/reward
   public Transform questRewardParent, questRequirementParent;

   // Reference to the generic selection
   public GenericSelectionPopup genericSelectionPopup;

   // Dropdown selection of the item to create
   public Dropdown rewardDropdown, requirementDropDown;

   private void Awake () {
      addItemRequirementButton.onClick.AddListener(() => {
         GenericItemUITemplate itemTemplate = Instantiate(itemUITemplate.gameObject, questRequirementParent).GetComponent<GenericItemUITemplate>();
         itemTemplate.itemButton.onClick.AddListener(() => {
            string categoryText = requirementDropDown.options[rewardDropdown.value].text;
            Item.Category category = (Item.Category) Enum.Parse(typeof(Item.Category), categoryText);
            itemTemplate.itemCategory.text = ((int) category).ToString();
            genericSelectionPopup.callItemTypeSelectionPopup(category, itemTemplate.itemName, itemTemplate.itemId, itemTemplate.itemIcon);
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
      });
   }

   public void setDialogueData (QuestDialogueNode dialogueData) {
      npcDialogue.text = dialogueData.npcDialogue;
      playerDialogue.text = dialogueData.playerDialogue;

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
            break;
         case Item.Category.Hats:
            HatStatData hatData = EquipmentXMLManager.self.getHatData(item.itemTypeId);
            itemTemplate.itemName.text = hatData.equipmentName;
            break;
         case Item.Category.Armor:
            ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(item.itemTypeId);
            itemTemplate.itemName.text = armorData.equipmentName;
            break;
         case Item.Category.CraftingIngredients:
            itemTemplate.itemName.text = CraftingIngredients.getName((CraftingIngredients.Type) item.itemTypeId);
            break;
      }
   }
}
