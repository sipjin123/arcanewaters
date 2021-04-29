using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using DG.Tweening;

public class WorldMapBiome : MonoBehaviour
{
   #region Public Variables

   // The biome represented by this map area
   public Biome.Type biome;

   // The clouds
   public Image clouds;

   // The cloud shadows
   public Image cloudShadows;

   // The land
   public Image land;

   // The home town button
   public WorldMapTownButton homeTownButton;

   // The canvas group containing the town icons
   public CanvasGroup townContainer;

   #endregion

   public void setHomeTown (string townAreaKey) {
      homeTownButton.initialize(townAreaKey, biome);
   }

   public void reveal () {
      Util.setAlpha(clouds, 0f);
      Util.setAlpha(cloudShadows, 0f);
      Util.setAlpha(land, 1f);
      townContainer.Show();
   }

   public void revealWithAnimation () {
      hideWithClouds();

      // Make the town canvasgroup interactable, but invisible
      townContainer.Show();
      townContainer.alpha = 0f;

      // Highlight the home town button
      homeTownButton.enableArrows();

      Sequence revealSequence = DOTween.Sequence();

      // Reveal the land
      revealSequence.Insert(0, clouds.DOColor(new Color(clouds.color.r, clouds.color.g, clouds.color.b, 0f), LAND_REVEAL_DURATION));
      revealSequence.Insert(0, cloudShadows.DOColor(new Color(cloudShadows.color.r, cloudShadows.color.g, cloudShadows.color.b, 0f), LAND_REVEAL_DURATION));
      revealSequence.Insert(0, land.DOColor(new Color(land.color.r, land.color.g, land.color.b, 1f), LAND_REVEAL_DURATION));

      // Reveal the towns
      revealSequence.Insert(LAND_REVEAL_DURATION, townContainer.DOFade(1f, TOWN_REVEAL_DURATION));
   }

   public void hideWithClouds () {
      Util.setAlpha(clouds, 1f);
      Util.setAlpha(cloudShadows, 1f);
      Util.setAlpha(land, 0f);
      townContainer.Hide();
   }

   public void onLocationButtonPressed (string areaKey) {
      WorldMapPanel.self.onBiomeHomeTownButtonPressed(biome);
      PanelManager.self.unlinkPanel();
   }

   #region Private Variables

   // The duration of the reveal animations
   private float LAND_REVEAL_DURATION = 1.2f;
   private float TOWN_REVEAL_DURATION = 0.7f;

   #endregion
}
