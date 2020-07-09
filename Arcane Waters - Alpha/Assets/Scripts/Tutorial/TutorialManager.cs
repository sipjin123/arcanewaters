using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using UnityEngine.Events;

public class TutorialManager : MonoBehaviour {
   #region Public Variables

   // The current step that this player is on
   public static int currentStep = 0;

   // Our Canvas Group
   public CanvasGroup canvasGroup;

   // Self
   public static TutorialManager self;

   // Event notifying that the setup of the XML is finished
   public UnityEvent finishSetupEvent = new UnityEvent();

   // If xml data is initialized
   public bool hasInitialized;

   #endregion

   private void Awake () {
      self = this;

      // Start out with the tutorial panel hidden
      TutorialPanel.self.gameObject.SetActive(false);
   }

   public TutorialData currentTutorialData () {
      return fetchTutorialData(currentStep);
   }

   public int fetchMaxTutorialCount () {
      return _tutorialDataList.Count;
   }

   public List<TutorialData> tutorialDataList () {
      return _tutorialDataList;
   }

   public void initializeDataCache () {
      _tutorialDataList = new List<TutorialData>();

      // Get the info from the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getTutorialXML();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               TutorialData tutorialData = Util.xmlLoad<TutorialData>(newTextAsset);
               _tutorialDataList.Add(tutorialData);
            }
            finishSetupEvent.Invoke();
            hasInitialized = true;
         });
      });
   }

   public void receiveListFromServer (TutorialData[] tutorialDataArray) {
      if (!hasInitialized) {
         _tutorialDataList = new List<TutorialData>();
         foreach (TutorialData tutorialData in tutorialDataArray) {
            _tutorialDataList.Add(tutorialData);
         }
         hasInitialized = true;
         finishSetupEvent.Invoke();
      }
   }

   private void Update () {
      // Keep our current step updated for other classes to inspect
      currentStep = getCurrentStep();

      // There are quite a few things that cause the tutorial panel to be hidden
      bool shouldHideTutorialPanel = TitleScreen.self.isShowing() || CharacterScreen.self.isShowing();

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

      TutorialData currTutorialData = fetchTutorialData(currentStep);
      string areaType = "";
      if (currTutorialData != null) {
         if (currTutorialData.requirementType == RequirementType.Area) {
            areaType = currTutorialData.rawDataJson;
         }
      }

      // Check if we just completed a step
      if (justCompletedStep) {
         EventManager.TriggerEvent(EventManager.COMPLETED_TUTORIAL_STEP);
      }

      // Make sure the tutorial panel shows
      TutorialPanel.self.gameObject.SetActive(true);
      TutorialPanel.self.updatePanel(tutorialInfo);

      // If we're on the ship step and in our ship, we're done
      if (Global.player is PlayerShipEntity && AreaManager.self.getArea(areaType)?.isSea == true) {
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

   protected int getCurrentStep () {
      if (!_hasReceivedInitialData) {
         return 0;
      }

      return getHighestCompletedStep() + 1;
   }

   protected int getHighestCompletedStep () {
      int returnInt = getHighestCompletedStep(_progressInfo);
      returnInt = Mathf.Clamp(returnInt, 0, _tutorialDataList.Count);
      return returnInt;
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

   public TutorialData fetchTutorialData (int stepID) {
      TutorialData tutorialData = _tutorialDataList.Find(_ => _.stepOrder == stepID);

      if (tutorialData == null) {
         D.debug("Found no tutorial data for step " + stepID + " so returning an empty TutorialData.");
         tutorialData = new TutorialData();
      }

      return tutorialData;
   }

   public TutorialData fetchTutorialData (string stepTitle) {
      TutorialData returnData = _tutorialDataList.Find(_ => _.tutorialName == stepTitle);
      if (returnData == null) {
         D.debug("Problem with Tutorial XML Data: Please create data in tutorial data editor : ({" + stepTitle + "})");
      }
      return returnData;
   }

   #region Private Variables

   // The info on our tutorial progress
   protected List<TutorialInfo> _progressInfo = new List<TutorialInfo>();

   // Gets set to true after we receive data
   protected static bool _hasReceivedInitialData = false;

   // The server caches the tutorial info for everyone
   protected static Dictionary<int, List<TutorialInfo>> _tutorialData = new Dictionary<int, List<TutorialInfo>>();

   // Maps tutorial steps to the associated summary
   protected static Dictionary<int, TutorialStep> _tutorialSteps = new Dictionary<int, TutorialStep>();

   // List of tutorial data
   [SerializeField]
   protected List<TutorialData> _tutorialDataList;

   #endregion
}
