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

   // The warning panel activated if the user has not selected their house and farm
   public GameObject setCustomMapWarningObj;

   // The text ui of the panel
   public Text pageText, friendCountText;

   // The max display of visitable players in a page
   public const int MAX_ITEM_PER_PAGE = 10;
   
   #endregion

   public override void Awake () {
      base.Awake();
      self = this;

      nextPageButton.onClick.AddListener(() => nextPage());
      previousPageButton.onClick.AddListener(() => previousPage());
   }

   public void updatePanelWithFriendshipInfo (bool isCustomMapSet, List<FriendshipInfo> friendshipInfoList, int totalFriends) {
      _maxPage = (totalFriends / MAX_ITEM_PER_PAGE) + (totalFriends % MAX_ITEM_PER_PAGE > 0 ? 1 : 0);

      visitTempHolder.gameObject.DestroyChildren();
      FriendListManager.self.cachedFriendshipInfoList = friendshipInfoList;
      if (FriendListManager.self.cachedFriendshipInfoList == null) {
         FriendListManager.self.cachedFriendshipInfoList = new List<FriendshipInfo>();
      }

      setCustomMapWarningObj.SetActive(!isCustomMapSet);

      // Sort the list by online status
      friendshipInfoList = friendshipInfoList.OrderByDescending(f => f.isOnline).ToList();

      friendCountText.text = friendshipInfoList.Count + "/" + totalFriends;
      pageText.text = _currentPage + " / " + _maxPage;

      // Create the friend rows
      foreach (FriendshipInfo friend in friendshipInfoList) {
         // Instantiate and initialize the row
         VisitListTemplate rowFriend = Instantiate(visitTemplatePrefab, visitTempHolder.transform, false);
         rowFriend.setRowForFriendshipInfo(friend, isCustomMapSet);
      }
   }

   public void refreshPanel () {
      Global.player.rpc.Cmd_RequestFriendshipVisitFromServer(_currentPage, MAX_ITEM_PER_PAGE);
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

   #endregion
}
