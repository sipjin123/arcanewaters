public class GroupStats {
   #region Public Variables

   // The user id
   public int userId;

   // The user stats
   public int totalDamageDealt = 0;
   public int totalTankedDamage = 0;
   public int totalHeals = 0;
   public int totalBuffs = 0;

   #endregion
}

public class DamageRecord {
   // The last time the attack was triggered
   public double lastAttackTime;

   // The total damage dealt
   public int totalDamage;
}