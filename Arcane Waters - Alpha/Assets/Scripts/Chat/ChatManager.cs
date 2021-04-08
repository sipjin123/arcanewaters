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

public class ChatManager : GenericGameManager
{
   #region Public Variables

   // The Chat Panel, need to have direct reference in case something gets logged during Awake()
   public ChatPanel chatPanel;

   // The panel managing the display and changing of the auto-complete options
   [HideInInspector]
   public AutoCompletePanel autoCompletePanel;

   // The panel managing the display and changing of the whisper name auto-complete options
   public WhisperAutoCompletePanel whisperAutoCompletePanel;

   // Self
   public static ChatManager self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;

      // Setup auto-complete panel
      GameObject optionsPanel = Instantiate(Resources.Load<GameObject>("Prefabs/Auto-completes/Auto-complete Section"), chatPanel.inputField.transform.parent.parent);
      autoCompletePanel = optionsPanel.GetComponentInChildren<AutoCompletePanel>();
   }

   private void Start () {
      // Add the various chat commands that we're going to allow
      _commandData.Add(new CommandData("/bug", "Sends a bug report to the server", BugReportManager.self.sendBugReport, parameterNames: new List<string>() { "bugInformation" }));
      _commandData.Add(new CommandData("/emote", "Performs an emote", sendEmoteMessageToServer, parameterNames: new List<string>() { "emoteDescription" }));
      _commandData.Add(new CommandData("/invite", "Invites a user to your group", VoyageGroupManager.self.handleInviteCommand, parameterNames: new List<string>() { "userName" }));
      _commandData.Add(new CommandData("/group", "Send a message to your group", sendGroupMessageToServer, parameterNames: new List<string>() { "message" }));
      _commandData.Add(new CommandData("/officer", "Executes an officer command, if you have officer privileges", sendOfficerMessageToServer, parameterNames: new List<string>() { "officerCommand" }));
      _commandData.Add(new CommandData("/guild", "Executes a guild command", sendGuildMessageToServer, parameterNames: new List<string>() { "guildCommand" }));
      _commandData.Add(new CommandData("/complain", "Sends a complaint about a user", sendComplainToServer, parameterNames: new List<string>() { "userName", "details" }));
      _commandData.Add(new CommandData("/roll", "Rolls a die", sendRollToServer, parameterNames: new List<string>() { "max", "min" }));
      _commandData.Add(new CommandData("/whisper", "Sends a private message to a user", sendWhisperMessageToServer, parameterNames: new List<string>() { "userName", "message" }));
      _commandData.Add(new CommandData("/w", "Sends a private message to a user", sendWhisperMessageToServer, parameterNames: new List<string>() { "userName", "message" }));
      _commandData.Add(new CommandData("/r", "Sends a private message to the last user that whispered to you", tryReply, parameterNames: new List<string>() { "message" }));
   }

   private void Update () {
      if (InputManager.getKeyActionDown(KeyAction.Reply) && !chatPanel.inputField.isFocused && !chatPanel.nameInputField.isFocused && !string.IsNullOrEmpty(_lastWhisperSender)) {
         chatPanel.inputField.text = "/whisper " + _lastWhisperSender + " ";
         chatPanel.focusInputField();
      }

      if (!isTyping()) {
         return;
      }

      if (KeyUtils.GetKeyDown(Key.UpArrow)) {
         if (ChatPanel.self.inputField.isFocused) {
            changeNumMessagesAgo(increment: true);
         } else if (ChatPanel.self.nameInputField.isFocused) {
            changeWhisperNamesAgo(increment: true);
         }
      }

      if (KeyUtils.GetKeyDown(Key.DownArrow)) {
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

   public void addChatInfo (ChatInfo chatInfo) {
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

      if (chatType == ChatInfo.Type.Officer) {
         if (Global.player.guildId == 0) {
            this.addChat("You are not currently in a guild!", ChatInfo.Type.Error);
            return;
         } else if (!Global.player.canPerformAction(GuildPermission.OfficerChat)) {
            this.addChat("You have insufficient permissions to access officer chat!", ChatInfo.Type.Error);
            return;
         }
      }

      // Pass the message along to the server
      Global.player.rpc.Cmd_SendChat(message, chatType);
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
      autoCompletePanel.inputFieldFocused = false;
      autoCompletePanel.updatePanel();
   }

   public void onChatGainedFocus () {
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

   private void sendComplainToServer (string message) {
      string username = extractComplainNameFromChat(message);
      string details = extractComplainMessageFromChat(message, username);
      string machineIdentifier = SystemInfo.deviceName;

      int deploymentId = Util.getDeploymentId();

      if (Global.player != null) {
         Global.player.rpc.Cmd_SubmitComplaint(username, details, getChatLog(), Util.getTextureBytesForTransport(Util.getScreenshot()), machineIdentifier, deploymentId);
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
      string inputString = chatPanel.inputField.text;

      // Remove autocompletes when we are typing a bug report
      if (inputString.StartsWith("/bug ")) {
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

      autoCompletePanel.setAutoCompletes(autoCompleteCommands);
      autoCompletePanel.setAutoCompletesWithParameters(autoCompleteParameters);
   }

   private void tryAutofillOldMessage () {
      if (_numMessagesAgo < 1 || _numMessagesAgo > _sentMessageHistory.Count) {
         return;
      }

      int indexInList = _sentMessageHistory.Count - _numMessagesAgo;
      chatPanel.inputField.text = _sentMessageHistory[indexInList];
      chatPanel.inputField.MoveTextEnd(false);
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
      _lastWhisperSender = sender;
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

   #endregion
}
