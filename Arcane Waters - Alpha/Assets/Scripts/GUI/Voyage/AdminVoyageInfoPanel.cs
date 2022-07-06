using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using TMPro;

public class AdminVoyageInfoPanel : SubPanel
{
   #region Public Variables

   // The container for the map cell
   public GameObject mapCellContainer;

   // The prefab we use for creating map cells
   public VoyageMapCell mapCellPrefab;

   // The container for the user rows
   public GameObject userRowsContainer;

   // The prefab we use for creating user rows
   public AdminVoyageInfoRow userRowPrefab;

   // Load Blocker when data is fetching
   public GameObject loadBlocker;

   #endregion

   public void updatePanelWithVoyage (GroupInstance groupInstance) {
      _groupInstance = groupInstance;

      show();

      // Clear out any old info
      mapCellContainer.DestroyChildren();
      userRowsContainer.DestroyChildren();

      // Instantiate the cell
      VoyageMapCell cell = Instantiate(mapCellPrefab, mapCellContainer.transform, false);
      cell.setCellForVoyage(groupInstance, () => onWarpToVoyageButtonPressed());

      // Ask the server the list of users present in the instance
      setLoadBlocker(true);
      Global.player.rpc.Cmd_RequestUserListForAdminVoyageInfoPanelFromServer(groupInstance.groupInstanceId, groupInstance.instanceId);
   }

   public void updatePanelWithUserList (List<UserInfo> userInfoList) {
      setLoadBlocker(false);

      // Clear out any current rows
      userRowsContainer.DestroyChildren();

      foreach (UserInfo userInfo in userInfoList) {
         AdminVoyageInfoRow row = Instantiate(userRowPrefab, userRowsContainer.transform);
         row.setRowForUser(userInfo);
      }
   }

   public void onWarpToVoyageButtonPressed () {
      hide();
      AdminInstanceListPanel.self.warpToVoyage(_groupInstance);
   }

   public void setLoadBlocker (bool isOn) {
      loadBlocker.SetActive(isOn);
   }

   #region Private Variables

   // The group instance (part of the voyage) being displayed by the panel
   private GroupInstance _groupInstance = null;

   #endregion
}