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

   // Cached item category selected in the popup
   public Item.Category selectedCategory;

   // Button to confirm the item selection
   public Button confirmSelectionButton;

   // Button to close the item selection
   public Button exitSelectionButton;

   // Holds the address of the image icon
   public string npcIconPath;

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

   // Button that allows changing of avatar
   public Button changeAvatarButton;

   // Button that closes popup window of avatar selection
   public Button closeAvatarSelectionButton;

   // The cache list for avatar icon selection
   public Dictionary<string,Sprite> iconSpriteList = new Dictionary<string, Sprite>();

   // Enum to determine the current item category
   public enum ItemSelectionType
   {
      None,
      Gift,
      Reward,
      Delivery
   }

   #endregion

   public void Awake () {
      itemTypeSelectionPanel.SetActive(false);
      exitSelectionButton.onClick.AddListener(() => { itemTypeSelectionPanel.SetActive(false); });
      confirmSelectionButton.onClick.AddListener(() => {
         itemTypeSelectionPanel.SetActive(false);
      });

      changeAvatarButton.onClick.AddListener(() => {
         showIconSelector();
      });
      closeAvatarSelectionButton.onClick.AddListener(() => {
         iconSelectionPanel.SetActive(false);
      });
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

      if (!_initSpriteList) {
         _initSpriteList = true;
         string[] spriteIconFiles = Directory.GetFiles(Application.dataPath + "/Sprites/Icons/NPCAvatarIcon", "*.png", SearchOption.AllDirectories);

         foreach (string spritefile in spriteIconFiles) {
            string assetPath = "Assets" + spritefile.Replace(Application.dataPath, "").Replace('\\', '/');
            Sprite sourceSprite = (Sprite) UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, typeof(Sprite));
            iconSpriteList.Add(assetPath, sourceSprite);
         }
      }

      avatarIcon.sprite = iconSpriteList[npcData.iconPath];

      _npcId = npcData.npcId;
      _lastUsedQuestId = npcData.lastUsedQuestId;

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
   }

   public void toggleGiftView () {
      giftInfo.SetActive(!giftInfo.activeSelf);
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
      NPCData data = NPCToolManager.self.getNPCData(_npcId);

      // Overwrite the panel values
      updatePanelWithNPC(data);
   }

   public void saveButtonClickedOn () {
      // Retrieve the quest list
      List<Quest> questList = new List<Quest>();
      foreach (QuestRow questRow in questRowsContainer.GetComponentsInChildren<QuestRow>()) {
         questList.Add(questRow.getModifiedQuest());
      }

      List<NPCGiftData> newGiftDataList = new List<NPCGiftData>();
      foreach(ItemRewardRow itemRow in giftNode.cachedItemRowsList) {
         NPCGiftData newGiftData = new NPCGiftData();
         newGiftData.itemCategory = itemRow.getItem().category;
         newGiftData.itemTypeId = itemRow.getItem().itemTypeId;

         newGiftDataList.Add(newGiftData);
      }

      // Create a new npcData object and initialize it with the values from the UI
      NPCData npcData = new NPCData(_npcId, greetingStranger.text, greetingAcquaintance.text,
         greetingCasualFriend.text, greetingCloseFriend.text, greetingBestFriend.text, giftOfferText.text,
         giftLiked.text, giftNotLiked.text, npcName.text, (Faction.Type) faction.value,
         (Specialty.Type) specialty.value, hasTradeGossip.isOn, hasGoodbye.isOn, _lastUsedQuestId,
         questList, newGiftDataList, npcIconPath);

      // Save the data
      NPCToolManager.self.updateNPCData(npcData);

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
         template.SetActive(true);
      }
      updateTypeOptions(selectionType);
   }

   public void showIconSelector () {
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
      }
   }

   private void updateTypeOptions (ItemSelectionType selectionType) {
      // Dynamically handles the type of item
      Type itemType = Util.getItemType(selectedCategory);
      itemTypeParent.gameObject.DestroyChildren();

      Dictionary<int, string> itemNameList = new Dictionary<int, string>();
      if (itemType != null) {
         foreach (object item in Enum.GetValues(itemType)) {
            int newVal = (int) item;
            itemNameList.Add(newVal, item.ToString());
         }

         var sortedList = itemNameList.OrderBy(r => r.Value);
         foreach (var item in sortedList) {
            GameObject template = Instantiate(itemTypePrefab, itemTypeParent);
            ItemTypeTemplate itemTemp = template.GetComponent<ItemTypeTemplate>();
            itemTemp.itemTypeText.text = item.Value.ToString();
            itemTemp.itemIndexText.text = "" + item.Key;
            itemTemp.spriteIcon.sprite = Util.getRawSpriteIcon(selectedCategory, item.Key);

            itemTemp.selectButton.onClick.AddListener(() => {
               selectedTypeID = (int) item.Key;

               if (selectionType == ItemSelectionType.Gift) {
                  giftNode.currentItemModifying.itemCategory.text = ((int) selectedCategory).ToString();
                  giftNode.currentItemModifying.itemTypeId.text = selectedTypeID.ToString();

                  giftNode.currentItemModifying.itemCategoryName.text = selectedCategory.ToString();
                  giftNode.currentItemModifying.itemTypeName.text = item.Value.ToString();
                  giftNode.currentItemModifying.itemIcon.sprite = Util.getRawSpriteIcon(selectedCategory, item.Key);

                  giftNode.cachedGiftData.itemCategory = selectedCategory;
                  giftNode.cachedGiftData.itemTypeId = selectedTypeID;
               } else if(selectionType == ItemSelectionType.Reward) {
                  currentQuestModified.currentQuestNode.currentItemModifying.itemCategory.text = ((int) selectedCategory).ToString();
                  currentQuestModified.currentQuestNode.currentItemModifying.itemTypeId.text = selectedTypeID.ToString();

                  currentQuestModified.currentQuestNode.currentItemModifying.itemCategoryName.text = selectedCategory.ToString();
                  currentQuestModified.currentQuestNode.currentItemModifying.itemTypeName.text = item.Value.ToString();
                  currentQuestModified.currentQuestNode.currentItemModifying.itemIcon.sprite = Util.getRawSpriteIcon(selectedCategory, item.Key);

                  currentQuestModified.currentQuestNode.cachedReward.category = selectedCategory;
                  currentQuestModified.currentQuestNode.cachedReward.itemTypeId = selectedTypeID;
               } else if (selectionType == ItemSelectionType.Delivery) {
                  currentQuestModified.currentQuestNode.currentDeliverObjective.itemCategory.text = ((int) selectedCategory).ToString();
                  currentQuestModified.currentQuestNode.currentDeliverObjective.itemTypeId.text = selectedTypeID.ToString();

                  currentQuestModified.currentQuestNode.currentDeliverObjective.itemCategoryName.text = selectedCategory.ToString();
                  currentQuestModified.currentQuestNode.currentDeliverObjective.itemTypeName.text = item.Value.ToString();
                  currentQuestModified.currentQuestNode.currentDeliverObjective.itemIcon.sprite = Util.getRawSpriteIcon(selectedCategory, item.Key);
               }

               confirmSelectionButton.onClick.Invoke();
            });
         }
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

   // If the sprite list is initialized
   private bool _initSpriteList;

   #endregion
}
