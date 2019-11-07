using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AttackManager : ClientMonoBehaviour {
   #region Public Variables

   // The Game Object that shows where we would attack - clamped to the attack zone
   public SpriteRenderer attackCircleClampedIndicator;

   // The Game Object that shows where we would attack - free to move anywhere
   public SpriteRenderer attackCircleFreeIndicator;

   // The cursor that highlights enemies being targeted
   public AttackCircleAimCursor aimCursor;

   // The circles that shows the targeted zones
   public AttackZoneCircles attackZoneCircles;

   // The line that shows the trajectory of the projectile
   public AttackTrajectory trajectory;

   // The container for the scheduled shots indicators
   public Transform scheduledShotIndicatorsContainer;

   // A prefab we use for creating moving dots
   public MovingDot dotPrefab;

   // The prefab we use for creating sheduled shots indicators
   public GameObject scheduledShotIndicatorPrefab;

   // Self
   public static AttackManager self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;

      // Create the scheduled shots indicators
      for (int i = 0; i < PlayerShipEntity.MAX_SCHEDULED_SHOTS; i++) {
         GameObject indicator = Instantiate(scheduledShotIndicatorPrefab, scheduledShotIndicatorsContainer);
         _scheduledShotIndicators.Add(indicator);
         indicator.SetActive(false);
      }
   }

   void Update () {
      if (Global.player == null || Global.player.isDead() || !(Global.player is ShipEntity) ||
         SeaManager.combatMode != SeaManager.CombatMode.Circle || SeaManager.selectedAttackType == Attack.Type.Air) {
         attackCircleClampedIndicator.enabled = false;
         attackCircleFreeIndicator.enabled = false;
         attackZoneCircles.hide();
         trajectory.hide();
         aimCursor.deactivate();
         Cursor.visible = true;
         return;
      }

      // Update the range circles if the range has changed
      PlayerShipEntity playerShipEntity = (PlayerShipEntity) Global.player;
      if (playerShipEntity.getAttackRange() != _attackRange) {
         _attackRange = playerShipEntity.getAttackRange();
         attackZoneCircles.draw(_attackRange);
      }

      // Move the range circle to the player position
      Util.setXY(attackZoneCircles.transform, Global.player.transform.position);

      // Hide the cursor when the right mouse is down
      Cursor.visible = !Input.GetMouseButton(1);

      // Hide all the indicators
      attackCircleClampedIndicator.enabled = false;
      attackCircleFreeIndicator.enabled = false;
      trajectory.hide();
      aimCursor.deactivate();

      // Check if the mouse right button is pressed
      if (Input.GetMouseButton(1)) {
         // Move the clamped aim indicator to the mouse position - clamped inside the range zone
         Util.setXY(attackCircleClampedIndicator.transform,
            playerShipEntity.clampToRange(Util.getMousePos()));

         // Determine the targeted attack zone
         AttackZone.Type attackZone = playerShipEntity.getAttackZone(Util.getMousePos());

         // Move the trajectory to the player position
         Util.setXY(trajectory.transform, Global.player.transform.position);

         // Draw the trajectory
         trajectory.draw(Global.player.transform.position, attackCircleClampedIndicator.transform.position, attackZone);

         // Move the free aim indicator to the mouse position
         Util.setXY(attackCircleFreeIndicator.transform, Util.getMousePos());

         // Display the range circles for the targeted attack zone
         attackZoneCircles.show(attackZone);

         // Display the relevant indicators
         attackCircleClampedIndicator.enabled = true;
         attackCircleFreeIndicator.enabled = true;
         trajectory.show();
         aimCursor.activate();
      }

      // Hide all the scheduled shot indicators
      foreach (GameObject indicator in _scheduledShotIndicators) {
         indicator.SetActive(false);
      }

      // Check if there are scheduled shots
      if (playerShipEntity.scheduledShots.Count > 0) {
         // Place an indicator at each scheduled shot position
         int k = 0;
         foreach (Vector2 position in playerShipEntity.scheduledShots) {
            _scheduledShotIndicators[k].SetActive(true);
            _scheduledShotIndicators[k].transform.position = position;
            k++;
         }
      }

      // Hide the attack range circles if not relevant
      if (!Input.GetMouseButton(1)) {
         attackZoneCircles.hide();
      }
   }

   public bool isHoveringOver (NetEntity entity) {
      return aimCursor.isCursorHoveringOver(entity);
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

   // The attack range of the player's ship entity
   private float _attackRange = 0;

   // The list of target indicators for scheduled shots
   private List<GameObject> _scheduledShotIndicators = new List<GameObject>();

   #endregion
}
