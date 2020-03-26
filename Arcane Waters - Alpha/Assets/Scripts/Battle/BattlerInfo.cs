using System;

[Serializable]
public class BattlerInfo
{
   // The type of battler if ai or player controlled
   public BattlerType battlerType;

   // The enemy type
   public Enemy.Type enemyType;

   // The name of the battler
   public string battlerName;

   // The companion id if is a companion of the player
   public int companionId = -1;
}