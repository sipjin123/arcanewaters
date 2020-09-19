using UnityEngine;
using System;

[Serializable]
public class BoundKeyAction
{
   #region Public Variables

   // Key for saving action locally
   public const string LOCAL_SAVE_KEY = "keybindings_action";

   // The type of action we are binding
   public KeyAction action;

   // The primary key for performing this action
   public KeyCode primary;

   // The secondary key for performing this action
   public KeyCode secondary;

   #endregion

   public void loadLocal () {
      if (PlayerPrefs.HasKey(LOCAL_SAVE_KEY + (int) action)) {
         BoundKeyAction savedAction = JsonUtility.FromJson<BoundKeyAction>(PlayerPrefs.GetString(LOCAL_SAVE_KEY + (int) action));

         if (savedAction.primary != KeyCode.None) {
            primary = savedAction.primary;
         }

         if (savedAction.secondary != KeyCode.None) {
            secondary = savedAction.secondary;
         }
      }
   }

   public void saveLocal () {
      PlayerPrefs.SetString(LOCAL_SAVE_KEY + (int) action, JsonUtility.ToJson(this));
      PlayerPrefs.SetString(LOCAL_SAVE_KEY + (int) action, JsonUtility.ToJson(this));
   }

   #region Private Variables

   #endregion
}