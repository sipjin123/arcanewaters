using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using UnityEngine.Events;

public class NPCEditScreen : MonoBehaviour
{
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // The main parameters of the NPC
   public Text npcIdText;
   public InputField npcName;
   public Toggle interactable;
   public Toggle hasTradeGossip;
   public Toggle hasGoodbye;
   public Toggle isActive;
   public Toggle isStationary;
   public InputField greetingStranger;
   public InputField greetingAcquaintance;
   public InputField greetingCasualFriend;
   public InputField greetingCloseFriend;
   public InputField greetingBestFriend;
   public InputField giftOfferText;
   public InputField giftLiked;
   public InputField giftNotLiked;
   public InputField npcID;

   // Sliders that alter shadow transform
   public Slider shadowScale;
   public Slider shadowOffsetY;

   // Reference to the shadow
   public Transform shadowTransform;

   // The indication on how big is the offset adjustment of the shadow
   public Text shadowOffsetYText;

   // The indication on how big is the shadow is
   public Text shadowScaleText;

   // The npc preview in game
   public SpriteRenderer npcGamePreview;

   // The quest id
   public Button changeQuestButton;
   public Text questNameText;
   public Text questIdText;
   public Image questImage;
   public UnityEvent changedQuestSelection = new UnityEvent();

   // Holds reference to the inputfields for character limit alterations
   public List<InputField> longTextInputfields;
   public List<InputField> shortTextInputfields;

   // The panel scrollbar
   public Scrollbar scrollbar;

   // If the npc is recruitable
   public Toggle isHireableToggle;

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

   // An image indicator for dropdown capabilities of gifts
   public GameObject dropDownIndicatorGifts;

   // The starting id of the npc
   public int startingID;

   // Save Button
   public Button saveButton;

   // Achievement requirement info for hiring a companion
   public Button achievementRequirementHireButton;
   public Text achievementRequirementHireID;
   public Text achievmentRequirementHireName;

   // Battler type selection
   public Button selectBattlertype;
   public Text selectedBattlertype;
   public Text selectedBattlerIndex;
   public GameObject selectedBattlerGroup;

   // Reference to the selection popup
   public GenericSelectionPopup genericSelectionPopup;

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
      shadowOffsetY.onValueChanged.AddListener(_ => {
         shadowOffsetYText.text = _.ToString("f2");
         shadowTransform.localPosition = new Vector3(0, _, 0);
      });
      shadowScale.onValueChanged.AddListener(_ => {
         shadowScaleText.text = _.ToString("f2");
         shadowTransform.localScale = new Vector3(_, _, _);
      });

      foreach (InputField inputField in longTextInputfields) {
         inputField.characterLimit = 60;
      }
      foreach (InputField inputField in shortTextInputfields) {
         inputField.characterLimit = 30;
      }

      itemTypeSelectionPanel.SetActive(false);
      exitSelectionButton.onClick.AddListener(() => { itemTypeSelectionPanel.SetActive(false); });
      confirmSelectionButton.onClick.AddListener(() => {
         itemTypeSelectionPanel.SetActive(false);
      });

      changeQuestButton.onClick.AddListener(() => {
         changedQuestSelection.AddListener(() => {
            int questIndex = int.Parse(questIdText.text);
            try {
               QuestData questData = NPCToolManager.instance.questDataList.Find(_=>_.questId == questIndex);
               questImage.sprite = ImageManager.getSprite(questData.iconPath);
            } catch {
               questImage.sprite = ImageManager.self.blankSprite;
            }
         });

         genericSelectionPopup.callTextNameIndexPopup(GenericSelectionPopup.selectionType.QuestSelection, questNameText, questIdText, changedQuestSelection);
      });

