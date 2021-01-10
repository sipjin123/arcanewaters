﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System;
using System.Linq;

public class ChatPanel : MonoBehaviour {
   #region Public Variables

   // The height of 1 chat line
   public static int chatLineHeight = 22;

   // The visible number of lines in each mode, which defines the panel height
   public static int CHAT_LINES_MINIMIZED = 3;
   public static int CHAT_LINES_NORMAL = 4;
   public static int CHAT_LINES_EXPANDED = 18;

   // The speed at which the panel fades in and out
   public static float FADE_SPEED = 8f;

   // The parameter for smooth movement (smaller is faster)
   public static float SMOOTH_TIME = 0.05f;

   // The input field where the whisper recipient name will be input
   public InputField nameInputField;

   // The constant whisper prefix for server message processing
   public const string WHISPER_PREFIX = "/w ";

   // The panel modes
   public enum Mode {
      Minimized = 0,
      Normal = 1,
      Expanded = 2
   }

   // The chat tabs
   public enum Tab
   {
      All = 0,
      Group = 1
   }

   // The chat types displayed in each tab
   public static Dictionary<Tab, HashSet<ChatInfo.Type>> tabToChatType = new Dictionary<Tab, HashSet<ChatInfo.Type>>() {
      {Tab.All, new HashSet<ChatInfo.Type>(((ChatInfo.Type[])Enum.GetValues(typeof(ChatInfo.Type))).ToList()) },
      {Tab.Group,  new HashSet<ChatInfo.Type> {ChatInfo.Type.Group, ChatInfo.Type.Whisper, ChatInfo.Type.Error, ChatInfo.Type.System } },
   };

   // The container of all of our children components
   public GameObject mainContainer;

   // The container of our various Chat components
   public GameObject chatContainer;

   // The container of the chat messages
   public GameObject messagesContainer;

   // The various components we manage
   public ScrollRect scrollRect;
   public RectTransform contentRect;
   public RectTransform messageBackgroundRect;
   public Image messageBackgroundImage;
   public RectTransform toolbarRect;
   public CanvasGroup toolbarCanvas;
   public InputField inputField;
   public GameObject scrollBarContainer;
   public Text chatModeText;

   // The placeholder message
   public GameObject placeHolder;

   // The prefabs we use for creating various chat lines
   public SpeakChatLine speakChatLinePrefab;
   public TradeChatLine tradeChatLinePrefab;
   public GuildChatLine guildChatLinePrefab;
   public GuildInviteChatLine guildInviteChatLinePrefab;
   public GameObject speakChatRow;
   public GameObject tradeChatRow;
   public GameObject guildChatRow;
   public GameObject guildInviteChatRow;

   // Our currently selected chatType
   public ChatInfo.Type currentChatType = ChatInfo.Type.Global;

   // When the mouse is over this defined zone, we consider that it hovers the message panel
   public RectTransform messagePanelHoveringZone;

   // The expand button
   public GameObject expandButton;

   // The collapse button
   public GameObject collapseButton;

   // The chat tab toggles
   public Toggle allTabToggle;
   public Toggle groupTabToggle;

   // Self
   public static ChatPanel self;

   // The custom color for each entity speaking
   public Color enemySpeechColor, playerSpeechColor, otherPlayerSpeechColor, serverChatColor, systemChatColor, globalChatLocalColor, globalChatOtherColor;
   public Color enemyNameColor, playerNameColor, otherPlayerNameColor, serverNameColor, systemNameColor, globalNameLocalColor, globalNameOtherColor;
   public Color whisperNameColor, whisperMessageColor, whisperReceiverNameColor, whisperReceiverMessageColor;
   public Color groupNameLocalColor, groupMessageLocalColor, groupNameOtherColor, groupMessageOtherColor;

   #endregion

   void Awake () {
      self = this;

      // Disable this panel when the server is running in batch mode
      if (Util.isBatch()) {
         this.gameObject.SetActive(false);
      }
   }

