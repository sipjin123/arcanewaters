using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using DG.Tweening;

public class WorldMapTownButton : MonoBehaviour
{
   #region Public Variables

   // The town name container
   public GameObject townNameContainer;

   // The town name
   public Text townName;

   // The animated arrows
   public GameObject arrows;

   // The town button
   public Button townButton;

   #endregion

   public void initialize (string townAreaKey, Biome.Type biome) {
      _biome = biome;
      townName.text = Area.getName(townAreaKey);

      // Capture the values for the click events
      string destinationAreaKey = townAreaKey;

      townButton.onClick.RemoveAllListeners();
      townButton.onClick.AddListener(() => onLocationButtonPressed(destinationAreaKey));

      disableArrows();
   }

   public void enableArrows () {
      arrows.SetActive(true);
      townNameContainer.SetActive(false);
   }

   public void disableArrows () {
      arrows.SetActive(false);
      townNameContainer.SetActive(true);
   }

   public void onLocationButtonPressed (string areaKey) {
      PanelManager.self.hideCurrentPanel();
   }

   #region Private Variables

   // The biome this town is located in
   private Biome.Type _biome = Biome.Type.None;

   #endregion
}
