using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using ProceduralMap;
using Mirror;

public class RandomMapRow : MonoBehaviour {
   #region Public Variables

   // The frame UI reference
   public Image frameImage;

   // The plaque UI reference
   public Image plaqueImage;

   // The plaque players count outline UI reference
   public Outline plaqueCountOutline;

   // The biome image UI reference
   public Image biomeImage;

   // The randomized map name based on biome type
   public Text mapNameText;

   // Number of players on given map at given server
   public Text playerCountText;

   // The summary of the map associated with this row
   public MapSummary mapSummary;

   #endregion

   public void setRowFromSummary (MapSummary mapSummary) {
      // Store for later reference
      this.mapSummary = mapSummary;

      // Set current player count before entering map
      playerCountText.text = mapSummary.playersCount.ToString() + "/" + mapSummary.maxPlayersCount.ToString();
   }

   public void joinInstance () {
      // No more players can enter this room
      if (isFull()) {
         return;
      }

      // Create tiles for random map before moving player to map
      Global.player.rpc.Cmd_GetMapConfigFromServer(mapSummary.areaKey);

      // Send a request to the server to join the specified map
      Global.player.Cmd_SpawnIntoGeneratedMap(mapSummary);

      // Hide the panel
      Panel panel = PanelManager.self.get(Panel.Type.RandomMaps);
      panel.hide();
   }

   public bool isFull () {
      return mapSummary.playersCount >= mapSummary.maxPlayersCount;
   }

   public void setPlaqueNames () {
      // Set plaque based on difficulty - they don't change on hover/pressed
      plaqueImage.sprite = ImageManager.getSprite(_seaMapPath + "count_plaque_" + getFrameName());
   }

   public void setPlaqueOutlineColor () {
      // Set plaque players count outline color based on difficulty level
      Color chosenColor = Color.white;
      switch (mapSummary.mapDifficulty) {
         case MapSummary.MapDifficulty.Easy:
            chosenColor = RandomMapsPanel.self.outlineColorEasy;
            break;
         case MapSummary.MapDifficulty.Medium:
            chosenColor = RandomMapsPanel.self.outlineColorMedium;
            break;
         case MapSummary.MapDifficulty.Hard:
            chosenColor = RandomMapsPanel.self.outlineColorHard;
            break;
      }
      plaqueCountOutline.effectColor = chosenColor;
   }

   public void OnPointerEnter () {
      // Change frame sprite - HOVERED
      frameImage.sprite = ImageManager.getSprite(_seaMapPath + getFrameName() + "_frame_hover");

      // Change biome sprite - HOVERED
      biomeImage.sprite = ImageManager.getSprite(_seaMapPath + getBiomeName() + "_hover");
   }

   public void OnPointerExit () {
      // Change frame sprite - DEFAULT
      frameImage.sprite = ImageManager.getSprite(_seaMapPath + getFrameName() + "_frame_default");

      // Change biome sprite - DEFAULT
      biomeImage.sprite = ImageManager.getSprite(_seaMapPath + getBiomeName() + "_default");
   }

   public void OnPointerDown () {
      // Change frame sprite - PRESSED
      frameImage.sprite = ImageManager.getSprite(_seaMapPath + getFrameName() + "_frame_pressed");

      // Change biome sprite - PRESSED
      biomeImage.sprite = ImageManager.getSprite(_seaMapPath + getBiomeName() + "_pressed");
   }

   public void OnPointerUp () {
      // Set default sprites
      OnPointerExit();
   }

   private string getFrameName() {
      switch (mapSummary.mapDifficulty) {
         case MapSummary.MapDifficulty.Easy:
            return "bronze";
         case MapSummary.MapDifficulty.Medium:
            return "silver";
         case MapSummary.MapDifficulty.Hard:
            return "gold";
      }
      return "";
   }

   private string getBiomeName () {
      return mapSummary.biomeType.ToString().ToLower();
   }

   #region Private Variables

   // Path for Sea Random map sprites on UI
   private string _seaMapPath = "GUI/Sea Random/";

   #endregion
}
