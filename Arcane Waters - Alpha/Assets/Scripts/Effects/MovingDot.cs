using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MovingDot : ClientMonoBehaviour
{
   #region Public Variables

   // The actual dot sprite
   public SpriteRenderer dotSprite;

   // The dot shadow
   public SpriteRenderer dotShadow;

   #endregion

   private void Start() {
      _creationTime = Time.time;
      _creationPos = this.transform.position;

      // Start out invisible
      this.dotSprite.enabled = this.dotShadow.enabled = false;
   }

   void Update() {
      // If there's no player, we're done
      if (Global.player == null) {
         return;
      }

      // Hide our sprite while the attack circle is hidden
      this.dotSprite.enabled = this.dotShadow.enabled = AttackManager.self.attackCircleIndicator.enabled;

      // Check how long it's been since we were created
      float timeSinceCreation = Time.time - _creationTime;
      float lerpTime = timeSinceCreation / 3.0f;

      // Move towards the attack circle indicator
      Vector2 startPos = Global.player.transform.position;
      Vector2 endPos = AttackManager.self.attackCircleIndicator.transform.position;
      Vector2 newPos = Vector2.Lerp(startPos, endPos, lerpTime);
      Util.setXY(this.transform, newPos);

      // Adjusts the height of the dot in an arch
      Util.setLocalY(dotSprite.transform, AttackManager.getArcHeight(startPos, endPos, lerpTime));

      // If we're close enough, we're done
      if (Vector2.Distance(this.transform.position, AttackManager.self.attackCircleIndicator.transform.position) < .02f) {
         Destroy(this.gameObject);
      }
   }

   #region Private Variables

   // The time at which we were created
   protected float _creationTime;

   // The position at which we were created
   protected Vector2 _creationPos;

   #endregion
}