   void Start () {
      // We don't need the placeholder, it's just nice for testing in the Editor
      placeHolder.SetActive(false);

      // Clear out any example texts from the editor scene
      messagesContainer.DestroyChildren();

      // Rebuild our message list once a second to hide any old messages
      InvokeRepeating(nameof(rebuildMessageList), 1f, 1f);

      // Call the autocomplete function when the user writes in chat
      inputField.onValueChanged.AddListener((string inputString) => ChatManager.self.onChatInputValuechanged(inputString));
   }

   void Update () {
      if (!shouldShowChat()) {
         mainContainer.SetActive(false);
         return;
      }

      mainContainer.SetActive(true);

      // Focus the chat window if the forward slash key is released
      if ((Input.GetKeyUp(KeyCode.Slash))) {

         if (!wasJustFocused()) {
            inputField.text = "/";

            // Activate the input field in the next frame to avoid weird interactions
            StartCoroutine(activateAfterDelay());
         }
      }

      // Modify the chat mode button based on our current selection
      chatModeText.text = getChatModeString();

      // Any time the mouse button is released, reset the scroll click boolean
      if (Input.GetMouseButtonUp(0)) {
         _isScrolling = false;
      }

      // Keep track of when the chat input is focused
      if (inputField.isFocused) {
         _lastFocusTime = Time.time;
      }

      // In minimized mode, switch to normal mode when clicking the input field
      if (_mode == Mode.Minimized && wasJustFocused()) {
         setMode(Mode.Normal);
      }

      // Handle panel animations depending on the mode
      switch (_mode) {
         case Mode.Minimized:
            if (_isMouseOverInputField) {
               // While the mouse is over the input box, switch to normal mode without toolbar
               animateToolbarAlpha(0f);
               animatePanelBackgroundAlpha(1f);
               animatePanelHeight(CHAT_LINES_NORMAL);
            } else {
               animateToolbarAlpha(0f);
               animatePanelBackgroundAlpha(0f);
               animatePanelHeight(CHAT_LINES_MINIMIZED);
            }
            break;
         case Mode.Normal:
            // Display the toolbar if the mouse is over the panel
            if (RectTransformUtility.RectangleContainsScreenPoint(messagePanelHoveringZone, Input.mousePosition)) {
               animateToolbarAlpha(1f);
            } else {
               animateToolbarAlpha(0f);
            }
            animatePanelBackgroundAlpha(1f);
            animatePanelHeight(CHAT_LINES_NORMAL);
            break;
         case Mode.Expanded:
            animatePanelBackgroundAlpha(1f);
            animateToolbarAlpha(1f);
            animatePanelHeight(CHAT_LINES_EXPANDED);
            break;
         default:
            break;
      }
   }

   private void animateToolbarAlpha (float targetAlpha) {
      if (toolbarCanvas.alpha == targetAlpha) {
         return;
      }

      float direction = toolbarCanvas.alpha < targetAlpha ? 1 : -1;
      float alpha = toolbarCanvas.alpha + direction * FADE_SPEED * Time.deltaTime;
      alpha = Mathf.Clamp(alpha, 0, 1);

      toolbarCanvas.alpha = alpha;
   }

   private void animatePanelBackgroundAlpha (float targetAlpha) {
      if (messageBackgroundImage.color.a == targetAlpha) {
         return;
      }

      float direction = messageBackgroundImage.color.a < targetAlpha ? 1 : -1;
      float alpha = messageBackgroundImage.color.a + direction * FADE_SPEED * Time.deltaTime;
      alpha = Mathf.Clamp(alpha, 0, 1);

      Util.setAlpha(messageBackgroundImage, alpha);
   }

   private void animatePanelHeight (int visibleLinesCount) {
      float targetHeight = toolbarRect.sizeDelta.y + 12 + visibleLinesCount * chatLineHeight;

      if (Mathf.Approximately(messageBackgroundRect.sizeDelta.y, targetHeight)) {
         return;
      }

      // Disable the elastic scroll movement while animating the height
      if (Mathf.Abs(targetHeight - messageBackgroundRect.sizeDelta.y) > 0.1f) {
         scrollRect.movementType = ScrollRect.MovementType.Clamped;
      } else {
         scrollRect.movementType = ScrollRect.MovementType.Elastic;         
      }

      // In minimized mode, move the scrollbar to the bottom
      if (_mode == Mode.Minimized) {
         scrollRect.verticalNormalizedPosition = 0f;
      }

      messageBackgroundRect.sizeDelta = new Vector2(
         messageBackgroundRect.sizeDelta.x,
         Mathf.SmoothDamp(messageBackgroundRect.sizeDelta.y, targetHeight, ref _messagePanelVelocity,
            SMOOTH_TIME, float.MaxValue, Time.deltaTime));
   }

