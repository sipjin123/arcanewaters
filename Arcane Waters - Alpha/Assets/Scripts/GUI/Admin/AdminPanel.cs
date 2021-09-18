﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System;
using static UnityEngine.UI.Dropdown;
using System.Text;
using System.Linq;
using TMPro;
using UnityEngine.Events;

public class AdminPanel : Panel
{
   #region Public Variables

   // The slider that allows to set the Players Count Limit on a server
   public Slider sliderPlayersCountMax;

   // The input field that allows to set the Players Count Limit on a server
   public TMP_InputField txtPlayersCountMax;

   // The toggle that sets the Admin Only flag
   public Toggle toggleAdminOnlyMode;

   // The reference to the input field that displays the message shown to users when the server is in Admin Only mode
   public TMP_InputField txtAdminOnlyModeMessage;

   // The reference to the Apply button
   public Button btnApply;

   // The reference to the Refresh button
   public Button btnRefresh;

   // The message to display when kicking all users from the servers
   public TMP_InputField txtKickAllUsersMessage;
	
   // The reference to the load blocker
   public GameObject loadBlocker;

   // Reference to the Tabs
   public AdminPanelTabs tabs;

   // Reference to the Tabs Switcher
   public AdminPanelTabSwitcher tabSwitcher;

   // Reference to the Player Count Metric Holder
   public AdminPanelMetricsHolder playersCountMetricsHolder;

   // Self
   public static AdminPanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;

