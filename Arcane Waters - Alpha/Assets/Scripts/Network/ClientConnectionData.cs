using System;
using System.Collections;
using Mirror;
using UnityEngine;

public class ClientConnectionData
{
   // The account ID this client is using
   public int accountId = -1;

   // The connection ID
   public int connectionId = -1;

   // The IP address
   public string address;

   // The NetworkConnection for this client
   public NetworkConnection connection;

   // The current NetEntity for this client. It can be null.
   public NetEntity netEntity;

   // The user ID of the current NetEntity
   public int userId = -1;

   // The Steam ID of this connection
   public string steamId;

   // The time at which the client disconnected
   public DateTime timeOfDisconnection;

   // Whether this user is currently reconnecting
   public bool isReconnecting;

   // Was the first spawn handled?
   public bool wasFirstSpawnHandled;

   public bool isAuthenticated () {
      return accountId >= 0;
   }

   public override string ToString () {
      return $"AccountID: {accountId}\nUserID: {userId}\nSteamID: {steamId}\nConnectionID: {connectionId}\nIP: {address}\nConnection: {connection}\nNetEntity:{(netEntity != null ? netEntity.entityName : "null")}";
   }
}