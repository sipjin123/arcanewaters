using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using TMPro;

public class ChatManager : MonoBehaviour {
   #region Public Variables

   // The types of chat commands
   public enum Type {
      None = 0,
      Admin = 1,
      Bug = 2,
      Follow = 3,
      Emote = 4,
      Invite = 5,
      Group = 6,
   }

   // The Chat Panel, need to have direct reference in case something gets logged during Awake()
   public ChatPanel chatPanel;

   // Self
   public static ChatManager self;

   #endregion

   void Awake () {
      self = this;

      // Add the various chat commands that we're going to allow
      _commands.Add(Type.Admin, new List<string> { "/admin", "/a", "/ad", "/adm" });
      _commands.Add(Type.Bug, new List<string> { "/bug" });
      _commands.Add(Type.Follow, new List<string> { "/follow" });
      _commands.Add(Type.Emote, new List<string> { "/emote","/em", "/e", "/emo", "/me" });
      _commands.Add(Type.Invite, new List<string> { "/invite", "/inv" });
      _commands.Add(Type.Group, new List<string> { "/group", "/party", "/gr", "/p" });
   }

   private void Start () {
      // Check for guild messages every now and then
      InvokeRepeating("checkForGuildMessages", 0f, 1f);
   }

   public void addChat (string message, ChatInfo.Type chatType) {
      long timestamp = System.DateTime.UtcNow.ToBinary();
      addChat(message, timestamp, chatType);
   }

   public void addChat (string message, long timestamp, ChatInfo.Type chatType) {
      ChatInfo chatInfo = new ChatInfo(0, message, System.DateTime.FromBinary(timestamp), chatType);
      addChatInfo(chatInfo);
   }

   public void addChatInfo (ChatInfo chatInfo) {
      // Store it locally
      _chats.Add(chatInfo);

      // Show it in the chat panel
      if (chatPanel != null) {
         if (chatInfo.messageType == ChatInfo.Type.Guild) {
            chatPanel.addGuildChatInfo(chatInfo);
         } else {
            chatPanel.addChatInfo(chatInfo);
         }
      }
   }

   public void sendMessageToServer (string message, ChatInfo.Type chatType) {
      // Check if they're trying to send guild chat without being in a guild
      if (chatType == ChatInfo.Type.Guild && Global.player.guildId == 0) {
         this.addChat("You are not currently in a guild!", ChatInfo.Type.Error);
         return;
      }

      if (chatType == ChatInfo.Type.Whisper && Global.player.entityName.ToLower() == extractWhisperNameFromChat(message).ToLower()) {
         this.addChat("You cannot send a message to yourself!", ChatInfo.Type.Error);
         return;
      }

      if (chatType == ChatInfo.Type.Group && !VoyageGroupManager.isInGroup(Global.player)) {
         this.addChat("You are not currently in a group!", ChatInfo.Type.Error);
         return;
      }

      // Pass the message along to the server
      Global.player.rpc.Cmd_SendChat(message, chatType);
   }

   public static string extractWhisperNameFromChat (string message) {
      message = message.Replace(ChatPanel.WHISPER_PREFIX, "");
      string extractedUserName = "";
      foreach (char letter in message) {
         if (letter != ' ') {
            extractedUserName += letter;
         } else {
            if (letter == ' ') {
               break;
            }
         }
      }
      return extractedUserName;
   }

   public static string extractWhisperMessageFromChat (string extractedUserName, string message) {
      return message.Replace(ChatPanel.WHISPER_PREFIX + extractedUserName + " ", "");
   }

   public void processChatInput (string textToProcess) {
      if (Util.isEmpty(textToProcess)) {
         return;
      }

      // Check if it's a chat command
      if (textToProcess.StartsWith("/") && !textToProcess.StartsWith(ChatPanel.WHISPER_PREFIX)) {
         executeChatCommand(textToProcess);
      } else {
         sendMessageToServer(textToProcess, ChatPanel.self.currentChatType);
      }
   }

   public void onChatInputValuechanged (string inputString) {
      Global.player.admin.tryAutoCompleteForGetItemCommand(inputString);
   }

   protected void checkForGuildMessages () {
      // Only the server does this
      if (!NetworkServer.active) {
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Get all of the recent guild chat
         List<ChatInfo> list = DB_Main.getChat(ChatInfo.Type.Guild, 5, MyNetworkManager.self.networkAddress);

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Cycle through all of the chat from the database
            foreach (ChatInfo chat in list) {
               // If we haven't processed this chat already, do so now
               if (chat.chatId > _lastGuildChatId) {
                  // Note that highest chat ID that we've processed
                  _lastGuildChatId = chat.chatId;

                  // Send the chat to everyone online in that guild
                  foreach (NetEntity player in MyNetworkManager.getPlayers().Values) {
                     if (player != null && player.connectionToClient != null && player.guildId == chat.guildId) {
                        player.Target_ReceiveSpecialChat(player.connectionToClient, chat.chatId, chat.text, chat.sender, chat.chatTime.ToBinary(), chat.messageType, chat.guildIconData, chat.senderId);
                     }
                  }
               }
            }
         });
      });
   }

   protected void executeChatCommand (string message) {
      string prefix = "";
      Type type = Type.None;

      // Cycle over each of the command types
      foreach (Type t in System.Enum.GetValues(typeof(Type))) {
         // Make sure there's actually an entry for said command
         if (_commands.ContainsKey(t)) {
            // Figure out which version of the command they used, so we can strip it out
            foreach (string command in _commands[t]) {
               if (message.StartsWith(command)) {
                  type = t;
                  prefix = command;
                  break;
               }
            }
         }
      }

      // Check if we actually found a corresponding command type
      if (prefix == "" || type == Type.None) {
         D.debug("Unrecognized command.");
         return;
      }

      string trimmedMessage = message.Remove(0, prefix.Length).Trim();

      if (type == Type.Bug) {
         BugReportManager.self.sendBugReportToServer(trimmedMessage);
      } else if (type == Type.Admin) {
         Global.player.admin.handleAdminCommandString(trimmedMessage);
      } else if (type == Type.Emote) {
         sendMessageToServer(trimmedMessage, ChatInfo.Type.Emote);
      } else if (type == Type.Invite) {
         VoyageGroupManager.self.handleInviteCommand(trimmedMessage);
      } else if (type == Type.Group) {
         sendMessageToServer(trimmedMessage, ChatInfo.Type.Group);
      }
   }

   public static bool isTyping () {
      GameObject currentSelection = EventSystem.current.currentSelectedGameObject;

      // Check if we're typing in an input field
      if (currentSelection != null && Util.hasInputField(currentSelection)) {
         return true;
      }

      return ChatPanel.self.inputField.isFocused;
   }

   #region Private Variables

   // The commands available to the user through the chat prompt
   protected Dictionary<Type, List<string>> _commands = new Dictionary<Type, List<string>>();

   // A list of all of the messages we've received
   protected List<ChatInfo> _chats = new List<ChatInfo>();

   // The last guild chat ID that we processed
   protected int _lastGuildChatId = 0;

   #endregion
}
