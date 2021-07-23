using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using NubisDataHandling;

public class PvpArenaPanel : Panel
{
   #region Public Variables

   // The container for the rows
   public GameObject rowsContainer;

   // The prefab we use for creating rows
   public PvpArenaRow pvpArenaRowPrefab;

   // Load Blocker when data is fetching
   public GameObject loadBlocker;

   // The sub-panel displaying an individual instance info
   public PvpArenaInfoPanel pvpArenaInfoPanel;

   // Self
   public static PvpArenaPanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;

      // Clear out any current rows
      rowsContainer.DestroyChildren();
   }

   public void togglePanel () {
      if (Global.player == null) {
         return;
      }

      if (!isShowing()) {
         SoundEffectManager.self.playGuiMenuOpenSfx();

         refreshPanel();
      } else {
         PanelManager.self.togglePanel(Type.PvpArena);
      }
   }

   public void refreshPanel () {
      setLoadBlocker(true);
      Global.player.rpc.Cmd_RequestPvpArenaListFromServer();
   }

   public void receivePvpArenasFromServer (List<Voyage> pvpArenaList) {
      setLoadBlocker(false);

      // Clear out any current rows
      rowsContainer.DestroyChildren();

      // Sort by time
      pvpArenaList.Sort((a, b) => { return a.creationDate.CompareTo(b.creationDate); });

      for (int i = 0; i < pvpArenaList.Count; i++) {
         PvpArenaRow row = Instantiate(pvpArenaRowPrefab, rowsContainer.transform);
         row.setRowForVoyage(pvpArenaList[i], i == 0, i == pvpArenaList.Count - 1);
      }
   }

   public void onReloadButtonPressed () {
      if (!loadBlocker.activeSelf) {
         refreshPanel();
      }
   }

   public void onPvpArenaRowPressed (Voyage voyage) {
      Global.player.rpc.Cmd_RequestPvpArenaInfoFromServer(voyage.voyageId);
   }

   public void joinPvpArena (Voyage voyage, PvpTeamType team) {
      Global.player.rpc.Cmd_JoinPvpArena(voyage.voyageId, team);
      PanelManager.self.unlinkPanel();
   }

   public override void hide () {
      base.hide();
      pvpArenaInfoPanel.hide();
   }

   public void setLoadBlocker (bool isOn) {
      loadBlocker.SetActive(isOn);
   }

   #region Private Variables

   #endregion
}

