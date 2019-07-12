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

   // The prefab we use for creating cargo box GUI elements
   public CargoBox cargoBoxPrefab;

   // Self
   public static CargoBoxManager self;

   #endregion

   void Awake()
   {
      self = this;

      // We start out empty
      this.gameObject.DestroyChildren();
   }

   private void Update () {
      // Hide this panel in certain situations
      bool shouldHidePanel = TitleScreen.self.isShowing() || CharacterScreen.self.isShowing() || Global.isInBattle();

      // Keep the panel hidden until we're in the game
      canvasGroup.alpha = shouldHidePanel ? 0 : 1f;
   }

   public void updateCargoBoxes (List<SiloInfo> siloInfo) {
      // Clear out the old
      this.gameObject.DestroyChildren();

      foreach (SiloInfo info in siloInfo) {
         // Ignore 0 counts
         if (info.cropCount == 0) {
            continue;
         }

         CargoBox box = Instantiate(cargoBoxPrefab);
         box.transform.SetParent(this.transform, false);
         box.updateBox(info);
      }
   }

   public int getCargoCount (Crop.Type cropType) {
      foreach (CargoBox box in GetComponentsInChildren<CargoBox>()) {
         if (box.cropType == cropType) {
            return int.Parse(box.cargoCountText.text);
         }
      }

      return 0;
   }

   #region Private Variables

   #endregion
}
