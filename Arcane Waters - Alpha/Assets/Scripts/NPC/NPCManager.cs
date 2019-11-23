using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System;
using System.Linq;

public class NPCManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static NPCManager self;

   // The files containing the NPC data
   public TextAsset[] npcDataAssets;

   // Checks if basic data has been set
   public bool basicDataInitialized;

   // Returns the list of basic data
   public List<NPCBasicData> getBasicDataList () {
      return _npcBasicData.Values.ToList();
   }

   #endregion

   public void Awake () {
      self = this;
   }

   public void initializeQuestCache () {
      // Server translate npc data to xml
      List<NPCBasicData> newDataList = new List<NPCBasicData>();

      // Iterate over the files
      foreach (TextAsset textAsset in npcDataAssets) {
         // Read and deserialize the file
         NPCData npcData = Util.xmlLoad<NPCData>(textAsset);

         // Save the NPC data in the memory cache
         _npcCompleteData.Add(npcData.npcId, npcData);

         // Setup basic data to provide to clients
         NPCBasicData basicData = new NPCBasicData {
            npcId = npcData.npcId,
            spritePath = npcData.spritePath,
            name = npcData.name,
            faction = npcData.faction,
            specialty = npcData.specialty,
         };
         newDataList.Add(basicData);
      }

      // Server initializes basic data to send to clients
      initializeBasicData(newDataList.ToArray());
   }

   public void initializeBasicData (NPCBasicData[] basicDataList) {
      if (basicDataInitialized == false) {
         basicDataInitialized = true;
         foreach (NPCBasicData basicData in basicDataList) {
            // Save the NPC data in the memory cache
            if (_npcBasicData.ContainsKey(basicData.npcId)) {
               D.log("Key already exists: " + basicData.npcId);
            } else {
               _npcBasicData.Add(basicData.npcId, basicData);
            }

            // Initializes basic info of the npc
            if (_npcs.ContainsKey(basicData.npcId)) {
               _npcs[basicData.npcId].initData();
            }
         }
      }
   }

   public void storeNPC (NPC npc) {
      _npcs[npc.npcId] = npc;
   }

   public NPC getNPC (int npcId) {
      return _npcs[npcId];
   }

   public NPCBasicData getNPCBasicData (int npcID) {
      if (_npcBasicData.ContainsKey(npcID)) {
         return _npcBasicData[npcID];
      } else {
         return null;
      }
   }

   public List<Quest> getQuests (int npcId) {
      if (_npcCompleteData.ContainsKey(npcId)) {
         return _npcCompleteData[npcId].quests;
      } else {
         return new List<Quest>();
      }
   }

   public Quest getQuest (int npcId, int questId) {
      NPCData npcData;
      if (!_npcCompleteData.TryGetValue(npcId, out npcData)) {
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
      if (_npcCompleteData.ContainsKey(npcId)) {
         switch (NPCFriendship.getRank(friendshipLevel)) {
            case NPCFriendship.Rank.Stranger:
               return _npcCompleteData[npcId].greetingTextStranger;
            case NPCFriendship.Rank.Acquaintance:
               return _npcCompleteData[npcId].greetingTextAcquaintance;
            case NPCFriendship.Rank.CasualFriend:
               return _npcCompleteData[npcId].greetingTextCasualFriend;
            case NPCFriendship.Rank.CloseFriend:
               return _npcCompleteData[npcId].greetingTextCloseFriend;
            case NPCFriendship.Rank.BestFriend:
               return _npcCompleteData[npcId].greetingTextBestFriend;
            default:
               return _npcCompleteData[npcId].greetingTextStranger;
         }
      } else {
         return "Hello! How can I help you?";
      }
   }

   public string getName (int npcId) {
      if (_npcBasicData.ContainsKey(npcId)) {
         return _npcBasicData[npcId].name;
      } else {
         return null;
      }
   }

   public Faction.Type getFaction (int npcId) {
      if (_npcBasicData.ContainsKey(npcId)) {
         return _npcBasicData[npcId].faction;
      } else {
         return Faction.Type.None;
      }
   }

   public Specialty.Type getSpecialty (int npcId) {
      if (_npcBasicData.ContainsKey(npcId)) {
         return _npcBasicData[npcId].specialty;
      } else {
         return Specialty.Type.None;
     }
   }

   public bool canOfferGift (int friendshipLevel) {
      return NPCFriendship.isRankAboveOrEqual(friendshipLevel, NPCFriendship.Rank.Acquaintance);
   }

   public string getGiftOfferNPCText (int npcId) {
      if (_npcCompleteData.ContainsKey(npcId)) {
         return _npcCompleteData[npcId].giftOfferNPCText;
      } else {
         return "";
      }
   }

   public string getGiftLikedText (int npcId) {
      if (_npcCompleteData.ContainsKey(npcId)) {
         return _npcCompleteData[npcId].giftLikedText;
      } else {
         return "";
      }
   }

   public string getGiftNotLikedText (int npcId) {
      if (_npcCompleteData.ContainsKey(npcId)) {
         return _npcCompleteData[npcId].giftNotLikedText;
      } else {
         return "";
      }
   }

   public int getRewardedFriendshipForGift (int npcId, Item gift) {
      int rewardedFriendship = 0;

      // Determine if the npc has data
      if (!_npcCompleteData.ContainsKey(npcId)) {
         return 0;
      }

      // Look for a gift that the npc likes with the same category and type
      foreach (NPCGiftData likedGift in _npcCompleteData[npcId].gifts) {
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
      if (_npcCompleteData.ContainsKey(npcId)) {
         return _npcCompleteData[npcId].hasTradeGossipDialogue;
      } else {
         return true;
      }
   }

   public bool hasGoodbyeDialogue (int npcId) {
      if (_npcCompleteData.ContainsKey(npcId)) {
         return _npcCompleteData[npcId].hasGoodbyeDialogue;
      } else {
         return true;
      }
   }

   #region Private Variables

   // Keeps track of the NPCs, based on their id
   protected Dictionary<int, NPC> _npcs = new Dictionary<int, NPC>();

   // The cached NPC data for interactive NPCs
   private Dictionary<int, NPCData> _npcCompleteData = new Dictionary<int, NPCData>();

   // The cached NPC data holding basic info of the npc
   private Dictionary<int, NPCBasicData> _npcBasicData = new Dictionary<int, NPCBasicData>();

   #endregion
}
