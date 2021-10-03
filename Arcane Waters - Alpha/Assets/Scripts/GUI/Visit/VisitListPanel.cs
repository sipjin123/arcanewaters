using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class VisitListPanel : Panel {
   #region Public Variables

   // Self
   public static VisitListPanel self;

   // The prefab holder
   public Transform visitTempHolder;

   // The prefab of visit template
   public VisitListTemplate visitTemplatePrefab;

   // The next page button
   public Button nextPageButton;

   // The previous page button
   public Button previousPageButton;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;
   }

   public void updatePanelWithFriendshipInfo (List<FriendshipInfo> friendshipInfoList, int totalFriends) {
      visitTempHolder.gameObject.DestroyChildren();
      FriendListManager.self.cachedFriendshipInfoList = friendshipInfoList;
      if (FriendListManager.self.cachedFriendshipInfoList == null) {
         FriendListManager.self.cachedFriendshipInfoList = new List<FriendshipInfo>();
      }

      // Sort the list by online status
      friendshipInfoList = friendshipInfoList.OrderByDescending(f => f.isOnline).ToList();

      // Update the rows per page
      _rowsPerPage = Friendship.MAX_FRIENDS;

      // Create the friend rows
      foreach (FriendshipInfo friend in friendshipInfoList) {
         // Instantiate and initialize the row
         VisitListTemplate rowFriend = Instantiate(visitTemplatePrefab, visitTempHolder.transform, false);
         rowFriend.setRowForFriendshipInfo(friend);
      }
   }

   public void refreshPanel () {
      Global.player.rpc.Cmd_RequestFriendshipVisitFromServer(_currentPage, _rowsPerPage);
   }

   public void nextPage () {
      if (_currentPage < _maxPage) {
         _currentPage++;
         refreshPanel();
      }
   }

   public void previousPage () {
      if (_currentPage > 1) {
         _currentPage--;
         refreshPanel();
      }
   }

   private void updateNavigationButtons () {
      // Activate or deactivate the navigation buttons if we reached a limit
      previousPageButton.enabled = true;
      nextPageButton.enabled = true;

      if (_currentPage <= 1) {
         previousPageButton.enabled = false;
      }

      if (_currentPage >= _maxPage) {
         nextPageButton.enabled = false;
      }
   }

   #region Private Variables

   // The index of the current page
   private int _currentPage = 1;

   // The maximum page index (starting at 1)
   private int _maxPage = 1;

   // The number of rows per page
   private int _rowsPerPage = Friendship.MAX_FRIENDS;

   #endregion
}