   void OnGUI () {
      // Submit the field when enter is pressed and the field is focused
      if (inputField.isFocused && Input.GetKeyDown(KeyCode.Return)) {

         if (inputField.text != "") {
            // Send the message off to the server for processing
            string message = inputField.text;
            if (currentChatType == ChatInfo.Type.Whisper) {
               message = WHISPER_PREFIX + nameInputField.text + " " + message;
            }

            ChatManager.self.processChatInput(message);

            // Clear out the text now that it's been used
            inputField.text = "";
         }

         // Deselect the input field
         inputField.DeactivateInputField();

         // Unselect the input field UI from the event system so ChatManager.isTyping() will be set to false
         GameObject currentSelection = EventSystem.current.currentSelectedGameObject;
         
         // Check if we're typing in an input field
         if (currentSelection != null && Util.hasInputField(currentSelection)) {
            EventSystem.current.SetSelectedGameObject(null);
         }
      }

      // Activate the input field when enter is pressed and the field is unfocused, except if the
      // player is writing a mail      
      if (Input.GetKeyDown(KeyCode.Return) && !((MailPanel) PanelManager.self.get(Panel.Type.Mail)).isWritingMail()) {
         if (!wasJustFocused()) {
            // Activate the input field in the next frame to avoid weird interactions
            StartCoroutine(activateAfterDelay());
         }
      }
   }

   public void onExpandButtonPressed () {      
      if (_mode == Mode.Normal) {
         setMode(Mode.Expanded);
      }
   }

   public void onCollapseButtonPressed () {
      switch (_mode) {
         case Mode.Normal:
            setMode(Mode.Minimized);
            break;
         case Mode.Expanded:
            setMode(Mode.Normal);
            break;
         default:
            break;
      }
   }

   public void onMouseEnterInputBox () {
      _isMouseOverInputField = true;
      if (_mode == Mode.Minimized) {
         // Toggle messages visibility
         rebuildMessageList();
      }
   }

   public void onMouseExitInputBox () {
      _isMouseOverInputField = false;
      if (_mode == Mode.Minimized) {
         // Toggle messages visibility
         rebuildMessageList();
      }
   }

   public bool wasJustFocused () {
      // Check if the chat input field was focused recently
      if (Time.time - _lastFocusTime < .200) {
         return true;
      }

      return false;
   }

   public void addGuildInvite (GuildInvite invite) {
      // Create a new Chat Line instance and assign the parent
      GameObject chatRow = Instantiate(guildInviteChatRow, messagesContainer.transform);
      GuildInviteChatLine chatLine = chatRow.GetComponentInChildren<GuildInviteChatLine>();
      chatLine.name = "Guild Invite";
   }

