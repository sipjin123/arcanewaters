using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System;

[XmlRoot("NPCData")]
public class NPCData
{
   #region Public Variables

   // The NPC ID
   public int npcId;

   // The name of the NPC
   public string name = "";

   // The faction of the NPC
   [XmlIgnore]
   public Faction.Type faction = Faction.Type.None;

   [XmlElement("faction")]
   public int FactionInt
   {
      get { return (int) faction; }
      set { faction = (Faction.Type) value; }
   }

   // The specialty of the NPC
   [XmlIgnore]
   public Specialty.Type specialty = Specialty.Type.None;

   [XmlElement("specialty")]
   public int SpecialtyInt
   {
      get { return (int) specialty; }
      set { specialty = (Specialty.Type) value; }
   }

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

   // The list of quests
   [XmlArray("Quests"), XmlArrayItem("Quest")]
   public List<Quest> quests;

   // The list of items that the NPC likes to be gifted
   [XmlArray("Gifts"), XmlArrayItem("Gift")]
   public List<NPCGiftData> gifts;

   #endregion

   public NPCData () {

   }

   public NPCData (int npcId, string greetingTextStranger, string greetingTextAcquaintance,
      string greetingTextCasualFriend, string greetingTextCloseFriend, string greetingTextBestFriend,
      string giftOfferNPCText, string giftLikedText, string giftNotLikedText, string name,
      Faction.Type faction, Specialty.Type specialty, bool hasTradeGossipDialogue, bool hasGoodbyeDialogue,
      List<Quest> quests, List<NPCGiftData> gifts) {
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
      this.faction = faction;
      this.specialty = specialty;
      this.hasTradeGossipDialogue = hasTradeGossipDialogue;
      this.hasGoodbyeDialogue = hasGoodbyeDialogue;
      this.quests = quests;
      this.gifts = gifts;
   }

   #region Private Variables

   #endregion
}
