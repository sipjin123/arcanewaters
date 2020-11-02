using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TipManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static TipManager self;

   #endregion

   public void Awake () {
      self = this;
   }

   public void updateCropTips () {
      // Clear out the old ones
      _tips.Clear();

      // Loop over all of the areas
      foreach (Area area in AreaManager.self.getAreas()) {
         // Loop over all of the NPCs in that Area
         foreach (NPC npc in area.GetComponentsInChildren<NPC>()) {
            List<CropOffer> offers = ShopManager.self.getOffersByShopName(npc.shopName);

            // If there are no offers available, continue
            if (offers.Count == 0)
               continue;

            CropOffer offer = offers.ChooseRandom();

            // Create a new Random Tip for this NPC to share
            Tip tip = new Tip(area, offer);

            // Store the Tip
            _tips[npc] = tip;
         }
      }
   }

   #region Private Variables

   // Stores the current Tip for each NPC
   protected Dictionary<NPC, Tip> _tips = new Dictionary<NPC, Tip>();
      
   #endregion
}
