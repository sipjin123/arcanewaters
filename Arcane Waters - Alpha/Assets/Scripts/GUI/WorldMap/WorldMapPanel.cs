using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using TMPro;

public class WorldMapPanel : Panel
{
   #region Public Variables

   // The biomes areas
   public List<WorldMapBiome> mapBiomesList;

   // Self
   public static WorldMapPanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;
   }

   public void displayMap () {
      displayMap(Biome.Type.None);
   }

   public void displayMap (Biome.Type newBiomeToReveal) {
      if (TutorialManager3.self.getCurrentTrigger() == TutorialTrigger.OpenMap) {
         TutorialManager3.self.tryCompletingStep(TutorialTrigger.OpenMap);
      }

      _newBiomeToReveal = newBiomeToReveal;
      Global.player.rpc.Cmd_RequestUnlockedBiomeListFromServer();
   }

   public void updatePanelWithUnlockedBiomes (List<Biome.Type> unlockedBiomeList, string forestHomeTownAreaKey,
      string desertHomeTownAreaKey, string snowHomeTownAreaKey, string pineHomeTownAreaKey, string lavaHomeTownAreaKey,
      string mushroomHomeTownAreaKey) {

      foreach (WorldMapBiome mapBiome in mapBiomesList) {
         // Set the home town name and button
         switch (mapBiome.biome) {
            case Biome.Type.Forest:
               mapBiome.setHomeTown(forestHomeTownAreaKey);
               break;
            case Biome.Type.Desert:
               mapBiome.setHomeTown(desertHomeTownAreaKey);
               break;
            case Biome.Type.Pine:
               mapBiome.setHomeTown(pineHomeTownAreaKey);
               break;
            case Biome.Type.Snow:
               mapBiome.setHomeTown(snowHomeTownAreaKey);
               break;
            case Biome.Type.Lava:
               mapBiome.setHomeTown(lavaHomeTownAreaKey);
               break;
            case Biome.Type.Mushroom:
               mapBiome.setHomeTown(mushroomHomeTownAreaKey);
               break;
            default:
               break;
         }

         bool isAccessible = false;

         // Check if the biome is accessible and should be revealed
         foreach (Biome.Type accessibleBiome in unlockedBiomeList) {
            if (mapBiome.biome == accessibleBiome) {
               isAccessible = true;
               break;
            }
         }

         if (isAccessible) {
            // Reveal the new biome area with an animation if requested
            if (mapBiome.biome == _newBiomeToReveal) {
               mapBiome.revealWithAnimation();
            } else {
               mapBiome.reveal();
            }
         } else {
            mapBiome.hideWithClouds();
         }
      }
   }

   public void onBiomeHomeTownButtonPressed (Biome.Type biome) {
      Global.player.rpc.Cmd_RequestWarpToBiomeHomeTown(biome);
      
      // Close any opened panel
      PanelManager.self.unlinkPanel();
   }

   #region Private Variables

   // The new biome area to reveal when opening the panel
   private Biome.Type _newBiomeToReveal = Biome.Type.None;

   #endregion
}