using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;

public class NPCEditScreen : MonoBehaviour
{
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // The main parameters of the NPC
   public Text npcIdText;
   public InputField npcName;
   public Slider faction;
   public Slider specialty;
   public Text factionText;
   public Text specialtyText;
   public Text factionCountText;
   public Text specialtyCountText;
   public Toggle hasTradeGossip;
   public Toggle hasGoodbye;
   public InputField greetingStranger;
   public InputField greetingAcquaintance;
   public InputField greetingCasualFriend;
   public InputField greetingCloseFriend;
   public InputField greetingBestFriend;
   public InputField giftOfferText;
   public InputField giftLiked;
   public InputField giftNotLiked;
   public InputField npcID;

   // Holds reference to the inputfields for character limit alterations
   public List<InputField> longTextInputfields;
   public List<InputField> shortTextInputfields;

   // Icons for the selected Faction/Specialty
   public Image factionImage;
   public Image specialtyImage;

   // The container for the quests
   public GameObject questRowsContainer;

   // The prefab we use for creating quest rows
   public QuestRow questPrefab;

   // The reference to determine which quest is being modified
   public QuestRow currentQuestModified;

   // The panel scrollbar
   public Scrollbar scrollbar;

   // Holds the info of the quests
   public GameObject questInfo;

   // Holds the info of the gifts
   public GameObject giftInfo;

   // Holds the info of the gifts
   public GiftNodeRow giftNode;

   // Item panel for selecting item type and category
   public GameObject itemTypeSelectionPanel;

   // Item prefab for item categories
   public GameObject itemCategoryPrefab;

   // Item prefab for item types
   public GameObject itemTypePrefab;

   // Parent holder of item categories
   public Transform itemCategoryParent;

   // Parent holder of item types
   public Transform itemTypeParent;

   // Cached int of current item type
   public int resultItemTypeInt;

   // Cached item type selected in the popup
   public int selectedTypeID;

   // Cached item data selected in the popup
   public string selectedData;

   // Cached item category selected in the popup
   public Item.Category selectedCategory;

   // Button to confirm the item selection
   public Button confirmSelectionButton;

   // Button to close the item selection
   public Button exitSelectionButton;

   // Holds the address of the image icon
   public string npcIconPath;

   // Holds the address of the image sprite
   public string npcSpritePath;

   // The panel that allows you to select an icon
   public GameObject iconSelectionPanel;

   // The prefab for icon templates
   public GameObject iconTemplatePrefab;

   // The container for the icon templates
   public Transform iconTemplateParent;

   // The current npc being modified
   public NPCSelectionRow currentNPCRow;

   // The icon of the NPC avatar
   public Image avatarIcon;

   // The sprite of the NPC avatar
   public Image avatarSprite;

   // Image to preview selection Icon
   public Image previewImage;

   // Button that allows changing of avatar and sprites
   public Button changeAvatarButton, changeGameSpriteButton;

   // Button that closes popup window of avatar selection
   public Button closeAvatarSelectionButton;

   // The cache list for avatar icon selection
   public Dictionary<string,Sprite> iconSpriteList = new Dictionary<string, Sprite>();

   // The cache list for avatar sprite selection
   public Dictionary<string, Sprite> avatarSpriteList = new Dictionary<string, Sprite>();

   // An image indicator for dropdown capabilities of quests
   public GameObject dropDownIndicatorQuest;

   // An image indicator for dropdown capabilities of gifts
   public GameObject dropDownIndicatorGifts;

   // The starting id of the npc
   public int startingID;

   // Save Button
   public Button saveButton;

   // Enum to determine the current item category
   public enum ItemSelectionType
   {
      None,
      Gift,
      Reward,
      Delivery,
      QuestRequirement
   }

   #endregion

   public void Awake () {
      foreach (InputField inputField in longTextInputfields) {
         inputField.characterLimit = 30;
      }
      foreach (InputField inputField in shortTextInputfields) {
         inputField.characterLimit = 20;
      }

      itemTypeSelectionPanel.SetActive(false);
      exitSelectionButton.onClick.AddListener(() => { itemTypeSelectionPanel.SetActive(false); });
      confirmSelectionButton.onClick.AddListener(() => {
         itemTypeSelectionPanel.SetActive(false);
      });

      changeAvatarButton.onClick.AddListener(() => {
         showIconSelector();
      });
      changeGameSpriteButton.onClick.AddListener(() => {
         showSpriteSelector();
      });
      closeAvatarSelectionButton.onClick.AddListener(() => {
         iconSelectionPanel.SetActive(false);
      });

      if (!MasterToolAccountManager.canAlterData()) {
         saveButton.gameObject.SetActive(false);
      }
   }

