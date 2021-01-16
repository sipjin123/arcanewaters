using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System;

public class VoyageManager : MonoBehaviour {

   #region Public Variables
   
   // Self
   public static VoyageManager self;

   #endregion

   void Awake () {
      self = this;

      // Randomize the starting value of the PvP parameter
      _isNewVoyagePvP = UnityEngine.Random.Range(0, 2) == 0;
   }

   public void startVoyageManagement () {
      // At server startup, for dev builds, make all the sea maps accessible by creating one voyage for each
#if !CLOUD_BUILD
      StartCoroutine(CO_CreateInitialVoyages());
#endif

      // Regularly check that there are enough voyage instances open and create more
      InvokeRepeating(nameof(createVoyageInstanceIfNeeded), 20f, 10f);
   }

   /// <summary>
   /// Note that only the Master server can launch voyage instance creations. Use requestVoyageInstanceCreation() to ensure the Master server handles it.
   /// </summary>
   public void createVoyageInstance (int voyageId, string areaKey, bool isPvP, Biome.Type biome, Voyage.Difficulty difficulty) {
      // Check if the area is defined
      if (string.IsNullOrEmpty(areaKey)) {
         // Get the list of sea maps area keys
         List<string> seaMaps = getVoyageAreaKeys();

         // If there are no available areas, do nothing
         if (seaMaps.Count == 0) {
            return;
         }

         // Randomly choose an area
         areaKey = seaMaps[UnityEngine.Random.Range(0, seaMaps.Count)];
      }

      // Randomize the biome if it is not defined
      if (biome == Biome.Type.None) {
         biome = Util.randomEnumStartAt<Biome.Type>(1);
      }

      // Randomize the difficulty if it is not defined
      if (difficulty == Voyage.Difficulty.None) {
         difficulty = Util.randomEnumStartAt<Voyage.Difficulty>(1);
      }

      // Create the area instance
      InstanceManager.self.createNewInstance(areaKey, false, true, voyageId, isPvP, difficulty, biome);
   }

   public Voyage getVoyage (int voyageId) {
      // Search the voyage in all the servers we know about
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (Voyage voyage in server.voyages) {
            if (voyage.voyageId == voyageId) {
               return voyage;
            }
         }
      }

