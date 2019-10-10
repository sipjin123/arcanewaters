using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class NPCEditionScreen : MonoBehaviour
{
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // The main parameters of the NPC
   public Text npcIdText;
   public InputField npcName;
   public InputField faction;
   public InputField specialty;
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

   // The container for the quests
   public GameObject questRowsContainer;

   // The prefab we use for creating quest rows
   public QuestRow questPrefab;

   // The panel scrollbar
   public Scrollbar scrollbar;

   #endregion

   public void updatePanelWithNPC (NPCData npcData) {
      _npcId = npcData.npcId;
      _lastUsedQuestId = npcData.lastUsedQuestId;

      // Fill all the fields with the values from the data file
      npcIdText.text = npcData.npcId.ToString();
      npcName.text = npcData.name;
      faction.text = ((int) npcData.faction).ToString();
      specialty.text = ((int) npcData.specialty).ToString();
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
            row.setRowForQuest(quest);
         }
      }
   }

   public void createQuestButtonClickedOn () {
      // Increment the last used quest id
      _lastUsedQuestId++;

      // Create a new empty quest
      Quest quest = new Quest(_lastUsedQuestId, "", NPCFriendship.Rank.Stranger, false, -1, null);

      // Create a new quest row
      QuestRow row = Instantiate(questPrefab, questRowsContainer.transform, false);
      row.transform.SetParent(questRowsContainer.transform, false);
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

      // Create a new npcData object and initialize it with the values from the UI
      NPCData npcData = new NPCData(_npcId, greetingStranger.text, greetingAcquaintance.text,
         greetingCasualFriend.text, greetingCloseFriend.text, greetingBestFriend.text, giftOfferText.text,
         giftLiked.text, giftNotLiked.text, npcName.text, (Faction.Type) int.Parse(faction.text),
         (Specialty.Type) int.Parse(specialty.text), hasTradeGossip.isOn, hasGoodbye.isOn, _lastUsedQuestId,
         questList, null);

      // Save the data
      NPCToolManager.self.updateNPCData(npcData);

      // Hide the screen
      hide();
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