   public void convertFaction () {
      factionText.text = ((Faction.Type)faction.value).ToString();
      factionCountText.text = (int)faction.value + "/" + faction.maxValue;
      factionImage.sprite = Faction.getIcon((Faction.Type) faction.value);
   }

   public void convertSpecialty () {
      specialtyText.text = ((Specialty.Type) specialty.value).ToString();
      specialtyCountText.text = (int) specialty.value + "/" + specialty.maxValue;
      specialtyImage.sprite = Specialty.getIcon((Specialty.Type) specialty.value);
   }

   public void updatePanelWithNPC (NPCData npcData) {
      faction.maxValue = Enum.GetValues(typeof(Faction.Type)).Length-1;
      specialty.maxValue = Enum.GetValues(typeof(Specialty.Type)).Length-1;

      factionCountText.text = (int) faction.value + "/" + faction.maxValue;
      specialtyCountText.text = (int) specialty.value + "/" + specialty.maxValue;

      factionImage.sprite = Faction.getIcon((Faction.Type) faction.value);
      specialtyImage.sprite = Specialty.getIcon((Specialty.Type) specialty.value);

      try {
         avatarIcon.sprite = iconSpriteList[npcData.iconPath];
      } catch {
         avatarIcon.sprite = ImageManager.self.blankSprite;
      }

      try {
         avatarSprite.sprite = avatarSpriteList[npcData.spritePath];
      } catch {
         avatarSprite.sprite = ImageManager.self.blankSprite;
      }

      npcIconPath = npcData.iconPath;
      npcSpritePath = npcData.spritePath;
      _npcId = npcData.npcId;
      _lastUsedQuestId = npcData.lastUsedQuestId;
      startingID = npcData.npcId;

      // Fill all the fields with the values from the data file
      npcIdText.text = npcData.npcId.ToString();
      npcName.text = npcData.name;
      faction.value = (int) npcData.faction;
      specialty.value = (int) npcData.specialty;
      hasTradeGossip.isOn = npcData.hasTradeGossipDialogue;
      hasGoodbye.isOn = npcData.hasGoodbyeDialogue;
      greetingStranger.text = npcData.greetingTextStranger;
      greetingAcquaintance.text = npcData.greetingTextAcquaintance;
      greetingCasualFriend.text = npcData.greetingTextCasualFriend;
      greetingCloseFriend.text = npcData.greetingTextCloseFriend;
      greetingBestFriend.text = npcData.greetingTextBestFriend;
      giftOfferText.text = npcData.giftOfferNPCText;
      giftLiked.text = npcData.giftLikedText;
      giftNotLiked.text = npcData.giftNotLikedText;
      npcID.text = npcData.npcId.ToString();

      // Clear all the rows
      questRowsContainer.DestroyChildren();

      // Create a row for each quest
      if (npcData.quests != null) {
         foreach (Quest quest in npcData.quests) {
            // Create a new row
            QuestRow row = Instantiate(questPrefab, questRowsContainer.transform, false);
            row.transform.SetParent(questRowsContainer.transform, false);
            row.npcEditionScreen = this;
            row.setRowForQuest(quest);
         }
      }

      if (npcData.gifts != null) {
         giftNode.setRowForQuestNode(npcData.gifts);
      } else {
         giftNode.setRowForQuestNode(new List<NPCGiftData>());
      }
      giftNode.npcEditScreen = this;
   }

   public void toggleQuestView() {
      questInfo.SetActive(!questInfo.activeSelf);
      dropDownIndicatorQuest.SetActive(!questInfo.activeSelf);
   }

   public void toggleGiftView () {
      giftInfo.SetActive(!giftInfo.activeSelf);
      dropDownIndicatorGifts.SetActive(!giftInfo.activeSelf);
   }

   public void createQuestButtonClickedOn () {
      // Increment the last used quest id
      _lastUsedQuestId++;

      // Create a new empty quest
      Quest quest = new Quest(_lastUsedQuestId, "", NPCFriendship.Rank.Stranger, false, -1, null);

      // Create a new quest row
      QuestRow row = Instantiate(questPrefab, questRowsContainer.transform, false);
      row.transform.SetParent(questRowsContainer.transform, false);
      row.npcEditionScreen = this;
      row.setRowForQuest(quest);
   }