   public void addChatInfo (ChatInfo chatInfo) {
      // If it is a whisper chat and receiver text is blank, means user does not exist and this function will be skipped
      if (chatInfo.messageType == ChatInfo.Type.Whisper && string.IsNullOrEmpty(chatInfo.recipient)) {
         return;
      }

      // Create a new Chat Row instance and assign the parent
      GameObject chatRow = Instantiate(speakChatRow, messagesContainer.transform);
      SpeakChatLine chatLine = chatRow.GetComponentInChildren<SpeakChatLine>();
      GuildIcon rowGuildIcon = chatRow.GetComponentInChildren<GuildIcon>();
      chatLine.name = "Chat Message";
      chatLine.chatInfo = chatInfo;

      // Assign guild icon parts if the guild info of the sender is available
      if (chatInfo.guildIconData != null) {
         rowGuildIcon.gameObject.SetActive(true);
         rowGuildIcon.setBorder(chatInfo.guildIconData.iconBorder);
         rowGuildIcon.setBackground(chatInfo.guildIconData.iconBackground, chatInfo.guildIconData.iconBackPalettes);
         rowGuildIcon.setSigil(chatInfo.guildIconData.iconSigil, chatInfo.guildIconData.iconSigilPalettes);
      } else {
         rowGuildIcon.gameObject.SetActive(false);
      }

      // We'll set the message up differently based on whether a sender was defined
      if (Util.isEmpty(chatInfo.sender)) {
         chatLine.text.text = string.Format("<color={0}>{1}</color>", getSenderNameColor(chatInfo.messageType), chatInfo.text);
      } else if (chatInfo.messageType == ChatInfo.Type.Emote) {
         chatLine.text.text = string.Format("<color={0}>{1} {2}</color>", getColorString(chatInfo.messageType), chatInfo.sender, chatInfo.text);
      } else if (chatInfo.messageType == ChatInfo.Type.Group) {
         chatLine.text.text = string.Format("<color={0}>[GROUP] {1}:</color> <color={2}>{3}</color>", getSenderNameColor(chatInfo.messageType), chatInfo.sender, getColorString(chatInfo.messageType), chatInfo.text);
      } else if (chatInfo.messageType == ChatInfo.Type.Officer || chatInfo.messageType == ChatInfo.Type.Guild) {
         chatLine.text.text = string.Format("<color={0}>{1}:</color> <color={2}>{3}</color>", getSenderNameColor(chatInfo.messageType, false), chatInfo.sender, getColorString(chatInfo.messageType, false), chatInfo.text);
      } else {
         bool isLocalPlayer = true;
         if (Global.player != null) {
            isLocalPlayer = chatInfo.senderId == Global.player.userId ? true : false;
         }

         string messageSource = chatInfo.sender;
         if (chatInfo.messageType == ChatInfo.Type.Whisper) {
            messageSource = isLocalPlayer ? ("You whispered to " + chatInfo.recipient) : (chatInfo.sender + " whispers");
         }

         chatLine.text.text = string.Format("<color={0}>{1}:</color> <color={2}>{3}</color>", getSenderNameColor(chatInfo.messageType, isLocalPlayer), messageSource, getColorString(chatInfo.messageType, isLocalPlayer), chatInfo.text);
      }

      // In minimized mode, keep the scrollbar at the bottom
      if (_mode == Mode.Minimized) {
         scrollRect.verticalNormalizedPosition = 0f;
      }

      // Hide the chat line if the current tab filters it
      if (!isChatLineVisibleInTab(chatLine.chatInfo)) {
         chatLine.transform.parent.gameObject.SetActive(false);
         return;
      }

      // If we're just starting up, some Managers may not exist yet
      if (BodyManager.self == null) {
         return;
      }

      if (chatInfo.messageType != ChatInfo.Type.Emote && chatInfo.messageType != ChatInfo.Type.Whisper) {
         // If we have a Body for the specified sender, create a speech bubble
         BodyEntity body = BodyManager.self.getBody(chatInfo.senderId);
         if (body != null) {
            SpeechManager.self.showSpeechBubble(body, chatInfo.text);
         } else {
            // If we have a Ship for the specified sender, create a speech bubble
            SeaEntity seaEntity = SeaManager.self.getEntityByUserId(chatInfo.senderId);
            if (seaEntity != null && seaEntity is PlayerShipEntity) {
               SpeechManager.self.showSpeechBubble((PlayerShipEntity) seaEntity, chatInfo.text);
            }
         }
      }
   }

   public string getSenderNameColor (ChatInfo.Type chatType, bool isLocalPlayer = false) {
      Color newColor = Color.white;
      switch (chatType) {
         case ChatInfo.Type.Global:
            if (isLocalPlayer) {
               newColor = globalNameLocalColor;
            } else {
               newColor = globalNameOtherColor;
            }
            break;
         case ChatInfo.Type.Local:
            if (isLocalPlayer) {
               newColor = playerNameColor;
            } else {
               newColor = otherPlayerNameColor;
            }
            break;
         case ChatInfo.Type.System:
            newColor = systemNameColor;
            break;
         case ChatInfo.Type.Whisper:
            if (isLocalPlayer) {
               newColor = whisperReceiverNameColor;
            } else {
               newColor = whisperNameColor;
            }
            break;
         case ChatInfo.Type.Group:
            if (isLocalPlayer) {
               newColor = groupNameLocalColor;
            } else {
               newColor = groupNameOtherColor;
            }
            break;
      }
      return "#" + ColorUtility.ToHtmlStringRGBA(newColor);
   }

