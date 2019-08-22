using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using ProceduralMap;
using Mirror;

public class RandomMapRow : MonoBehaviour {
   #region Public Variables

   // The server name text
   public Text serverText;

   // The biome type name and map difficulty text
   public Text biomeLevelText;

   // Number of players on given map at given server
   public Text playerCountText;

   // The summary of the map associated with this row
   public MapSummary mapSummary;

   #endregion

   public void setRowFromSummary (MapSummary mapSummary) {
      // Store for later reference
      this.mapSummary = mapSummary;

      // Update the name displayed
      serverText.text = mapSummary.serverAddress + ":" + mapSummary.serverPort;

      // Show biome name
      biomeLevelText.text = mapSummary.biomeType.ToString();

      // Set current player count before entering map
      playerCountText.text = "Players: " + mapSummary.playersCount.ToString() + "/" + mapSummary.maxPlayersCount.ToString();      

      // Fill data based on type
      switch (mapSummary.areaType) {
         case Area.Type.SeaRandom_1:
            biomeLevelText.text += " - Easy";
            break;

         case Area.Type.SeaRandom_2:
            biomeLevelText.text += " - Medium";
            break;

         case Area.Type.SeaRandom_3:
            biomeLevelText.text += " - Hard";
            break;
      }
   }

   public void joinInstance () {
      // No more players can enter this room
      if (isFull()) {
         return;
      }

      // Send a request to the server to join the specified map
      Global.player.Cmd_SpawnIntoGeneratedMap(mapSummary);

      // Hide the panel
      Panel panel = PanelManager.self.get(Panel.Type.RandomMaps);
      panel.hide();
   }

   public bool isFull () {
      return mapSummary.playersCount >= mapSummary.maxPlayersCount;
   }

   #region Private Variables

   #endregion
}
