using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using UnityEngine.InputSystem;
using Crosstales.BWF.Manager;
using TMPro;

public class ChatPanel : MonoBehaviour {
   #region Public Variables

   // The height of 1 chat line
   public static float chatLineHeight = 22.6f;

   // The height of the bottom bar in word space
   public static float bottomBarWorldSpaceHeight = 0.16f;

   // The visible number of lines in each mode, which defines the panel height
   public static int CHAT_LINES_MINIMIZED = 4;
   public static int CHAT_LINES_NORMAL = 4;
   public static int CHAT_LINES_EXPANDED = 18;

   // The speed at which the panel fades in and out
   public static float FADE_SPEED = 8f;

   // The parameter for smooth movement (smaller is faster)
   public static float SMOOTH_TIME = 0.05f;

   // The panel min and max width
   public static float MIN_WIDTH = 415;
   public static float MAX_WIDTH = 850;

   // The panel min and max height
   public static float MIN_HEIGHT = 100;
   public static float MAX_HEIGHT = 600;

   // The input field where the whisper recipient name will be input
   public InputField nameInputField;

   // A reference to the gameobject that contains the whisper name input, and the auto-complete panel
   public GameObject whisperNameInput;

   // The constant whisper prefix for server message processing
   public const string WHISPER_PREFIX = "/w ";
   public const string WHISPER_PREFIX_FULL = "/whisper ";

   // Position markers for height calculation
   public RectTransform topPositionMarker = null;
   public RectTransform botPositionMarker = null;

   // The panel modes
   public enum Mode
   {
      Minimized = 0,
      Normal = 1,
      Expanded = 2,
      Freeform = 3
   }

   // The chat tabs
   public enum Tab
   {
      All = 0,
      Custom = 1
   }

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
   public TMP_InputField inputField;
   public GameObject scrollBarContainer;
   public GameObject resizeHandle;
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
   public Toggle customTabToggle;

   // The panel which holds every chat type to choose by player
   public GameObject choosingChatType;

   // The button used to show chat types
   public GameObject expandChatTypesButtons;

   // The width resize handle
   public RectTransform resizeHandleZone;

   // A reference to the whisper auto-complete panel
   public WhisperAutoCompletePanel whisperAutoCompletePanel;

   // Font that is being used for local chat in the form of bubble
   public TMPro.TMP_FontAsset chatBubbleFont;

   // The rect transform of the input field zone
   public RectTransform inputFieldZoneRect;

   // Reference to the containing canvas container
   public Canvas canvas;

   // Self
   public static ChatPanel self;

   // The custom color for each entity speaking
   public Color enemySpeechColor, playerSpeechColor, otherPlayerSpeechColor, serverChatColor, systemChatColor, globalChatLocalColor, globalChatOtherColor;
   public Color enemyNameColor, playerNameColor, otherPlayerNameColor, serverNameColor, systemNameColor, globalNameLocalColor, globalNameOtherColor;
   public Color whisperNameColor, whisperMessageColor, whisperReceiverNameColor, whisperReceiverMessageColor;
   public Color groupNameLocalColor, groupMessageLocalColor, groupNameOtherColor, groupMessageOtherColor;
   public Color guildChatLocalColor, guildChatOtherColor, officerChatLocalColor, officerChatOtherColor;
   public Color adminNameColor;

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
      nameInputField.onValueChanged.AddListener((string inputString) => ChatManager.self.onWhisperNameInputValueChanged(inputString));

