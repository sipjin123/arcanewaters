using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool;

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

   public void receiveData (MapCreationTool.Serialization.DataField[] dataFields) {
      foreach (MapCreationTool.Serialization.DataField field in dataFields) {
         switch (field.k.ToLower()) {
            case "step id":
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
