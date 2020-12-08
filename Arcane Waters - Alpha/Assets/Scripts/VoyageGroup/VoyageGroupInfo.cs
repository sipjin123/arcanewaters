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

   // The userId of the members
   public List<int> members = new List<int>();

   #endregion

   public VoyageGroupInfo () { }

   public VoyageGroupInfo (int groupId, int voyageId, DateTime creationDate, bool isQuickmatchEnabled, bool isPrivate) {
      this.groupId = groupId;
      this.voyageId = voyageId;
      this.creationDate = creationDate.ToBinary();
      this.isQuickmatchEnabled = isQuickmatchEnabled;
      this.isPrivate = isPrivate;
   }

   #region Private Variables

   #endregion
}
