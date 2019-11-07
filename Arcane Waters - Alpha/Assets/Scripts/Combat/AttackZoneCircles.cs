using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AttackZoneCircles : MonoBehaviour
{
   #region Public Variables

   // The rotation speed
   public static float ROTATION_SPEED = 1f;

   // The range circles
   public AttackRangeCircle minRangeCircle;
   public AttackRangeCircle lowerNormalCircle;
   public AttackRangeCircle lowerStrongCircle;
   public AttackRangeCircle maxRangeCircle;

   #endregion

   public void draw (float maxAttackRange) {
      // Draw the range circles
      minRangeCircle.draw(PlayerShipEntity.MIN_RANGE);
      lowerNormalCircle.draw(PlayerShipEntity.LOWER_LIMIT_NORMAL_ATTACK * maxAttackRange);
      lowerStrongCircle.draw(PlayerShipEntity.LOWER_LIMIT_STRONG_ATTACK * maxAttackRange);
      maxRangeCircle.draw(maxAttackRange);
   }

   public void Update () {
      // Slowly rotate while active
      transform.Rotate(Vector3.forward, ROTATION_SPEED * Time.deltaTime);
   }

   public void show (AttackZone.Type attackZone) {
      gameObject.SetActive(true);

      // If the zone is already being displayed, do nothing
      if (attackZone == _currentAttackZone) {
         return;
      }

      // Hide all the circles
      minRangeCircle.hide();
      lowerNormalCircle.hide();
      lowerStrongCircle.hide();
      maxRangeCircle.hide();

      // Display the circles that delimit the zone
      switch (attackZone) {
         case AttackZone.Type.Weak:
            minRangeCircle.show(attackZone);
            lowerNormalCircle.show(attackZone);
            break;
         case AttackZone.Type.Normal:
            lowerNormalCircle.show(attackZone);
            lowerStrongCircle.show(attackZone);
            break;
         case AttackZone.Type.Strong:
            lowerStrongCircle.show(attackZone);
            maxRangeCircle.show(attackZone);
            break;
         default:
            break;
      }

      // Save the currently displayed attack zone
      _currentAttackZone = attackZone;
   }

   public void hide () {
      if (gameObject.activeSelf) {
         // Hide all the circles
         minRangeCircle.hide();
         lowerNormalCircle.hide();
         lowerStrongCircle.hide();
         maxRangeCircle.hide();

         // Save the currently displayed zone
         _currentAttackZone = AttackZone.Type.None;

         // Disable the gameobject and its animation
         gameObject.SetActive(false);
      }
   }

   #region Private Variables

   // The currently displayed attack zone
   private AttackZone.Type _currentAttackZone = AttackZone.Type.None;

   #endregion
}