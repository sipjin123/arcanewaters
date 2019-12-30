using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeedBag : ClientMonoBehaviour {
   #region Public Variables

   // The Container for our sprites
   public GameObject spriteContainer;

   #endregion

   void Start () {
      // Start listening for events
      EventManager.StartListening(EventManager.COMPLETED_TUTORIAL_STEP, completedTutorialStep);
   }

   void Update () {
      // We only show the seed bag if they're on step 1 of the tutorial
      spriteContainer.SetActive(isSeedBagActive());
   }

   void OnTriggerEnter2D (Collider2D other) {
      PlayerBodyEntity body = other.transform.GetComponent<PlayerBodyEntity>();

      // If this client's player picked up the seed bag, tell the server
      if (body != null && body.isLocalPlayer && isSeedBagActive()) {
         body.Cmd_CompletedTutorialStep(TutorialManager.currentStep);

         // Make the bag react right away
         EffectManager.show(Effect.Type.Pickup_Effect, this.transform.position);
         foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>()) {
            Util.setAlpha(renderer, 0f);
         }
      }
   }

   protected void completedTutorialStep () {
      // If they just completed the first tutorial step, show an effect
      if (isSeedBagActive()) {
         //EffectManager.show("cannon_smoke", this.transform.position);
      }
   }

   private bool isSeedBagActive () {
      TutorialData currentTutorialData = TutorialManager.self.fetchTutorialData(TutorialManager.currentStep);
      Item item = null;
      if (currentTutorialData.requirementType == RequirementType.Item) {
         item = JsonUtility.FromJson<Item>(currentTutorialData.rawDataJson);
      }

      if (item != null) {
         if (item.category == Item.Category.Weapon && item.itemTypeId == (int)Weapon.Type.Seeds) {
            return true;
         }
      }

      return false;
   }

   #region Private Variables

   #endregion
}
