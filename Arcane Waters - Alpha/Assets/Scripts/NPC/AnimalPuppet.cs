using UnityEngine;
using System.Linq;

public class AnimalPuppet : TemporaryController
{
   #region Public Variables

   #endregion

   public void setData(Vector2 endPos, float maxTime) {
      _endPos = endPos;
      _maxTime = maxTime;
   }

   protected override void onForceFastForward (ControlData puppet) {
      puppet.entity.transform.position = puppet.endPos;
   }

   protected override void startControl (ControlData puppet) {
      puppet.endPos = _endPos;
   }

   protected override void controlUpdate (ControlData puppet) {
      // Move animal linearly to the player
      float t = puppet.time / _maxTime;
      puppet.entity.getRigidbody().MovePosition(Vector3.Lerp(puppet.startPos, puppet.endPos, t));

      Vector2 dir = puppet.endPos - puppet.startPos;
      dir.Normalize();

      foreach (Animator animator in GetComponents<Animator>()) {
         // Calculate an angle for that direction
         float angle = Util.angle(dir);

         // Set our facing direction based on that angle
         puppet.entity.facing = puppet.entity.hasDiagonals ? Util.getFacingWithDiagonals(angle) : Util.getFacing(angle);

         animator.SetFloat("velocityX", dir.x);
         animator.SetFloat("velocityY", dir.y);
         animator.SetBool("isMoving", true);
         animator.SetInteger("facing", (int) puppet.entity.facing);
         animator.SetBool("inBattle", false);
      }

      // End control if time has run out
      if (puppet.time >= _maxTime) {
         puppet.entity.getRigidbody().MovePosition(puppet.endPos);
         puppet.entity.fallDirection = 0;

         // Reset animator to idle state
         foreach (Animator animator in GetComponents<Animator>()) {
            animator.SetFloat("velocityX", 0.0f);
            animator.SetFloat("velocityY", 0.0f);
            animator.SetBool("isMoving", false);
            animator.SetInteger("facing", (int) puppet.entity.facing);
            animator.SetBool("inBattle", false);
         }
         endControl(puppet);
      }
   }

   #region Private Variables

   // Destination of animal movement
   private Vector2 _endPos;

   // Time to reach destination by animal
   private float _maxTime = 0.0f;

   #endregion
}
