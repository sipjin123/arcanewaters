using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using System.IO;
using Newtonsoft.Json;
using MLAPI;

public class ServerNetworkingManager : MonoBehaviour {
   #region Public Variables

   #endregion

   private void Start () {
      // This is where we register any classes that we want to serialize between the Server processes
      RegisterSerializableClass<VoyageGroupInfo>();
   }

   public static NetworkingManager get () {
      // This wrapper function is just to help clarify the difference between the "Server" Networking Manager, and the regular Network Manager
      return NetworkingManager.Singleton;
   }

   private static void RegisterSerializableClass<T> () {
      // This is just a long-winded way of saying that we want to use Json to serialize and deserialize an object
      SerializationManager.RegisterSerializationHandlers<T>(
         (Stream stream, T objectInstance) => {
            using (PooledBitWriter writer = PooledBitWriter.Get(stream)) {
               writer.WriteStringPacked(JsonConvert.SerializeObject(objectInstance));
            }
         },
         (Stream stream) => {
            using (PooledBitReader reader = PooledBitReader.Get(stream)) {
               return JsonConvert.DeserializeObject<T>(reader.ReadStringPacked().ToString());
            }
         }
      );
   }

   #region Private Variables

   #endregion
}