      return null;
   }

   public List<Voyage> getAllVoyages () {
      List<Voyage> voyages = new List<Voyage>();

      // Get all the voyages we know about in all the servers we know about
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (Voyage voyage in server.voyages) {
            voyages.Add(voyage);
         }
      }

      return voyages;
   }

   public List<Voyage> getAllOpenVoyageInstances () {
      // Get the number of groups in all voyage instances
      Dictionary<int, int> allGroupCount = VoyageGroupManager.self.getGroupCountInAllVoyages();

      // Select the voyages that are open to new groups
      List<Voyage> allOpenVoyages = new List<Voyage>();
      foreach (Voyage voyage in getAllVoyages()) {
         allGroupCount.TryGetValue(voyage.voyageId, out int groupCount);
         if (isVoyageOpenToNewGroups(voyage, groupCount)) {
            allOpenVoyages.Add(voyage);
         }
      }

      return allOpenVoyages;
   }

   public bool doesVoyageExists (int voyageId) {
      return getVoyage(voyageId) != null;
   }

   public bool isVoyageArea (string areaKey) {
      return getVoyageAreaKeys().Contains(areaKey);
   }

   public static bool isTreasureSiteArea (string areaKey) {
      return AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.TreasureSite;
   }

   public List<string> getVoyageAreaKeys () {
      return AreaManager.self.getSeaAreaKeys().Where(k => AreaManager.self.getAreaSpecialType(k) == Area.SpecialType.Voyage).ToList();
   }

   public static bool isVoyageOpenToNewGroups (Voyage voyage) {
      return isVoyageOpenToNewGroups(voyage, VoyageGroupManager.self.getGroupCountInVoyage(voyage.voyageId));
   }

   public static bool isVoyageOpenToNewGroups (Voyage voyage, int groupCount) {
      // Calculate the time left until the voyage closes to new groups
      DateTime voyageCreationDate = DateTime.FromBinary(voyage.creationDate);
      TimeSpan timeLeft = voyageCreationDate.AddSeconds(Voyage.INSTANCE_OPEN_DURATION).Subtract(DateTime.UtcNow);

      // Check that the voyage is still in the open phase
      if (timeLeft.TotalSeconds <= 0) {
         return false;
      } else {
         // Check if the maximum number of groups has been reached
         if (groupCount >= Voyage.getMaxGroupsPerInstance(voyage.difficulty)) {
            return false;
         } else {
            return true;
         }
      }
   }

   public void showVoyagePanel (NetEntity entity) {
      if (Global.player == null || !Global.player.isClient || entity == null || !entity.isLocalPlayer) {
         return;
      }

      Global.player.rpc.Cmd_RequestVoyageListFromServer();
   }

   public void displayWarpToVoyageConfirmScreen () {
      PanelManager.self.showConfirmationPanel("Do you want to warp to your current voyage?",
         () => confirmWarpToVoyageMap());
   }

   public void confirmWarpToVoyageMap () {
      Global.player.rpc.Cmd_WarpToCurrentVoyageMap();
   }

   protected void createVoyageInstanceIfNeeded () {
      // Only the master server launches the creation of voyages instances
      NetworkedServer server = ServerNetworkingManager.self.server;
      if (server == null || !server.isMasterServer()) {
         return;
      }

      // If there are missing voyages, create a new one
      if (getAllOpenVoyageInstances().Count < Voyage.OPEN_VOYAGE_INSTANCES) {
         // Alternate PvP and PvE instances
         _isNewVoyagePvP = !_isNewVoyagePvP;

         requestVoyageInstanceCreation("", _isNewVoyagePvP);
      }
   }

   private IEnumerator CO_CreateInitialVoyages () {
      // Wait until our server is defined
      while (ServerNetworkingManager.self == null || ServerNetworkingManager.self.server == null) {
         yield return null;
      }

      // Wait until our server port is initialized
      while (ServerNetworkingManager.self.server.networkedPort.Value == 0) {
         yield return null;
      }

      // Only the master server launches the creation of voyages instances
      if (ServerNetworkingManager.self.server.isMasterServer()) {

         // Create a voyage instance for each available sea map
         List<string> seaMaps = getVoyageAreaKeys();
         foreach (string areaKey in seaMaps) {
            // Alternate PvP and PvE instances
            _isNewVoyagePvP = !_isNewVoyagePvP;

            requestVoyageInstanceCreation(areaKey, _isNewVoyagePvP);
         }
      }
   }

   public void requestVoyageInstanceCreation (string areaKey = "", bool isPvP = false, Biome.Type biome = Biome.Type.None, Voyage.Difficulty difficulty = Voyage.Difficulty.None) {
      // Only the master server launches the creation of voyages instances
      if (!ServerNetworkingManager.self.server.isMasterServer()) {
         ServerNetworkingManager.self.requestVoyageInstanceCreation(areaKey, isPvP, biome, difficulty);
         return;
      }

      // Find the server with the least people
      NetworkedServer bestServer = ServerNetworkingManager.self.getRandomServerWithLeastPlayers();

      if (bestServer != null) {
         ServerNetworkingManager.self.createVoyageInstanceInServer(bestServer.networkedPort.Value, ++_lastVoyageId, areaKey, isPvP, biome, difficulty);
      }
   }

   #region Private Variables

   // Gets toggled every time a new voyage is created, to ensure an equal number of PvP and PvE instances
   private bool _isNewVoyagePvP = false;

   // The last id used to create a voyage group
   private int _lastVoyageId = 0;

   #endregion
}
