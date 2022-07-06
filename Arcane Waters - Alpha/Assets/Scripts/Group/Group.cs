using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

[Serializable]
public class Group
{
   #region Public Variables

   // The group ID
   public int groupId;

   // The group instance id this group is linked to
   public int groupInstanceId;

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

   // The current group stats
   public List<GroupStats> groupStats = new List<GroupStats>();

   // Where members of this group will be spawned in pvp
   public string pvpSpawn = "";

   // The creator of the group
   public int groupCreator = 0;

   #endregion

   public Group () { }

   public int getTotalDamage (int userId) {
      GroupStats userStats = groupStats.Find(_ => _.userId == userId);
      if (userStats != null) {
         return userStats.totalDamageDealt;
      }
      return 0;
   }

   public int getTotalTank (int userId) {
      GroupStats userStats = groupStats.Find(_ => _.userId == userId);
      if (userStats != null) {
         return userStats.totalTankedDamage;
      }
      return 0;
   }

   public int getTotalBuffs (int userId) {
      GroupStats userStats = groupStats.Find(_ => _.userId == userId);
      if (userStats != null) {
         return userStats.totalBuffs;
      }
      return 0;
   }

   public int getTotalHeals (int userId) {
      GroupStats userStats = groupStats.Find(_ => _.userId == userId);
      if (userStats != null) {
         return userStats.totalHeals;
      }
      return 0;
   }

   public void addDamageStatsForUser (int userId, int stat) {
      GroupStats userStats = groupStats.Find(_ => _.userId == userId);
      if (userStats == null) {
         groupStats.Add(new GroupStats {
            userId = userId,
            totalDamageDealt = stat
         });
      } else {
         userStats.totalDamageDealt += stat;
      }
   }

   public void addTankStatsForUser (int userId, int stat) {
      GroupStats userStats = groupStats.Find(_ => _.userId == userId);
      if (userStats == null) {
         groupStats.Add(new GroupStats {
            userId = userId,
            totalTankedDamage = stat
         });
      } else {
         userStats.totalTankedDamage += stat;
      }
   }

   public void addHealStatsForUser (int userId, int stat) {
      GroupStats userStats = groupStats.Find(_ => _.userId == userId);
      if (userStats == null) {
         groupStats.Add(new GroupStats {
            userId = userId,
            totalHeals = stat
         });
      } else {
         userStats.totalHeals += stat;
      }
   }

   public void addBuffStatsForUser (int userId, int stat) {
      GroupStats userStats = groupStats.Find(_ => _.userId == userId);
      if (userStats == null) {
         groupStats.Add(new GroupStats {
            userId = userId,
            totalBuffs = stat
         });
      } else {
         userStats.totalBuffs += stat;
      }
   }

   public Group (int groupId, int creatorId, int groupInstanceId, DateTime creationDate, bool isQuickmatchEnabled, bool isPrivate, bool isGhost) {
      this.groupId = groupId;
      this.groupInstanceId = groupInstanceId;
      this.groupCreator = creatorId;
      this.creationDate = creationDate.ToBinary();
      this.isQuickmatchEnabled = isQuickmatchEnabled;
      this.isPrivate = isPrivate;
      this.isGhost = isGhost;
   }

   #region Private Variables

   #endregion
}