      initializeTabs();
   }

   public override void Start () {
      sliderPlayersCountMax.onValueChanged.RemoveAllListeners();
      sliderPlayersCountMax.onValueChanged.AddListener(onSliderPlayersCountMaxValueChanged);

      txtPlayersCountMax.onValueChanged.RemoveAllListeners();
      txtPlayersCountMax.onValueChanged.AddListener(onTxtPlayersCountMaxValueChanged);
   }

   public new void show () {
      if (!Global.isLoggedInAsAdmin()) {
         return;
      }

      if (!isShowing()) {
         btnApply.interactable = true;
         btnRefresh.interactable = true;
         toggleBlocker(true);
         requestRemoteSettings();
         requestPlayersCountMetrics();
      }

      PanelManager.self.linkIfNotShowing(Panel.Type.Admin);
   }

   private void requestRemoteSettings () {
      Global.player.rpc.Cmd_RequestRemoteSettings(
         new string[] {
                  RemoteSettingsManager.SettingNames.PLAYERS_COUNT_MAX,
                  RemoteSettingsManager.SettingNames.ADMIN_ONLY_MODE,
                  RemoteSettingsManager.SettingNames.ADMIN_ONLY_MODE_MESSAGE
         });
   }

   private void requestPlayersCountMetrics () {
     Global.player.rpc.Cmd_RequestPlayersCountMetrics();
   }

   private void toggleBlocker (bool show) {
      if (loadBlocker != null) {
         loadBlocker.gameObject.SetActive(show);
      }
   }

   public void onRemoteSettingsReceived (RemoteSettingCollection collection) {
      if (!Global.isLoggedInAsAdmin()) {
         return;
      }

      sliderPlayersCountMax.value = collection.getSetting(RemoteSettingsManager.SettingNames.PLAYERS_COUNT_MAX).toInt();
      toggleAdminOnlyMode.SetIsOnWithoutNotify(collection.getSetting(RemoteSettingsManager.SettingNames.ADMIN_ONLY_MODE).toBool());
      txtAdminOnlyModeMessage.text = collection.getSetting(RemoteSettingsManager.SettingNames.ADMIN_ONLY_MODE_MESSAGE).value;

      toggleBlocker(false);
   }

   public void onPlayersCountMetricsReceived (MetricCollection collection) {
      if (!Global.isLoggedInAsAdmin()) {
         return;
      }

      toggleBlocker(false);
      btnRefresh.interactable = true;
      playersCountMetricsHolder.clearMetrics();

      if (collection == null || collection.metrics.Count() == 0) {
         playersCountMetricsHolder.addMetric("No", "Metrics");
         return;
      }

      // Compute total
      int total = collection.metrics.Sum(_ => int.Parse(_.value));
      playersCountMetricsHolder.addMetric("TOTAL", total.ToString());

      // Show details
      foreach (Metric metric in collection.metrics) {
         string displayName = $"{metric.machineId} {metric.processName} ({metric.processId})";
         playersCountMetricsHolder.addMetric(displayName, metric.value);
      }
   }

   public void onApplyButtonPressed () {
      // Disable Apply Button
      btnApply.interactable = false;

      // Show confirmation screen
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(onApplyConfirmPressed);
      PanelManager.self.confirmScreen.cancelButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.cancelButton.onClick.AddListener(onApplyCancelPressed);
      PanelManager.self.confirmScreen.show("Apply Changes", newDescription: "Do you really want to apply your changes?");
   }

   private void onApplyConfirmPressed () {
      if (!Global.isLoggedInAsAdmin()) {
         close();
      }

      // Disable confirm and cancel button
      PanelManager.self.confirmScreen.confirmButton.interactable = false;
      PanelManager.self.confirmScreen.cancelButton.interactable = false;

      // Update the remote settings
      int newPlayersCountMax = Mathf.FloorToInt(sliderPlayersCountMax.value);
      bool newIsAdminOnlyModeEnabled = toggleAdminOnlyMode.isOn;
      string newAdminOnlyModeMessage = txtAdminOnlyModeMessage.text;

      RemoteSettingCollection collection = new RemoteSettingCollection();
      collection.addSetting(RemoteSetting.create(RemoteSettingsManager.SettingNames.PLAYERS_COUNT_MAX, newPlayersCountMax.ToString(), settingValueType: RemoteSetting.RemoteSettingValueType.INT));
      collection.addSetting(RemoteSetting.create(RemoteSettingsManager.SettingNames.ADMIN_ONLY_MODE, newIsAdminOnlyModeEnabled.ToString(), settingValueType: RemoteSetting.RemoteSettingValueType.STRING));
      collection.addSetting(RemoteSetting.create(RemoteSettingsManager.SettingNames.ADMIN_ONLY_MODE_MESSAGE, newAdminOnlyModeMessage.ToString(), settingValueType: RemoteSetting.RemoteSettingValueType.STRING));

      Global.player.rpc.Cmd_SetRemoteSettings(collection);
   }

   private void onApplyCancelPressed () {
      btnApply.interactable = true;
   }

   public void onSetRemoteSettings (bool success) {
      // Re-enable the confirm button before hiding the confirmation screen
      PanelManager.self.confirmScreen.confirmButton.interactable = true;
      PanelManager.self.confirmScreen.cancelButton.interactable = true;
      PanelManager.self.confirmScreen.hide();

      // Notify the user
      if (success) {
         PanelManager.self.noticeScreen.show("Changes Applied Successfully!");
      } else {
         PanelManager.self.noticeScreen.show("Error");
      }

      btnApply.interactable = true;
   }

   public void refreshMetrics () {
      // Disable Refresh button
      btnRefresh.interactable = false;

      // Show loading blocker
      toggleBlocker(true);

      // Request the metrics again
      requestPlayersCountMetrics();
   }

   private void onTxtPlayersCountMaxValueChanged (string newText) {
      // Validate the new value
      if (int.TryParse(newText, out int newValue)) {
         newText = Mathf.Max(newValue, 0).ToString();
      } else {
         newText = "0";
      }

      sliderPlayersCountMax.value = int.Parse(newText);
   }

   private void onSliderPlayersCountMaxValueChanged (float newValue) {
      txtPlayersCountMax.SetTextWithoutNotify(Mathf.FloorToInt(newValue).ToString());
   }

   private void initializeTabs () {
      if (tabs == null) {
         return;
      }

      tabs.onTabPressed.RemoveAllListeners();
      tabs.onTabPressed.AddListener(onTabPressed);

      // Switch to the first tab
      tabs.performTabPressed(0);
   }

   private void onTabPressed (int tabIndex) {
      if (tabSwitcher == null) {
         return;
      }

      tabSwitcher.performSwitch(tabIndex);
   }

   public void onKickAllNonAdminUsersButtonPressed () {
      // Deactivate "Confirm" Button until admin types in the word "Confirm" into the inputfield
      PanelManager.self.confirmScreen.enableConfirmInputField("CONFIRM");

      // Ask the admin for confirmation.
      PanelManager.self.showConfirmationPanel("Are you sure you want to kick all non-admins from the server?", () => onConfirmKickAllNonAdminUsers()) ;
   }

   private void onConfirmKickAllNonAdminUsers () {
      Global.player.rpc.Cmd_RequestKickAllNonAdminUsers(txtKickAllUsersMessage.text);
   }

   #region Private Variables

   #endregion
}
