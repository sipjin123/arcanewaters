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

   // The gameobject that is displayed if the user is a friend of the current player
   public GameObject isFriendIcon;

   // The gameobject that is displayed if the user is not a friend of the current player
   public GameObject isNotFriendIcon;

   #endregion

   public void populate (UserSearchResult result) {
      userName.text = result.name;
      userZone.text = Area.getName(result.area);
      userBiome.text = Biome.getName(result.biome);
      userLevel.text = result.level.ToString();

      onlineIcon.SetActive(result.isOnline);
      offlineIcon.SetActive(!result.isOnline);

      isFriendIcon.SetActive(result.isFriend);
      isNotFriendIcon.SetActive(!result.isFriend);
   }

   #region Private Variables

   #endregion
}
