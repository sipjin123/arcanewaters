using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class ChatPanel : MonoBehaviour {
   #region Public Variables

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
   public InputField inputField;
   public GameObject scrollBarContainer;
   public Image arrowImage;
   public Text chatModeText;

   // The placeholder message
   public GameObject placeHolder;

   // The prefabs we use for creating various chat lines
   public SpeakChatLine speakChatLinePrefab;
   public TradeChatLine tradeChatLinePrefab;
   public GuildChatLine guildChatLinePrefab;
   public GuildInviteChatLine guildInviteChatLinePrefab;

   // Our currently selected chatType
   public ChatInfo.Type currentChatType = ChatInfo.Type.Global;

   // The predefined max height of our chat background image
   public static int BACKGROUND_MAX_HEIGHT = 106;

   // Self
   public static ChatPanel self;

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
      InvokeRepeating("rebuildMessageList", 1f, 1f);

      // Call the autocomplete function when the user writes in chat
      inputField.onValueChanged.AddListener((string inputString) => ChatManager.self.onChatInputValuechanged(inputString));
   }

   void Update () {
      mainContainer.SetActive(shouldShowChat());

      // Only show the scroll bars if we're in that mode
      scrollBarContainer.SetActive(_showScrollBars);

      // Adjust the height of the background image based on the size of the chat message content area
      messageBackgroundRect.sizeDelta = new Vector2(messageBackgroundRect.sizeDelta.x, getNewBackgroundSize());

      // Modify the chat mode button based on our current selection
      chatModeText.text = getChatModeString();

      // Any time the mouse button is released, reset the scroll click boolean
      if (Input.GetMouseButtonUp(0)) {
         _isScrolling = false;
      }

      // Allow scrolling up and down with the scroll wheel
      float scrollWheel = Input.GetAxis("Mouse ScrollWheel");
      if (scrollWheel > 0f) {
         scrollRect.verticalScrollbar.value += .1f;
      } else if (scrollWheel < 0f) {
         scrollRect.verticalScrollbar.value -= .1f;
      }

      // Keep track of when the chat input is focused
      if (inputField.isFocused) {
         _lastFocusTime = Time.time;
      }
   }

   void OnGUI () {
      // Submit the field when enter is pressed and the field is focused
      if (inputField.isFocused && Input.GetKeyDown(KeyCode.Return)) {

         if (inputField.text != "") {
            // Send the message off to the server for processing
            ChatManager.self.processChatInput(inputField.text);

            // Clear out the text now that it's been used
            inputField.text = "";
         }

         // Deselect the input field
         inputField.DeactivateInputField();
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

   public void setScrollBarVisible (bool isVisible) {
      _showScrollBars = isVisible;
   }

   public void toggleScrollBarVisibility () {
      setScrollBarVisible(!_showScrollBars);

      // If we just turned off the scroll bars, make sure we're at the bottom
      if (!_showScrollBars) {
         scrollRect.verticalNormalizedPosition = 0f;
      }

      // Flip the arrow image on the button
      arrowImage.transform.localScale = _showScrollBars ? new Vector3(1, -1, 1) : Vector3.one;

      // Need to rebuild the message list since some messages may toggle their visibility
      rebuildMessageList();
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
      GuildInviteChatLine chatLine = Instantiate(guildInviteChatLinePrefab);
      chatLine.name = "Guild Invite";
      chatLine.transform.SetParent(messagesContainer.transform);
   }

   public void addChatInfo (ChatInfo chatInfo) {
      // Create a new Chat Line instance and assign the parent
      SpeakChatLine chatLine = Instantiate(speakChatLinePrefab);
      chatLine.name = "Chat Message";
      chatLine.chatInfo = chatInfo;
      chatLine.transform.SetParent(messagesContainer.transform);

      // We'll set the message up differently based on whether a sender was defined
      if (Util.isEmpty(chatInfo.sender)) {
         chatLine.text.text = string.Format("<color=yellow>{0}</color>", chatInfo.text);
      } else if (chatInfo.messageType == ChatInfo.Type.Emote) {
         chatLine.text.text = string.Format("<color={0}>{1} {2}</color>", getColorString(chatInfo.messageType), chatInfo.sender, chatInfo.text);
      } else {
         chatLine.text.text = string.Format("<color=yellow>{0}:</color> <color={1}>{2}</color>", chatInfo.sender, getColorString(chatInfo.messageType), chatInfo.text);
      }

      // If we're just starting up, some Managers may not exist yet
      if (BodyManager.self == null) {
         return;
      }

      // If we have a Body for the specified sender, create a speech bubble
      BodyEntity body = BodyManager.self.getBody(chatInfo.senderId);
      if (body != null && chatInfo.messageType != ChatInfo.Type.Emote) {
         SpeechManager.self.showSpeechBubble(body, chatInfo.text);
      }
   }

   public void addGuildChatInfo (ChatInfo chatInfo) {
      // Create a new Chat Line instance and assign the parent
      GuildChatLine chatLine = Instantiate(guildChatLinePrefab);
      chatLine.name = "Guild Chat Message";
      chatLine.chatInfo = chatInfo;
      chatLine.transform.SetParent(messagesContainer.transform);
      chatLine.text.text = string.Format("<color=yellow>[GUILD] {0}:</color> <color=white>{1}</color>", chatInfo.sender, chatInfo.text);
   }

   public void setIsScrolling () {
      _isScrolling = true;
   }

   public bool isScrolling () {
      return _isScrolling;
   }

   public void chatModeButtonPressed () {
      if (currentChatType == ChatInfo.Type.Global) {
         currentChatType = ChatInfo.Type.Local;
      } else if (currentChatType == ChatInfo.Type.Local) {
         currentChatType = ChatInfo.Type.Guild;
      } else if (currentChatType == ChatInfo.Type.Guild) {
         currentChatType = ChatInfo.Type.Global;
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

         // If the message was recently created, or we're in scroll bar mode, show the message
         if (Time.time - chatLine.creationTime < CHAT_MESSAGE_DISPLAY_DURATION || _showScrollBars) {
            chatLine.gameObject.SetActive(true);
         } else {
            chatLine.gameObject.SetActive(false);
         }

         num++;
      }

      // Delete any chat lines that we marked
      foreach (ChatLine chatLine in deleteList) {
         Destroy(chatLine.gameObject);
      }
   }

   protected int getNewBackgroundSize () {
      float contentSize = contentRect.sizeDelta.y;

      if (contentSize >= BACKGROUND_MAX_HEIGHT || _showScrollBars) {
         return BACKGROUND_MAX_HEIGHT;
      } else if (contentSize >= 70) {
         return 84;
      } else if (contentSize >= 50) {
         return 56;
      } else if (contentSize >= 30) {
         return 34;
      }

      return 0;
   }

   protected string getChatModeString () {
      string colorString = "#000000FF";

      switch (currentChatType) {
         case ChatInfo.Type.Local:
            return string.Format("<color={0}>Local</color>", colorString);
         case ChatInfo.Type.Global:
            return string.Format("<color={0}>Global</color>", colorString);
         case ChatInfo.Type.Guild:
            return string.Format("<color={0}>Guild</color>", colorString);
      }

      return "";
   }

   protected string getColorString (ChatInfo.Type chatType) {
      Color color = getChatColor(chatType);

      return "#" + ColorUtility.ToHtmlStringRGBA(color);
   }

   protected Color getChatColor (ChatInfo.Type chatType) {
      switch (chatType) {
         case ChatInfo.Type.Global:
            return Color.white;
         case ChatInfo.Type.Local:
            return Color.cyan;
         case ChatInfo.Type.Guild:
            return Color.white;
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

   #region Private Variables

   // How long we show messages for before they disappear
   protected static float CHAT_MESSAGE_DISPLAY_DURATION = 12f;

   // The maximum number of messages that we'll keep in our chat log
   protected static int MAX_MESSAGE_COUNT = 50;

   // The time at which the chat input was last focused
   protected float _lastFocusTime;

   // Whether the chat panel should show the scroll bars
   protected bool _showScrollBars = true;

   // Whether we're currently clicking on the scroll bar
   protected bool _isScrolling = false;

   #endregion
}
