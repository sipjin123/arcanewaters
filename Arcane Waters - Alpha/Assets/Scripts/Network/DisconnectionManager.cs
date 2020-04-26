using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class DisconnectionManager : MonoBehaviour
{
   #region Public Variables

   // The number of seconds a player remains active in the server after the client suddenly disconnects
   public static float SECONDS_UNTIL_PLAYERS_DESTROYED = 10f;

   // Self
   public static DisconnectionManager self;

   #endregion

   public void Awake () {
      self = this;
   }

   public void Start () {
      InvokeRepeating("destroyDisconnectedUsers", 5.5f, 1f);
   }

   public void addToDisconnectedUsers (NetEntity player) {
      _disconnectedPlayers.Add(player, DateTime.UtcNow);
   }

   public void removeFromDisconnectedUsers (NetEntity player) {
      _disconnectedPlayers.Remove(player);
   }

   public void clearDisconnectedUsers () {
      _disconnectedPlayers.Clear();
   }

   public void reconnectDisconnectedUser (UserInfo userInfo, ShipInfo shipInfo) {
      // Check if the player disconnected a few seconds ago and its object is still in the server
      NetEntity disconnectedPlayer = null;
      foreach (NetEntity entity in _disconnectedPlayers.Keys) {
         if (entity.userId == userInfo.userId) {
            disconnectedPlayer = entity;
            break;
         }
      }

      if (disconnectedPlayer != null) {
         // Retrieve the player attributes that are saved in DB when the player is destroyed
         userInfo.localPos = disconnectedPlayer.transform.localPosition;
         userInfo.facingDirection = (int) disconnectedPlayer.facing;
         shipInfo.health = disconnectedPlayer.currentHealth;

         // Destroy the player object
         MyNetworkManager.self.destroyPlayer(disconnectedPlayer);
      }
   }

   private void destroyDisconnectedUsers () {
      List<NetEntity> playersToDestroy = new List<NetEntity>();

      foreach (KeyValuePair<NetEntity, DateTime> KV in _disconnectedPlayers) {
         // Check if the user has been disconnected for long enough
         if (DateTime.UtcNow.Subtract(KV.Value).TotalSeconds > SECONDS_UNTIL_PLAYERS_DESTROYED) {
            playersToDestroy.Add(KV.Key);
         }
      }

      // Destroy the selected player objects
      foreach (NetEntity player in playersToDestroy) {
         MyNetworkManager.self.destroyPlayer(player);
      }
   }

   #region Private Variables

   // Keep players on the server for a few seconds after they disconnect
   protected static Dictionary<NetEntity, DateTime> _disconnectedPlayers = new Dictionary<NetEntity, DateTime>();

   #endregion
}
