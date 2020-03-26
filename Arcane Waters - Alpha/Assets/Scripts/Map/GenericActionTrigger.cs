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

   // The list of actions that can be defined and triggered
   public static Dictionary<string, Action<NetEntity>> actions = new Dictionary<string, Action<NetEntity>> {
      { "test debug1", testDebugLog },
      { "test debug2", (e) => testDebugLog(e, "Other test log") },
      { "Voyage Panel", showVoyagePanel }
   };

   // The type of interaction that is needed to trigger the action
   public InteractionType interactionType;

   // The name of the action that should be triggered
   public string actionName;

   #endregion

   private static void testDebugLog (NetEntity entity) {
      Debug.Log("Test action - debug log");
   }

   private static void testDebugLog (NetEntity entity, string message) {
      Debug.Log(message);
   }

   private static void showVoyagePanel (NetEntity entity) {
      VoyageManager.self.showVoyagePanel(entity);
   }

   private void Awake () {
      _collider = GetComponent<BoxCollider2D>();
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         switch (field.k.ToLower()) {
            case DataField.GENERIC_ACTION_TRIGGER_INTERACTION_TYPE:
               interactionType = MapImporter.parseInteractionType(field.v);
               break;
            case DataField.GENERIC_ACTION_TRIGGER_ACTION_NAME:
               actionName = field.v.Trim(' ');
               break;
            case DataField.GENERIC_ACTION_TRIGGER_WIDTH_KEY:
               _collider.size = new Vector2(float.Parse(field.v), _collider.size.y);
               break;
            case DataField.GENERIC_ACTION_TRIGGER_HEIGHT_KEY:
               _collider.size = new Vector2(_collider.size.x, float.Parse(field.v));
               break;
            default:
               Debug.LogWarning($"Unrecognized data field key: {field.k}");
               break;
         }
      }
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      NetEntity entity = collision.GetComponent<NetEntity>();

      if (entity != null && interactionType == InteractionType.Enter) {
         if (actions.TryGetValue(actionName, out Action<NetEntity> action)) {
            action.Invoke(entity);
         }
      }
   }

   private void OnTriggerExit2D (Collider2D collision) {
      NetEntity entity = collision.GetComponent<NetEntity>();

      if (entity != null && interactionType == InteractionType.Exit) {
         if (actions.TryGetValue(actionName, out Action<NetEntity> action)) {
            action.Invoke(entity);
         }
      }
   }

   private void OnTriggerStay2D (Collider2D collision) {
      NetEntity entity = collision.GetComponent<NetEntity>();

      if (entity != null && interactionType == InteractionType.Stay) {
         if (actions.TryGetValue(actionName, out Action<NetEntity> action)) {
            action.Invoke(entity);
         }
      }
   }

   #region Private Variables

   // The collider, which triggers the action
   private BoxCollider2D _collider;

   #endregion
}
