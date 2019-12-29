using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TreasureSiteGate : ClientMonoBehaviour {
   #region Public Variables

   // The container for our gate statuses
   public GameObject statusesContainer;

   // The sprites we use to represent the various levels of damage to the gate
   public SpriteRenderer gateStatus1;
   public SpriteRenderer gateStatus2;
   public SpriteRenderer gateStatus3;
   public SpriteRenderer gateStatus4;

   #endregion

   public void Start () {
      _collider = GetComponent<PolygonCollider2D>();

      // Store the gate statuses in list form
      _gates.Add(gateStatus1);
      _gates.Add(gateStatus2);
      _gates.Add(gateStatus3);
      _gates.Add(gateStatus4);
   }

   private void Update () {
      // Stay hidden until we have the info we need
      statusesContainer.SetActive(Global.player != null && TutorialManager.currentStep > 0);

      // We only mess with the warp if we have a player object
      if (Global.player != null) {
         // If we're past the tutorial step, we can clear the gate
         if (TutorialManager.currentStep > 13) {
            // TODO: 13 is a placeholder for : Step.DestroyGate
            foreach (SpriteRenderer gate in _gates) {
               gate.enabled = false;
            }
            this.gameObject.SetActive(false);
         }
      }
   }

   public bool colliderContainsPoint (Vector2 point) {
      return _collider.OverlapPoint(point);
   }

   public void wasHit () {
      // If the gate is still up, show a damage effect
      if (isEntranceStillBlocked()) {
         Effect.Type damageEffect = (Effect.Type) System.Enum.Parse(typeof(Effect.Type), "Gate_Damage_" + getCurrentGateStatus());
         EffectManager.show(damageEffect, this.transform.position);

         // Show some flying debris
         ExplosionManager.createExplosion(this.transform.position);
      }

      // Disable one of the gates
      foreach (SpriteRenderer gateStatus in _gates) {
         if (gateStatus.enabled) {
            gateStatus.enabled = false;
            break;
         }
      }

      if (!isEntranceStillBlocked()) {
         int stepIndex = TutorialManager.self.tutorialDataList().Find(_ => _.tutorialName.Contains("DestroyGate")).stepOrder;
         Global.player.Cmd_CompletedTutorialStep(stepIndex);
      }
   }

   protected bool isEntranceStillBlocked () {
      foreach (SpriteRenderer gateStatus in _gates) {
         if (gateStatus.enabled) {
            return true;
         }
      }

      return false;
   }

   protected int getCurrentGateStatus () {
      if (gateStatus1.enabled) {
         return 1;
      } else if (gateStatus2.enabled) {
         return 2;
      } else if (gateStatus3.enabled) {
         return 3;
      } else if (gateStatus4.enabled) {
         return 4;
      } else {
         return 5;
      }
   }

   #region Private Variables

   // Our gate sprites, in a list
   List<SpriteRenderer> _gates = new List<SpriteRenderer>();

   // Our trigger collider
   protected PolygonCollider2D _collider;

   #endregion
}
