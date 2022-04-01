using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CargoBoxManager : MonoBehaviour
{
   #region Public Variables

   // Our Canvas Group
   public CanvasGroup canvasGroup;

   // The top visual of the list
   public Image topVisual = null;

   // The prefab we use for creating cargo box GUI elements
   public CargoBox cargoBoxPrefab;

   // The prefab of the image we show at the bottom of the list
   public Image listBottomPrefab;

   // The number of boxes being shown
   public int boxesCount;

   // Self
   public static CargoBoxManager self;

   #endregion

   void Awake () {
      self = this;
   }

   private void Update () {
      // Hide this panel in certain situations
      bool shouldHidePanel = TitleScreen.self.isShowing() || CharacterScreen.self.isShowing() || Global.isInBattle();

      // Hide this panel, if the player is in a PvpMatch
      if (Global.player != null) {
         Instance instance = Global.player.getInstance();
         if (instance != null) {
            shouldHidePanel = instance.isPvP;
         }
      }

      // Keep the panel hidden until we're in the game
      canvasGroup.alpha = shouldHidePanel ? 0 : 1f;
   }

   // Cargo boxes are disabled
   //public void updateCargoBoxes (List<SiloInfo> siloInfo) {
   //   // Clear out the old
   //   foreach (GameObject go in _dynamicElements) {
   //      if (go != null) {
   //         Destroy(go);
   //      }
   //   }
   //   _dynamicElements.Clear();

   //   boxesCount = 0;

   //   foreach (SiloInfo info in siloInfo) {
   //      // Ignore 0 counts
   //      if (info.cropCount == 0) {
   //         continue;
   //      }

   //      CargoBox box = Instantiate(cargoBoxPrefab);
   //      _dynamicElements.Add(box.gameObject);
   //      box.transform.SetParent(this.transform, false);
   //      box.updateBox(info);

   //      boxesCount++;
   //   }

   //   if (boxesCount > 0) {
   //      Image bot = Instantiate(listBottomPrefab, transform, false);
   //      _dynamicElements.Add(bot.gameObject);
   //   }

   //   topVisual.gameObject.SetActive(boxesCount > 0);
   //}

   // Cargo boxes are disabled
   //public int getCargoCount (Crop.Type cropType) {
   //   foreach (CargoBox box in GetComponentsInChildren<CargoBox>()) {
   //      if (box.cropType == cropType) {
   //         return int.Parse(box.cargoCountText.text);
   //      }
   //   }

   //   return 0;
   //}

   #region Private Variables

   // List of elements we have instantiated
   private List<GameObject> _dynamicElements = new List<GameObject>();


   #endregion
}