   public void revertButtonClickedOn () {
      // Get the unmodified data
      NPCData data = NPCToolManager.instance.getNPCData(_npcId);

      // Overwrite the panel values
      updatePanelWithNPC(data);

      // Hide the screen
      hide();

      NPCToolManager.instance.loadAllDataFiles();
   }

   public void saveButtonClickedOn () {
      // Retrieve the quest list
      List<Quest> questList = new List<Quest>();
      foreach (QuestRow questRow in questRowsContainer.GetComponentsInChildren<QuestRow>()) {
         questList.Add(questRow.getModifiedQuest());
      }

      List<NPCGiftData> newGiftDataList = new List<NPCGiftData>();
      foreach(ItemRewardRow itemRow in giftNode.cachedItemRowsList) {
         if (itemRow.isValidItem()) {
            NPCGiftData newGiftData = new NPCGiftData();
            newGiftData.itemCategory = itemRow.getItem().category;
            newGiftData.itemTypeId = itemRow.getItem().itemTypeId;
            newGiftData.rewardedFriendship = int.Parse(itemRow.GetComponent<FriendshipField>().friendshipPts.text);
            newGiftDataList.Add(newGiftData);
         }
      }

      // Create a new npcData object and initialize it with the values from the UI
      NPCData npcData = new NPCData(int.Parse(npcID.text), greetingStranger.text, greetingAcquaintance.text,
         greetingCasualFriend.text, greetingCloseFriend.text, greetingBestFriend.text, giftOfferText.text,
         giftLiked.text, giftNotLiked.text, npcName.text, (Faction.Type) faction.value,
         (Specialty.Type) specialty.value, hasTradeGossip.isOn, hasGoodbye.isOn, _lastUsedQuestId,
         questList, newGiftDataList, npcIconPath, npcSpritePath);

      if (startingID != int.Parse(npcID.text)) {
         // Delete overwritten npc
         NPCToolManager.instance.overWriteNPC(npcData, startingID);
      } else {
         // Save the new data
         NPCToolManager.instance.saveNPCDataToFile(npcData);
      }

      // Hide the screen
      hide();
   }

   public void toggleItemSelectionPanel (ItemSelectionType selectionType) {
      itemTypeSelectionPanel.SetActive(true);
      itemCategoryParent.gameObject.DestroyChildren();

      foreach (Item.Category category in Enum.GetValues(typeof(Item.Category))) {
           GameObject template = Instantiate(itemCategoryPrefab, itemCategoryParent);
            ItemCategoryTemplate categoryTemp = template.GetComponent<ItemCategoryTemplate>();
            categoryTemp.itemCategoryText.text = category.ToString();
            categoryTemp.itemIndexText.text = ((int) category).ToString();
            categoryTemp.itemCategory = category;
            categoryTemp.selectButton.onClick.AddListener(() => {
               selectedCategory = category;
               updateTypeOptions(selectionType);
            });

            if (category == Item.Category.Potion) {
               categoryTemp.selectButton.interactable = false;
               categoryTemp.selectButton.GetComponent<Image>().color = Color.gray;
            } else {
               template.SetActive(true);
            }
      }
      updateTypeOptions(selectionType);
   }

   public void toggleActionSelectionPanel () {
      itemTypeSelectionPanel.SetActive(true);
      itemCategoryParent.gameObject.DestroyChildren();

      foreach (KeyValuePair<int, AchievementData> achievementData in NPCToolManager.instance.achievementCollection) {
         GameObject template = Instantiate(itemCategoryPrefab, itemTypeParent);
         ItemCategoryTemplate actionTemplate = template.GetComponent<ItemCategoryTemplate>();
         actionTemplate.itemCategoryText.text = achievementData.Value.achievementName;
         actionTemplate.itemIndexText.text = achievementData.Key.ToString();

         actionTemplate.selectButton.onClick.AddListener(() => {
            currentQuestModified.currentQuestNode.cachedRequiredActionRow.actionTypeIndex = achievementData.Key;
            currentQuestModified.currentQuestNode.cachedRequiredActionRow.actionTypeLabel.text = achievementData.Value.achievementName;
            confirmSelectionButton.onClick.Invoke();
         });
      }
   }

