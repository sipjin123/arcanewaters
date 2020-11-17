using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MLAPI;
using MLAPI.NetworkedVar;
using MLAPI.NetworkedVar.Collections;
using MLAPI.Serialization;
using System.IO;
using MLAPI.Serialization.Pooled;
using Newtonsoft.Json;

public class NetworkedServer : NetworkedBehaviour {
   #region Public Variables

   // The port associated with this Networked Server
   public NetworkedVar<int> networkedPort = new NetworkedVar<int>(new NetworkedVarSettings { WritePermission = NetworkedVarPermission.OwnerOnly });

   // An example of how to set up a Networked Dictionary with a custom object type
   public NetworkedDictionary<int, VoyageGroupInfo> voyagesDictionary = new NetworkedDictionary<int, VoyageGroupInfo>(new NetworkedVarSettings { WritePermission = NetworkedVarPermission.OwnerOnly });

   #endregion

   private void Start () {
      // We only want to make modifications to this Networked Server if we are the owner
      if (this.IsOwner) {
         // Assign this server's port to this Networked Server object
         this.networkedPort.Value = MyNetworkManager.getCurrentPort();

         // For demonstration purposes, create some random Voyage Group Info
         for (int i = 0; i < 5; i++) {
            VoyageGroupInfo voyage = new VoyageGroupInfo();
            voyage.groupId = UnityEngine.Random.Range(5, 10000);
            voyagesDictionary.Add(voyage.groupId, voyage);
         }
      }
   }

   private void Update () {
      // Set the name of this object based on the port
      this.name = "Networked Server #" + this.networkedPort.Value;
   }

   #region Private Variables

   #endregion
}