      // Set initial chat types
      onAllChatPressed();
   }

   void Update () {
      updateBottomBarHeight();
      processGuiInputfield();

      if (!shouldShowChat()) {
         mainContainer.SetActive(false);
         return;
      }

      mainContainer.SetActive(true);
      messagesContainer.SetActive(true);

      // Adjust chat types panel position to current chat window size
      choosingChatType.GetComponent<RectTransform>().position = toolbarRect.position;

      // Focus the chat window if the forward slash key is released
      if (KeyUtils.GetKeyUp(Key.Slash)) {
         if (MailPanel.self == null || !MailPanel.self.isWritingMail()) {
            if (!wasJustFocused() && !nameInputField.isFocused) {
               inputField.text = "/";

               // Activate the input field in the next frame to avoid weird interactions
               StartCoroutine(CO_FocusAfterDelay(inputField));
            }
         }
      }

      // Modify the chat mode button based on our current selection
      chatModeText.text = getChatModeString();

      // Remove ASCII characters which aren't present in TMP font
      for (int i = inputField.text.Length - 1; i >= 0; i--) {
         char c = inputField.text[i];

         if (!chatBubbleFont.HasCharacter(c)) {
            inputField.text = inputField.text.Remove(i, 1);
         }
      }

      // Any time the mouse button is released, reset the scroll click boolean
      if (KeyUtils.GetButtonUp(MouseButton.Left)) {
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

      Vector2 mousePosition = MouseUtils.mousePosition;
      bool isMouseOverResizeHandle = RectTransformUtility.RectangleContainsScreenPoint(resizeHandleZone, mousePosition);

      // Update cursor
      if (MouseManager.self != null && isMouseOverResizeHandle) {
         MouseManager.self.setHandCursor();
      }

      // Enable resizing mode when clicking the resize handle
      if (KeyUtils.GetButtonDown(MouseButton.Left) && _mode != Mode.Minimized && isMouseOverResizeHandle) {
         _isResizing = true;
         _startMessageBackgroundRectSizeDelta = messageBackgroundRect.sizeDelta;
         _startMousePosition = mousePosition;
      }

      // Maintain the resize mode while the mouse button is held
      if (KeyUtils.GetButton(MouseButton.Left)) {
         if (_isResizing) {
            setMode(Mode.Freeform);
            Vector2 mouseDelta = mousePosition - _startMousePosition;
            Vector2 scaledSizeDelta = mouseDelta / canvas.scaleFactor;
            Vector2 targetRectSizeDelta = _startMessageBackgroundRectSizeDelta + scaledSizeDelta;
            Vector2 clampedRectSizeDelta = new Vector2(Mathf.Clamp(targetRectSizeDelta.x, MIN_WIDTH, MAX_WIDTH), Mathf.Clamp(targetRectSizeDelta.y, MIN_HEIGHT, MAX_HEIGHT));
            messageBackgroundRect.sizeDelta = clampedRectSizeDelta;
         }
      } else {
         _isResizing = false;
      }

      toggleResizeHandle(true);

      // Handle panel animations depending on the mode
      switch (_mode) {
         case Mode.Freeform:
            animatePanelBackgroundAlpha(1f);
            animateToolbarAlpha(1f);
            break;
         case Mode.Minimized:
            if (choosingChatType.activeSelf) {
               choosingChatType.SetActive(false);
            }

            if (_isMouseOverInputField) {
               // While the mouse is over the input box, switch to normal mode without toolbar              
               animatePanelBackgroundAlpha(1f);
               animatePanelHeight(CHAT_LINES_NORMAL);
            } else {
               animatePanelBackgroundAlpha(0f);
               animatePanelHeight(CHAT_LINES_MINIMIZED);
            }

            animateToolbarAlpha(0f);
            toggleResizeHandle(false);
            break;
         case Mode.Normal:
            animateToolbarAlpha(1f);
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

   private void updateBottomBarHeight () {
      Camera cam = CameraManager.defaultCamera.getCamera();
      if (cam != null) {
         float top = cam.ScreenToWorldPoint(topPositionMarker.transform.position).y;
         float bot = cam.ScreenToWorldPoint(botPositionMarker.transform.position).y;
         bottomBarWorldSpaceHeight = top - bot;
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
      float targetHeight = computeTargetHeight(visibleLinesCount);

      if (Mathf.Approximately(messageBackgroundRect.sizeDelta.y, targetHeight)) {
         return;
      }

      // Disable the elastic scroll movement while animating the height
      if (Mathf.Abs(targetHeight - messageBackgroundRect.sizeDelta.y) > 0.1f) {
         scrollRect.movementType = ScrollRect.MovementType.Clamped;

         // In normal and minimized mode, move the scrollbar to the bottom
         if (_mode == Mode.Minimized || _mode == Mode.Normal) {
            scrollRect.verticalNormalizedPosition = 0f;
         }
      } else {
         scrollRect.movementType = ScrollRect.MovementType.Elastic;
      }

      messageBackgroundRect.sizeDelta = new Vector2(
         messageBackgroundRect.sizeDelta.x,
         Mathf.SmoothDamp(messageBackgroundRect.sizeDelta.y, targetHeight, ref _messagePanelVelocity,
            SMOOTH_TIME, float.MaxValue, Time.deltaTime));
   }

   private void toggleResizeHandle (bool show) {
      resizeHandleZone.gameObject.SetActive(show);
   }

   void processGuiInputfield () {
      bool wasInputFocused = _isInputFocused;

      // If the input field has just gained / lost focus, call appropriate events
      if (inputField.isFocused && !_isInputFocused) {
         ChatManager.self.onChatGainedFocus();
      } else if (!inputField.isFocused && _isInputFocused) {
         ChatManager.self.onChatLostFocus();
      }

      _isInputFocused = inputField.isFocused;

      // If the name input field has just gained / lost focus, call appropriate events
      if (nameInputField.isFocused && !_isNameInputFocused) {
         ChatManager.self.onWhisperInputGainedFocus();
      } else if (!nameInputField.isFocused && _isNameInputFocused) {
         ChatManager.self.onWhisperInputLostFocus();
      }

      _isNameInputFocused = nameInputField.isFocused;

      // Submit the field when enter is pressed and the field was already focused
      if (wasInputFocused && KeyUtils.GetEnterKeyDown()) {
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

      if (nameInputField.isFocused && InputManager.self.inputMaster.Chat.SelectChat.WasPerformedThisFrame()) {
         inputField.Select();
      }

      // If we press TAB while the autocomplete panel displays a single value, apply it
      if (
         inputField.isFocused && 
         InputManager.self.inputMaster.Chat.Autocomplete.WasPressedThisFrame() &&
         ChatManager.self.autoCompletePanel.isActive() && 
         ChatManager.self.autoCompletePanel.getNumAutoCompletes() == 1
      ) {
         ChatManager.self.autoCompletePanel.performOptionClicked(0);
      }

      // Activate the input field when enter is pressed and the field is unfocused, except if the
      // player is writing a mail      
      if (KeyUtils.GetEnterKeyDown() && !((MailPanel) PanelManager.self.get(Panel.Type.Mail)).isWritingMail()) {
         if (!wasJustFocused()) {

            if (Global.player != null) {
               PlayerShipEntity playerShip = Global.player.GetComponent<PlayerShipEntity>();
               if (playerShip) {
                  if (!NetworkServer.active) {
                     playerShip.Cmd_ClearMovementInput();
                  } else {
                     playerShip.clearMovementInput();
                  }
               }
            }

            // Activate the input field in the next frame to avoid weird interactions
            StartCoroutine(CO_FocusAfterDelay(inputField));
         }
      }
   }

   public void addItemInsertToInput (Item item) {
      if (item == null) {
         return;
      }

      string toAdd = "[itemid=" + item.id + "]";

      if (inputField.text.Length + toAdd.Length > inputField.characterLimit) {
         // Too long
         return;
      }

      inputField.text += toAdd;
   }

   private float computeTargetHeight (int visibleLinesCount) {
      return toolbarRect.sizeDelta.y + 12 + visibleLinesCount * chatLineHeight;
   }

   public void onExpandButtonPressed () {
      setMode(Mode.Expanded);
   }

   public void onCollapseButtonPressed () {
      switch (_mode) {
         case Mode.Normal:
            setMode(Mode.Minimized);
            break;
         case Mode.Expanded:
            setMode(Mode.Normal);
            break;
         case Mode.Freeform:
            if (messageBackgroundRect.sizeDelta.y <= computeTargetHeight(CHAT_LINES_NORMAL)) {
               setMode(Mode.Minimized);
            } else {
               setMode(Mode.Normal);
            }
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
      SpeakChatRow chatRowComponent = chatRow.GetComponentInChildren<SpeakChatRow>();
      SpeakChatLine chatLine = chatRow.GetComponentInChildren<SpeakChatLine>();
      GuildIcon rowGuildIcon = chatRow.GetComponentInChildren<GuildIcon>();
      chatLine.name = "Chat Message";
      chatLine.chatInfo = chatInfo;
      setChatLineText(chatLine);

      if (chatInfo.messageType == ChatInfo.Type.PvpAnnouncement) {
         return;
      }

      // Assign guild icon parts if the guild info of the sender is available
      if (chatInfo.guildIconData != null) {
         rowGuildIcon.gameObject.SetActive(true);
         rowGuildIcon.setBorder(chatInfo.guildIconData.iconBorder);
         rowGuildIcon.setBackground(chatInfo.guildIconData.iconBackground, chatInfo.guildIconData.iconBackPalettes);
         rowGuildIcon.setSigil(chatInfo.guildIconData.iconSigil, chatInfo.guildIconData.iconSigilPalettes);
         rowGuildIcon.setGuildName(chatInfo.guildName);
      } else {
         rowGuildIcon.gameObject.SetActive(false);
      }

      bool isLocalPlayer = true;
      if (Global.player != null) {
         isLocalPlayer = chatInfo.senderId == Global.player.userId ? true : false;
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

      if (chatInfo.messageType != ChatInfo.Type.Emote && chatInfo.messageType != ChatInfo.Type.Whisper && chatInfo.messageType != ChatInfo.Type.UserOnline && chatInfo.messageType != ChatInfo.Type.UserOffline) {
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
	  
	  // Highlight the message if directed at the local player
      if (Global.player != null) {
         bool shouldHighlight = chatLine.getFormattedText().ToLower().Contains("@" + Global.player.entityName.ToLower());
         chatRowComponent.toggleHighlight(shouldHighlight);
      }
   }

   private void setChatLineText(SpeakChatLine chatLine) {
      ChatInfo chatInfo = chatLine.chatInfo;

      if (chatInfo == null) {
         return;
      }

      if (chatInfo.messageType == ChatInfo.Type.PvpAnnouncement) {
         chatLine.setFormattedText(string.Format("<color={0}>[PVP]:</color> <color={1}>{2}</color>", getSenderNameColor(chatInfo.messageType, false), getColorString(chatInfo.messageType, false), chatInfo.text));
         return;
      }

      if (chatInfo.senderId > 0) {
         // Filter out any bad words
         bool containsBadWord = BadWordManager.Contains(chatInfo.text);
         if (containsBadWord) {
            string filteredMessage = BadWordManager.ReplaceAll(chatInfo.text);
            chatInfo.text = filteredMessage;
         }
      }

      chatLine.setFormattedText(getFormattedChatLine(chatInfo, chatInfo.text));
   }

   public void refreshChatLines() {
      SpeakChatLine[] chatLines = messagesContainer.GetComponentsInChildren<SpeakChatLine>();

      if (chatLines == null || chatLines.Length == 0) {
         return;
      }

      foreach (SpeakChatLine chatLine in chatLines) {
         setChatLineText(chatLine);
      }
   }

   public string getFormattedChatLine (ChatInfo chatInfo, string message) {
      bool isLocalPlayer = true;
      if (Global.player != null) {
         isLocalPlayer = chatInfo.senderId == Global.player.userId ? true : false;
      }

      // We'll set the message up differently based on whether a sender was defined
      if (Util.isEmpty(chatInfo.sender)) {
         return string.Format("<color={0}>{1}</color>", getSenderNameColor(chatInfo.messageType), message);
      } else if (chatInfo.messageType == ChatInfo.Type.Emote) {
         return string.Format("<color={0}>{1} {2}</color>", getColorString(chatInfo.messageType), chatInfo.sender, message);
      } else if (chatInfo.messageType == ChatInfo.Type.Group) {
         return string.Format("<color={0}>[GROUP] {1}:</color> <color={2}>{3}</color>", getSenderNameColor(chatInfo.messageType), chatInfo.sender, getColorString(chatInfo.messageType), message);
      } else if (chatInfo.messageType == ChatInfo.Type.Guild) {
         return string.Format("<color={0}>[GUILD] {1}:</color> <color={2}>{3}</color>", getSenderNameColor(chatInfo.messageType, false), chatInfo.sender, getColorString(chatInfo.messageType, isLocalPlayer), message);
      } else if (chatInfo.messageType == ChatInfo.Type.Officer) {
         return string.Format("<color={0}>[OFFICER] {1}:</color> <color={2}>{3}</color>", getSenderNameColor(chatInfo.messageType, false), chatInfo.sender, getColorString(chatInfo.messageType, isLocalPlayer), message);
      } else if (chatInfo.messageType == ChatInfo.Type.UserOnline || chatInfo.messageType == ChatInfo.Type.UserOffline) {
         return string.Format("<color={0}>{1}</color>", getColorString(chatInfo.messageType, isLocalPlayer), message);
      } else {
         string messageSource = chatInfo.sender;
         if (chatInfo.messageType == ChatInfo.Type.Whisper) {
            messageSource = isLocalPlayer ? ("To " + chatInfo.recipient) : (chatInfo.sender + " whispers");
         }

         // If the message is from an Admin, set color of message to Admin color
         if (chatInfo.isSenderAdmin) {
            string senderAdminColor = "#" + ColorUtility.ToHtmlStringRGBA(adminNameColor);
            return string.Format("<color={0}>[ADMIN] {1}:</color> <color={2}>{3}</color>", senderAdminColor, messageSource, getColorString(chatInfo.messageType, isLocalPlayer), message);
         } else {
            string stringFormat = chatInfo.messageType == ChatInfo.Type.Global ? "<color={0}>[GLOBAL] {1}:</color> <color={2}>{3}</color>" : "<color={0}>{1}:</color> <color={2}>{3}</color>";
            return string.Format(stringFormat, getSenderNameColor(chatInfo.messageType, isLocalPlayer), messageSource, getColorString(chatInfo.messageType, isLocalPlayer), message);
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
         case ChatInfo.Type.PvpAnnouncement:
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
         case ChatInfo.Type.Guild:
            if (isLocalPlayer) {
               newColor = guildChatLocalColor;
            } else {
               newColor = guildChatOtherColor;
            }
            break;
         case ChatInfo.Type.Officer:
            if (isLocalPlayer) {
               newColor = officerChatLocalColor;
            } else {
               newColor = officerChatOtherColor;
            }
            break;
         case ChatInfo.Type.UserOnline:
            newColor = systemNameColor;
            break;
         case ChatInfo.Type.UserOffline:
            newColor = systemNameColor;
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

   public void toggleChoosingChatTypes () {
      choosingChatType.SetActive(!choosingChatType.activeSelf);
   }

   public void onLocalChatPressed () {
      toggleChatType(ChatInfo.Type.Local);
   }

   public void onWhisperChatPressed () {
      toggleChatType(ChatInfo.Type.Whisper);
   }

   public void onLogChatPressed () {
      toggleChatType(ChatInfo.Type.Log);
   }

   public void onWarningChatPressed () {
      toggleChatType(ChatInfo.Type.Warning);
   }

   public void onSystemChatPressed () {
      toggleChatType(ChatInfo.Type.System);
   }

   public void onDebugChatPressed () {
      toggleChatType(ChatInfo.Type.Debug);
   }

   public void onErrorChatPressed () {
      toggleChatType(ChatInfo.Type.Error);
   }

   public void onTradeChatPressed () {
      toggleChatType(ChatInfo.Type.Trade);
   }

   public void onPermitChatPressed () {
      toggleChatType(ChatInfo.Type.Permit);
   }

   public void onGuildChatPressed () {
      toggleChatType(ChatInfo.Type.Guild);
   }

   public void onEmoteChatPressed () {
      toggleChatType(ChatInfo.Type.Emote);
   }

   public void onGlobalChatPressed () {
      toggleChatType(ChatInfo.Type.Global);
   }

   public void onGroupChatPressed () {
      toggleChatType(ChatInfo.Type.Group);
   }

   public void onOfficerChatPressed () {
      toggleChatType(ChatInfo.Type.Officer);
   }

   public void onAllChatPressed () {
      if (!_tabPressed) {
         choosingChatType.SetActive(false);
         expandChatTypesButtons.SetActive(false);

         _tabPressed = true;
         _tab = Tab.All;
         allTabToggle.isOn = true;
         customTabToggle.isOn = false;
         _tabPressed = false;

         _modifiedByCode = true;
         choosingChatType.GetComponentsInChildren<Toggle>().ToList().ForEach(x => x.isOn = true);
         _visibleChatTypes = new HashSet<ChatInfo.Type>(((ChatInfo.Type[]) Enum.GetValues(typeof(ChatInfo.Type))).ToList());
         onChatTabPressed();
         _modifiedByCode = false;
      }
   }

   public void onCustomChatPressed () {
      if (!_tabPressed) {
         choosingChatType.SetActive(true);
         expandChatTypesButtons.SetActive(true);

         _tabPressed = true;
         _tab = Tab.Custom;
         allTabToggle.isOn = false;
         customTabToggle.isOn = true;
         _tabPressed = false;

         if (PlayerPrefs.HasKey("ChatPrefs")) {
            _modifiedByCode = true;
            int chatPrefs = PlayerPrefs.GetInt("ChatPrefs");
            choosingChatType.GetComponentsInChildren<Toggle>().ToList().ForEach(x => x.isOn = false);
            _visibleChatTypes.Clear();

            List<ChatTypeToggle> toggles = choosingChatType.GetComponentsInChildren<ChatTypeToggle>().ToList();
            List<ChatInfo.Type> types = ((ChatInfo.Type[]) Enum.GetValues(typeof(ChatInfo.Type))).ToList();
            foreach (ChatInfo.Type type in types) {
               if ((chatPrefs & (1 << ((int) type - 1))) != 0) {
                  toggles.Find(x => x.type == type).GetComponentInChildren<Toggle>().isOn = true;
               }
            }

            onChatTabPressed();
            _modifiedByCode = false;
         } else {
            choosingChatType.GetComponentsInChildren<Toggle>().ToList().ForEach(x => x.isOn = true);
            _visibleChatTypes = new HashSet<ChatInfo.Type>(((ChatInfo.Type[]) Enum.GetValues(typeof(ChatInfo.Type))).ToList());
            onChatTabPressed();
         }
      }
   }

   private void savePrefs () {
      int chatPrefs = 0;
      foreach (ChatInfo.Type type in _visibleChatTypes) {
         chatPrefs += 1 << ((int) type - 1);
      }
      PlayerPrefs.SetInt("ChatPrefs", chatPrefs);
   }

   private void toggleChatType (ChatInfo.Type type) {
      if (_visibleChatTypes.Contains(type)) {
         _visibleChatTypes.Remove(type);
      } else {
         _visibleChatTypes.Add(type);
      }

      if (!_modifiedByCode) {
         savePrefs();
      }

      onChatTabPressed();
   }

   private void onChatTabPressed () {
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

   public void setCurrentChatType (ChatInfo.Type chatType) {
      currentChatType = chatType;
      whisperNameInput.gameObject.SetActive(currentChatType == ChatInfo.Type.Whisper);
   }

   public void sendWhisperTo (string userName) {
      // Input the user name in the whisper name input field
      nameInputField.text = userName;

      setCurrentChatType(ChatInfo.Type.Whisper);

      if (!wasJustFocused()) {
         // Activate the input field in the next frame to avoid weird interactions
         StartCoroutine(CO_FocusAfterDelay(inputField));
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

   public void censorGlobalMessagesFromUser (int userId) {
      // Cycle over all of the chat lines in our container
      foreach (SpeakChatLine chatLine in messagesContainer.GetComponentsInChildren<SpeakChatLine>(true)) {
         if (chatLine.chatInfo.senderId == userId) {
            chatLine.setFormattedText(getFormattedChatLine(chatLine.chatInfo, "<Message deleted>"));
         }
      }
   }

   public void clearChat () {
      messagesContainer.DestroyChildren();
      nameInputField.text = "";
      inputField.SetTextWithoutNotify("");
   }

   protected bool isChatLineVisibleInTab (ChatInfo chatInfo) {
      if (_visibleChatTypes.Contains(chatInfo.messageType)) {
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
            return isLocalPlayer ? guildChatLocalColor : guildChatOtherColor;
         case ChatInfo.Type.Officer:
            return isLocalPlayer ? officerChatLocalColor : officerChatOtherColor;
         case ChatInfo.Type.Group:
            return isLocalPlayer ? groupMessageLocalColor : groupMessageOtherColor;
         case ChatInfo.Type.PvpAnnouncement:
            return Color.magenta;
         case ChatInfo.Type.Emote:
            return Color.magenta;
         case ChatInfo.Type.UserOnline:
         case ChatInfo.Type.UserOffline:
            return globalChatLocalColor;
         default:
            return Color.white;
      }
   }

   public void focusInputField () {
      StartCoroutine(CO_FocusAfterDelay(inputField));
   }

   public void focusWhisperInputField () {
      StartCoroutine(CO_FocusAfterDelay(nameInputField));
   }

   protected IEnumerator CO_FocusAfterDelay (TMP_InputField field) {
      // Wait a frame
      yield return null;

      // Now we can activate
      field.ActivateInputField();

      // Have to do this in a separate Coroutine, it's ridiculous
      StartCoroutine(CO_MoveCaretToEnd(field));
   }

   public IEnumerator CO_MoveCaretToEnd (TMP_InputField field) {
      // Hide the text selection
      Color selectionColor = inputField.selectionColor;
      inputField.selectionColor = new Color(selectionColor.r, selectionColor.g, selectionColor.b, 0f);

      // Wait a frame
      yield return null;

      // Don't select the text, that's annoying
      field.MoveTextEnd(false);

      // Restore the text selection color
      inputField.selectionColor = selectionColor;
   }

   protected IEnumerator CO_FocusAfterDelay (InputField field) {
      // Wait a frame
      yield return null;

      // Now we can activate
      field.ActivateInputField();

      // Have to do this in a separate Coroutine, it's ridiculous
      StartCoroutine(CO_MoveCaretToEnd(field));
   }

   public IEnumerator CO_MoveCaretToEnd (InputField field) {
      // Hide the text selection
      Color selectionColor = inputField.selectionColor;
      inputField.selectionColor = new Color(selectionColor.r, selectionColor.g, selectionColor.b, 0f);

      // Wait a frame
      yield return null;

      // Don't select the text, that's annoying
      field.MoveTextEnd(false);

      // Restore the text selection color
      inputField.selectionColor = selectionColor;
   }

   private void setMode (Mode mode) {
      _mode = mode;

      // Panel show, hide and resize is handled in the update
      switch (_mode) {
         case Mode.Freeform:
            expandButton.SetActive(messageBackgroundRect.sizeDelta.y < computeTargetHeight(CHAT_LINES_EXPANDED));
            break;
         case Mode.Minimized:
            // Allow minimizing even if the input field is focused
            _lastFocusTime = 0;
            toolbarCanvas.gameObject.SetActive(false);
            scrollBarContainer.SetActive(false);
            messageBackgroundImage.raycastTarget = false;

            if (_mode != mode) {
               // Need to rebuild the message list since some messages may toggle their visibility
               rebuildMessageList();
            }

            break;
         case Mode.Normal:
            toolbarCanvas.gameObject.SetActive(true);
            scrollBarContainer.SetActive(true);
            expandButton.SetActive(true);
            collapseButton.SetActive(true);
            messageBackgroundImage.raycastTarget = true;
            scrollRect.vertical = true;

            if (_mode != mode) {
               // Need to rebuild the message list since some messages may toggle their visibility
               rebuildMessageList();
            }

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

   public bool isPointerOverInputFieldZone () {
      return mainContainer.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(self.inputFieldZoneRect, MouseUtils.mousePosition);
   }

   #region Private Variables

   // How long we show messages for before they disappear
   protected static float CHAT_MESSAGE_DISPLAY_DURATION = 20f;

   // The maximum number of messages that we'll keep in our chat log
   protected static int MAX_MESSAGE_COUNT = 50;

   // The time at which the chat input was last focused
   protected float _lastFocusTime;

   // Gets set to true when the input field is focused
   protected bool _isInputFocused = false;

   // Gets set to true when the name input field is focused
   protected bool _isNameInputFocused = false;

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

   // Currently chosen chat types that should be shown on the screen
   protected HashSet<ChatInfo.Type> _visibleChatTypes = new HashSet<ChatInfo.Type>(((ChatInfo.Type[]) Enum.GetValues(typeof(ChatInfo.Type))).ToList());

   // Check whether "all" tab was pressed by player or if attached function was called by code
   protected bool _tabPressed = false;

   // Check whether toggle values were modified by player interaction or in code
   protected bool _modifiedByCode = false;

   // Gets set to true when the user is resizing the panel
   protected bool _isResizing = false;

   // Position of the mouse cursor at the start of the resizing process
   private Vector2 _startMousePosition = Vector2.zero;

   // Size of the chat messages panel at the beginning of the resizing process
   private Vector2 _startMessageBackgroundRectSizeDelta = Vector2.zero;

   #endregion
}
