using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class TutorialItem : ClientMonoBehaviour, IMapEditorDataReceiver {
   #region Public Variables

   // The step that picking up this item completes
   public int tutorialStepForThisItem;

   // The Container for our sprites
   public GameObject spriteContainer;

   // The Container for extra details for the quest
   public GameObject infoContainer;

   // If Set In Map Editor
   public bool isSetInMapEditor;

   // Sprite references
   public Image iconSprite;

   // Info of the text
   public Text inGameText;

   // Determines if data was setup
   public bool isActivated = false;

   // If can be picked up
   public bool canBePickedUp = false;

   #endregion

   void Start () {
      // Start listening for events
      EventManager.StartListening(EventManager.COMPLETED_TUTORIAL_STEP, completedTutorialStep);
   }

   void Update () {
      if (isSetInMapEditor) {
         return;
      }
      // We only show the item if they're on the relevant step of the tutorial
      spriteContainer.SetActive(TutorialManager.currentStep == tutorialStepForThisItem);
      if (isActivated == false) {
         if (TutorialManager.currentStep == tutorialStepForThisItem) {
            TutorialData currData = TutorialManager.self.currentTutorialData();
            isActivated = true;
            if (currData.tutorialIndicatorMessage != "") {
               infoContainer.SetActive(true);
               iconSprite.sprite = ImageManager.getSprite(currData.tutorialIndicatorImgPath);
               inGameText.text = currData.tutorialIndicatorMessage;
            }
         }
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

   void OnTriggerEnter2D (Collider2D other) {
      if (!canBePickedUp) {
         return;
      }

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

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         switch (field.k.ToLower()) {
            case DataField.TUTORIAL_ITEM_STEP_ID_KEY:
               tutorialStepForThisItem = int.Parse(field.v.Trim(' '));
               GetComponent<TutorialLocation>().tutorialStepType = tutorialStepForThisItem;
               break;
            default:
               Debug.LogWarning($"Unrecognized data field key: {field.k}");
               break;
         }
      }
   }

   #region Private Variables=

   #endregion
}
