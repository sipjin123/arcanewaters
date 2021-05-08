using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CheckVersionMessage : NetworkMessage
{
   // The network instance id associated with this message
   public uint netId;

   // The client platform
   public RuntimePlatform clientPlatform;

   // The client's version of the game
   public int clientGameVersion;

   public CheckVersionMessage () { }

   public CheckVersionMessage (uint netId) {
      this.netId = netId;
   }

   public CheckVersionMessage (uint netId, int clientGameVersion, RuntimePlatform clientPlatform) {
      this.netId = netId;
      this.clientGameVersion = clientGameVersion;
      this.clientPlatform = clientPlatform;
   }
}
