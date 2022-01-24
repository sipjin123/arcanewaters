using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UINavigationManager : MonoBehaviour {
   #region Public Variables
   public static UINavigationManager self;
   #endregion

   public void ControllerEnabled (UINavigationController navigationController) {
      _navigationControllers.Add(navigationController);
      RefreshControllers();
   }

   public void ControllerDisabled (UINavigationController navigationController) {
      _navigationControllers.Remove(navigationController);
      RefreshControllers();
   }
      
   private void Awake () {
      self = this;
      _navigationControllers = new List<UINavigationController>();
   }

   private void RefreshControllers () {
      // Disable all controllers
      foreach (var navigationController in _navigationControllers.Take(_navigationControllers.Count-1)) {
         navigationController.isLocked = true;
      }
      
      // Enable last one
      if (_navigationControllers.Count > 0) {
         _navigationControllers.Last().isLocked = false;
         _navigationControllers.Last().updateSelection(true);
      }
   }
   
   #region Private Variables
   private List<UINavigationController> _navigationControllers;
   #endregion
}
