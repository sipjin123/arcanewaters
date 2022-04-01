using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using TMPro;
using System.Text;
using System;
using UnityEngine.InputSystem;
using System.Linq;

public class ChatManager : GenericGameManager
{
   #region Public Variables

   // When we add a <link> tag to the text to mark item insert position, we use this transparent text
   public const string ITEM_INSERT_TEXT_PLACEHOLDER = "XX";

   // Item insert id prefix we use in texh mesh pro <link> tag
   public const string ITEM_INSERT_ID_PREFIX = "iteminsertid";

   // Maximum number of item tags that a message can contain
   public const int MAX_ITEM_TAGS_IN_MESSAGE = 5;

   // The time interval at which missed chat message are re-sent
   public const float MISSED_CHAT_MESSAGE_RESEND_INTERVAL = 0.5f;

   // The Chat Panel, need to have direct reference in case something gets logged during Awake()
   public ChatPanel chatPanel;

   // The panel managing the display and changing of the auto-complete options
   [HideInInspector]
   public AutoCompletePanel autoCompletePanel;

   // The panel managing the display and changing of the whisper name auto-complete options
   public WhisperAutoCompletePanel whisperAutoCompletePanel;

   // Self
   public static ChatManager self;

   // The font size of the chat
   public float chatFontSize = 20;

   // Player pref key for the chat font size
   public const string CHAT_FONT_SIZE_PREF = "chatFontSizePref";

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;

      // Setup auto-complete panel
      GameObject optionsPanel = Instantiate(Resources.Load<GameObject>("Prefabs/Auto-completes/Auto-complete Section"), chatPanel.inputField.transform.parent.parent);
      autoCompletePanel = optionsPanel.GetComponentInChildren<AutoCompletePanel>();

