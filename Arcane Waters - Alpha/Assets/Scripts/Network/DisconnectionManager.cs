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
   public static float SECONDS_UNTIL_PLAYERS_DESTROYED = 20f;

   // Self
   public static DisconnectionManager self;

   #endregion

   public void Awake () {
      self = this;
   }

   public void Start () {
      InvokeRepeating(nameof(destroyDisconnectedUsers), 5.5f, 1f);
   }

   public void addToDisconnectedUsers (ClientConnectionData data) {
      if (data == null) {
         return;
      }

      int userId = data.userId;

      if (!_disconnectedPlayers.ContainsKey(userId)) {
         data.isReconnecting = false;
         data.timeOfDisconnection = DateTime.UtcNow;
         _disconnectedPlayers.Add(userId, data);
      }
   }

   public void removeFromDisconnectedUsers (int userId) {
      if (_disconnectedPlayers.ContainsKey(userId)) {
         _disconnectedPlayers.Remove(userId);
      }      
   }

   public void clearDisconnectedUsers () {
      _disconnectedPlayers.Clear();
   }

   public bool isUserPendingDisconnection (int userId) {
      return _disconnectedPlayers.ContainsKey(userId);
   }

   public ClientConnectionData getConnectionDataForUser (int userId) {
      _disconnectedPlayers.TryGetValue(userId, out ClientConnectionData data);

      return data;
   }

   private void destroyDisconnectedUsers () {
      List<int> removedPlayers = new List<int>();

      foreach (ClientConnectionData data in _disconnectedPlayers.Values) {
         if (!data.isReconnecting) {
            // Check if the user has been disconnected for long enough
            if (DateTime.UtcNow.Subtract(data.timeOfDisconnection).TotalSeconds > SECONDS_UNTIL_PLAYERS_DESTROYED) {
               MyNetworkManager.self.finishPlayerDisconnection(data);
               removedPlayers.Add(data.userId);
            }
         }
      }

      foreach(int userId in removedPlayers) {
         // Remove the player from the list of disconnected players
         removeFromDisconnectedUsers(userId);
         _disconnectedPlayers.Remove(userId);
      }
   }

   #region Private Variables

   // Keep players on the server for a few seconds after they disconnect
   protected static Dictionary<int, ClientConnectionData> _disconnectedPlayers = new Dictionary<int, ClientConnectionData>();

   #endregion
}