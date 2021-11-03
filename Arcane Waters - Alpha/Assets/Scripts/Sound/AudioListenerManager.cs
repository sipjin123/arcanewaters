using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AudioListenerManager : GenericGameManager
{
   #region Public Variables

   // A static reference to the single instance of this class
   public static AudioListenerManager self;

   // An event that triggers every time the audio listener is changed
   public System.Action onListenerChanged;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   private void Start () {
      AudioListener startingListener = FindObjectOfType<AudioListener>();
      FMODUnity.StudioListener startingFmodListener = FindObjectOfType<FMODUnity.StudioListener>();

      setActiveListener(startingListener);
      setActiveFmodListener(startingFmodListener);

      GameObject fmodStudioGo = new GameObject();
      fmodStudioGo.name = "FMOD Studio Listener";
      fmodStudioGo.transform.SetParent(this.transform);
      _playerFmodListener = fmodStudioGo.AddComponent<FMODUnity.StudioListener>();
      _playerFmodListener.enabled = false;
   }

   private void Update () {
      if (Global.player != null) {
         this._playerFmodListener.transform.position = new Vector3(Global.player.transform.position.x, Global.player.transform.position.y, CameraManager.getCurrentCamera().transform.position.z);
      }
   }

   public void setActiveListener (AudioListener newListener) {
      if (_activeListener) {
         _activeListener.enabled = false;
      }

      _activeListener = newListener;
      _activeListener.enabled = true;
      onListenerChanged?.Invoke();
   }

   public void setActiveFmodListener (FMODUnity.StudioListener newListener) {
      if (newListener == null) {
         newListener = _playerFmodListener;
      }

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

   // A reference to the FMOD Studio Listener that will follow the player's X and Y, but keep the current camera's Z position.
   private FMODUnity.StudioListener _playerFmodListener;

   #endregion
}
