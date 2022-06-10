using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using NubisDataHandling;

public class AdminInstanceListPanel : Panel
{
   #region Public Variables

   // The container for the rows
   public GameObject rowsContainer;

   // The prefab we use for creating rows
   public AdminInstanceListRow instanceListRowPrefab;

   // Load Blocker when data is fetching
   public GameObject loadBlocker;

   // The auction info panel
   //public AuctionInfoPanel auctionInfoPanel;

   // The sorting icons for each column
   [Header("Sorting icons")]
   public GameObject idAsc;
   public GameObject idDesc;
   public GameObject portAsc;
   public GameObject portDesc;
   public GameObject areaAsc;
   public GameObject areaDesc;
   public GameObject leagueIndexAsc;
   public GameObject leagueIndexDesc;
   public GameObject difficultyAsc;
   public GameObject difficultyDesc;
   public GameObject playerCountAsc;
   public GameObject playerCountDesc;
   public GameObject enemyCountAsc;
   public GameObject enemyCountDesc;
   public GameObject isPvPAsc;
   public GameObject isPvPDesc;
   public GameObject timeAsc;
   public GameObject timeDesc;
   public GameObject biomeAsc;
   public GameObject biomeDesc;

   // The sub-panel displaying an individual voyage info
   public AdminVoyageInfoPanel adminVoyageInfoPanel;

   // Self
   public static AdminInstanceListPanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;

