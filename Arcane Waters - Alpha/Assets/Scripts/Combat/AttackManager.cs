using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AttackManager : ClientMonoBehaviour {
   #region Public Variables

   // The Game Object that shows where we would attack
   public SpriteRenderer attackCircleIndicator;

   // The sprite we use for an enabled attack
   public Sprite enabledAttack;

   // The sprite we use for a disabled attack
   public Sprite disabledAttack;

   // The currently selected attack target point
   public Vector2 targetPoint;

   // A prefab we use for creating moving dots
   public MovingDot dotPrefab;

   // Self
   public static AttackManager self;

   #endregion

   private void Start() {
      self = this;

      // Continually create dots moving towards our current attack target
      InvokeRepeating("createMovingDot", 0f, .2f);
   }

   void Update () {
      if (SeaManager.combatMode != SeaManager.CombatMode.Circle || SeaManager.selectedAttackType == Attack.Type.Air) {
         attackCircleIndicator.enabled = false;
         return;
      }

      // Check if the right mouse is being held down
      bool rightMouseDown = Input.GetMouseButton(1);

      // Only show the indicator while the right mouse is down
      attackCircleIndicator.enabled = rightMouseDown;

      // If we don't have a player, or the player isn't at sea, we're done
      if (Global.player == null || !(Global.player is SeaEntity)) {
         return;
      }

      // If the mouse button isn't down, we don't have to do anything else
      if (!rightMouseDown) {
         return;
      }

      // Figure out where the mouse is in world coordinates
      targetPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);

      // Move the indicator to the mouse position
      Util.setXY(attackCircleIndicator.transform, targetPoint);

      // Update the sprite based on whether it's in position
      attackCircleIndicator.sprite = isInValidSpot() ? enabledAttack : disabledAttack;

   }

   public static bool isAttackableSpot (Vector2 pos) {
      SeaEntity entity = (SeaEntity) Global.player;

      // If it's inside one of the attack boxes, we're done
      return (entity.leftAttackBox.OverlapPoint(pos) || entity.rightAttackBox.OverlapPoint(pos));
   }

   protected bool isInValidSpot () {
      if (!(Global.player is SeaEntity)) {
         return false;
      }

      SeaEntity entity = (SeaEntity) Global.player;

      // Check where our indicator is at
      Vector3 pos = attackCircleIndicator.transform.position;

      // If it's inside one of the attack boxes, we're done
      return (entity.leftAttackBox.OverlapPoint(pos) || entity.rightAttackBox.OverlapPoint(pos));
   }

   protected void createMovingDot () {
      // We only do this when we have a player ship
      if (Global.player == null || !(Global.player is SeaEntity) || SeaManager.selectedAttackType == Attack.Type.Air) {
         return;
      }

      // Create the dot
      MovingDot dot = Instantiate(dotPrefab, this.transform);

      // Start its position at the player ship
      Util.setXY(dot.transform, Global.player.transform.position);
   }

   public static float getArcHeight (Vector2 startPos, Vector2 endPos, float lerpTime) {
      float attackDistance = Vector2.Distance(startPos, endPos);
      float distanceModifier = Mathf.Clamp(attackDistance, .25f, 2f);
      float angleInDegrees = lerpTime * 180f;
      float ballHeight = Util.getSinOfAngle(angleInDegrees) * distanceModifier * .5f;

      return ballHeight;
   }

   #region Private Variables

   #endregion
}
