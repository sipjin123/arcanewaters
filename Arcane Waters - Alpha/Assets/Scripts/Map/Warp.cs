﻿using UnityEngine;
using MapCreationTool.Serialization;
using MapCreationTool;

public class Warp : MonoBehaviour, MapCreationTool.IMapEditorDataReceiver {
   #region Public Variables

   // The area for this warp
   public string areaTarget;

   // The spawn for this warp
   public string spawnTarget;

   // The facing direction we should have after spawning
   public Direction newFacingDirection = Direction.South;

   // Hard coded quest index
   public const int GET_DRESSED_QUEST_INDEX = 1;
   public const int HEAD_TO_DOCKS_QUEST_INDEX = 8;
   public const int ENTER_TREASURE_SITE_QUEST_INDEX = 14;

   #endregion

   void Awake() {
      _collider = GetComponent<BoxCollider2D>();
   }

   void OnTriggerEnter2D (Collider2D other) {
      NetEntity player = other.transform.GetComponent<NetEntity>();

      // Make sure the player meets the requirements to use this warp
      if (player == null || !meetsRequirements(player)) {
         return;
      }

      // If a player entered this warp on the server, move them
      if (player.isServer && player.connectionToClient != null) {
         Spawn spawn = SpawnManager.self.getSpawn(areaTarget, spawnTarget);
         player.spawnInNewMap(spawn.AreaKey, spawn, newFacingDirection);
      }
   }

   protected bool meetsRequirements (NetEntity player) {
      Spawn spawn = SpawnManager.self.getSpawn(areaTarget, spawnTarget);
      int currentStep = TutorialManager.getHighestCompletedStep(player.userId) + 1;
      TutorialData currTutData = TutorialManager.self.fetchTutorialData(currentStep);
      string currArea = "none";
      if (currTutData != null && currTutData.requirementType == RequirementType.Area) {
         currArea = currTutData.rawDataJson;
      }

      // We can't warp to the sea until we've gotten far enough into the tutorial
      if (AreaManager.self.getArea(spawn.AreaKey)?.isSea == true && currentStep <= HEAD_TO_DOCKS_QUEST_INDEX) {
         return false;
      }

      // Requires the player to finish the first quest requirement
      if (Spawn.HOUSE_EXIT.Equals(spawnTarget) && currentStep == GET_DRESSED_QUEST_INDEX) {
         if (player.connectionToClient != null) {
            ServerMessageManager.sendError(ErrorMessage.Type.Misc, player, "You need to get dressed before leaving the house!");
         }
         return false;
      }

      // We can't warp into the treasure site until we clear the gate
      if (Area.isTreasureSite(spawn.AreaKey) && currentStep < ENTER_TREASURE_SITE_QUEST_INDEX) {
         return false;
      }

      return true;
   }

   public void receiveData (MapCreationTool.Serialization.DataField[] dataFields) {
      foreach (MapCreationTool.Serialization.DataField field in dataFields) {
         switch (field.k.ToLower()) {
            case "target map":
               areaTarget = field.v.Trim(' ');
               break;
            case "target spawn":
               spawnTarget = field.v.Trim(' ');
               break;
            case "width":
               _collider.size = new Vector2(float.Parse(field.v), _collider.size.y);
               break;
            case "height":
               _collider.size = new Vector2(_collider.size.x, float.Parse(field.v));
               break;
            case "arrive facing":
               Direction? dir = MapImporter.ParseDirection(field.v);
               if (dir != null)
                  newFacingDirection = dir.Value;
               break;                 
            default:
               Debug.LogWarning($"Unrecognized data field key: {field.k}");
               break;
         }
      }
   }


   #region Private Variables

   // The the collider, which will trigger the warp to activate
   protected BoxCollider2D _collider;

   #endregion
}
