using System.Collections;
using Mirror;
using UnityEngine;

public class ConnectedClientData
{
   // The account ID this client is using
   public int accountId;

   // The connection ID
   public int connectionId;

   // The IP address
   public string address;

   // The NetworkConnection for this client
   public NetworkConnection connection;

   // The current NetEntity for this client. It can be null.
   public NetEntity netEntity;

   // The user ID of the current NetEntity
   public int userId;

   // The Steam ID of this connection
   public string steamId;

   public override string ToString () {
      return $"AccountID: {accountId}\nUserID: {userId}\nSteamID: {steamId}\nConnectionID: {connectionId}\nIP: {address}\nConnection: {connection}\nNetEntity:{(netEntity != null ? netEntity.entityName : "null")}";
   }
}