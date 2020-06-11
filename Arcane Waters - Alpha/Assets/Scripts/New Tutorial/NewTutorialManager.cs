using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class NewTutorialManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static NewTutorialManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   public void storeInfoFromDatabase () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         _tutorialList = DB_Main.getNewTutorialList();
         _tutorialAreaKeys = DB_Main.getTutorialAreaKeys();
         _tutorialStepActionList = DB_Main.getTutorialStepActions();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string key in _tutorialAreaKeys) {
               if (!_areaKeyTutorialIdDictionary.ContainsKey(key)) {
                  _areaKeyTutorialIdDictionary.Add(key, _tutorialList.SingleOrDefault(x => x.tutorialAreaKey == key).tutorialId);
               }
            }

            foreach (NewTutorialData data in _tutorialList) {
               if (!_tutorialDataByIdDictionary.ContainsKey(data.tutorialId)) {
                  _tutorialDataByIdDictionary.Add(data.tutorialId, data);
                  _tutorialViewModelList.Add(new TutorialViewModel(data));
               }
            }

            foreach (TutorialStepAction action in _tutorialStepActionList) {
               _tutorialStepActionDictionary.Add(action.code, action);
            }
         });
      });
   }

   public void completeUserStep (string areaKey, int userId, string actionCode) {
      List<UserTutorialStep> completedUserSteps = new List<UserTutorialStep>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         //

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {

         });
      });
   }

   public void showTutorialPanel () {
      NewTutorialPanel panel = (NewTutorialPanel) PanelManager.self.get(Panel.Type.NewTutorial);
      panel.showNewTutorialPanel(_tutorialViewModelList);
   }

   public int getTutorialIdByAreaKey (string areaKey) {
      return isTutorialAreaKey(areaKey) ? _areaKeyTutorialIdDictionary[areaKey] : 0;
   }

   public bool isTutorialAreaKey (string areaKey) {
      return _tutorialAreaKeys.Contains(areaKey);
   }

   public TutorialViewModel getTutorialViewModelForUser (int tutorialId, List<UserTutorialStep> completedUserSteps, int userId) {
      return new TutorialViewModel(_tutorialDataByIdDictionary[tutorialId], completedUserSteps);
   }

   public bool isCurrentMapTutorial () {
      return isTutorialAreaKey(Global.player.areaKey);
   }

   #region Private Variables

   // The list holding the tutorial (and steps) info, as it comes from the db.
   private List<NewTutorialData> _tutorialList;

   // The list holding the area keys that have tutorials assigned, as it comes from the db.
   private List<string> _tutorialAreaKeys;

   // The dictionary holding the area keys by its area keys, for easier access
   private Dictionary<string, int> _areaKeyTutorialIdDictionary = new Dictionary<string, int>();

   // The dictionary holding the tutorial data by its ids, for easier access.
   private Dictionary<int, NewTutorialData> _tutorialDataByIdDictionary = new Dictionary<int, NewTutorialData>();

   // The list containing the tutorials mapped to view models for use in clients
   [SerializeField]
   private List<TutorialViewModel> _tutorialViewModelList = new List<TutorialViewModel>();

   // The list containing the tutorial step actions as they come from the db
   private List<TutorialStepAction> _tutorialStepActionList = new List<TutorialStepAction>();

   // The dictionary holding the available step actions by code as key 
   private Dictionary<string, TutorialStepAction> _tutorialStepActionDictionary = new Dictionary<string, TutorialStepAction>();

   #endregion
}
