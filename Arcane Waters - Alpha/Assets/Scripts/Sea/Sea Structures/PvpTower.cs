using UnityEngine;

public class PvpTower : SeaStructureTower
{
   #region Public Variables

   // How far away this unit can target and attack enemies
   public static float ATTACK_RANGE = 3.5f;

   // The range at which the attack range circle will be displayed
   public static float WARNING_RANGE = 4.5f;

   #endregion

   private void OnDrawGizmosSelected () {
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(transform.position, ATTACK_RANGE);
   }

   public override bool isPvpTower () {
      return true;
   }

   #region Private Variables

   #endregion
}