      // Clear out any current rows
      rowsContainer.DestroyChildren();
   }

   public void togglePanel () {
      if (Global.player == null || !Global.player.isAdmin()) {
         return;
      }

      if (!isShowing()) {
         SoundEffectManager.self.playGuiMenuOpenSfx();

         refreshPanel();
      } else {
         PanelManager.self.togglePanel(Type.AdminInstanceList);
      }
   }

   public void refreshPanel () {
      // Clear out any current rows
      rowsContainer.DestroyChildren();
      _rows.Clear();

      setLoadBlocker(true);
      Global.player.rpc.Cmd_RequestNetworkInstanceList();
   }

   public void receiveNetworkInstanceOverview (InstanceOverview ovw) {
      setLoadBlocker(false);

      AdminInstanceListRow row = Instantiate(instanceListRowPrefab, rowsContainer.transform);
      row.setRowForInstance(ovw);
      _rows.Add(row);

      sortByPlayerCount(true);
   }

   public void onReloadButtonPressed () {
      if (!loadBlocker.activeSelf) {
         refreshPanel();
      }
   }

   public void onVoyageInstanceRowPressed (Voyage voyage) {
      if (voyage != null && voyage.voyageId > 0 && voyage.instanceId > 0) {
         adminVoyageInfoPanel.updatePanelWithVoyage(voyage);
      }
   }

   public void warpToVoyage (Voyage voyage) {
      Global.player.rpc.Cmd_WarpAdminToVoyageInstanceAsGhost(voyage.voyageId, voyage.areaKey);
      PanelManager.self.unlinkPanel();
   }

   public override void hide () {
      base.hide();
      adminVoyageInfoPanel.hide();
   }

   public void setLoadBlocker (bool isOn) {
      loadBlocker.SetActive(isOn);
   }

   public void sortById () {
      toggleIcons(idAsc, idDesc);
      _rows.Sort((a, b) => {
         return idAsc.activeSelf
         ? a.instance.id.CompareTo(b.instance.id)
         : b.instance.id.CompareTo(a.instance.id);
      });
      orderRows();
   }

   public void sortByPort () {
      toggleIcons(portAsc, portDesc);
      _rows.Sort((a, b) => {
         return portAsc.activeSelf
         ? a.instance.port.CompareTo(b.instance.port)
         : b.instance.port.CompareTo(a.instance.port);
      });
      orderRows();
   }

   public void sortByArea () {
      toggleIcons(areaAsc, areaDesc);
      _rows.Sort((a, b) => {
         return areaAsc.activeSelf
         ? a.instance.area.CompareTo(b.instance.area)
         : b.instance.area.CompareTo(a.instance.area);
      });
      orderRows();
   }

   public void sortByLeagueIndex () {
      toggleIcons(leagueIndexAsc, leagueIndexDesc);
      _rows.Sort((a, b) => {
         return leagueIndexAsc.activeSelf
         ? a.instance.voyage.leagueIndex.CompareTo(b.instance.voyage.leagueIndex)
         : b.instance.voyage.leagueIndex.CompareTo(a.instance.voyage.leagueIndex);
      });
      orderRows();
   }

   public void sortByDifficulty () {
      toggleIcons(difficultyAsc, difficultyDesc);
      _rows.Sort((a, b) => {
         return difficultyAsc.activeSelf
         ? a.instance.difficulty.CompareTo(b.instance.difficulty)
         : b.instance.difficulty.CompareTo(a.instance.difficulty);
      });
      orderRows();
   }

   public void sortByPlayerCount (bool forceDesc = false) {
      if (forceDesc) {
         disableAllSortIcons();
         playerCountAsc.SetActive(false);
         playerCountDesc.SetActive(true);
      } else {
         toggleIcons(playerCountAsc, playerCountDesc);
      }
      _rows.Sort((a, b) => {
         return playerCountAsc.activeSelf
         ? a.instance.pCount.CompareTo(b.instance.pCount)
         : b.instance.pCount.CompareTo(a.instance.pCount);
      });
      orderRows();
   }

   public void sortByEnemyCount () {
      toggleIcons(enemyCountAsc, enemyCountDesc);
      _rows.Sort((a, b) => {
         return enemyCountAsc.activeSelf
         ? a.instance.aliveEnemyCount.CompareTo(b.instance.aliveEnemyCount)
         : b.instance.aliveEnemyCount.CompareTo(a.instance.aliveEnemyCount);
      });
      orderRows();
   }

   public void sortByIsPvP () {
      toggleIcons(isPvPAsc, isPvPDesc);
      _rows.Sort((a, b) => {
         return isPvPAsc.activeSelf
         ? a.instance.isPvp.CompareTo(b.instance.isPvp)
         : b.instance.isPvp.CompareTo(a.instance.isPvp);
      });
      orderRows();
   }

   public void sortByTime () {
      toggleIcons(timeAsc, timeDesc);
      _rows.Sort((a, b) => {
         return timeAsc.activeSelf
         ? a.instance.creationDate.CompareTo(b.instance.creationDate)
         : b.instance.creationDate.CompareTo(a.instance.creationDate);
      });
      orderRows();
   }

   public void sortByBiome () {
      toggleIcons(biomeAsc, biomeDesc);
      _rows.Sort((a, b) => {
         return biomeAsc.activeSelf
         ? a.instance.biome.CompareTo(b.instance.biome)
         : b.instance.biome.CompareTo(a.instance.biome);
      });
      orderRows();
   }

   private void toggleIcons (GameObject asc, GameObject desc) {
      if (!asc.activeSelf && !desc.activeSelf) {
         disableAllSortIcons();
         asc.SetActive(true);
      } else {
         bool nameAscBool = !asc.activeSelf;
         disableAllSortIcons();
         asc.SetActive(nameAscBool);
         desc.SetActive(!nameAscBool);
      }
   }

   private void disableAllSortIcons () {
      idAsc.SetActive(false);
      idDesc.SetActive(false);
      portAsc.SetActive(false);
      portDesc.SetActive(false);
      areaAsc.SetActive(false);
      areaDesc.SetActive(false);
      leagueIndexAsc.SetActive(false);
      leagueIndexDesc.SetActive(false);
      difficultyAsc.SetActive(false);
      difficultyDesc.SetActive(false);
      playerCountAsc.SetActive(false);
      playerCountDesc.SetActive(false);
      enemyCountAsc.SetActive(false);
      enemyCountDesc.SetActive(false);
      isPvPAsc.SetActive(false);
      isPvPDesc.SetActive(false);
      timeAsc.SetActive(false);
      timeDesc.SetActive(false);
      biomeAsc.SetActive(false);
      biomeDesc.SetActive(false);
   }

   private void orderRows () {
      for (int i = 0; i < _rows.Count; i++) {
         _rows[i].transform.SetSiblingIndex(i);
      }
   }

   #region Private Variables

   // References to the row gameobjects
   private List<AdminInstanceListRow> _rows = new List<AdminInstanceListRow>();

   #endregion
}
