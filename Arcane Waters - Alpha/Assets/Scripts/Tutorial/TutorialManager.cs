using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

// The tutorial step names
public enum Step {
   None = 0,
   GetDressed = 1,
   FindSeedBag = 2,
   PlantSeeds = 3,
   GetWateringCan = 4,
   StartWatering = 5,
   FinishWatering = 6,
   GetPitchfork = 7,
   HarvestCrops = 8,
   HeadToDocks = 9,
   FindTown = 10,
   SellCrops = 11,
   FindTreasureSite = 12,
   DestroyGate = 13,
   EnterTreasureSite = 14,
   ExploreTreasureSite = 15,
}

public class TutorialManager : MonoBehaviour {
   #region Public Variables

   // The current step that this player is on
   public static Step currentStep = Step.None;

   // Our Canvas Group
   public CanvasGroup canvasGroup;

   // Self
   public static TutorialManager self;

   #endregion

   private void Awake () {
      self = this;

      // Start out with the tutorial panel hidden
      TutorialPanel.self.gameObject.SetActive(false);
   }

   private void Update () {
      // Keep our current step updated for other classes to inspect
      currentStep = getCurrentStep();

      // There are quite a few things that cause the tutorial panel to be hidden
      bool shouldHideTutorialPanel = TitleScreen.self.isShowing() || CharacterScreen.self.isShowing() ||
         GrampsManager.isShowing() || !GrampsManager.hasBeenClosedOnce;

      // Keep the panel hidden until we're ready for it
      canvasGroup.alpha = shouldHideTutorialPanel ? 0 : 1f;
   }

   public static TutorialStep getCurrentTutorialStepData () {
      if (!_tutorialSteps.ContainsKey(currentStep)) {
         return new TutorialStep("", "");
      }
      return _tutorialSteps[currentStep];
   }
   
   [ServerOnly]
   public void sendTutorialInfo (NetEntity player, bool justCompletedStep) {
      List<TutorialInfo> info = new List<TutorialInfo>();

      // Get the info from the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         info = DB_Main.getTutorialInfo(player.userId);

         // Back to Unity
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send the info to the player
            player.Target_ReceiveTutorialInfo(player.connectionToClient, info.ToArray(), justCompletedStep);

            // Store it for later reference
            _tutorialData[player.userId] = info;
         });
      });
   }

   public void receivedTutorialInfo (List<TutorialInfo> tutorialInfo, bool justCompletedStep) {
      _progressInfo = tutorialInfo;
      _hasReceivedInitialData = true;
      currentStep = getCurrentStep();

      // Check if we just completed a step
      if (justCompletedStep) {
         EventManager.TriggerEvent(EventManager.COMPLETED_TUTORIAL_STEP);
      }

      // Make sure the tutorial panel shows
      TutorialPanel.self.gameObject.SetActive(true);
      TutorialPanel.self.updatePanel(tutorialInfo);

      // If we're on the ship step and in our ship, we're done
      if (Global.player is PlayerShipEntity && currentStep == Step.HeadToDocks) {
         Global.player.Cmd_CompletedTutorialStep(TutorialManager.currentStep);
      }
   }

   public static int getHighestCompletedStep (int userId) {
      List<TutorialInfo> list = new List<TutorialInfo>();

      if (_tutorialData.ContainsKey(userId)) {
         list.AddRange(_tutorialData[userId]);
      }

      return getHighestCompletedStep(list);
   }

   protected Step getCurrentStep () {
      if (!_hasReceivedInitialData) {
         return Step.None;
      }

      return (Step) getHighestCompletedStep() + 1;
   }

   protected int getHighestCompletedStep () {
      return getHighestCompletedStep(_progressInfo);
   }

   protected static int getHighestCompletedStep (List<TutorialInfo> tutorialInfo) {
      int highestStep = 0;

      foreach (TutorialInfo info in tutorialInfo) {
         if (info.stepNumber > highestStep) {
            highestStep = info.stepNumber;
         }
      }

      return highestStep;
   }

   #region Private Variables

   // The info on our tutorial progress
   protected List<TutorialInfo> _progressInfo = new List<TutorialInfo>();

   // Gets set to true after we receive data
   protected static bool _hasReceivedInitialData = false;

   // The server caches the tutorial info for everyone
   protected static Dictionary<int, List<TutorialInfo>> _tutorialData = new Dictionary<int, List<TutorialInfo>>();

   // Maps tutorial steps to the associated summary
   protected static Dictionary<Step, TutorialStep> _tutorialSteps = new Dictionary<Step, TutorialStep>() {
        { Step.None, new TutorialStep("", "") },
        { Step.GetDressed, new TutorialStep("Get Dressed!", "Walk up to the dresser with the arrow over it and get dressed.") },
        { Step.FindSeedBag, new TutorialStep("Find the Seed Bag", "Try and find the lost bag of seeds somewhere nearby.") },
        { Step.PlantSeeds, new TutorialStep("Plant some Seeds", "Press 1 to equip the Seed Bag and use it to plant some crops in the holes.") },
        { Step.GetWateringCan, new TutorialStep("Get the Watering Pot", "Grab the watering pot from your house.") },
        { Step.StartWatering, new TutorialStep("Water the Seeds", "Equip the watering can by pressing 2, and use it to water the seeds you planted.") },
        { Step.FinishWatering, new TutorialStep("Water the Crops", "Keep watering the crops until they're fully grown.") },
        { Step.GetPitchfork, new TutorialStep("Get the Pitchfork", "Grab the Pitchfork that Gramps left in the house.") },
        { Step.HarvestCrops, new TutorialStep("Use the Pitchfork", "Press 3 to equip the Pitchfork, and walk over the crops to harvest them.") },
        { Step.HeadToDocks, new TutorialStep("Set Sail!", "Head to the docks at the south end of town to board your ship!") },
        { Step.FindTown, new TutorialStep("Find the Nearby Town", "Sail north until you find a town to sell your crops.") },
        { Step.SellCrops, new TutorialStep("Sell your Crops", "Go into the store to sell the crops.") },
        { Step.FindTreasureSite, new TutorialStep("Find the Treasure", "Sail north and look for the hidden treasure site.") },
        { Step.DestroyGate, new TutorialStep("Attack the Gates", "Hold right-click to fire your cannons at the encampment gates!") },
        { Step.EnterTreasureSite, new TutorialStep("Enter the Treasure Site", "Now that the gates are clear, enter the treasure site!") },
        { Step.ExploreTreasureSite, new TutorialStep("Explore the Treasure Site", "Look around the treasure site to see what you can find!") },
    };

   #endregion
}
