using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using static UnityEngine.UI.Dropdown;
using System.Linq;
using System;

public class HelpPanel : Panel
{
   #region Public Variables

   // The Bugs Panel Button
   public Button bugsButton;

   // The Commands Panel Button
   public Button commandsButton;

   // The Suppot Panel Button
   public Button supportButton;

   // The Bugs Panel
   public RectTransform bugsPanel;

   // The Commands Panel
   public RectTransform commandsPanel;

   // The Support Panel
   public RectTransform supportPanel;

   // The Support Message Send Button
   public Button supportSendButton;

   // The Discord button
   public Button discordButton;

   // The Forums button
   public Button forumsButton;

   // The Go Home Button
   public Button goHomeButton;

   // The Stuck Button
   public Button stuckButton;

   // The Text Field for the Support message
   public TMPro.TMP_InputField supportInputField;

   // The word count of the message
   public Text messageWordCountText;

   // Minimum Internval between support message sending operations
   public int minSendIntervalMinutes = 5;

   // Panel Types
   public enum HelpPanelTabs
   {
      // None
      None = 0,

      // Bugs
      Bugs = 1,

      // Commands
      Commands = 2,

      // Support
      Support = 3
   }

   // Self
   public static HelpPanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      supportInputField.characterLimit = MailManager.MAX_MESSAGE_LENGTH;
      self = this;
   }

   public override void Start () {
      if (_tabsRegistry == null) {
         _tabsRegistry = new Dictionary<HelpPanelTabs, RectTransform>();
      }

      _tabsRegistry.Clear();
      _tabsRegistry.Add(HelpPanelTabs.Bugs, bugsPanel);
      _tabsRegistry.Add(HelpPanelTabs.Commands, commandsPanel);
      _tabsRegistry.Add(HelpPanelTabs.Support, supportPanel);

      if (supportButton) {
         supportButton.onClick.RemoveAllListeners();
         supportButton.onClick.AddListener(() => toggleTab(HelpPanelTabs.Support, true));
      }

      if (bugsButton) {
         bugsButton.onClick.RemoveAllListeners();
         bugsButton.onClick.AddListener(() => toggleTab(HelpPanelTabs.Bugs, true));
      }

      if (commandsButton) {
         commandsButton.onClick.RemoveAllListeners();
         commandsButton.onClick.AddListener(() => toggleTab(HelpPanelTabs.Commands, true));
      }

      if (supportSendButton) {
         supportSendButton.onClick.RemoveAllListeners();
         supportSendButton.onClick.AddListener(onSupportSendButtonPressed);
      }

      if (goHomeButton) {
         goHomeButton.onClick.RemoveAllListeners();
         goHomeButton.onClick.AddListener(onGoHomeButtonPressed);
      }
      
      if (stuckButton) {
         stuckButton.onClick.RemoveAllListeners();
         stuckButton.onClick.AddListener(onStuckButtonPressed);
      }

      if (forumsButton) {
         forumsButton.onClick.RemoveAllListeners();
         forumsButton.onClick.AddListener(onForumsButtonPressed);
      }

      if (discordButton) {
         discordButton.onClick.RemoveAllListeners();
         discordButton.onClick.AddListener(onDiscordButtonPressed);
      }

      if (forumsButton) {
         forumsButton.onClick.RemoveAllListeners();
         forumsButton.onClick.AddListener(onForumsButtonPressed);
      }

      supportInputField.SetTextWithoutNotify("");
      supportInputField.onValueChanged.RemoveAllListeners();
      supportInputField.onValueChanged.AddListener(onSupportInputFieldValueChanged);

      supportSendButton.interactable = false;

      toggleTab(HelpPanelTabs.Bugs, true);

      updateMessageWordCount();
      supportInputField.characterLimit = MailManager.MAX_MESSAGE_LENGTH;
   }

   public void toggleTab (HelpPanelTabs tab, bool show) {
      hideAllPanels();

      if (_tabsRegistry == null) {
         return;
      }

      if (_tabsRegistry.TryGetValue(tab, out RectTransform tabPanel)) {
         tabPanel.gameObject.SetActive(show);
      }

      _currentTab = tab;
   }

   private void onSupportSendButtonPressed () {
      if (Global.player == null || PanelManager.self == null) {
         return;
      }

      if (supportInputField  == null || Util.isEmpty(supportInputField.text)) {
         PanelManager.self.noticeScreen.confirmButton.onClick.RemoveAllListeners();
         PanelManager.self.noticeScreen.show("The message is empty!");
         return;
      }

      var currTime = DateTime.UtcNow;
      if (currTime < _lastSupportMessageSendTimestamp + TimeSpan.FromMinutes(minSendIntervalMinutes)) {
         var timeLeft = (_lastSupportMessageSendTimestamp + TimeSpan.FromMinutes(minSendIntervalMinutes)) - _lastSupportMessageSendTimestamp;
         PanelManager.self.noticeScreen.confirmButton.onClick.RemoveAllListeners();
         PanelManager.self.noticeScreen.show($"To avoid overwhelming the servers, please, wait another {(int)timeLeft.TotalMinutes} minutes before sending your message.");
         return;
      }

      _lastSupportMessageSendTimestamp = currTime;

      // Send the support message
      SupportTicketManager.self.sendSupportTicket(Global.player.accountId, Global.player.userId, Global.player.entityName, supportInputField.text, SupportTicketManager.SupportTicketType.Feedback);

      PanelManager.self.noticeScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.noticeScreen.show("Message Sent!");
}

   private void onSupportInputFieldValueChanged (string newValue) {
      if (supportSendButton && Global.player != null) {
         supportSendButton.interactable = !Util.isEmpty(newValue);
      }
   }

   private void onForumsButtonPressed () {
      Application.OpenURL(FORUMS_URL);
   }

   private void onDiscordButtonPressed () {
      Application.OpenURL(DISCORD_URL);
   }

   private void onGoHomeButtonPressed () {
      if (Global.player == null) {
         return;
      }

      Global.player.Cmd_GoHome();

      // Close this panel
      if (isShowing()) {
         PanelManager.self.hideCurrentPanel();
      }
   }

   private void onStuckButtonPressed () {
      if (ChatManager.self == null) {
         return;
      }

      ChatManager.self.requestUnstuck();
   }

   private void hideAllPanels () {
      if (bugsPanel != null) {
         bugsPanel.gameObject.SetActive(false);
      }


      if (supportPanel != null) {
         supportPanel.gameObject.SetActive(false);
      }


      if (commandsPanel != null) {
         commandsPanel.gameObject.SetActive(false);
      }
   }

   public void updateMessageWordCount () {
      if (messageWordCountText && supportInputField) {
         int wordCount = supportInputField.text.Length;
         messageWordCountText.text = wordCount.ToString() + "/" + MailManager.MAX_MESSAGE_LENGTH.ToString();
      }
   }

   public bool isWritingMessage () {
      return isShowing() && supportInputField.isFocused;
   }

   #region Private Variables

   // The current panel tab
   private HelpPanelTabs _currentTab;

   // Tabs registry
   private Dictionary<HelpPanelTabs, RectTransform> _tabsRegistry;

   // URl to the forums
   private const string FORUMS_URL = "https://forums.arcanewaters.com";

   // URL to the Discord
   private const string DISCORD_URL = "https://discord.gg/Evj3a5PNYb";

   // The time at which the latest support message was sent
   private DateTime _lastSupportMessageSendTimestamp;

   #endregion
}
