using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SteamFriendRow : MonoBehaviour
{
   #region Public Variables

   // The data this row represents
   public SteamFriendData data;

   // Various UI elements for showing friend data
   public Text nameText = null;
   public Text statusText = null;
   public Image avatarImage = null;
   public Button inviteToGameButton = null;
   public Button viewCharactersButton = null;

   #endregion

   public void setData (SteamFriendData data) {
      this.data = data;

      nameText.text = data.name;
      statusText.text = data.getStatusDisplay();
      inviteToGameButton.gameObject.SetActive(!data.playingArcaneWaters);
      inviteToGameButton.GetComponent<ToolTipComponent>().message = "Invite Friend To Play";
      viewCharactersButton.GetComponent<ToolTipComponent>().message = "View Friend's Characters";
   }

   public void setAvatarImage (Sprite sprite) {
      avatarImage.sprite = sprite;
   }

   public void inviteToGameButtonClick () {
      SteamFriendsManager.inviteFriendToGame(data);
      inviteToGameButton.interactable = false;
   }

   public void viewAccountCharactersClick () {
      if (Global.player != null) {
         Global.player.rpc.Cmd_SearchUser(new UserSearchInfo { filter = UserSearchInfo.FilteringMode.SteamId, input = data.steamId.ToString(), page = 0, resultsPerPage = 50 });
         FriendListPanel.self.toggleBlocker(true);
      }
   }

   #region Private Variables

   #endregion
}