      achievementRequirementHireButton.onClick.AddListener(() => {
         toggleActionSelectionPanel(true);
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

      selectBattlertype.onClick.AddListener(() => {
         toggleBattlerDataSelectionPanel();
      });

      isHireableToggle.onValueChanged.AddListener(isOn => {
         selectedBattlerGroup.SetActive(isOn);
         if (!isOn) {
            selectedBattlerIndex.text = "0";
            selectedBattlertype.text = "None";
         }
      });
   }

   public void updatePanelWithNPC (NPCData npcData) {
      avatarIcon.sprite = ImageManager.getSprite(npcData.iconPath);
      avatarSprite.sprite = ImageManager.getSprite(npcData.spritePath);
      npcGamePreview.sprite = ImageManager.getSprite(npcData.spritePath);

      npcIconPath = npcData.iconPath;
      npcSpritePath = npcData.spritePath;
      _npcId = npcData.npcId;
      _lastUsedQuestId = npcData.lastUsedQuestId;
      startingID = npcData.npcId;

      // Fill all the fields with the values from the data file
      npcIdText.text = npcData.npcId.ToString();
      npcName.text = npcData.name;
      interactable.isOn = npcData.interactable;
      isStationary.isOn = npcData.isStationary;
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
      isHireableToggle.isOn = npcData.isHireable;
      selectedBattlerIndex.text = npcData.landMonsterId.ToString();
      achievementRequirementHireID.text = npcData.achievementIdHiringRequirement.ToString();
      isActive.isOn = npcData.isActive;

      shadowOffsetY.value = npcData.shadowOffsetY;
      shadowScale.value = npcData.shadowScale;
      shadowOffsetYText.text = npcData.shadowOffsetY.ToString("f2");
      shadowScaleText.text = npcData.shadowScale.ToString("f2");

      if (NPCToolManager.instance.achievementCollection.ContainsKey(npcData.achievementIdHiringRequirement)) {
         achievmentRequirementHireName.text = NPCToolManager.instance.achievementCollection[npcData.achievementIdHiringRequirement].achievementName;
      } else {
         achievmentRequirementHireName.text = "None";
      }

      if (NPCToolManager.instance.battlerList.ContainsKey(npcData.landMonsterId)) {
         selectedBattlertype.text = NPCToolManager.instance.battlerList[npcData.landMonsterId].enemyName;
      } else {
         selectedBattlertype.text = "None";
      }

      if (npcData.questId > 0) {
         QuestData questData = NPCToolManager.instance.questDataList.Find(_ => _.questId == npcData.questId);
         questNameText.text = questData.questGroupName;
         questIdText.text = questData.questId.ToString();
         questImage.sprite = ImageManager.getSprite(questData.iconPath);
      } else {
         questNameText.text = "None";
         questIdText.text = "0";
         questImage.sprite = ImageManager.self.blankSprite;
      }

      if (npcData.gifts != null) {
         giftNode.setRowForQuestNode(npcData.gifts);
      } else {
         giftNode.setRowForQuestNode(new List<NPCGiftData>());
      }
      giftNode.npcEditScreen = this;
   }

   public void toggleGiftView () {
      giftInfo.SetActive(!giftInfo.activeSelf);
      dropDownIndicatorGifts.SetActive(!giftInfo.activeSelf);
   }

   public void revertButtonClickedOn () {
      // Get the unmodified data
      NPCData data = NPCToolManager.instance.getNPCData(_npcId);

      // Overwrite the panel values
      updatePanelWithNPC(data);

      // Hide the screen
      hide();

      //NPCToolManager.instance.loadAllDataFiles();
   }

   public void saveButtonClickedOn () {
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
         giftLiked.text, giftNotLiked.text, npcName.text, interactable.isOn, hasTradeGossip.isOn, hasGoodbye.isOn, _lastUsedQuestId,
         int.Parse(questIdText.text), newGiftDataList, npcIconPath, npcSpritePath, isHireableToggle.isOn, int.Parse(selectedBattlerIndex.text), 
         int.Parse(achievementRequirementHireID.text), isActive.isOn, shadowOffsetY.value, shadowScale.value, isStationary.isOn);

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

   public void toggleBattlerDataSelectionPanel () {
      itemTypeSelectionPanel.SetActive(true);
      itemCategoryParent.gameObject.DestroyChildren();
      itemTypeParent.gameObject.DestroyChildren();

      foreach (KeyValuePair<int, BattlerData> battlerData in NPCToolManager.instance.battlerList) {
         GameObject template = Instantiate(itemCategoryPrefab, itemTypeParent);
         ItemCategoryTemplate actionTemplate = template.GetComponent<ItemCategoryTemplate>();
         actionTemplate.itemCategoryText.text = battlerData.Value.enemyName;
         actionTemplate.itemIndexText.text = battlerData.Key.ToString();

         actionTemplate.selectButton.onClick.AddListener(() => {
            selectedBattlertype.text = battlerData.Value.enemyName;
            selectedBattlerIndex.text = battlerData.Key.ToString();
            confirmSelectionButton.onClick.Invoke();
         });
      }
   }

   public void toggleActionSelectionPanel (bool useForCompanionSetup = false) {
      itemTypeSelectionPanel.SetActive(true);
      itemCategoryParent.gameObject.DestroyChildren();
      itemTypeParent.gameObject.DestroyChildren();

      foreach (KeyValuePair<int, AchievementData> achievementData in NPCToolManager.instance.achievementCollection) {
         GameObject template = Instantiate(itemCategoryPrefab, itemTypeParent);
         ItemCategoryTemplate actionTemplate = template.GetComponent<ItemCategoryTemplate>();
         actionTemplate.itemCategoryText.text = achievementData.Value.achievementName;
         actionTemplate.itemIndexText.text = achievementData.Key.ToString();

         actionTemplate.selectButton.onClick.AddListener(() => {
            if (useForCompanionSetup) {
               achievementRequirementHireID.text = achievementData.Key.ToString();
               achievmentRequirementHireName.text = achievementData.Value.achievementName;
               itemTypeSelectionPanel.SetActive(false);
            } else {
               confirmSelectionButton.onClick.Invoke();
            }
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
            npcGamePreview.sprite = sourceSprite.Value;
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
            } 

            confirmSelectionButton.onClick.Invoke();
         });
      }
   }

   private void setupSpriteIcon (Image imageSprite, Item.Category category, int itemID, string data) {
      if (category == Item.Category.Hats) {
         string spritePath = EquipmentXMLManager.self.getHatData(itemID).equipmentIconPath;
         imageSprite.sprite = ImageManager.getSprite(spritePath);
      } else if (category == Item.Category.Weapon) {
         string spritePath = EquipmentXMLManager.self.getWeaponData(itemID).equipmentIconPath;
         imageSprite.sprite = ImageManager.getSprite(spritePath);
      } else if (category == Item.Category.Armor) {
         string spritePath = EquipmentXMLManager.self.getArmorDataByType(itemID).equipmentIconPath;
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
            ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataByType(modifiedID);
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
