using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using NubisDataHandling;

public class PvpArenaPanelSection : MonoBehaviour
{
   #region Public Variables

   // The container for the cells
   public GameObject cellContainer;

   // The prefab we use for creating cells
   public PvpArenaCell pvpArenaCellPrefab;

   // Load Blocker when data is fetching
   public GameObject loadBlocker;

   // The sub-panel displaying an individual instance info
   public PvpArenaInfoPanel pvpArenaInfoPanel;

   // Self
   public static PvpArenaPanelSection self;

   #endregion

   public void Awake () {
      self = this;

      // Clear out any current rows
      cellContainer.DestroyChildren();
   }

   public void show () {
      gameObject.SetActive(true);
      refreshPanel();
   }

   public void hide () {
      pvpArenaInfoPanel.hide();
      gameObject.SetActive(false);
   }

   public void refreshPanel () {
      setLoadBlocker(true);
      Global.player.rpc.Cmd_RequestPvpArenaListFromServer();
   }

   public void receivePvpArenasFromServer (List<Voyage> pvpArenaList) {
      setLoadBlocker(false);

      // Clear out any current rows
      cellContainer.DestroyChildren();

      // Sort by time
      pvpArenaList.Sort((a, b) => { return a.creationDate.CompareTo(b.creationDate); });

      foreach (Voyage pvpArena in pvpArenaList) {
         PvpArenaCell cell = Instantiate(pvpArenaCellPrefab, cellContainer.transform);
         cell.setCellForPvpArena(pvpArena, () => onPvpArenaRowPressed(pvpArena));
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
      PanelManager.self.hideCurrentPanel();
   }

   public bool isShowing () {
      return gameObject.activeSelf;
   }

   public void setLoadBlocker (bool isOn) {
      loadBlocker.SetActive(isOn);
   }

   #region Private Variables

   #endregion
}