   public void showIconSelector () {
      previewImage.sprite = ImageManager.self.blankSprite;
      iconSelectionPanel.SetActive(true);
      iconTemplateParent.gameObject.DestroyChildren();

      foreach (KeyValuePair<string, Sprite> sourceSprite in iconSpriteList) {
         GameObject iconTempObj = Instantiate(iconTemplatePrefab, iconTemplateParent);
         ItemTypeTemplate iconTemp = iconTempObj.GetComponent<ItemTypeTemplate>();
         iconTemp.spriteIcon.sprite = sourceSprite.Value;
         iconTemp.itemTypeText.text = sourceSprite.Value.name;
         iconTemp.selectButton.onClick.AddListener(() => {
            npcIconPath = sourceSprite.Key;
            avatarIcon.sprite = sourceSprite.Value;
            closeAvatarSelectionButton.onClick.Invoke();
         });

         iconTemp.previewButton.onClick.AddListener(() => {
            previewImage.sprite = sourceSprite.Value;
         });
      }
   }

   public void showSpriteSelector () {
      previewImage.sprite = ImageManager.self.blankSprite;
      iconSelectionPanel.SetActive(true);
      iconTemplateParent.gameObject.DestroyChildren();

      foreach (KeyValuePair<string, Sprite> sourceSprite in avatarSpriteList) {
         GameObject iconTempObj = Instantiate(iconTemplatePrefab, iconTemplateParent);
         ItemTypeTemplate iconTemp = iconTempObj.GetComponent<ItemTypeTemplate>();
         iconTemp.spriteIcon.sprite = sourceSprite.Value;
         iconTemp.itemTypeText.text = sourceSprite.Value.name;
         iconTemp.selectButton.onClick.AddListener(() => {
            npcSpritePath = sourceSprite.Key;
            avatarSprite.sprite = sourceSprite.Value;
            closeAvatarSelectionButton.onClick.Invoke();
         });

         iconTemp.previewButton.onClick.AddListener(() => {
            previewImage.sprite = sourceSprite.Value;
         });
      }
   }

