using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Linq;

namespace ServerCommunicationHandlerv2 {
   public class SharedServerDataHandler : MonoBehaviour
   {
      #region Public Variables

      // Self
      public static SharedServerDataHandler self;

      // Reference to the server communication handler
      public ServerCommunicationHandler serverCommuncationHandler;

      // List of voyage invitations
      public List<VoyageInviteData> pendingVoyageInvites = new List<VoyageInviteData>();

      // The chat list existing in the database
      public List<ChatInfo> chatInfoList = new List<ChatInfo>();

      // The latest chat info
      public ChatInfo latestChatinfo = new ChatInfo();

      #endregion

      private void Awake () {
         self = this;
      }

      public void initializeHandler () {
         // Continously handle shared server data
         InvokeRepeating("handleSharedServerData", 2, .5f);
      }

      private void handleSharedServerData () {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            pendingVoyageInvites = DB_Main.getAllVoyageInvites();
            ChatInfo latestChatInfo = DB_Main.getLatestChatInfo();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               // Handle new voyage invite data
               handleVoyageInvites();

               // Handle new chat data
               if (latestChatinfo.chatId > 0) {
                  List<ChatInfo> chatList = chatInfoList.OrderByDescending(_ => _.chatTime).ToList();
                  if (chatList.Count > 0) {
                     if (latestChatInfo.chatTime > chatList[0].chatTime) {
                        processChatList();
                     }
                  }
               } else {
                  processChatList();
               }
            });
         });
      }

      private void processChatList () {
         bool isFirstChatLog = false;
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<ChatInfo> serverChatList = DB_Main.getChat(ChatInfo.Type.Global, 0, false, 20);
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               List<ChatInfo> sortedChatList = serverChatList.OrderByDescending(_ => _.chatTime).ToList();
               if (latestChatinfo == null) {
                  latestChatinfo = sortedChatList[0];
                  isFirstChatLog = true;
               }
               chatInfoList = sortedChatList;

               // Make sure to fetch only the chats that are created after our last chat cache
               if (sortedChatList[0].chatTime > latestChatinfo.chatTime || isFirstChatLog) {
                  // Cache the latest chat time
                  latestChatinfo = sortedChatList[0];

                  // Send chat to all connected users
                  foreach (int userId in serverCommuncationHandler.ourServerData.connectedUserIds) {
                     NetEntity targetEntity = EntityManager.self.getEntity(userId);
                     targetEntity.Target_ReceiveGlobalChat(latestChatinfo.chatId, latestChatinfo.text, latestChatinfo.chatTime.ToBinary(), "Server", 0);
                  }
               }
            });
         });
      }

      public void createVoyage (List<PendingVoyageCreation> voyageList) {
         foreach (PendingVoyageCreation voyageEntry in voyageList) {
            VoyageManager.self.createVoyageInstance(voyageEntry.areaKey, voyageEntry.isPvP);
         }
      }

      public void createInvite (Server server, int groupId, int inviterId, string inviterName, int inviteeId) {
         VoyageInviteData newVoyageInvite = new VoyageInviteData(server, inviterId, inviterName, inviteeId, groupId, InviteStatus.Created, DateTime.UtcNow);

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.createVoyageInvite(newVoyageInvite);
         });
      }

      public void respondVoyageInvite (int groupId, string inviterName, int inviteeId, InviteStatus status) {
         VoyageInviteData selectedVoyageInvite = pendingVoyageInvites.Find(_ => _.voyageGroupId == groupId && _.inviterName == inviterName && _.inviteeId == inviteeId);
         selectedVoyageInvite.inviteStatus = status;

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.modifyServerVoyageInvite(selectedVoyageInvite.voyageXmlId, selectedVoyageInvite.inviteStatus);
         });
      }

      private void handleVoyageInvites () {
         foreach (VoyageInviteData voyageInvite in pendingVoyageInvites) {
            if (voyageInvite.inviteStatus == InviteStatus.Created) {
               Server selectedServer = ServerNetwork.self.getServerContainingUser(voyageInvite.inviteeId);
               if (selectedServer != null) {
                  selectedServer.handleVoyageGroupInvite(voyageInvite.voyageGroupId, voyageInvite.inviterName, voyageInvite.inviteeId);
                  voyageInvite.inviteStatus = InviteStatus.Pending;
                  UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
                     DB_Main.modifyServerVoyageInvite(voyageInvite.voyageXmlId, voyageInvite.inviteStatus);
                  });
               } else {
                  D.editorLog("Could not find the server: " + voyageInvite.serverName + " - " + voyageInvite.serverPort + " for user: " + voyageInvite.inviteeId, Color.red);
               }
            }
         }
      }

      #region Private Variables

      #endregion
   }
}