   public void setIsScrolling () {
      _isScrolling = true;
   }

   public bool isScrolling () {
      return _isScrolling;
   }

   public void onChatTabPressed () {
      if (allTabToggle.isOn) {
         _tab = Tab.All;
         setCurrentChatType(ChatInfo.Type.Global);
      } else if (groupTabToggle.isOn) {
         _tab = Tab.Group;
         setCurrentChatType(ChatInfo.Type.Group);
      }

      rebuildMessageList();
   }

   public void chatModeButtonPressed () {
      if (currentChatType == ChatInfo.Type.Whisper) {
         setCurrentChatType(ChatInfo.Type.Local);
      } else if (currentChatType == ChatInfo.Type.Local) {
         setCurrentChatType(ChatInfo.Type.Guild);
      } else if (currentChatType == ChatInfo.Type.Guild) {
         setCurrentChatType(ChatInfo.Type.Group);
      } else if (currentChatType == ChatInfo.Type.Group) {
         setCurrentChatType(ChatInfo.Type.Global);
      } else if (currentChatType == ChatInfo.Type.Global) {
         setCurrentChatType(ChatInfo.Type.Whisper);
      }
   }

   private void setCurrentChatType (ChatInfo.Type chatType) {
      currentChatType = chatType;
      nameInputField.gameObject.SetActive(currentChatType == ChatInfo.Type.Whisper);
   }

   public void sendWhisperTo (string userName) {
      // Input the user name in the whisper name input field
      nameInputField.text = userName;

      setCurrentChatType(ChatInfo.Type.Whisper);

      if (!wasJustFocused()) {
         // Activate the input field in the next frame to avoid weird interactions
         StartCoroutine(activateAfterDelay());
      }
   }

   protected bool shouldShowChat () {
      if (Global.player == null && !Global.isRedirecting) {
         return false;
      }

      return ClientScene.ready || Global.isRedirecting;
   }

   protected void rebuildMessageList () {
      int num = 0;
      List<ChatLine> deleteList = new List<ChatLine>();
      HashSet<ChatInfo.Type> visibleChatTypes = tabToChatType[_tab];

      // Cycle over all of the chat lines in our container
      foreach (ChatLine chatLine in messagesContainer.GetComponentsInChildren<ChatLine>(true)) {
         // If we're over our maximum number of messages, add the oldest messages in the list to be deleted
         if (messagesContainer.transform.childCount - MAX_MESSAGE_COUNT > num) {
            deleteList.Add(chatLine);
         }

         num++;

         // Hide messages that are filtered by the tab
         if (!isChatLineVisibleInTab(chatLine.chatInfo)) {
            chatLine.transform.parent.gameObject.SetActive(false);
            continue;
         }

         // In minimized mode, only show recent messages, unless the mouse is over the input field
         if (_mode == Mode.Minimized) {
            if (!_isMouseOverInputField) {
               if (Time.time - chatLine.creationTime > CHAT_MESSAGE_DISPLAY_DURATION) {
                  chatLine.transform.parent.gameObject.SetActive(false);
               }
            }
         } else {
            chatLine.transform.parent.gameObject.SetActive(true);
         }
      }

      // Delete any chat lines that we marked
      foreach (ChatLine chatLine in deleteList) {
         Destroy(chatLine.transform.parent.gameObject);
      }
   }

   protected bool isChatLineVisibleInTab (ChatInfo chatInfo) {
      if (tabToChatType[_tab].Contains(chatInfo.messageType)) {
         return true;
      } else {
         return false;
      }
   }

