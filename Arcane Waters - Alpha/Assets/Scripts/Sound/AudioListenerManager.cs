using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AudioListenerManager : GenericGameManager {
   #region Public Variables

   // A static reference to the single instance of this class
   public static AudioListenerManager self;

   // An event that triggers every time the audio listener is changed
   public System.Action onListenerChanged;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
      AudioListener startingListener = FindObjectOfType<AudioListener>();
      FMODUnity.StudioListener startingFmodListener = FindObjectOfType<FMODUnity.StudioListener>();

      setActiveListener(startingListener);
      setActiveFmodListener(startingFmodListener);
   }
   
   public void setActiveListener (AudioListener newListener) {
      if (_activeListener) {
         _activeListener.enabled = false;
      }
      
      _activeListener = newListener;
      _activeListener.enabled = true;
      onListenerChanged?.Invoke();
   }

   public void setActiveFmodListener(FMODUnity.StudioListener newListener) {
      if (_activeFmodListener) {
         _activeFmodListener.enabled = false;
      }

      _activeFmodListener = newListener;
      _activeFmodListener.enabled = true;
      onListenerChanged?.Invoke();
   }

   public AudioListener getActiveListener () {
      return _activeListener;
   }

   public FMODUnity.StudioListener getActiveFmodListener () {
      return _activeFmodListener;
   }

   public float getActiveListenerZ () {
      return _activeListener.transform.position.z;
   }

   #region Private Variables

   // A reference to the AudioListener that is being used
   private AudioListener _activeListener;

   // A reference to the FMOD Studio Listener that is being used
   private FMODUnity.StudioListener _activeFmodListener;

   #endregion
}
