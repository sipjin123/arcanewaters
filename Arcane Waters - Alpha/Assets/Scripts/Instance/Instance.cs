using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Instance : Photon.PunBehaviour {
   #region Public Variables

   // The id of this instance
   public int id;

   // The type of Area this Instance is
   public Area.Type areaType;

   // The list of Entities in this instance
   public List<NetworkBehaviour> entities = new List<NetworkBehaviour>();

   // For debugging in the Editor
   public int entityCount;

   // The number assigned to this instance based on the area type
   public int numberInArea;

   #endregion

   private void Awake () {
      _view = GetComponent<PhotonView>();
   }

   private void Start () {
      if (NetworkServer.active) {
         Area area = AreaManager.self.getArea(this.areaType);
         List<BotSpot> spots = BotManager.self.getSpots(area.areaType);

         foreach (BotSpot spot in spots) {
            Vector2 spawnPosition = spot.transform.position;

            // Figure out which waypoint is the closest
            if (spot.route != null) {
               spawnPosition = spot.route.getClosest(spawnPosition).transform.position;
            }

            BotShipEntity bot = Instantiate(spot.prefab, spawnPosition, Quaternion.identity);
            bot.instanceId = this.id;
            bot.areaType = area.areaType;
            bot.npcType = spot.npcType;
            bot.faction = NPC.getFaction(bot.npcType);
            bot.route = spot.route;
            bot.nationType = spot.nationType;
            bot.maxForceOverride = spot.maxForceOverride;
            bot.speed = Ship.getBaseSpeed(Ship.Type.Caravel);
            bot.entityName = "Bot";
            this.entities.Add(bot);

            // Spawn the bot on the Clients
            NetworkServer.Spawn(bot.gameObject);
         }
      }

      // Routinely check if the instance is empty
      InvokeRepeating("checkIfInstanceIsEmpty", 10f, 30f);
   }

   void OnPhotonSerializeView (PhotonStream stream, PhotonMessageInfo info) {
      if (stream.isWriting) {
         // We own this object: send the others our data 
         stream.SendNext(id);
         stream.SendNext(areaType);
         stream.SendNext(entities.Count);
      } else {
         // Someone else owns this object, receive data 
         id = (int) stream.ReceiveNext();
         this.name = (string) stream.ReceiveNext();
         this.entityCount = (int) stream.ReceiveNext();
      }
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
      if (areaType == Area.Type.Farm || areaType == Area.Type.House) {
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

   // Our Photon View
   protected PhotonView _view;

   // The number of consecutive times we've checked this instance and found it empty
   protected int _consecutiveEmptyChecks = 0;

   #endregion
}