   private void updateTypeOptions (ItemSelectionType selectionType) {
      // Dynamically handles the type of item
      itemTypeParent.gameObject.DestroyChildren();

      Dictionary<int, Item> itemNameList = GenericSelectionPopup.getItemCollection(selectedCategory, NPCToolManager.instance.craftingDataList);
      IOrderedEnumerable<KeyValuePair<int, Item>> sortedList = itemNameList.OrderBy(r => r.Value.itemName);
      foreach (KeyValuePair<int, Item> item in sortedList) {
         GameObject template = Instantiate(itemTypePrefab, itemTypeParent);
         ItemTypeTemplate itemTemp = template.GetComponent<ItemTypeTemplate>();
         itemTemp.itemTypeText.text = item.Value.itemName;
         itemTemp.itemIndexText.text = "" + item.Key;
         setupSpriteIcon(itemTemp.spriteIcon, selectedCategory, item.Key, item.Value.data);

         itemTemp.selectButton.onClick.AddListener(() => {
            selectedTypeID = item.Key;
            selectedData = item.Value.data;
            int newID = selectedTypeID;

            if (selectionType == ItemSelectionType.Gift) {
               giftNode.currentItemModifying.itemCategory.text = ((int) selectedCategory).ToString();
               giftNode.currentItemModifying.itemTypeId.text = selectedTypeID.ToString();

               giftNode.currentItemModifying.itemCategoryName.text = selectedCategory.ToString();
               giftNode.currentItemModifying.itemTypeName.text = item.Value.itemName.ToString();
               setupSpriteIcon(giftNode.currentItemModifying.itemIcon, selectedCategory, item.Key, item.Value.data);

               giftNode.cachedGiftData.itemCategory = selectedCategory;
               giftNode.cachedGiftData.itemTypeId = selectedTypeID;
            } else if (selectionType == ItemSelectionType.Reward) {
               currentQuestModified.currentQuestNode.currentRewardRow.itemCategory.text = ((int) selectedCategory).ToString();
               currentQuestModified.currentQuestNode.currentRewardRow.itemTypeId.text = newID.ToString();
               currentQuestModified.currentQuestNode.currentRewardRow.itemData = selectedData;

               currentQuestModified.currentQuestNode.currentRewardRow.itemCategoryName.text = selectedCategory.ToString();
               currentQuestModified.currentQuestNode.currentRewardRow.itemTypeName.text = item.Value.itemName.ToString();
               setupSpriteIcon(currentQuestModified.currentQuestNode.currentRewardRow.itemIcon, selectedCategory, item.Key, item.Value.data);

               currentQuestModified.currentQuestNode.cachedReward.category = selectedCategory;
               currentQuestModified.currentQuestNode.cachedReward.itemTypeId = newID;

               if (selectedCategory == Item.Category.Quest_Item) {
                  currentQuestModified.currentQuestNode.currentRewardRow.count.text = "1";
                  currentQuestModified.currentQuestNode.currentRewardRow.count.gameObject.SetActive(false);
               } else {
                  currentQuestModified.currentQuestNode.currentRewardRow.count.gameObject.SetActive(true);
               }
            } else if (selectionType == ItemSelectionType.Delivery) {
               currentQuestModified.currentQuestNode.currentDeliverObjective.itemCategory.text = ((int) selectedCategory).ToString();
               currentQuestModified.currentQuestNode.currentDeliverObjective.itemTypeId.text = newID.ToString();
               currentQuestModified.currentQuestNode.currentDeliverObjective.itemData = selectedData;

               currentQuestModified.currentQuestNode.currentDeliverObjective.itemCategoryName.text = selectedCategory.ToString();
               currentQuestModified.currentQuestNode.currentDeliverObjective.itemTypeName.text = item.Value.itemName.ToString();
               setupSpriteIcon(currentQuestModified.currentQuestNode.currentDeliverObjective.itemIcon, selectedCategory, item.Key, item.Value.data);

               currentQuestModified.currentQuestNode.cachedDeliverObjective.category = selectedCategory;
               currentQuestModified.currentQuestNode.cachedDeliverObjective.itemTypeId = newID;

               if (selectedCategory == Item.Category.Quest_Item) {
                  currentQuestModified.currentQuestNode.currentDeliverObjective.count.text = "1";
                  currentQuestModified.currentQuestNode.currentDeliverObjective.count.gameObject.SetActive(false);
               } else {
                  currentQuestModified.currentQuestNode.currentDeliverObjective.count.gameObject.SetActive(true);
               }
            }

            confirmSelectionButton.onClick.Invoke();
         });
      }
   }

   private void setupSpriteIcon (Image imageSprite, Item.Category category, int itemID, string data) {
      if (category == Item.Category.Helm) {
         string spritePath = EquipmentXMLManager.self.getHelmData(itemID).equipmentIconPath;
         imageSprite.sprite = ImageManager.getSprite(spritePath);
      } else if (category == Item.Category.Weapon) {
         string spritePath = EquipmentXMLManager.self.getWeaponData(itemID).equipmentIconPath;
         imageSprite.sprite = ImageManager.getSprite(spritePath);
      } else if (category == Item.Category.Armor) {
         string spritePath = EquipmentXMLManager.self.getArmorData(itemID).equipmentIconPath;
         imageSprite.sprite = ImageManager.getSprite(spritePath);
      } else if (category == Item.Category.Blueprint) {
         if (data.StartsWith(Blueprint.WEAPON_DATA_PREFIX)) {
            int modifiedID = Blueprint.modifyID(Item.Category.Blueprint, itemID);
            WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(modifiedID);
            if (weaponData != null) {
               imageSprite.sprite = ImageManager.getSprite(weaponData.equipmentIconPath);
            } else {
               Debug.LogError("Cant find this: " + modifiedID);
            }
         } else if (data.StartsWith(Blueprint.ARMOR_DATA_PREFIX)) {
            int modifiedID = Blueprint.modifyID(Item.Category.Blueprint, itemID);
            ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(modifiedID);
            if (armorData != null) {
               imageSprite.sprite = ImageManager.getSprite(armorData.equipmentIconPath);
            } else {
               Debug.LogError("Cant find this: " + modifiedID);
            }
         }
      } else {
         imageSprite.sprite = Util.getRawSpriteIcon(category, itemID);
      }
   }
   
   public void show () {
      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;
      this.gameObject.SetActive(true);
   }

   public void hide () {
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
      this.gameObject.SetActive(false);
   }

   #region Private Variables

   // The id of the NPC being edited
   private int _npcId;

   // The the last used quest id
   private int _lastUsedQuestId;

   #endregion
}
