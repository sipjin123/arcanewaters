using UnityEngine;

public class PvpTower : SeaStructureTower
{
   #region Public Variables

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
