using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AttackManager : ClientMonoBehaviour {
   #region Public Variables

   // The cursor showing where we would attack - clamped to the max range
   public AttackCircleAimCursor clampedCursor;

   // The renderer of the cursor that shows where we would attack - free to move anywhere
   public SpriteRenderer freeCursor;

   // The max range dotted circle
   public AttackRangeCircle maxRangeCircle;

   // The next shot indicator
   public GameObject nextShotIndicator;

   // The color of the indicators over the target distance
   public Gradient colorOverDistance;

   // The trajectory dotted line
   public AttackTrajectory trajectory;

   // Self
   public static AttackManager self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
      nextShotIndicator.SetActive(false);
   }

   void Update () {
      if (Global.player == null || Global.player.isDead() || !(Global.player is ShipEntity) ||
         SeaManager.combatMode != SeaManager.CombatMode.Circle || SeaManager.selectedAttackType == Attack.Type.Air ||
         PanelManager.self.hasPanelInStack()) {
         freeCursor.enabled = false;
         maxRangeCircle.hide();
         clampedCursor.deactivate();
         trajectory.hide();
         Cursor.visible = true;
         return;
      }

      // Update the range circles if the range has changed
      PlayerShipEntity playerShipEntity = (PlayerShipEntity) Global.player;
      if (playerShipEntity.getAttackRange() != _attackRange) {
         _attackRange = playerShipEntity.getAttackRange();
         maxRangeCircle.draw(_attackRange);
      }

      // Move the range circle to the player position
      Util.setXY(maxRangeCircle.transform, Global.player.transform.position);

      // Hide the cursor when the right mouse is down
      Cursor.visible = !Input.GetMouseButton(1);

      // Get the mouse position
      Vector2 mousePosition = Util.getMousePos();

      // Check if the mouse right button is pressed
      if (Input.GetMouseButton(1)) {
         // Move the clamped aim indicator to the mouse position - clamped inside the range zone
         Util.setXY(clampedCursor.transform, playerShipEntity.clampToRange(mousePosition));

         // Determine the modifier for the target
         _normalizedDistanceToCursor = playerShipEntity.getNormalizedTargetDistance(mousePosition);

         // Determine the color of the indicators
         Color indicatorColor = getColorForDistance(_normalizedDistanceToCursor);

         // Set the color of the cursor
         clampedCursor.setColor(indicatorColor);

         // Set the speed of the cursor animation
         clampedCursor.setAnimationSpeed(0.5f + 0.5f / _normalizedDistanceToCursor);

         // Move the free aim indicator to the mouse position
         Util.setXY(freeCursor.transform, mousePosition);

         // Display the range circle
         maxRangeCircle.show();

         // Move the trajectory to the player position
         Util.setXY(trajectory.transform, Global.player.transform.position);

         // Draw the trajectory
         trajectory.draw(Global.player.transform.position, clampedCursor.transform.position,
            indicatorColor, playerShipEntity.getAttackRange());

         // Display the relevant indicators
         freeCursor.enabled =true;
         clampedCursor.activate();
      } else {
         // Hide the indicators
         freeCursor.enabled = false;
         clampedCursor.deactivate();
         maxRangeCircle.hide();
         trajectory.hide();
      }

      // Check if the next shot is defined
      if (playerShipEntity.isNextShotDefined) {
         nextShotIndicator.SetActive(true);

         // Place the indicator at the next shot coordinates
         Util.setXY(nextShotIndicator.transform, playerShipEntity.nextShotTarget);
      } else {
         nextShotIndicator.SetActive(false);
      }
   }

   public bool isHoveringOver (NetEntity entity) {
      return clampedCursor.isCursorHoveringOver(entity);
   }

   public int getPredictedDamage (SeaEntity enemy) {
      if (Global.player == null || !(Global.player is SeaEntity) || SeaManager.selectedAttackType == Attack.Type.Air 
         || Global.player == enemy || !isHoveringOver(enemy)) {
         return 0;
      } else {
         return ((SeaEntity) Global.player).getDamageForShot(SeaManager.selectedAttackType, 
            ShipEntity.getDamageModifierForDistance(_normalizedDistanceToCursor));
      }
   }

   public Color getColorForDistance(float normalizedDistance) {
      return colorOverDistance.Evaluate(normalizedDistance);
   }

   public float getNormalizedDistanceToCursor () {
      return _normalizedDistanceToCursor;
   }

   public static float getArcHeight (Vector2 startPos, Vector2 endPos, float lerpTime, bool applyHeightModifier) {
      float attackDistance = Vector2.Distance(startPos, endPos);
      float distanceModifier = Mathf.Clamp(attackDistance, .25f, 2f);
      float heightModifier = 1f;
      if (applyHeightModifier) {
         heightModifier = Mathf.Clamp(attackDistance, PlayerShipEntity.MIN_RANGE, 1.5f) / 1.5f;
      }
      float angleInDegrees = lerpTime * 180f;
      float ballHeight = Util.getSinOfAngle(angleInDegrees) * distanceModifier * .5f * heightModifier;

      return ballHeight;
   }

   #region Private Variables

   // The attack range of the player's ship entity
   private float _attackRange = 0;

   // The current distance between the player and the aiming cursor, normalized to the max range
   private float _normalizedDistanceToCursor = 0f;

   #endregion
}