   protected string getChatModeString () {
      string colorString = "#000000FF";

      switch (currentChatType) {
         case ChatInfo.Type.Local:
            return string.Format("<color={0}>Local</color>", colorString);
         case ChatInfo.Type.Global:
            return string.Format("<color={0}>Global</color>", colorString);
         case ChatInfo.Type.Guild:
         case ChatInfo.Type.Officer:
            return string.Format("<color={0}>Guild</color>", colorString);
         case ChatInfo.Type.Group:
            return string.Format("<color={0}>Group</color>", colorString);
         case ChatInfo.Type.Whisper:
            return string.Format("<color={0}>Whisper</color>", colorString);
      }

      return "";
   }

   protected string getColorString (ChatInfo.Type chatType, bool isLocalPlayer = false) {
      Color color = getChatColor(chatType, isLocalPlayer);

      return "#" + ColorUtility.ToHtmlStringRGBA(color);
   }

   protected Color getChatColor (ChatInfo.Type chatType, bool isLocalPlayer = false) {
      switch (chatType) {
         case ChatInfo.Type.Global:
            return isLocalPlayer ? globalChatLocalColor : globalChatOtherColor;
         case ChatInfo.Type.Local:
            return isLocalPlayer ? playerSpeechColor : otherPlayerSpeechColor;
         case ChatInfo.Type.Whisper:
            var chatColor = isLocalPlayer ? whisperReceiverMessageColor : whisperMessageColor;
            return chatColor;
         case ChatInfo.Type.Guild:
         case ChatInfo.Type.Officer:
            return Color.white;
         case ChatInfo.Type.Group:
            return isLocalPlayer ? groupMessageLocalColor : groupMessageOtherColor;
         case ChatInfo.Type.Emote:
            return Color.magenta;
         default:
            return Color.white;
      }
   }

   protected IEnumerator activateAfterDelay () {
      // Wait a frame
      yield return null;

      // Now we can activate
      inputField.ActivateInputField();

      // Have to do this in a separate Coroutine, it's ridiculous
      StartCoroutine(moveCaretToEnd());
   }

   public IEnumerator moveCaretToEnd () {
      // Wait a frame
      yield return null;

      // Don't select the text, that's annoying
      inputField.MoveTextEnd(false);
   }

   private void setMode(Mode mode) {
      if (_mode == mode) {
         return;
      }

      _mode = mode;

      // Panel show, hide and resize is handled in the update
      switch (_mode) {
         case Mode.Minimized:
            toolbarCanvas.gameObject.SetActive(false);
            scrollBarContainer.SetActive(false);
            messageBackgroundImage.raycastTarget = false;

            // Need to rebuild the message list since some messages may toggle their visibility
            rebuildMessageList();
            break;
         case Mode.Normal:
            toolbarCanvas.gameObject.SetActive(true);
            scrollBarContainer.SetActive(true);
            expandButton.SetActive(true);
            collapseButton.SetActive(true);
            messageBackgroundImage.raycastTarget = true;
            scrollRect.vertical = true;

            // Need to rebuild the message list since some messages may toggle their visibility
            rebuildMessageList();
            break;
         case Mode.Expanded:
            toolbarCanvas.gameObject.SetActive(true);
            scrollBarContainer.SetActive(true);
            expandButton.SetActive(false);
            collapseButton.SetActive(true);
            messageBackgroundImage.raycastTarget = true;
            scrollRect.vertical = true;
            break;
         default:
            break;
      }
   }

   #region Private Variables

   // How long we show messages for before they disappear
   protected static float CHAT_MESSAGE_DISPLAY_DURATION = 12f;

   // The maximum number of messages that we'll keep in our chat log
   protected static int MAX_MESSAGE_COUNT = 50;

   // The time at which the chat input was last focused
   protected float _lastFocusTime;

   // Whether we're currently clicking on the scroll bar
   protected bool _isScrolling = false;

   // Gets set to true when the mouse is over the bottom input field area
   protected bool _isMouseOverInputField = false;

   // Velocity parameters used for animations
   protected float _messagePanelVelocity;

   // The panel mode
   protected Mode _mode = Mode.Normal;

   // The selected tab
   protected Tab _tab = Tab.All;

   #endregion
}
