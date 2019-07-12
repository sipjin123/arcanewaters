using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class GuildInvite {
   #region Public Variables

   // The sender ID
   public int senderId;

   // The recipient ID
   public int recipientId;

   // The sender name
   public string senderName;

   // The guild name
   public string guildName;

   // The guild ID
   public int guildId;

   // The time of the invite
   public long inviteTime;

   #endregion

   public GuildInvite () { }

   public override bool Equals (object obj) {
      if (!(obj is GuildInvite))
         return false;

      GuildInvite other = (GuildInvite) obj;
      return senderId == other.senderId && recipientId == other.recipientId && guildId == other.guildId;
   }

   public override int GetHashCode () {
      unchecked // Overflow is fine, just wrap
      {
         int hash = 17;
         hash = hash * 23 + senderId.GetHashCode();
         hash = hash * 23 + recipientId.GetHashCode();
         hash = hash * 23 + guildId.GetHashCode();
         return hash;
      }
   }

   #region Private Variables

   #endregion
}
