using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AudioListenerManager : MonoBehaviour {
   #region Public Variables

   // A static reference to the single instance of this class
   public static AudioListenerManager self;

   // An event that triggers every time the audio listener is changed
   public System.Action onListenerChanged;

   #endregion

   private void Awake () {
      self = this;
      AudioListener startingListener = FindObjectOfType<AudioListener>();
      setActiveListener(startingListener);
   }
   
   public void setActiveListener (AudioListener newListener) {
      if (_activeListener) {
         _activeListener.enabled = false;
      }
      
      _activeListener = newListener;
      _activeListener.enabled = true;
      onListenerChanged?.Invoke();
   }

   public AudioListener getActiveListener () {
      return _activeListener;
   }

   public float getActiveListenerZ () {
      return _activeListener.transform.position.z;
   }

   #region Private Variables

   // A reference to the AudioListener that is being used
   private AudioListener _activeListener;

   #endregion
}
