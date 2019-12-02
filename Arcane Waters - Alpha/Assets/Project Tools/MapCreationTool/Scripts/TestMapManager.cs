using UnityEngine;
using System.Linq;
public class TestMapManager : MonoBehaviour
{

   private bool holdingControl = false;
   public void Update () {
      if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
         holdingControl = true;
      else if (Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl))
         holdingControl = false;

      if (holdingControl) {
         if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) {
            var player = Global.player;
            // If a player entered this warp on the server, move them
            if (player.isServer && player.connectionToClient != null) {
               Spawn spawn = SpawnManager.self.getSpawn(SpawnManager.self.startingLocationAreaKey, SpawnManager.self.startingLocationSpawnKey);
               Debug.Log("Starting warp to target area: " + spawn.AreaKey);
               player.spawnInNewMap(spawn.AreaKey, spawn, Direction.South);
            }
         } else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) {
            var player = Global.player;
            // If a player entered this warp on the server, move them
            if (player.isServer && player.connectionToClient != null) {
               Spawn spawn = FindObjectsOfType<Spawn>().FirstOrDefault(s => s.AreaKey == "TonyTest1");
               if (spawn != null) {
                  Debug.Log("Starting warp to target area: " + spawn.AreaKey);
                  player.spawnInNewMap(spawn.AreaKey, spawn, Direction.South);
               }
            }
         } else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) {
            var player = Global.player;
            // If a player entered this warp on the server, move them
            if (player.isServer && player.connectionToClient != null) {
               Spawn spawn = FindObjectsOfType<Spawn>().FirstOrDefault(s => s.AreaKey == "CollisionTestMap");
               if (spawn != null) {
                  Debug.Log("Starting warp to target area: " + spawn.AreaKey);
                  player.spawnInNewMap(spawn.AreaKey, spawn, Direction.South);
               }
            }
         } else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) {
            var player = Global.player;
            // If a player entered this warp on the server, move them
            if (player.isServer && player.connectionToClient != null) {
               Spawn spawn = FindObjectsOfType<Spawn>().FirstOrDefault(s => s.AreaKey == "PogsideOasis");
               if (spawn != null) {
                  Debug.Log("Starting warp to target area: " + spawn.AreaKey);
                  player.spawnInNewMap(spawn.AreaKey, spawn, Direction.South);
               }
            }
         } else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) {
            var player = Global.player;
            // If a player entered this warp on the server, move them
            if (player.isServer && player.connectionToClient != null) {
               Spawn spawn = SpawnManager.get().getSpawn("Andrius Test", "main");
               if (spawn != null) {
                  Debug.Log("Starting warp to target area: " + spawn.AreaKey);
                  player.spawnInNewMap(spawn.AreaKey, spawn, Direction.South);
               }
            }
         }
      }
   }
}
