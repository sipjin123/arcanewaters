using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class AdminGameSettingsManager : GenericGameManager
{
   #region Public Variables

   // The current game settings
   public AdminGameSettings settings = new AdminGameSettings();

   // Self
   public static AdminGameSettingsManager self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   [Server]
   public void onServerStart () {
      updateServerSettings(false);
   }

   public bool isBiomeLegalForDemoUser (Biome.Type biome) {
      // Lets allow Forest as a bit of a fail-safe
      if (biome == Biome.Type.Forest) {
         return true;
      }

      return (int) biome <= settings.maxDemoBiome;
   }

   [Server]
   public void updateAndStoreSettings (AdminGameSettings newSettings) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         if ((DateTime.UtcNow - DateTime.FromBinary(settings.creationDate)).TotalSeconds < 60) {
            // If less than a minute has passed since the last save, update the latest entry
            newSettings.id = settings.id;
            newSettings.creationDate = settings.creationDate;
            DB_Main.updateAdminGameSettings(newSettings);
         } else {
            // Create a new settings entry in the DB
            newSettings.creationDate = DateTime.UtcNow.ToBinary();
            DB_Main.addAdminGameSettings(newSettings);
         }

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Make all servers in the server network update their settings
            ServerNetworkingManager.self.updateAdminGameSettings();
         });
      });
   }

   [Server]
   public void updateServerSettings (bool updateConnectedClients) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Load the latest game settings
         AdminGameSettings newSettings = DB_Main.getAdminGameSettings();

         // Set default values if the table is empty
         if (newSettings == null) {
            newSettings = new AdminGameSettings();
         }

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Update this server settings
            updateLocalSettings(newSettings);

            if (updateConnectedClients) {
               // Set the parameters in all connected clients
               foreach (NetEntity netEntity in MyNetworkManager.getPlayers()) {
                  netEntity.rpc.setAdminBattleParameters();
               }
            }
         });
      });
   }

   public void updateLocalSettings (AdminGameSettings newSettings) {
      this.settings = newSettings;
   }

   #region Private Variables

   #endregion
}


