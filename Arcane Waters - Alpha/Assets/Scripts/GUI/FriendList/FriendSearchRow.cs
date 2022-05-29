using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class FriendSearchRow : MonoBehaviour
{
   #region Public Variables

   // The name of the user
   public Text userName;

   // The level of the user
   public Text userLevel;

   // The zone location of the user
   public Text userZone;

   // The biome location of the user
   public Text userBiome;

   // The status of the member - online or when was last active
   public Text userOnlineStatus;

   // The online icon
   public GameObject onlineIcon;

   // The offline icon
   public GameObject offlineIcon;

   // The button to invite player to friends
   public GameObject inviteToFriendsButton;

   #endregion

   public void populate (UserSearchResult result) {
      _userId = result.userId;

      userName.text = result.name;
      userZone.text = Area.getName(result.area);
      userBiome.text = Biome.getName(result.biome);
      userLevel.text = result.level.ToString();

      onlineIcon.SetActive(result.isOnline);
      offlineIcon.SetActive(!result.isOnline);

      inviteToFriendsButton.SetActive(!result.isFriend);
      if (inviteToFriendsButton.TryGetComponent(out ToolTipComponent tooltip)) {
         tooltip.message = "Invite To Friends";
      }
   }

   public void onInviteToFriendsClick () {
      inviteToFriendsButton.SetActive(false);
      if (Global.player != null && _userId > 0) {
         Global.player.rpc.Cmd_SendFriendshipInvite(_userId);
      }
   }

   #region Private Variables

   // User id we represent
   private int _userId;

   #endregion
}
