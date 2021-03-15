using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using NubisDataHandling;
using System;

public class ServerStatusPanel : ClientMonoBehaviour
{
   #region Public Variables

   // The number of hours of previous events that must be displayed
   public static int HISTORY_HOURS = 24;

   // The number of rows the panel can contain
   public static int HISTORY_ROWS = 8;

   // The prefab we use for instantiating event rows
   public ServerStatusPanelRow rowPrefab;

   // The container of the event rows
   public GameObject rowContainer;

   // The containers of the server statuses
   public GameObject serverOnlineGO;
   public GameObject serverOfflineGO;

   // The canvas group
   public CanvasGroup canvasGroup;

   // Self
   public static ServerStatusPanel self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   public void Start () {
      if (Util.isBatch()) {
         return;
      }

      if (ServerHistoryManager.self.isServerHistoryActive()) {
         canvasGroup.Show();
      } else {
         canvasGroup.Hide();
         return;
      }

      // Empty the panel info
      rowContainer.DestroyChildren();
      serverOnlineGO.SetActive(false);
      serverOfflineGO.SetActive(false);

      refreshPanel();
      InvokeRepeating(nameof(refreshPanelIfActive), 30f, 30f);
   }

   private void refreshPanelIfActive () {
      if (!TitleScreen.self.isActive()) {
         return;
      }

      refreshPanel();
   }
   
   public void refreshPanel () {
      if (!ServerHistoryManager.self.isServerHistoryActive()) {
         return;
      }

      DateTime startDate = DateTime.UtcNow - new TimeSpan(HISTORY_HOURS, 0, 0);
      NubisDataFetcher.self.getServerHistory(HISTORY_ROWS, startDate);
   }

   public void updatePanelWithServerHistory (bool isServerOnline, List<ServerHistoryInfo> historyList) {
      rowContainer.DestroyChildren();

      foreach (ServerHistoryInfo info in historyList) {
         ServerStatusPanelRow row = Instantiate(rowPrefab, rowContainer.transform, false);
         row.setRowForServerEvent(info);
      }

      if (isServerOnline) {
         serverOnlineGO.SetActive(true);
         serverOfflineGO.SetActive(false);
      } else {
         serverOnlineGO.SetActive(false);
         serverOfflineGO.SetActive(true);
      }
   }

   #region Private Variables

   #endregion
}
