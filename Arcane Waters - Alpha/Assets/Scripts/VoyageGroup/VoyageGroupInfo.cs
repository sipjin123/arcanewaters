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

   // The current voyage group stats
   public List<VoyageGroupStats> voyageGroupStats = new List<VoyageGroupStats>();

   // Where members of this group will be spawned in pvp
   public string pvpSpawn = "";

   // The creator of the voyage group
   public int voyageCreator = 0;

   #endregion

   public VoyageGroupInfo () { }

   public int getTotalDamage (int userId) {
      VoyageGroupStats voyageStat = voyageGroupStats.Find(_ => _.userId == userId);
      if (voyageStat != null) {
         return voyageStat.totalDamageDealt;
      }
      return 0;
   }

   public int getTotalTank (int userId) {
      VoyageGroupStats voyageStat = voyageGroupStats.Find(_ => _.userId == userId);
      if (voyageStat != null) {
         return voyageStat.totalTankedDamage;
      }
      return 0;
   }

   public int getTotalBuffs (int userId) {
      VoyageGroupStats voyageStat = voyageGroupStats.Find(_ => _.userId == userId);
      if (voyageStat != null) {
         return voyageStat.totalBuffs;
      }
      return 0;
   }

   public int getTotalHeals (int userId) {
      VoyageGroupStats voyageStat = voyageGroupStats.Find(_ => _.userId == userId);
      if (voyageStat != null) {
         return voyageStat.totalHeals;
      }
      return 0;
   }

   public void addDamageStatsForUser (int userId, int stat) {
      VoyageGroupStats voyageStat = voyageGroupStats.Find(_ => _.userId == userId);
      if (voyageStat == null) {
         voyageGroupStats.Add(new VoyageGroupStats {
            userId = userId,
            totalDamageDealt = stat
         });
      } else {
         voyageStat.totalDamageDealt += stat;
      }
   }

   public void addTankStatsForUser (int userId, int stat) {
      VoyageGroupStats voyageStat = voyageGroupStats.Find(_ => _.userId == userId);
      if (voyageStat == null) {
         voyageGroupStats.Add(new VoyageGroupStats {
            userId = userId,
            totalTankedDamage = stat
         });
      } else {
         voyageStat.totalTankedDamage += stat;
      }
   }

   public void addHealStatsForUser (int userId, int stat) {
      VoyageGroupStats voyageStat = voyageGroupStats.Find(_ => _.userId == userId);
      if (voyageStat == null) {
         voyageGroupStats.Add(new VoyageGroupStats {
            userId = userId,
            totalHeals = stat
         });
      } else {
         voyageStat.totalHeals += stat;
      }
   }

   public void addBuffStatsForUser (int userId, int stat) {
      VoyageGroupStats voyageStat = voyageGroupStats.Find(_ => _.userId == userId);
      if (voyageStat == null) {
         voyageGroupStats.Add(new VoyageGroupStats {
            userId = userId,
            totalBuffs = stat
         });
      } else {
         voyageStat.totalBuffs += stat;
      }
   }

   public VoyageGroupInfo (int groupId, int creatorId, int voyageId, DateTime creationDate, bool isQuickmatchEnabled, bool isPrivate, bool isGhost) {
      this.groupId = groupId;
      this.voyageId = voyageId;
      this.voyageCreator = creatorId;
      this.creationDate = creationDate.ToBinary();
      this.isQuickmatchEnabled = isQuickmatchEnabled;
      this.isPrivate = isPrivate;
      this.isGhost = isGhost;
   }

   #region Private Variables

   #endregion
}
