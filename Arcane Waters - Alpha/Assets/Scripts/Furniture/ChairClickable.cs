using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ChairClickable : MonoBehaviour
{
   #region Public Variables

   // How close we have to be in order to sit down
   public static float SIT_DISTANCE = .32f;

   // The direction of the chair (only cardinal direction are supported)
   [Tooltip("Ignored by Stools.")]
   public Direction direction = Direction.North;

   // The type of chair
   public ChairType chairType = ChairType.Chair;

   // The collider used to check for obstacles on the right side of the chair
   public Collider2D eastObstructionCollider;

   // The collider used to check for obstacles on the left side of the chair
   public Collider2D westObstructionCollider;

   // The collider used to check for possible objects on the chair
   public Collider2D occupiedCollider;

   // The types of chair
   public enum ChairType
   {
      // None
      None = 0,

      // Chair
      Chair = 1,

      // Stool
      Stool = 2
   }

   #endregion

   private void Awake () {
      // Look up components
      _outline = GetComponentInChildren<SpriteOutline>();
      _clickableBox = GetComponentInChildren<ClickableBox>();
   }

   private void Start () {
      if (_clickableBox != null) {
         _clickableBox.mouseButtonUp += onClickableBoxMouseButtonUp;
      }
   }

   public void Update () {
      if (Util.isBatch()) {
         return;
      }

      // Figure out whether our outline should be showing
      handleSpriteOutline();
   }

   private void onClickableBoxMouseButtonUp (MouseButton button) {
      onClick();
   }

   public void onClick () {
      // Don't interact with chairs when we are customizing
      if (MapCustomizationManager.tryGetCurentLocalManager(out MapCustomizationManager manager) && manager.isLocalPlayerCustomizing) {
         return;
      }

      if (Global.player == null || Global.player.getPlayerBodyEntity() == null) {
         return;
      }

      PlayerBodyEntity body = Global.player.getPlayerBodyEntity();

      if (body.isJumping() || body.isEmoting() || body.isSitting()) {
         return;
      }

      if (isTooFarFromPlayer()) {
         FloatingCanvas.instantiateAt(transform.position + new Vector3(0f, 0f)).asTooFar();
         return;
      }

      Direction[] obstructedDirections = getObstructedDirections();
      if (chairType == ChairType.Chair && obstructedDirections.Contains(direction)) {
         FloatingCanvas.instantiateAt(transform.position + new Vector3(0f, 0f)).asCustomMessage("No Space!");
         return;
      }

      if (chairType == ChairType.Stool && isHoldingSomething()) {
         FloatingCanvas.instantiateAt(transform.position + new Vector3(0f, 0f)).asCustomMessage("No Space!");
         return;
      }

      if (isOccupied()) {
         return;
      }

      if (chairType == ChairType.Chair) {
         Global.player.getPlayerBodyEntity().enterChair(transform.position, direction, chairType);
      } else if (chairType == ChairType.Stool) {
         Vector2 playerPos = new Vector2(Global.player.transform.position.x, Global.player.transform.position.y);
         Vector2 chairPos = new Vector2(transform.position.x, transform.position.y);
         Direction? computedDirection = Util.getMajorDirectionFromVector(playerPos - chairPos);
         Global.player.getPlayerBodyEntity().enterChair(transform.position, computedDirection.HasValue ? computedDirection.Value : Direction.East, chairType);
      }
   }

   private bool isTooFarFromPlayer () {
      return Vector2.Distance(transform.position, Global.player.transform.position) > SIT_DISTANCE;
   }

   private Direction[] getObstructedDirections () {
      List<Direction> obstructedDirections = new List<Direction>();
      Collider2D[] obstructors = new[] { eastObstructionCollider, westObstructionCollider };

      foreach (Collider2D obsCollider in obstructors) {
         if (obsCollider == null) {
            continue;
         }

         Collider2D[] colliders = new Collider2D[10];
         int collidersCount = Physics2D.OverlapCollider(obsCollider, new ContactFilter2D(), colliders);

         // Filter the result
         foreach (Collider2D collider in colliders) {
            if (collider != null) {
               // Ignore the chair itself, players
               if (collider.transform.IsChildOf(transform) ||
                  collider.GetComponentInParent<PlayerBodyEntity>() != null) {
                  collidersCount -= 1;
               }
            }
         }

         // If possible obstacles are found, add the corresponding direction to the list
         if (collidersCount > 0) {
            if (obsCollider == eastObstructionCollider) {
               obstructedDirections.Add(Direction.East);
            } else if (obsCollider == westObstructionCollider) {
               obstructedDirections.Add(Direction.West);
            }
         }
      }

      return obstructedDirections.ToArray();
   }

   private bool isHoldingSomething () {
      if (occupiedCollider == null) {
         return false;
      }

      Collider2D[] colliders = new Collider2D[10];
      int collidersCount = Physics2D.OverlapCollider(occupiedCollider, new ContactFilter2D { useTriggers = true }, colliders);

      foreach (Collider2D collider in colliders) {
         if (collider == null) {
            continue;
         }

         // Ignore the chair itself, players and the camera bounds
         if (collider.transform.IsChildOf(transform) ||
            collider.GetComponentInParent<PlayerBodyEntity>() != null ||
            collider.TryGetComponent(out MapCameraBounds bounds)) {
            collidersCount -= 1;
         }
      }

      return collidersCount > 0;
   }

   private bool isOccupied () {
      // The chair is considered occupied if any player is very close to the chair and is sitting
      List<NetEntity> entities = EntityManager.self.getAllEntities();

      foreach (NetEntity entity in entities) {
         if (entity is PlayerBodyEntity body) {
            if (body.isSitting() && Util.areVectorsAlmostTheSame(transform.position, body.sittingInfo.chairPosition)) {
               return true;
            }
         }
      }

      return false;
   }

   private void handleSpriteOutline () {
      if ((MapCustomizationManager.tryGetCurentLocalManager(out var manager) && manager.isLocalPlayerCustomizing) || _outline == null || _clickableBox == null || MouseManager.self == null) {
         return;
      }

      // Only show our outline when the mouse is over us and player isn't in customization mode
      bool isHovering = MouseManager.self.isHoveringOver(_clickableBox);
      _outline.setNewColor(Color.white);
      _outline.setVisibility(isHovering);
   }

   private void OnDestroy () {
      if (_clickableBox != null) {
         _clickableBox.mouseButtonUp -= onClickableBoxMouseButtonUp;
      }
   }

   #region Private Variables

   // Outline of clock object
   protected SpriteOutline _outline;

   // Button which is used to check if mouse is above it
   protected ClickableBox _clickableBox;

   #endregion
}