using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using TMPro;
using System.Text;
using System;

public class ChatManager : MonoBehaviour {
   #region Public Variables

   // The Chat Panel, need to have direct reference in case something gets logged during Awake()
   public ChatPanel chatPanel;

   // The panel managing the display and changing of the auto-complete options
   [HideInInspector]
   public AutoCompletePanel autoCompletePanel;

   // Self
   public static ChatManager self;

   #endregion

   void Awake () {
      self = this;

      // Setup auto-complete panel
      GameObject optionsPanel = Instantiate(Resources.Load<GameObject>("Prefabs/Auto-completes/Auto-complete Section"), chatPanel.inputField.transform.parent.parent);
      autoCompletePanel = optionsPanel.GetComponentInChildren<AutoCompletePanel>();
   }

   private void Start () {
      // Add the various chat commands that we're going to allow
      _commandData.Add(new CommandData("/bug", "Sends a bug report to the server", BugReportManager.self.sendBugReportToServer, parameterNames: new List<string>() { "bugInformation" }));
      _commandData.Add(new CommandData("/emote", "Performs an emote", sendEmoteMessageToServer, parameterNames: new List<string>() { "emoteDescription" }));
      _commandData.Add(new CommandData("/invite", "Invites a user to your group", VoyageGroupManager.self.handleInviteCommand, parameterNames: new List<string>() { "userName" }));
      _commandData.Add(new CommandData("/group", "Send a message to your group", sendGroupMessageToServer, parameterNames: new List<string>() { "message" }));
      _commandData.Add(new CommandData("/officer", "Executes an officer command, if you have officer privileges", sendOfficerMessageToServer, parameterNames: new List<string>() { "officerCommand" }));
      _commandData.Add(new CommandData("/guild", "Executes a guild command", sendGuildMessageToServer, parameterNames: new List<string>() { "guildCommand" }));
      _commandData.Add(new CommandData("/complain", "Sends a complaint about a user", sendComplainToServer, parameterNames: new List<string>() { "userName", "details" }));
      _commandData.Add(new CommandData("/roll", "Rolls a die", sendRollToServer, parameterNames: new List<string>() { "max", "min" }));
   }

   private void Update () {
      if (!isTyping()) {
         return;
      }

      if (Input.GetKeyDown(KeyCode.UpArrow)) {
         changeNumMessagesAgo(increment: true);
      }

      if (Input.GetKeyDown(KeyCode.DownArrow)) {
         changeNumMessagesAgo(increment: false);
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

      // Show it in the chat panel
      if (chatPanel != null) {
         chatPanel.addChatInfo(chatInfo);
      }
   }

   public void sendEmoteMessageToServer (string message) {
      sendMessageToServer(message, ChatInfo.Type.Emote);
   }

   public void sendGroupMessageToServer (string message) {
      sendMessageToServer(message, ChatInfo.Type.Group);
   }

   public void sendOfficerMessageToServer (string message) {
      sendMessageToServer(message, ChatInfo.Type.Officer);
   }

   public void sendGuildMessageToServer (string message) {
      sendMessageToServer(message, ChatInfo.Type.Guild);
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

   public static string extractWhisperMessageFromChat (string extractedUserName, string message) {
      return message.Replace(ChatPanel.WHISPER_PREFIX + extractedUserName + " ", "");
   }

   public void onChatLostFocus () {
      autoCompletePanel.inputFieldFocused = false;
      autoCompletePanel.updatePanel();
   }

   public void onChatGainedFocus () {
      autoCompletePanel.inputFieldFocused = true;
      autoCompletePanel.updatePanel();
   }

   public void processChatInput (string textToProcess) {
      if (Util.isEmpty(textToProcess)) {
         return;
      }

      _sentMessageHistory.Add(textToProcess);

      // Check if it's a chat command
      if (textToProcess.StartsWith("/") && !textToProcess.StartsWith(ChatPanel.WHISPER_PREFIX)) {
         executeChatCommand(textToProcess);
      } else {
         sendMessageToServer(textToProcess, ChatPanel.self.currentChatType);
      }

      resetMessagesAgo();
   }

   public void onChatInputValuechanged (string inputString) {
      Global.player.admin.tryAutoCompleteForGetItemCommand(inputString);
      tryAutoCompleteChatCommand();

      if (Util.isEmpty(inputString)) {
         resetMessagesAgo();
      }
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

      if (Global.player != null) {         
         Global.player.rpc.Cmd_SubmitComplaint(username, details, getChatLog(), Util.getTextureBytesForTransport(Util.getScreenshot()), machineIdentifier);
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

      return ChatPanel.self.inputField.isFocused;
   }

   public void addCommand (CommandData newCommand) {
      _commandData.Add(newCommand);
   }

   public void tryAutoCompleteChatCommand () {
      string inputString = chatPanel.inputField.text;

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

   private void changeNumMessagesAgo (bool increment) {
      _numMessagesAgo += (increment) ? 1 : -1;

      if (_numMessagesAgo < 0) {
         _numMessagesAgo = 0;
      }

      if (_numMessagesAgo > _sentMessageHistory.Count) {
         _numMessagesAgo = _sentMessageHistory.Count;
      }

      tryAutofillOldMessage();
   }

   private void resetMessagesAgo () {
      _numMessagesAgo = 0;
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

   // How many messages ago we should autofill
   protected int _numMessagesAgo = 0;

   #endregion
}
