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

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string key in _tutorialAreaKeys) {
               _areaKeyTutorialIdDictionary.Add(key, _tutorialList.SingleOrDefault(x => x.tutorialAreaKey == key).tutorialId);
            }

            foreach (NewTutorialData data in _tutorialList) {
               _tutorialDataByIdDictionary.Add(data.tutorialId, data);
               _tutorialViewModelList.Add(new TutorialViewModel(data));
            }
         });
      });
   }

   // TODO
   public void loadUserTutorialInfo (string areaKey, int userId) {
      int tutorialId = _areaKeyTutorialIdDictionary[areaKey];
      loadCompletedStepsForUser(tutorialId, userId);
   }

   public void showTutorialPanel () {
      NewTutorialPanel panel = (NewTutorialPanel) PanelManager.self.get(Panel.Type.NewTutorial);
      panel.showNewTutorialPanel(_tutorialViewModelList);
   }

   // TODO
   private void loadCompletedStepsForUser (int tutorialId, int userId) {
      List<UserTutorialStep> completedUserSteps = new List<UserTutorialStep>();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         completedUserSteps = DB_Main.getUserCompletedSteps(userId, tutorialId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadUserTutorialData(tutorialId, completedUserSteps, userId);
         });
      });
   }

   // TODO
   private void loadUserTutorialData (int tutorialId, List<UserTutorialStep> completedUserSteps, int userId) {
      TutorialViewModel tutorialViewModel = new TutorialViewModel(_tutorialDataByIdDictionary[tutorialId], completedUserSteps);
      // Send data via RPC
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
   private List<TutorialViewModel> _tutorialViewModelList = new List<TutorialViewModel>();

   #endregion
}
