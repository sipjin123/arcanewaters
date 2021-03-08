using UnityEngine;
using System;
using System.Collections.Generic;
using MapCreationTool.Serialization;
using MapCreationTool;

public class GenericActionTrigger : MonoBehaviour, IMapEditorDataReceiver
{
   public enum InteractionType
   {
      Enter = 1,
      Exit = 2,
      Stay = 3
   }

   #region Public Variables

   // Hardcoded action strings
   public static string WARP_TO_LEAGUE_ACTION = "Warp To League";

   // The list of actions that can be defined and triggered
   public static Dictionary<string, Action<NetEntity>> actions = new Dictionary<string, Action<NetEntity>> {
      { "Voyage Panel", showVoyagePanel },
      { WARP_TO_LEAGUE_ACTION, warpToLeague },
      { "Exit League", exitLeague }
   };

   // The type of interaction that is needed to trigger the action
   public InteractionType interactionType;

   // The direction pointed by the arrow
   public Direction arrowDirection = Direction.South;

   // The name of the action that should be triggered
   public string actionName;

   // Arrow that is showed if this is a voyage trigger region
   public GameObject voyageArrow;

   #endregion

   private static void showVoyagePanel (NetEntity entity) {
      VoyageManager.self.showVoyagePanel(entity);
   }

   private static void warpToLeague (NetEntity entity) {
      VoyageManager.self.warpToLeague(entity);
   }

   private static void exitLeague (NetEntity entity) {
      VoyageManager.self.returnToTownFromLeague(entity);
   }

   private void Awake () {
      _collider = GetComponent<BoxCollider2D>();
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         switch (field.k.ToLower()) {
            case DataField.GENERIC_ACTION_TRIGGER_INTERACTION_TYPE:
               if (field.tryGetInteractionTypeValue(out InteractionType value)) {
                  interactionType = value;
               }
               break;
            case DataField.GENERIC_ACTION_TRIGGER_ACTION_NAME:
               actionName = field.v.Trim(' ');
               break;
            case DataField.GENERIC_ACTION_TRIGGER_ARROW_DIRECTION:
               if (field.tryGetDirectionValue(out Direction dir)) {
                  arrowDirection = dir;
               }
               break;
            case DataField.GENERIC_ACTION_TRIGGER_WIDTH_KEY:
               _collider.size = new Vector2(field.floatValue, _collider.size.y);
               break;
            case DataField.GENERIC_ACTION_TRIGGER_HEIGHT_KEY:
               _collider.size = new Vector2(_collider.size.x, field.floatValue);
               break;
         }
      }

      // Configure the optional arrow
      voyageArrow.SetActive(actionName.ToLower().Contains("voyage") || actionName.ToLower().Contains("league"));
      updateArrow();
   }

   /// <summary>
   /// Updates the arrow color and direction, based on this area type and target area type
   /// </summary>
   public void updateArrow () {
      string dir = arrowDirection.ToString().ToLower();
      string color = "unrecognized";

      string thisArea = transform.GetComponentInParent<Area>()?.areaKey;
      if (thisArea != null) {
         if (AreaManager.self.isSeaArea(thisArea)) {
            color = "blue";
         } else {
            color = "gold";
         }
      }

      string spriteName = $"warp_{color}_{dir}";

      Sprite arrowSprite = ImageManager.getSprite("Map/Warp Arrows/" + spriteName);
      if (arrowSprite != null) {
         SpriteRenderer ren = voyageArrow.GetComponent<SpriteRenderer>();
         if (ren != null) {
            ren.sprite = arrowSprite;
         }
      } else {
         D.warning("Could not find sprite for warp arrow. Target sprite name: " + spriteName);
      }

      voyageArrow.transform.localPosition = -DirectionUtil.getVectorForDirection(arrowDirection);
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      NetEntity entity = collision.GetComponent<NetEntity>();

      if (entity != null && interactionType == InteractionType.Enter && canActivateTrigger(entity)) {
         if (actions.TryGetValue(actionName, out Action<NetEntity> action)) {
            action.Invoke(entity);
         }
      }
   }

   private void OnTriggerExit2D (Collider2D collision) {
      NetEntity entity = collision.GetComponent<NetEntity>();

      if (entity != null && interactionType == InteractionType.Exit && canActivateTrigger(entity)) {
         if (actions.TryGetValue(actionName, out Action<NetEntity> action)) {
            action.Invoke(entity);
         }
      }
   }

   private void OnTriggerStay2D (Collider2D collision) {
      NetEntity entity = collision.GetComponent<NetEntity>();

      if (entity != null && interactionType == InteractionType.Stay && canActivateTrigger(entity)) {
         if (actions.TryGetValue(actionName, out Action<NetEntity> action)) {
            action.Invoke(entity);
         }
      }
   }

   private bool canActivateTrigger (NetEntity entity) {
      bool hasAlreadyTriggeredThisFrame = false;

      // Ignore the trigger if it has already been activated this frame
      if (_lastTriggerTime.TryGetValue(entity, out float lastActivationTime)) {
         if (Time.time - lastActivationTime < 0.1f) {
            hasAlreadyTriggeredThisFrame = true;
         }
      }

      _lastTriggerTime[entity] = Time.time;

      return !hasAlreadyTriggeredThisFrame;
   }

   #region Private Variables

   // The collider, which triggers the action
   private BoxCollider2D _collider;

   // The last time an entity has activated the trigger
   private Dictionary<NetEntity, float> _lastTriggerTime = new Dictionary<NetEntity, float>();

   #endregion
}
