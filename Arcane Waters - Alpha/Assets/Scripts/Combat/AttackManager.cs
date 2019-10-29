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

   // The circle that shows the zone were the attack is allowed
   public AttackRangeCircle attackRangeCircle;

   // The currently selected attack target point
   public Vector2 targetPoint;

   // A prefab we use for creating moving dots
   public MovingDot dotPrefab;

   // Self
   public static AttackManager self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   private void Start() {
      // Initialize the attack range circle
      attackRangeCircle.initialize(5f);
      attackRangeCircle.hide();

      // Continually create dots moving towards our current attack target
      InvokeRepeating("createMovingDot", 0f, .2f);
   }

   void Update () {
      if (Global.player == null || Global.player.isDead() || !(Global.player is ShipEntity) ||
         SeaManager.combatMode != SeaManager.CombatMode.Circle || SeaManager.selectedAttackType == Attack.Type.Air) {
         attackCircleClampedIndicator.enabled = false;
         attackCircleFreeIndicator.enabled = false;
         attackRangeCircle.hide();
         return;
      }

      // Check if the right mouse is being held down
      bool rightMouseDown = Input.GetMouseButton(1);

      // Only show the indicators while the right mouse is down
      attackCircleClampedIndicator.enabled = rightMouseDown;
      attackCircleFreeIndicator.enabled = rightMouseDown;
      if (rightMouseDown) {
         attackRangeCircle.show();
         aimCursor.activate();
      } else {
         attackRangeCircle.hide();
         aimCursor.deactivate();
      }

      // Update the range indicator if the range has changed
      ShipEntity shipEntity = (ShipEntity) Global.player;
      if (shipEntity.attackRange != _attackRange) {
         _attackRange = shipEntity.attackRange;
         attackRangeCircle.draw(_attackRange);
         attackRangeCircle.show();
      }

      // Move the range indicator to the player position
      Util.setXY(attackRangeCircle.transform, Global.player.transform.position);

      // By default, show the cursor
      Cursor.visible = true;

      // If the mouse button isn't down, we don't have to do anything else
      if (!rightMouseDown) {
         return;
      }

      // Hide the cursor when the right mouse is down
      Cursor.visible = !rightMouseDown;

      // Figure out where the mouse is in world coordinates
      targetPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);

      // Move the free aim indicator to the mouse position
      Util.setXY(attackCircleFreeIndicator.transform, targetPoint);

      // Move the clamped aim indicator to the mouse position - clamped inside the range zone
      Util.setXY(attackCircleClampedIndicator.transform,
         clampToRange(Global.player.transform.position, targetPoint, _attackRange));

      // Update the sprite based on whether it's in position
      //attackCircleClampedIndicator.sprite = isInValidSpot() ? enabledAttack : disabledAttack;
   }

   public bool isHoveringOver (NetEntity entity) {
      return aimCursor.isCursorHoveringOver(entity);
   }

   public static Vector2 clampToRange (Vector2 entityPosition, Vector2 targetPoint, float range) {
      Vector2 relativePosition = (targetPoint - entityPosition);
      Vector2 clampedRelativePosition = Vector2.ClampMagnitude(relativePosition, range);
      return entityPosition + clampedRelativePosition;
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
   private float _attackRange = 0f;

   #endregion
}
