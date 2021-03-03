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

   // If the server finished the initialization of data
   public bool serverInitialized;

   // List of npc data for editor reviewing
   public List<NPCData> npcList = new List<NPCData>();

   // The default npc face sprite
   public Sprite defaultNpcFaceSprite;

   // The default npc body sprite
   public Sprite defaultNpcBodySprite;

   #endregion

   public void Awake () {
      self = this;
   }

   public void initializeQuestCache () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getNPCXML();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               NPCData npcData = Util.xmlLoad<NPCData>(newTextAsset);

               // Save the NPC data in the memory cache
               if (!_npcData.ContainsKey(npcData.npcId) && npcData.isActive) {
                  _npcData.Add(npcData.npcId, npcData);
                  npcList.Add(npcData);
               }
            }
            serverInitialized = true;
         });
      });
   }

   public void storeNpcIdPerArea (int npcId, string areaKey) {
#if IS_SERVER_BUILD
      // Add data to area collection
      if (_npcIDPerArea.ContainsKey(areaKey)) {
         if (!_npcIDPerArea[areaKey].Contains(npcId)) {
            _npcIDPerArea[areaKey].Add(npcId);
         } else {
            D.editorLog("Npc already Existing!", Color.cyan);
         }
      } else {
         _npcIDPerArea[areaKey] = new List<int>();
         _npcIDPerArea[areaKey].Add(npcId);
      }
#endif
   }

   public void storeNPC (NPC npc) {
      _npcs[npc.npcId] = npc;
      npc.gameObject.name += " : " + _npcs[npc.npcId].name + " " + npc.npcId;

#if IS_SERVER_BUILD
      string areaKey = npc.areaKey;

      // Add data to area collection
      if (_npcIDPerArea.ContainsKey(areaKey)) {
         _npcIDPerArea[areaKey].Add(npc.npcId);
      } else {
         _npcIDPerArea[areaKey] = new List<int>();
         _npcIDPerArea[areaKey].Add(npc.npcId);
      }
#endif
   }

   // Returns the list of npc data
   public List<NPCData> getNPCDataInArea (string areaKey) {
      if (_npcIDPerArea.ContainsKey(areaKey)) {
         List<NPCData> returnList = new List<NPCData>();
         foreach (int npcID in _npcIDPerArea[areaKey]) {
            if (_npcData.ContainsKey(npcID)) {
               returnList.Add(_npcData[npcID]);
            }
         }
         return returnList;
      } else {
         return new List<NPCData>();
      }
   }

   public NPC getNPC (int npcId) {
      if (_npcs.ContainsKey(npcId)) {
         return _npcs[npcId];
      }
      return null;
   }

   public void storeNPCData (NPCData npcData) {
      _npcData[npcData.npcId] = npcData;
   }

   public NPCData getNPCData (int npcID) {
      if (_npcData.ContainsKey(npcID)) {
         return _npcData[npcID];
      } else {
         return null;
      }
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
            if (likedGift.palettes != "") {
               if (likedGift.palettes != gift.paletteNames) {
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

   public bool isHireable (int npcId) {
      if (_npcData.ContainsKey(npcId)) {
         return _npcData[npcId].isHireable;
      } else {
         return true;
      }
   }

   #region Private Variables

   // Keeps track of the NPCs, based on their id
   protected Dictionary<int, NPC> _npcs = new Dictionary<int, NPC>();

   // The cached NPC data for interactive NPCs
   private Dictionary<int, NPCData> _npcData = new Dictionary<int, NPCData>();

   // The cached NPC data for interactive NPCs
   private Dictionary<string, List<int>> _npcIDPerArea = new Dictionary<string, List<int>>();

   #endregion
}
