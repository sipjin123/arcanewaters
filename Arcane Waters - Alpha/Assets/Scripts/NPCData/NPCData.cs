using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System;

[Serializable]
[XmlRoot("NPCData")]
public class NPCData
{
   #region Public Variables

   // The name of the NPC
   public string name = "";

   // The NPC ID
   public int npcId;

   // Determines if this npc can be a companion
   public bool isHireable = false;

   // The battler id associated with the npc
   public int landMonsterId = 0;

   // The greeting texts of the NPC for each friendship rank, shown every time the conversation starts
   public string greetingTextStranger = "";
   public string greetingTextAcquaintance = "";
   public string greetingTextCasualFriend = "";
   public string greetingTextCloseFriend = "";
   public string greetingTextBestFriend = "";

   // The npc text when offered a gift
   public string giftOfferNPCText = "";

   // The npc text when the gift is liked
   public string giftLikedText = "";

   // The npc text when the gift is not liked
   public string giftNotLikedText = "";

   // Gets set to true when the NPC has the crop rumor dialogue option
   public bool hasTradeGossipDialogue = true;

   // Gets set to true when the NPC has the Goodbye dialogue option
   public bool hasGoodbyeDialogue = true;

   // The last used quest id, used when creating new quests
   public int lastUsedQuestId;

   // The achievement id that is required to hire this companion
   public int achievementIdHiringRequirement = -1;

   // Quest ID reference
   public int questId;

   // The list of items that the NPC likes to be gifted
   [XmlArray("Gifts"), XmlArrayItem("Gift")]
   public List<NPCGiftData> gifts;

   // Holds the address of the image icon
   public string iconPath;

   // Holds the address of the image sprite within the game
   public string spritePath = "";

   // If this object is active in the database
   public bool isActive;

   #endregion

   public NPCData () {

   }

   public NPCData (int npcId, string greetingTextStranger, string greetingTextAcquaintance,
      string greetingTextCasualFriend, string greetingTextCloseFriend, string greetingTextBestFriend,
      string giftOfferNPCText, string giftLikedText, string giftNotLikedText, string name, bool hasTradeGossipDialogue, bool hasGoodbyeDialogue,
      int lastUsedQuestId, int questId, List<NPCGiftData> gifts, string iconPath, string spritePath, bool isHireable, int landMonsterId, int achievementIdHiringRequirement, bool isActive) {
      this.npcId = npcId;
      this.greetingTextStranger = greetingTextStranger;
      this.greetingTextAcquaintance = greetingTextAcquaintance;
      this.greetingTextCasualFriend = greetingTextCasualFriend;
      this.greetingTextCloseFriend = greetingTextCloseFriend;
      this.greetingTextBestFriend = greetingTextBestFriend;
      this.giftOfferNPCText = giftOfferNPCText;
      this.giftLikedText = giftLikedText;
      this.giftNotLikedText = giftNotLikedText;
      this.name = name;
      this.hasTradeGossipDialogue = hasTradeGossipDialogue;
      this.hasGoodbyeDialogue = hasGoodbyeDialogue;
      this.lastUsedQuestId = lastUsedQuestId;
      this.questId = questId;
      this.gifts = gifts;
      this.iconPath = iconPath;
      this.spritePath = spritePath;
      this.isHireable = isHireable;
      this.landMonsterId = landMonsterId;
      this.achievementIdHiringRequirement = achievementIdHiringRequirement;
      this.isActive = isActive;
   }

   #region Private Variables

   #endregion
}