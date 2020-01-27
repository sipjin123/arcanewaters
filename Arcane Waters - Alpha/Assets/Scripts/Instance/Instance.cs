using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Instance : NetworkBehaviour
{
   #region Public Variables

   // The id of this instance
   [SyncVar]
   public int id;

   // The key determining the type of area this instance is
   [SyncVar]
   public string areaKey;

   // The type of Biome this Instance is
   [SyncVar]
   public Biome.Type biomeType = Biome.Type.None;

   // The seed used for making based on pseudo-random numbers on clients
   [SyncVar]
   public int mapSeed;

   // The list of Entities in this instance (server only)
   public List<NetworkBehaviour> entities = new List<NetworkBehaviour>();

   // For debugging in the Editor
   [SyncVar]
   public int entityCount;

   // The number assigned to this instance based on the area type
   [SyncVar]
   public int numberInArea;

   // The server address for this Instance
   [SyncVar]
   public string serverAddress;

   // The server port for this Instance
   [SyncVar]
   public int serverPort;

   // Our network ident
   public NetworkIdentity netIdent;

   #endregion

   public void Awake () {
      // Look up components
      netIdent = GetComponent<NetworkIdentity>();
   }

   private void Start () {
      // We only spawn Bots on the Server
      if (NetworkServer.active) {
         Area area = AreaManager.self.getArea(this.areaKey);
         List<BotSpot> spots = BotManager.self.getSpots(area.areaKey);

         foreach (BotSpot spot in spots) {
            Vector2 spawnPosition = spot.transform.position;

            // Figure out which waypoint is the closest
            if (spot.route != null) {
               spawnPosition = spot.route.getClosest(spawnPosition).transform.position;
            }

            BotShipEntity bot = Instantiate(spot.prefab, spawnPosition, Quaternion.identity);
            bot.instanceId = this.id;
            bot.areaKey = area.areaKey;
            bot.npcType = spot.npcType;
            bot.faction = NPC.getFactionFromType(bot.npcType);
            bot.route = spot.route;
            bot.nationType = spot.nationType;
            bot.maxForceOverride = spot.maxForceOverride;
            bot.speed = Ship.getBaseSpeed(Ship.Type.Caravel);
            bot.attackRangeModifier = Ship.getBaseAttackRange(Ship.Type.Caravel);
            bot.entityName = "Bot";
            this.entities.Add(bot);

            // Spawn the bot on the Clients
            NetworkServer.Spawn(bot.gameObject);
         }
      }

      // Routinely check if the instance is empty
      InvokeRepeating("checkIfInstanceIsEmpty", 10f, 30f);
   }

   public int getPlayerCount() {
      int count = 0;

      foreach (NetworkBehaviour entity in entities) {
         if (entity != null  && (entity is PlayerBodyEntity || entity is PlayerShipEntity)) {
            count++;
         }
      }

      return count;
   }

   public int getMaxPlayers () {
      if (Area.FARM.Equals(areaKey) || Area.HOUSE.Equals(areaKey)) {
         return 1;
      }

      return 50;
   }

   public void removeEntityFromInstance (NetworkBehaviour entity) {
      if (entities.Contains(entity)) {
         this.entities.Remove(entity);
      }
   }

   protected void checkIfInstanceIsEmpty () {
      // We only do this on the server
      if (!NetworkServer.active) {
         return;
      }

      // If there's no one in the instance right now, increase the  count
      if (getPlayerCount() <= 0) {
         _consecutiveEmptyChecks++;

         // If the Instance has been empty for long enough, just remove it
         if (_consecutiveEmptyChecks > 10) {
            InstanceManager.self.removeEmptyInstance(this);
         }

      } else {
         // There's someone in the instance, so reset the counter
         _consecutiveEmptyChecks = 0;
      }
   }

   #region Private Variables

   // The number of consecutive times we've checked this instance and found it empty
   protected int _consecutiveEmptyChecks = 0;

   #endregion
}
