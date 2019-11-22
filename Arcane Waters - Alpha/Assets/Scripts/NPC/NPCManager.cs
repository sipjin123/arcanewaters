﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System;

public class NPCManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static NPCManager self;

   // The files containing the NPC data
   public TextAsset[] npcDataAssets;

   #endregion

   public void Awake () {
      self = this;

      // Loads the NPC quest data
      initializeQuestCache();
   }

   public void initializeQuestCache () {
      // Iterate over the files
      foreach (TextAsset textAsset in npcDataAssets) {
         // Deserialize the file
         //NPCData npcData = JsonUtility.FromJson<NPCData>(textAsset.text);

         // Read and deserialize the file
         NPCData npcData = Util.xmlLoad<NPCData>(textAsset);

         // Save the NPC data in the memory cache
         _npcData.Add(npcData.npcId, npcData);
      }
   }

   public void storeNPC (NPC npc) {
      _npcs[npc.npcId] = npc;
   }

   public NPC getNPC (int npcId) {
      return _npcs[npcId];
   }

   public List<Quest> getQuests (int npcId) {
      if (_npcData.ContainsKey(npcId)) {
         return _npcData[npcId].quests;
      } else {
         return new List<Quest>();
      }
   }

   public Quest getQuest (int npcId, int questId) {
      NPCData npcData;
      if (!_npcData.TryGetValue(npcId, out npcData)) {
         D.error("The npc has no quest data: " + npcId);
         return null;
      }

      // Look for the quest
      foreach(Quest quest in npcData.quests) {
         if (quest.questId == questId) {
            return quest;
         }
      }

      D.error("The quest does not exist: " + npcId + "/" + questId);
      return null;
   }

   public QuestNode getQuestNode (int npcId, int questId, int questNodeId) {
      Quest quest = getQuest(npcId, questId);

      if (quest == null) {
         return null;
      }

      // Look for the quest node
      foreach (QuestNode node in quest.nodes) {
         if (node.nodeId == questNodeId) {
            return node;
         }
      }

      D.error("The quest node does not exist: " + npcId + "/" + questId + "/" + questNodeId);
      return null;
   }

   public string getGreetingText (int npcId, int friendshipLevel) {
      if (_npcData.ContainsKey(npcId)) {
         switch (NPCFriendship.getRank(friendshipLevel)) {
            case NPCFriendship.Rank.Stranger:
               return _npcData[npcId].greetingTextStranger;
            case NPCFriendship.Rank.Acquaintance:
               return _npcData[npcId].greetingTextAcquaintance;
            case NPCFriendship.Rank.CasualFriend:
               return _npcData[npcId].greetingTextCasualFriend;
            case NPCFriendship.Rank.CloseFriend:
               return _npcData[npcId].greetingTextCloseFriend;
            case NPCFriendship.Rank.BestFriend:
               return _npcData[npcId].greetingTextBestFriend;
            default:
               return _npcData[npcId].greetingTextStranger;
         }
      } else {
         return "Hello! How can I help you?";
      }
   }

   public string getName (int npcId) {
      if (_npcData.ContainsKey(npcId)) {
         return _npcData[npcId].name;
      } else {
         return null;
      }
   }

   public Faction.Type getFaction (int npcId) {
      if (_npcData.ContainsKey(npcId)) {
         return _npcData[npcId].faction;
      } else {
         return Faction.Type.None;
      }
   }

   public Specialty.Type getSpecialty (int npcId) {
      if (_npcData.ContainsKey(npcId)) {
         return _npcData[npcId].specialty;
      } else {
         return Specialty.Type.None;
     }
   }

   public bool canOfferGift (int friendshipLevel) {
      return NPCFriendship.isRankAboveOrEqual(friendshipLevel, NPCFriendship.Rank.Acquaintance);
   }

   public string getGiftOfferNPCText (int npcId) {
      if (_npcData.ContainsKey(npcId)) {
         return _npcData[npcId].giftOfferNPCText;
      } else {
         return "";
      }
   }

   public string getGiftLikedText (int npcId) {
      if (_npcData.ContainsKey(npcId)) {
         return _npcData[npcId].giftLikedText;
      } else {
         return "";
      }
   }

   public string getGiftNotLikedText (int npcId) {
      if (_npcData.ContainsKey(npcId)) {
         return _npcData[npcId].giftNotLikedText;
      } else {
         return "";
      }
   }

   public int getRewardedFriendshipForGift (int npcId, Item gift) {
      int rewardedFriendship = 0;

      // Determine if the npc has data
      if (!_npcData.ContainsKey(npcId)) {
         return 0;
      }

      // Look for a gift that the npc likes with the same category and type
      foreach (NPCGiftData likedGift in _npcData[npcId].gifts) {
         if (likedGift.itemCategory == gift.category && likedGift.itemTypeId == gift.itemTypeId) {
            
            // Compare the colors, if defined
            bool isColorMatch = true;
            if (likedGift.color1 != ColorType.None) {
               if (likedGift.color1 != gift.color1) {
                  isColorMatch = false;
               }
            }

            if (likedGift.color2 != ColorType.None) {
               if (likedGift.color2 != gift.color2) {
                  isColorMatch = false;
               }
            }

            // If everything matches, end the search and return the rewarded friendship
            if (isColorMatch) {
               rewardedFriendship = likedGift.rewardedFriendship;
               break;
            }
         }
      }

      return rewardedFriendship;
   }

   public bool hasTradeGossipDialogue (int npcId) {
      if (_npcData.ContainsKey(npcId)) {
         return _npcData[npcId].hasTradeGossipDialogue;
      } else {
         return true;
      }
   }

   public bool hasGoodbyeDialogue (int npcId) {
      if (_npcData.ContainsKey(npcId)) {
         return _npcData[npcId].hasGoodbyeDialogue;
      } else {
         return true;
      }
   }

   #region Private Variables

   // Keeps track of the NPCs, based on their id
   protected Dictionary<int, NPC> _npcs = new Dictionary<int, NPC>();

   // The cached NPC data for interactive NPCs
   private Dictionary<int, NPCData> _npcData = new Dictionary<int, NPCData>();

   #endregion
}
