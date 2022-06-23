using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System.IO;
using System;

public class AdminPanelServerOverview : MonoBehaviour
{
   #region Public Variables

   // Button used to expand additional info
   public Button expandButton = null;

   // Title of the server text
   public Text titleText = null;

   // Player count in server text
   public Text playerCountText = null;

   // FPS in server text
   public Text fpsText = null;

   // Text for displaying round trip time from server to master server
   public Text rttText = null;

   // Text for displaying server port
   public Text portText = null;

   // Text for displaying instance count
   public Text instanceCountText = null;

   // Gameobject holding additional info that can be expanded
   public GameObject descriptionRow = null;

   // Holder of the 'get log' button
   public GameObject getLogHolder = null;

   // Holder of log actions buttons
   public GameObject logActionsHolder = null;

   // Button that's used to download log
   public Button downloadLogButton = null;

   // Server overview we are responsible for
   public ServerOverview data = null;

   // Reference to the control that displays the server's uptime
   public Text uptimeText = null;

   #endregion

   private void Awake () {
      onExpandClick();

      getLogHolder.SetActive(true);
      logActionsHolder.SetActive(false);
   }

   public void setServerOverview (ServerOverview overview) {
      data = overview;

      portText.text = overview.port.ToString();
      titleText.text = "[" + overview.port + "] " + overview.machineName + " " + overview.processName;
      fpsText.text = "FPS: " + overview.fps;
      rttText.text = "RTT to Master: " + overview.toMasterRTT;
      playerCountText.text = "Players: " + overview.playerCount;
      uptimeText.text = "Uptime: " + TimeSpan.FromSeconds(overview.uptime).ToString(@"dd\.hh\:mm\:ss");
      instanceCountText.text = "Instances: " + overview.instanceCount;
   }

   public void getLog () {
      downloadLogButton.interactable = false;
      Global.player.admin.getRemoteServerLogString(data.port);
   }

   public void openLogFile () {
      if (_log == null) {
         return;
      }

      try {
         string path = Application.temporaryCachePath + "/" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".txt";
         File.WriteAllText(path, _log);
         System.Diagnostics.Process.Start(path);
      } catch (Exception e) {
         D.log($"Error opening log file. Message: {e.Message}");
      }
   }

   public void copyLogToClipboard () {
      if (_log == null) {
         return;
      }

      GUIUtility.systemCopyBuffer = _log;
   }

   public void receiveLog (string log) {
      _log = log;

      logActionsHolder.SetActive(true);
      getLogHolder.SetActive(false);
   }

   public void onExpandClick () {
      _expanded = !_expanded;
      descriptionRow.SetActive(_expanded);
      expandButton.GetComponentInChildren<Text>().text = _expanded ? "▼" : "▶";
   }

   #region Private Variables

   // Is description row expanded
   private bool _expanded = true;

   // The log of this server
   private string _log = null;

   #endregion
}
