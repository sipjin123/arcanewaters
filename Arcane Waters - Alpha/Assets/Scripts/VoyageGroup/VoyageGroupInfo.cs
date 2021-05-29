using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

[Serializable]
public class VoyageGroupInfo
{
   #region Public Variables

   // The group ID
   public int groupId;

   // The voyage ID
   public int voyageId;

   // The date at which the group was created
   public long creationDate;

   // Gets set to true when the group is waiting for more quickmatch members
   public bool isQuickmatchEnabled;

   // Gets set to true when the group is private
   public bool isPrivate;

   // Gets set to true when the group members must be invisible and untouchable (used by admins)
   public bool isGhost;

   // The userId of the members
   public List<int> members = new List<int>();

   // Where members of this group will be spawned in pvp
   public string pvpSpawn = "";

   #endregion

   public VoyageGroupInfo () { }

   public VoyageGroupInfo (int groupId, int voyageId, DateTime creationDate, bool isQuickmatchEnabled, bool isPrivate, bool isGhost) {
      this.groupId = groupId;
      this.voyageId = voyageId;
      this.creationDate = creationDate.ToBinary();
      this.isQuickmatchEnabled = isQuickmatchEnabled;
      this.isPrivate = isPrivate;
      this.isGhost = isGhost;
   }

   #region Private Variables

   #endregion
}
