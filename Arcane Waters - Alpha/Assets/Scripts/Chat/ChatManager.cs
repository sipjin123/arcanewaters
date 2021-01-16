using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using TMPro;
using System.Text;

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
      Officer = 7,
      Guild = 8,
      Complain = 9,
      Roll = 10,
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
      _commands.Add(Type.Officer, new List<string> { "/officer", "/off", "/of", "/o" });
      _commands.Add(Type.Guild, new List<string> { "/guild", "/gld", "/g" });
      _commands.Add(Type.Complain, new List<string>() { "/complain", "/report" });
      _commands.Add(Type.Roll, new List<string>() { "/roll", "/random" });

      // Setup auto-complete panel
      GameObject optionsPanel = Instantiate(new GameObject("AutoCompleteOptionsPanel"), chatPanel.inputField.transform.parent);
      _autoCompletePanel = optionsPanel.AddComponent<AutoCompletePanel>();
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
         chatPanel.addChatInfo(chatInfo);
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
      foreach (string command in _commands[Type.Complain]) {
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
      foreach (string command in _commands[Type.Complain]) {
         message = message.Replace(command, "");
      }

      return message.Replace($"{username} ", "").Trim();
   }

   public static string extractWhisperMessageFromChat (string extractedUserName, string message) {
      return message.Replace(ChatPanel.WHISPER_PREFIX + extractedUserName + " ", "");
   }

   private void Update () {
      // Pressing tab will auto-fill the selected auto-complete
      if (Input.GetKeyDown(KeyCode.Tab) && _autoCompletePanel.isActive()) {
         string autoComplete = _autoCompletePanel.getSelectedCommand();
         chatPanel.inputField.text = autoComplete;
         chatPanel.inputField.MoveTextEnd(false);
      }

      if (Input.GetKeyDown(KeyCode.UpArrow) && _autoCompletePanel.isActive()) {
         _autoCompletePanel.moveSelection(moveUp: true);
         chatPanel.inputField.MoveTextEnd(false);
      }

      if (Input.GetKeyDown(KeyCode.DownArrow) && _autoCompletePanel.isActive()) {
         _autoCompletePanel.moveSelection(moveUp: false);
         chatPanel.inputField.MoveTextEnd(false);
      }
   }

   public void onChatLostFocus () {
      _autoCompletePanel.setAutoCompletes(null);
   }

   public void onChatGainedFocus () {
      tryAutoCompleteChatCommand(chatPanel.inputField.text);
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
      tryAutoCompleteChatCommand(inputString);
   }

   protected void executeChatCommand (string message) {
      string prefix = "";
      Type type = Type.None;

      // Separate out just the command part
      string messageCommand = message.Split(' ')[0];

      // Cycle over each of the command types
      foreach (Type t in System.Enum.GetValues(typeof(Type))) {
         // Make sure there's actually an entry for said command
         if (_commands.ContainsKey(t)) {
            // Figure out which version of the command they used, so we can strip it out
            foreach (string command in _commands[t]) {
               if (command == messageCommand) {
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
      } else if (type == Type.Officer) {
         sendMessageToServer(trimmedMessage, ChatInfo.Type.Officer);
      } else if (type == Type.Guild) {
         sendMessageToServer(trimmedMessage, ChatInfo.Type.Guild);
      } else if (type == Type.Complain) {
         sendComplainToServer(message);
      } else if (type == Type.Roll) {
         sendRollToServer(trimmedMessage);
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

   private void tryAutoCompleteChatCommand (string inputString) {
      if (!inputString.StartsWith("/")) {
         _autoCompletePanel.setAutoCompletes(null);
         return;
      }

      string[] inputParts = inputString.Split(' ');

      List<string> possibleCommands = new List<string>();
      List<string> autoCompletes = new List<string>();

      // Handle as an admin command
      if (_commands[Type.Admin].Contains(inputParts[0]) && inputParts.Length > 1) {
         autoCompletes = Util.getAutoCompletes(inputParts[1], new List<string>(Global.player.admin.getCommandList().Values), "/admin ");
      
      // Handle as a regular command
      } else {
         // Check each command type
         foreach (Type t in System.Enum.GetValues(typeof(Type))) {
            // Make sure there's actually an entry for said command
            if (_commands.ContainsKey(t)) {
               possibleCommands.Add(_commands[t][0]);
            }
         }

         autoCompletes = Util.getAutoCompletes(inputString, possibleCommands);
      }

      _autoCompletePanel.resetSelection();
      _autoCompletePanel.setAutoCompletes(autoCompletes);
   }

   #region Private Variables

   // The commands available to the user through the chat prompt
   protected Dictionary<Type, List<string>> _commands = new Dictionary<Type, List<string>>();

   // A list of all of the messages we've received
   protected List<ChatInfo> _chats = new List<ChatInfo>();

   // The last guild chat ID that we processed
   protected int _lastGuildChatId = 0;

   // The default number of messages to include in the log when requested
   protected const int MAX_MESSAGES_IN_LOG = 50;

   // The panel managing the display and changing of the auto-complete options
   private AutoCompletePanel _autoCompletePanel;

   #endregion
}
