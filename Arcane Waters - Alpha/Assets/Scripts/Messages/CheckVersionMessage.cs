using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CheckVersionMessage : NetworkMessage
{
   // The client platform
   public RuntimePlatform clientPlatform;

   // The client's version of the game
   public int clientGameVersion;

   public CheckVersionMessage () { }

   public CheckVersionMessage (int clientGameVersion, RuntimePlatform clientPlatform) {
      this.clientGameVersion = clientGameVersion;
      this.clientPlatform = clientPlatform;
   }
}
