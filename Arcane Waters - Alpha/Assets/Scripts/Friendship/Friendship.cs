using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Friendship
{
   #region Public Variables

   // The maximum number of friends
   public static int MAX_FRIENDS = 100;

   // The different friendship statuses
   public enum Status
   {
      None = 0,
      InviteSent = 1,
      InviteReceived = 2,
      Friends = 3
   }

   #endregion

   #region Private Variables

   #endregion
}
