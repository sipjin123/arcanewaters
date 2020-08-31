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
      // Disable the sea combat UI when not relevant
      if (Global.player == null || Global.player.isDead() || !(Global.player is ShipEntity) ||
         SeaManager.combatMode != SeaManager.CombatMode.Circle || SeaManager.getAttackType() == Attack.Type.Air ||
         PanelManager.self.hasPanelInStack()) {
         freeCursor.enabled = false;
         maxRangeCircle.hide();
         clampedCursor.deactivate();
         trajectory.hide();
         Cursor.visible = true;
         return;
      }

      // Get the player ship
      PlayerShipEntity playerShipEntity = (PlayerShipEntity) Global.player;

      // Check if the player is aiming a shot
      bool isAiming = Input.GetMouseButton(1) && !VoyageGroupPanel.self.isMouseOverAnyMemberCell();

      // Hide the cursor when the player is aiming
      Cursor.visible = !isAiming;

      // Get the mouse position
      Vector2 mousePosition = Util.getMousePos();      

      // Show or hide the attack UI
      if (isAiming) {
         // Move the clamped aim indicator to the mouse position - clamped inside the range zone
         Util.setXY(clampedCursor.transform, playerShipEntity.clampToRange(mousePosition));

         // Determine the modifier for the target
         _normalizedDistanceToCursor = playerShipEntity.getNormalizedTargetDistance(mousePosition);

         // Determine the color of the indicators
         Color indicatorColor = getColorForDistance(_normalizedDistanceToCursor);

         // Set the color and animation speed of the cursor
         clampedCursor.update(indicatorColor, 0.5f + 0.5f / _normalizedDistanceToCursor);

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

      // Update the max range circle
      maxRangeCircle.update();

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
      if (Global.player == null || !(Global.player is SeaEntity) || SeaManager.getAttackType() == Attack.Type.Air 
         || Global.player == enemy || !enemy.isAttackCursorOver() || Global.player.isAllyOf(enemy)
         || Global.player.isAdversaryInPveInstance(enemy)) {
         return 0;
      } else {
         return ((SeaEntity) Global.player).getDamageForShot(SeaManager.getAttackType(), 
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

   // The current distance between the player and the aiming cursor, normalized to the max range
   private float _normalizedDistanceToCursor = 0f;

   #endregion
}