      updateChatFontSize(PlayerPrefs.GetFloat(CHAT_FONT_SIZE_PREF, 20));
   }

   private void Start () {
      // Add the various chat commands that we're going to allow
      _commandData.Add(new CommandData("/bug", "Sends a bug report to the server", BugReportManager.self.sendBugReport, parameterNames: new List<string>() { "bugInformation" }));
      _commandData.Add(new CommandData("/emote", "Sends a chat emote", sendEmoteMessageToServer, parameterNames: new List<string>() { "emoteDescription" }));
      _commandData.Add(new CommandData("/invite", "Invites a user to your group", VoyageGroupManager.self.handleInviteCommand, parameterNames: new List<string>() { "userName" }));
      _commandData.Add(new CommandData("/global", "Send a message to all users", sendGlobalMessageToServer, parameterNames: new List<string>() { "message" }));
      _commandData.Add(new CommandData("/group", "Send a message to your group", sendGroupMessageToServer, parameterNames: new List<string>() { "message" }));
      _commandData.Add(new CommandData("/officer", "Executes an officer command, if you have officer privileges", sendOfficerMessageToServer, parameterNames: new List<string>() { "officerCommand" }));
      _commandData.Add(new CommandData("/guild", "Executes a guild command", sendGuildMessageToServer, parameterNames: new List<string>() { "guildCommand" }));
      _commandData.Add(new CommandData("/complain", "Sends a complaint about a user", SupportTicketManager.self.requestComplaint, parameterNames: new List<string>() { "username", "details" }));
      _commandData.Add(new CommandData("/roll", "Rolls a die", sendRollToServer, parameterNames: new List<string>() { "max", "min" }));
      _commandData.Add(new CommandData("/whisper", "Sends a private message to a user", sendWhisperMessageToServer, parameterNames: new List<string>() { "userName", "message" }));
      _commandData.Add(new CommandData("/w", "Sends a private message to a user", sendWhisperMessageToServer, parameterNames: new List<string>() { "userName", "message" }));
      _commandData.Add(new CommandData("/r", "Sends a private message to the last user that whispered to you", tryReply, parameterNames: new List<string>() { "message" }));
      _commandData.Add(new CommandData("/who", "Search users", searchUsers, parameterNames: new List<string>() { "[is, in, level, help]", "username, area or level" }));
      _commandData.Add(new CommandData("/stuck", "Are you stuck? Use this to free yourself", requestUnstuck));
      _commandData.Add(new CommandData("/gif", "Make a GIF of what just happened in game", GIFReplayManager.self.userRequestedGIF));
      _commandData.Add(new CommandData("/e", "Lists the available emotes", requestEmoteList));
      _commandData.Add(new CommandData("/dance", "Dance!", (_) => requestPlayEmote(_, EmoteManager.EmoteTypes.Dance), parameterNames: new List<string>() { "target" }));
      _commandData.Add(new CommandData("/greet", "Wave your hand", (_) => requestPlayEmote(_, EmoteManager.EmoteTypes.Greet), parameterNames: new List<string>() { "target" }));
      _commandData.Add(new CommandData("/kneel", "Kneeling pose", (_) => requestPlayEmote(_, EmoteManager.EmoteTypes.Kneel), parameterNames: new List<string>() { "target" }));
      _commandData.Add(new CommandData("/point", "Point at something", (_) => requestPlayEmote(_, EmoteManager.EmoteTypes.Point), parameterNames: new List<string>() { "target" }));
      _commandData.Add(new CommandData("/wave", "Wave your hand", (_) => requestPlayEmote(_, EmoteManager.EmoteTypes.Wave), parameterNames: new List<string>() { "target" }));
      _commandData.Add(new CommandData("/sit", "Sit on the ground", (_) => requestPlayEmote(_, EmoteManager.EmoteTypes.Sit)));
   }

   public void startChatManagement () {
      // Start re-sending missed chat messages
      InvokeRepeating(nameof(resendMissedChatMessages), 5f, MISSED_CHAT_MESSAGE_RESEND_INTERVAL);
   }

   public void updateChatFontSize (float size) {
      size = Mathf.Clamp(size, 5, 20);
      chatFontSize = size;
      PlayerPrefs.SetFloat(CHAT_FONT_SIZE_PREF, size);
      foreach (Transform childObj in chatPanel.messagesContainer.transform) {
         SpeakChatLine speakChatLine = childObj.GetComponentInChildren<SpeakChatLine>();
         if (speakChatLine) {
            if (speakChatLine.textMeshReference != null) {
               speakChatLine.textMeshReference.fontSize = chatFontSize;
            }
         }
      }
   }

   private void Update () {
      // Skip update for batch mode - server
      if (Util.isBatch()) {
         return;
      }

      if (
         InputManager.self.inputMaster.UIShotcuts.ChatReply.WasPressedThisFrame() == true &&
         !chatPanel.inputField.isFocused &&
         !chatPanel.nameInputField.isFocused &&
         !string.IsNullOrEmpty(_lastWhisperSender)
      ) {
         chatPanel.inputField.setText("/whisper " + _lastWhisperSender + " ");
         chatPanel.focusInputField();
      }

      if (!isTyping()) {
         return;
      }

      if (InputManager.self.inputMaster?.Chat.HistoryUp.WasPressedThisFrame() == true) {
         if (ChatPanel.self.inputField.isFocused) {
            changeNumMessagesAgo(increment: true);
         } else if (ChatPanel.self.nameInputField.isFocused) {
            changeWhisperNamesAgo(increment: true);
         }
      }

      if (InputManager.self.inputMaster?.Chat.HistoryDown.WasPressedThisFrame() == true) {
         if (ChatPanel.self.inputField.isFocused) {
            changeNumMessagesAgo(increment: false);
         } else if (ChatPanel.self.nameInputField.isFocused) {
            changeWhisperNamesAgo(increment: false);
         }
      }

      if (_shouldUpdateAutoCompletes && !typedRecently()) {
         tryAutoCompleteChatCommand();
         _shouldUpdateAutoCompletes = false;
      }
   }

   public void removeAdminCommands () {
      for (int i = _commandData.Count - 1; i >= 0; i--) {
         CommandData data = _commandData[i];
         if (data.getRequiredPrefix().Equals("/admin") || data.getPrefix().Equals("/admin")) {
            _commandData.Remove(data);
         }
      }
   }

   public void addAdminCommand (AdminManager adminManager) {
      _commandData.Add(new CommandData("/admin", "Executes an admin command, if you have admin privileges", noAdminCommandFound, parameterNames: new List<string>() { "adminCommand" }));
   }

   public void addChat (string message, ChatInfo.Type chatType) {
      long timestamp = System.DateTime.UtcNow.ToBinary();
      addChat(message, timestamp, chatType);
   }

   public void addChat (string message, long timestamp, ChatInfo.Type chatType) {
      ChatInfo chatInfo = new ChatInfo(0, message, System.DateTime.FromBinary(timestamp), chatType);
      addChatInfo(chatInfo);
   }

   private void noAdminCommandFound (string command) {
      ChatManager.self.addChat("Couldn't find command: " + command, ChatInfo.Type.System);
   }

   private bool processUserOnlineStatusChangedMessage (ChatInfo chatInfo) {
      // Returns false if the message should be ignored
      if (chatInfo.messageType == ChatInfo.Type.UserOnline || chatInfo.messageType == ChatInfo.Type.UserOffline) {
         bool onlineReported = _chats.Any(_ => _.senderId == chatInfo.senderId && _.messageType == ChatInfo.Type.UserOnline);
         bool offlineReported = _chats.Any(_ => _.senderId == chatInfo.senderId && _.messageType == ChatInfo.Type.UserOffline);

         if (chatInfo.messageType == ChatInfo.Type.UserOnline && onlineReported && !offlineReported) {
            // If the message notifies that the sender is online, but the sender was online already, ignore the message
            return false;
         }

         if (chatInfo.messageType == ChatInfo.Type.UserOffline && !onlineReported && offlineReported) {
            // If the message notifies that the sender is offline, but the sender was offline already, ignore the message
            return false;
         }

         if (onlineReported && offlineReported) {
            List<ChatInfo> sortedChats = _chats.ToList();
            sortedChats.Sort((a, b) => DateTime.Compare(a.chatTime, b.chatTime));
            sortedChats.Reverse();

            // Find the last "online" message
            ChatInfo onlineNotification = sortedChats.First(_ => _.senderId == chatInfo.senderId && _.messageType == ChatInfo.Type.UserOnline);

            // Find the last "offline" message
            ChatInfo offlineNotification = sortedChats.First(_ => _.senderId == chatInfo.senderId && _.messageType == ChatInfo.Type.UserOffline);

            if (onlineNotification != null && offlineNotification != null) {
               // If the message notifies that the sender is online, but the sender was online already, ignore the message
               if (onlineNotification.chatTime > offlineNotification.chatTime && chatInfo.messageType == ChatInfo.Type.UserOnline) {
                  return false;
               }

               // If the message notifies that the sender is offline, but the sender was offline already, ignore the message
               if (offlineNotification.chatTime > onlineNotification.chatTime && chatInfo.messageType == ChatInfo.Type.UserOffline) {
                  return false;
               }
            }
         }
      }

      return true;
   }

   public void addChatInfo (ChatInfo chatInfo) {
      if (!processUserOnlineStatusChangedMessage(chatInfo)) {
         return;
      }

      // Store it locally
      _chats.Add(chatInfo);

      bool showMessage;

      // If the sender is muted, we only show the message to itself
      if (chatInfo.isSenderMuted) {
         if (Global.player.userId == chatInfo.senderId) {
            showMessage = true;
         } else {
            showMessage = false;
         }
      } else {
         showMessage = true;
      }

      // Show it in the chat panel
      if (showMessage && chatPanel != null) {
         chatPanel.addChatInfo(chatInfo);
      }

      if (chatInfo.messageType == ChatInfo.Type.Whisper) {
         noteWhisperReceived(chatInfo.sender);
      }
   }

   public void sendEmoteMessageToServer (string message) {
      sendMessageToServer(message, ChatInfo.Type.Emote);
   }

   private string computeEmoteChatMessage(EmoteManager.EmoteTypes emoteType, string target) {
      switch (emoteType) {
         case EmoteManager.EmoteTypes.Dance:
            return Util.isEmpty(target) ? $"dances" : $"dances and would like {target} to join!";
         case EmoteManager.EmoteTypes.Kneel:
            return $"kneels";
         case EmoteManager.EmoteTypes.Greet:
         case EmoteManager.EmoteTypes.Wave:
            return Util.isEmpty(target) ? $"waves" : $"waves at {target}";
         case EmoteManager.EmoteTypes.Point:
            return Util.isEmpty(target) ? $"is pointing at something" : $"points at {target}";
         case EmoteManager.EmoteTypes.None:
         default:
            return string.Empty;
      }
   }

   public void requestEmoteList() {
      StringBuilder sb = new StringBuilder();
      sb.Append("The available emotes are: ");

      // Get comma-separated list of emotes
      sb.Append(string.Join(", ", EmoteManager.getSupportedEmoteNames()));

      addChat(sb.ToString(), ChatInfo.Type.System);
   }

   private bool canEmote (EmoteManager.EmoteTypes emote) {
      if (Global.player == null ||
         Global.player.getPlayerBodyEntity() == null ||
         Global.player.getPlayerBodyEntity().isSitting() ||
         Global.player.isInBattle() ||
         Global.player.getPlayerBodyEntity().isEmoting() ||
         emote == EmoteManager.EmoteTypes.None) {
         return false;
      }

      return true;
   }

   public void requestPlayEmote (string parameters, EmoteManager.EmoteTypes emote) {
      if (!canEmote(emote)) {
         addChat("Can't do that now...", ChatInfo.Type.System);
         return;
      }

      if (Util.isEmpty(parameters)) {
         requestPlayTargetableEmote(emote, string.Empty);
         return;
      }

      // Extract user handle
      if (parameters.StartsWith("@")) {
         string handle = parameters.Substring(1);
         NetEntity handleEntity = EntityManager.self.getEntityWithName(handle);

         if (handleEntity != null && handleEntity.isPlayerEntity()) {
            requestPlayTargetableEmote(emote, handleEntity.entityName);
         } else {
            addChat("Invalid target", ChatInfo.Type.System);
         }

         return;
      }

      // If the player is pointing, use the parameters as the target
      if (emote == EmoteManager.EmoteTypes.Point || emote == EmoteManager.EmoteTypes.Wave || emote == EmoteManager.EmoteTypes.Greet) {
         requestPlayTargetableEmote(emote, parameters);
      }
   }

   public void requestPlayTargetableEmote (EmoteManager.EmoteTypes emote, string target) {
      if (!canEmote(emote)) {
         addChat("Can't do that now...", ChatInfo.Type.System);
         return;
      }

      Direction playerFacingDirection = Global.player.getPlayerBodyEntity().facing;
      Global.player.getPlayerBodyEntity().Cmd_PlayEmote(emote, playerFacingDirection);
      sendEmoteMessageToServer(computeEmoteChatMessage(emote, target));
   }

   public void sendGlobalMessageToServer (string message) {
      sendMessageToServer(message, ChatInfo.Type.Global);
      chatPanel.setCurrentChatType(ChatInfo.Type.Global);
   }

   public void sendGroupMessageToServer (string message) {
      sendMessageToServer(message, ChatInfo.Type.Group);
      chatPanel.setCurrentChatType(ChatInfo.Type.Group);
   }

   public void sendOfficerMessageToServer (string message) {
      sendMessageToServer(message, ChatInfo.Type.Officer);
   }

   public void sendGuildMessageToServer (string message) {
      sendMessageToServer(message, ChatInfo.Type.Guild);
      chatPanel.setCurrentChatType(ChatInfo.Type.Guild);
   }

   private void sendWhisperMessageToServer (string message) {
      sendMessageToServer(message, ChatInfo.Type.Whisper);

      // Add the whisper name to the history
      _sentWhisperNameHistory.Add(ChatPanel.self.nameInputField.text);
   }

   public void sendMessageToServer (string message, ChatInfo.Type chatType, string extra = "") {
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

      if (chatType == ChatInfo.Type.Officer) {
         if (Global.player.guildId == 0) {
            this.addChat("You are not currently in a guild!", ChatInfo.Type.Error);
            return;
         } else if (!Global.player.canPerformAction(GuildPermission.OfficerChat)) {
            this.addChat("You have insufficient permissions to access officer chat!", ChatInfo.Type.Error);
            return;
         }
      }

      // Prevent user from sending empty message
      if (message.Trim().Length == 0) {
         return;
      }

      // Pass the message along to the server
      Global.player.rpc.Cmd_SendChatWithExtra(message, chatType, extra);
   }

   public static string extractWhisperNameFromChat (string message) {
      if (message.StartsWith(ChatPanel.WHISPER_PREFIX)) {
         message = message.Replace(ChatPanel.WHISPER_PREFIX, "");
      } else if (message.StartsWith(ChatPanel.WHISPER_PREFIX_FULL)) {
         message = message.Replace(ChatPanel.WHISPER_PREFIX_FULL, "");
      }

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

   public string extractComplainNameFromChat (string message) {
      foreach (string command in ChatUtil.commandTypePrefixes[CommandType.Complain]) {
         message = message.Replace(command + " ", "");
      }

      StringBuilder extractedName = new StringBuilder();
      foreach (char letter in message) {
         if (letter == ' ') {
            break;
         }

         extractedName.Append(letter);
      }

      return extractedName.ToString();
   }

   public string extractComplainMessageFromChat (string message, string username) {
      foreach (string command in ChatUtil.commandTypePrefixes[CommandType.Complain]) {
         message = message.Replace(command, "");
      }

      return message.Replace($"{username} ", "").Trim();
   }

   public void onChatLostFocus () {
      if (Util.isBatch()) {
         return;
      }
      InputManager.self.actionMapStates.Restore();
      InputManager.self.inputMaster.Chat.Disable();
      autoCompletePanel.inputFieldFocused = false;
      autoCompletePanel.updatePanel();
   }

   public void onChatGainedFocus () {
      if (Util.isBatch()) {
         return;
      }
      InputManager.self.actionMapStates.Save();
      InputManager.self.actionMapStates.DisableAll();
      InputManager.self.inputMaster.Chat.Enable();
      autoCompletePanel.inputFieldFocused = true;
      autoCompletePanel.updatePanel();
   }

   public void onWhisperInputLostFocus () {
      whisperAutoCompletePanel.inputFieldFocused = false;
      whisperAutoCompletePanel.updatePanel();
   }

   public void onWhisperInputGainedFocus () {
      whisperAutoCompletePanel.inputFieldFocused = true;
      whisperAutoCompletePanel.updatePanel();
   }

   public void processChatInput (string textToProcess) {
      if (Util.isEmpty(textToProcess)) {
         return;
      }

      if (!Global.player.isAdmin()) {
         foreach (KeyValuePair<CommandType, List<string>> keyValue in ChatUtil.commandTypePrefixes) {
            if (keyValue.Key == CommandType.Admin) {
               foreach (String label in keyValue.Value) {
                  if (textToProcess.StartsWith(label)) {
                     this.addChat("You do not have Admin privileges ", ChatInfo.Type.System);
                     return;
                  }
               }
            }
         }
      }

      _sentMessageHistory.Add(textToProcess);

      // Check if it's a chat command
      if (textToProcess.StartsWith("/")) {
         executeChatCommand(textToProcess);
      } else {
         sendMessageToServer(textToProcess, ChatPanel.self.currentChatType);
      }

      resetMessagesAgo();
   }

   public void onChatInputValuechanged (string inputString) {
      Global.player.admin.tryAutoCompleteForGetItemCommand(inputString);
      _shouldUpdateAutoCompletes = true;

      if (Util.isEmpty(inputString)) {
         resetMessagesAgo();
         tryAutoCompleteChatCommand();
      }

      _lastTypeTime = (float) NetworkTime.time;
   }

   public void onWhisperNameInputValueChanged (string inputString) {
      tryAutoCompleteWhisperName();
   }

   public void changePlayerNameInChat (int userId, string oldName, string newName) {
      foreach (ChatInfo chat in _chats) {
         if (chat.senderId == userId || Util.areStringsEqual(oldName, chat.sender)) {
            chat.sender = newName;
         }
      }

      if (ChatPanel.self != null) {
         ChatPanel.self.refreshChatLines();
      }
   }

   public static string injectItemSnippetLinks (string text, out int itemTagCount) {
      itemTagCount = 0;

      // Split everything by the beginning of the tag
      string[] values = text.Split(new string[] { "[itemid=" }, StringSplitOptions.RemoveEmptyEntries);

      // If nothing was split, just return it
      if (values.Length == 0) {
         return text;
      }

      // Starting with the second entry, the beginning should attempt to close out the tag
      // Unless it begins with the tag itself

      int start = 1;
      string result = values[0];

      if (text.StartsWith("[itemid=")) {
         start = 0;
         result = "";
      }

      for (int i = start; i < values.Length; i++) {
         string id = "";
         bool found = false;
         string frag = values[i];

         for (int j = 0; j < frag.Length; j++) {
            if (char.IsNumber(frag[j])) {
               // Found a number, add to id
               id += frag[j];
            } else if (frag[j] == ']') {
               // Closing of item insert tag
               found = true;
               break;
            } else {
               // Invalid character in insert tag, cancel
               break;
            }
         }

         // Check if we found the valid tag, insert link if so, otherwise recreate whatever was there
         if (id.Length > 0 && found) {
            itemTagCount++;
            result += "<link=\"" + ITEM_INSERT_ID_PREFIX + id + "\"><nobr><color=#00000000>" +
               ITEM_INSERT_TEXT_PLACEHOLDER + "</color></nobr></link>" + frag.Substring(1 + id.Length);
         } else {
            result += "[itemid=" + frag;
         }
      }

      return result;
   }

   public static string turnItemSnippetLinksToItemTags (string text, out int itemTagCount, out bool takenTextOutOfLinks) {
      itemTagCount = 0;
      takenTextOutOfLinks = false;

      // Split everything by the ending of the insert
      string[] noEnds = text.Split(new string[] { "</color></nobr></link>" }, StringSplitOptions.RemoveEmptyEntries);

      int l = text.EndsWith("</color></nobr></link>") ? noEnds.Length : noEnds.Length - 1;

      // If nothing was split, just return it
      if (noEnds.Length == 0) {
         return text;
      }

      // Otherwise begin making a new string
      string result = "";

      // Go over all the beginnings
      for (int i = 0; i < l; i++) {
         // Try to find the start of the insert
         int insertStartIndex = noEnds[i].IndexOf("<link=\"" + ITEM_INSERT_ID_PREFIX);
         int idEndIndex = noEnds[i].IndexOf("\"><nobr><color=#00000000>");
         if (insertStartIndex == -1 || idEndIndex == -1) {
            result += noEnds[i] + "</color></nobr></link>";
            continue;
         }

         // We have something that resembles a tag, add text that came before it
         if (insertStartIndex > 0) {
            result += noEnds[i].Substring(0, insertStartIndex);
         }

         // Try to extract the item id
         int idStartIndex = insertStartIndex + ("<link=\"" + ITEM_INSERT_ID_PREFIX).Length;
         string itemId = "";
         for (int j = idStartIndex; j < idEndIndex; j++) {
            char c = noEnds[i][j];
            if (char.IsNumber(c)) {
               itemId += c;
            } else {
               itemId = "";
               break;
            }
         }

         // Try to get the placeholder characters
         string placeholder = noEnds[i].Substring(idEndIndex + ("\"><nobr><color=#00000000>").Length);

         // Check if insert isn't broken, we can turn it into item tag, otherwise discard
         if (itemId.Length > 0) {
            if (placeholder.Contains(ITEM_INSERT_TEXT_PLACEHOLDER)) {
               // If user wrote something in the beginning or end of placeholder, take it out of the tag
               int phStart = placeholder.IndexOf(ITEM_INSERT_TEXT_PLACEHOLDER);
               if (phStart > 0) {
                  result += placeholder.Substring(0, phStart);
                  takenTextOutOfLinks = true;
               }
               result += "[itemid=" + itemId + "]";
               if (phStart + ITEM_INSERT_TEXT_PLACEHOLDER.Length < placeholder.Length) {
                  result += placeholder.Substring(phStart + ITEM_INSERT_TEXT_PLACEHOLDER.Length);
                  takenTextOutOfLinks = true;
               }
               itemTagCount++;
            }
         }
      }

      if (l == noEnds.Length - 1) {
         result += noEnds[l];
      }

      return result;
   }

   protected void executeChatCommand (string message) {
      foreach (CommandData command in _commandData) {
         if (command.matchesInput(message, mustEqual: true)) {
            string[] messageParts = message.Split(' ');
            string prefix = messageParts[0];

            string trimmedMessage = message.Remove(0, prefix.Length).Trim();

            if (command.getRequiredPrefix() != "") {
               trimmedMessage = trimmedMessage.Remove(0, messageParts[1].Length).Trim();
            }
            command.invoke(trimmedMessage);
            return;
         }
      }
   }

   private void sendRollToServer (string message) {
      int min = 1;
      int max = 100;

      message = message.Trim();
      string[] parameters = message.Split(' ');
      List<string> finalParameters = new List<string>();
      foreach (string p in parameters) {
         if (p.Trim() != "") {
            finalParameters.Add(p.Trim());
         }
      }

      if (finalParameters.Count == 1) {
         if (!int.TryParse(finalParameters[0], out max)) {
            return;
         }
      } else if (finalParameters.Count == 2) {
         if (!int.TryParse(finalParameters[0], out min)) {
            return;
         }
         if (!int.TryParse(finalParameters[1], out max)) {
            return;
         }
      } else if (finalParameters.Count > 2) {
         return;
      }

      Global.player.rpc.Cmd_SendRollOutput(min, max);
   }

   public string getChatLog (int messages = -1) {
      string log = "";
      int messagesToInclude = messages < 1 ? MAX_MESSAGES_IN_LOG : messages;

      // Make sure we don't try to read more messages than we have
      messagesToInclude = Mathf.Min(messagesToInclude, _chats.Count);

      for (int i = _chats.Count - 1; i > _chats.Count - messagesToInclude; i--) {
         ChatInfo chat = _chats[i];
         log += Util.stripHTML($"{chat.sender}: {chat.text}\n");
      }

      return log;
   }

   public static bool isTyping () {
      // Skip for batch mode
      if (Util.isBatch()) return false;
      
      GameObject currentSelection = EventSystem.current.currentSelectedGameObject;

      // Check if we're typing in an input field
      if (currentSelection != null && Util.hasInputField(currentSelection)) {
         return true;
      }

      return ChatPanel.self.inputField.isFocused || ChatPanel.self.nameInputField.isFocused;
   }

   public void addCommand (CommandData newCommand) {
      _commandData.Add(newCommand);
   }

   public void tryAutoCompleteChatCommand () {
      string inputString = chatPanel.inputField.getTextData();

      // Lets just not deal with it if there are item tags
      // Remove autocompletes when we are typing a bug report
      if (inputString.StartsWith("/bug ") || chatPanel.inputField.hasItemTags()) {
         autoCompletePanel.setUserSuggestions(null);
         autoCompletePanel.setAutoCompletes(null);
         autoCompletePanel.setAutoCompletesWithParameters(null);
         return;
      }

      List<CommandData> autoCompleteCommands = new List<CommandData>();
      List<Tuple<CommandData, string>> autoCompleteParameters = new List<Tuple<CommandData, string>>();

      foreach (CommandData command in _commandData) {
         if (command.matchesInput(inputString, mustEqual: false)) {
            autoCompleteCommands.Add(command);

            // If the whole command has been written, try to offer parameter autocompletes
            if (command.containsWholePrefix(inputString)) {
               string[] inputParts = inputString.Split(' ');

               List<string> parameterAutoCompletes;

               // If the user has started typing a parameter, try to get auto-completes for it
               if (inputParts.Length >= 3) {
                  string parameters = "";
                  for (int i = 2; i < inputParts.Length; i++) {
                     if (inputParts[i] == "") {
                        break;
                     }

                     parameters += inputParts[i];
                     parameters += ' ';
                  }

                  // Remove space at the end
                  if (parameters.Length > 0) {
                     parameters = parameters.Remove(parameters.Length - 1);
                  }

                  parameterAutoCompletes = command.getParameterAutoCompletes(parameters);

                  // Otherwise, offer all parameter auto-completes
               } else {
                  parameterAutoCompletes = command.getParameterAutoCompletes();
               }

               foreach (string autoComplete in parameterAutoCompletes) {
                  autoCompleteParameters.Add(new Tuple<CommandData, string>(command, autoComplete));
               }
            }
         }
      }

      if (chatPanel.inputField.isFocused) {
         List<UserSuggestionData> userSuggestionDataList = new List<UserSuggestionData>();
         int symbolIndex = inputString.LastIndexOf('@');
         int caretIndex = chatPanel.inputField.caretPosition;
         string partialStr = "";
         bool shouldShowUserSuggestions = false;

         if (symbolIndex >= 0 && caretIndex >= symbolIndex) {
            partialStr = inputString.Substring(symbolIndex, caretIndex - symbolIndex);
            shouldShowUserSuggestions = !partialStr.Contains(" ");
         }

         if (shouldShowUserSuggestions) {
            autoCompleteCommands.Clear();
            autoCompleteParameters.Clear();

            // Fetch the set of users who wrote a message in chat so far
            if (!string.IsNullOrEmpty(partialStr) && _chats != null && _chats.Count > 0) {
               try {
                  IEnumerable<ChatInfo> filteredChats = _chats.Where(_ => !Util.isEmpty(_.sender) && _.senderId > 0 && $"@{_.sender}".ToLower().StartsWith(partialStr.ToLower()));
                  HashSet<string> uniqueNames = new HashSet<string>(filteredChats.Select(_ => _.sender));
                  IEnumerable<UserSuggestionData> suggestions = uniqueNames.Select(_ => new UserSuggestionData(_, "@" + _, inputString, partialStr));
                  userSuggestionDataList.AddRange(suggestions);
               } catch (Exception ex) {
                  D.error(ex.Message);
               }
            }
         }

         autoCompletePanel.setUserSuggestions(userSuggestionDataList);
      }

      autoCompletePanel.setAutoCompletes(autoCompleteCommands);
      autoCompletePanel.setAutoCompletesWithParameters(autoCompleteParameters);
   }

   private void tryAutofillOldMessage () {
      if (_numMessagesAgo < 1 || _numMessagesAgo > _sentMessageHistory.Count) {
         return;
      }

      int indexInList = _sentMessageHistory.Count - _numMessagesAgo;
      chatPanel.inputField.setText(_sentMessageHistory[indexInList]);
      chatPanel.inputField.moveTextEnd(false);
   }

   public void tryAutoCompleteWhisperName () {
      string inputString = chatPanel.nameInputField.text;

      List<string> autoCompletes = new List<string>();
      List<string> possibleNames = new List<string>();
      List<string> friends = FriendListManager.self.getFriendNames();

      // Add friends to possible names
      foreach (string name in friends) {
         if (!possibleNames.Contains(name)) {
            possibleNames.Add(name);
         }
      }

      // Add recently messaged to possible names
      foreach (string name in _sentWhisperNameHistory) {
         if (!possibleNames.Contains(name)) {
            possibleNames.Add(name);
         }
      }

      foreach (string name in possibleNames) {
         if (name.StartsWith(inputString)) {
            autoCompletes.Add(name);
         }
      }

      whisperAutoCompletePanel.setAutoCompletes(autoCompletes);
   }

   private void tryAutofillWhisperName () {
      if (_numWhisperNamesAgo < 1 || _numWhisperNamesAgo > _sentWhisperNameHistory.Count) {
         return;
      }

      int indexInList = _sentWhisperNameHistory.Count - _numWhisperNamesAgo;
      chatPanel.nameInputField.text = _sentWhisperNameHistory[indexInList];
      chatPanel.nameInputField.MoveTextEnd(false);
   }

   private void changeNumMessagesAgo (bool increment) {
      _numMessagesAgo += (increment) ? 1 : -1;

      if (_numMessagesAgo < 1) {
         _numMessagesAgo = 1;
      }

      if (_numMessagesAgo > _sentMessageHistory.Count) {
         _numMessagesAgo = _sentMessageHistory.Count;
      }

      tryAutofillOldMessage();
   }

   private void resetMessagesAgo () {
      _numMessagesAgo = 0;
   }

   private void changeWhisperNamesAgo (bool increment) {
      _numWhisperNamesAgo += (increment) ? 1 : -1;

      if (_numWhisperNamesAgo < 1) {
         _numWhisperNamesAgo = 1;
      }

      if (_numWhisperNamesAgo > _sentWhisperNameHistory.Count) {
         _numWhisperNamesAgo = _sentWhisperNameHistory.Count;
      }

      tryAutofillWhisperName();
   }

   private void resetWhisperNamesAgo () {
      _numWhisperNamesAgo = 0;
   }

   private void noteWhisperReceived (string sender) {
      if (Global.player == null || (Global.player != null && sender != Global.player.entityName)) {
         _lastWhisperSender = sender;
      }
   }

   private void tryReply (string message) {
      if (!string.IsNullOrEmpty(_lastWhisperSender)) {
         sendWhisperMessageToServer(_lastWhisperSender + " " + message);
      }
   }

   private bool typedRecently () {
      float timeSinceTyping = (float) (NetworkTime.time - _lastTypeTime);
      return timeSinceTyping < AUTO_COMPLETE_SHOW_DELAY;
   }

   private void searchUsers (string parameters) {
      if (Util.isEmpty(parameters)) {
         self.addChat("Search failed: Invalid parameters. Type '/who help' to learn more about this command.", ChatInfo.Type.System);
         return;
      }

      string[] inputs = parameters.Split(' ');
      if (inputs.Length < 2) {
         if (inputs.Length == 1) {
            if (Util.areStringsEqual(inputs[0], "help")) {
               showSearchHelpMessage();
               return;
            }
         } else {
            self.addChat("Search failed: The command requires at least two parameters! Type '/who help' to learn more about this command.", ChatInfo.Type.System);
            return;
         }
      }

      if (UserSearchInfo.tryParseFilteringMode(inputs[0], out UserSearchInfo.FilteringMode filter)) {
         string input = inputs[1];

         if (filter == UserSearchInfo.FilteringMode.Biome) {
            // Biome names could be space separated. Thus, capture all the string tokens, and compute a single string for the biome name
            input = string.Join(" ", inputs.Skip(1));
            if (Biome.fromName(input) == Biome.Type.None) {
               self.addChat($"Search failed: The biome '{input}' doesn't exist! Type '/who help' to learn more about this command.", ChatInfo.Type.System);
               return;
            }
         }

         if (filter == UserSearchInfo.FilteringMode.Level) {
            if (int.TryParse(input, out int inputLevel)) {
               if (inputLevel < 1) {
                  self.addChat("Search failed: The level should be greater than 0! Type '/who help' to learn more about this command.", ChatInfo.Type.System);
                  return;
               }
            } else {
               self.addChat("Search failed: Invalid Level! Type '/who help' to learn more about this command.", ChatInfo.Type.System);
               return;
            }
         }

         UserSearchInfo searchInfo = new UserSearchInfo {
            input = input,
            filter = filter
         };

         self.addChat($"Search started! keyword: '{searchInfo.input}'", ChatInfo.Type.System);
         Global.player.rpc.Cmd_SearchUser(searchInfo);
      } else {
         self.addChat($"Search failed: The filter must be one of: 'is', 'in', 'level'. Type '/who help' to learn more about this command.", ChatInfo.Type.System);
      }
   }

   private void showSearchHelpMessage () {
      string msg = "Help for the /who command:\n" +
         "The /who command must be in the form '/who [filter] [parameter]'.\n" +
         "[filter] must be one of: 'is', 'in', 'level'.\n" +
         "[parameter] depends on the value of [filter].\n" +
         $"If [filter] is 'is', then the command allows you to get detailed information about a specific user. In this case, [parameter] must be the name of the user to look up. Example: /who is { Global.player.entityName }\n" +
         "If [filter] is 'in', the command allows to list the players who are currently in a specific biome or area. In this case, [parameter] must be the name of the biome. The available biomes are: 'Forest', 'Desert', 'Pine', 'Snow', 'Lava' and 'Mushroom'. Example: /who in Forest\n" +
         "If [filter] is 'level', the command allows to list the online players who are at a specific level. In this case, [parameter] must be the required level. Example: /who level 10\n";
      self.addChat(msg, ChatInfo.Type.System);
   }

   private void requestUnstuck () {
      if (Global.player != null && Global.player.getPlayerBodyEntity() != null && !Global.player.getPlayerBodyEntity().isSitting() && !Global.player.getPlayerBodyEntity().isEmoting()) {
         Global.player.rpc.Cmd_TeleportToCurrentAreaSpawnLocation();
         return;
      }
      
      addChat("Can't do that now...", ChatInfo.Type.System);
   }

   public void addFriendRequestNotification () {
      string message = "You have received new friend requests. Click here to check them!";

      // Prevent spamming
      if (_chats != null && _chats.Count > 0 && Util.areStringsEqual(message, _chats.Last().text)) {
         return;
      }
      
      addChat(message, ChatInfo.Type.PendingFriendRequestsNotification);
   }

   [Server]
   public void receiveChatMessageForUser (int userId, int chatId, ChatInfo.Type messageType, string message, long timestamp, string senderName, string receiverName, int senderUserId, string guildIconDataString, string guildName, bool isSenderMuted, bool isSenderAdmin, string extra) {
      ChatInfo chatInfo = new ChatInfo(chatId, message, DateTime.FromBinary(timestamp), messageType, senderName, receiverName, senderUserId, GuildIconData.guildIconDataFromString(guildIconDataString), guildName, isSenderMuted, isSenderAdmin, userId, extra: extra);

      NetEntity player = EntityManager.self.getEntity(userId);
      if (player != null) {
         commonSendChatMessageToPlayer(player, chatInfo);
      } else {
         // Add the chat message to the missed chat messages and try to re-send it later
         _missedChatMessages.Add(chatInfo);
      }
   }

   [Server]
   private void resendMissedChatMessages () {
      if (!NetworkServer.active) {
         return;
      }

      List<ChatInfo> messagesToDelete = new List<ChatInfo>();

      foreach (ChatInfo chatInfo in _missedChatMessages) {
         // When the recipient is no longer assigned to this server, the user either moved to another server or disconnected, so the missed chat message is deleted
         if (!ServerNetworkingManager.self.server.assignedUserIds.ContainsKey(chatInfo.recipientId)) {
            messagesToDelete.Add(chatInfo);
            handleChatMessageDeliveryError(chatInfo.senderId, chatInfo.messageType, chatInfo.text, chatInfo.extra);
            continue;
         }

         // If the player entity does not exist yet, the user is still in the process of redirection between servers, so try again later
         NetEntity player = EntityManager.self.getEntity(chatInfo.recipientId);
         if (player == null) {
            continue;
         }

         commonSendChatMessageToPlayer(player, chatInfo);
         messagesToDelete.Add(chatInfo);
      }

      // Delete messages tagged for deletion
      foreach (ChatInfo chatInfo in messagesToDelete) {
         _missedChatMessages.Remove(chatInfo);
      }
   }

   [Server]
   private void commonSendChatMessageToPlayer (NetEntity player, ChatInfo chatInfo) {
      switch (chatInfo.messageType) {
         case ChatInfo.Type.Global:
            player.Target_ReceiveGlobalChat(chatInfo.chatId, chatInfo.text, chatInfo.chatTime.ToBinary(), chatInfo.sender, chatInfo.senderId, GuildIconData.guildIconDataToString(chatInfo.guildIconData), chatInfo.guildName, chatInfo.isSenderMuted, chatInfo.isSenderAdmin, chatInfo.extra);
            break;
         default:
            player.Target_ReceiveSpecialChat(player.connectionToClient, chatInfo.chatId, chatInfo.text, chatInfo.sender, chatInfo.recipient, chatInfo.chatTime.ToBinary(), chatInfo.messageType, chatInfo.guildIconData, chatInfo.guildName, chatInfo.senderId, chatInfo.isSenderMuted, chatInfo.extra);
            break;
      }
   }

   [Server]
   public void handleChatMessageDeliveryError (int senderUserId, ChatInfo.Type messageType, string originalMessage, string extra) {
      if (messageType == ChatInfo.Type.Whisper) {
         // For whispers, send a notification to the sender when the message failed to be delivered   
         ChatInfo chatInfo = new ChatInfo(0, "Could not find the recipient", DateTime.UtcNow, ChatInfo.Type.Error, extra: extra);
         chatInfo.recipient = "";
         ServerNetworkingManager.self.sendSpecialChatMessage(senderUserId, chatInfo);
      }
   }

   #region Private Variables

   // A list of the data for all available chat commands
   protected List<CommandData> _commandData = new List<CommandData>();

   // A list of all of the messages we've received
   protected List<ChatInfo> _chats = new List<ChatInfo>();

   // The last guild chat ID that we processed
   protected int _lastGuildChatId = 0;

   // The default number of messages to include in the log when requested
   protected const int MAX_MESSAGES_IN_LOG = 50;

   // A record of all messages the user has sent this login
   protected List<string> _sentMessageHistory = new List<string>();

   // A record of all names the user has sent a whisper to
   protected List<string> _sentWhisperNameHistory = new List<string>();

   // A record of the last person to send us a whisper
   protected string _lastWhisperSender;

   // How many messages ago we should autofill
   protected int _numMessagesAgo = 0;

   // How many whisper names ago we should autofill
   protected int _numWhisperNamesAgo = 0;

   // When we last typed
   protected float _lastTypeTime = 0.0f;

   // How long we have to have a break in typing for auto-completes to show
   protected const float AUTO_COMPLETE_SHOW_DELAY = 0.3f;

   // Flag telling us when we should update auto-completes
   protected bool _shouldUpdateAutoCompletes = false;

   // Chat messages sent to disconnected/redirected users
   protected List<ChatInfo> _missedChatMessages = new List<ChatInfo>();

   #endregion
}
