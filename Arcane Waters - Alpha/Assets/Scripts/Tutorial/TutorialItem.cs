using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TutorialItem : ClientMonoBehaviour {
   #region Public Variables

   // The step that picking up this item completes
   public Step tutorialStepForThisItem;

   // The Container for our sprites
   public GameObject spriteContainer;

   #endregion

   void Start () {
      // Start listening for events
      EventManager.StartListening(EventManager.COMPLETED_TUTORIAL_STEP, completedTutorialStep);
   }

   void Update () {
      // We only show the item if they're on the relevant step of the tutorial
      spriteContainer.SetActive(TutorialManager.currentStep == tutorialStepForThisItem);
   }

   void OnTriggerEnter2D (Collider2D other) {
      PlayerBodyEntity body = other.transform.GetComponent<PlayerBodyEntity>();

      // If this client's player picked up the item, tell the server
      if (body != null && body.isLocalPlayer && TutorialManager.currentStep == tutorialStepForThisItem) {
         body.Cmd_CompletedTutorialStep(tutorialStepForThisItem);

         // Make the item react right away
         EffectManager.show(Effect.Type.Pickup_Effect, this.transform.position);
         foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>()) {
            Util.setAlpha(renderer, 0f);
         }

         // Play a sound
         SoundManager.create3dSound("item_pick_up", Global.player.transform.position);
      }
   }

   protected void completedTutorialStep () {
      // Play a sound
      SoundManager.create3dSound("tutorial_step", Global.player.transform.position);

      // If they just completed the tutorial step, show an effect
      if (TutorialManager.currentStep == tutorialStepForThisItem + 1) {
         // EffectManager.show("cannon_smoke", this.transform.position);
         // SoundManager.play2DClip(SoundManager.Type.Powerup);
      }
   }

   #region Private Variables=

   #endregion
}
