using UnityEngine;
using FMODUnity;

public class FmodTester : MonoBehaviour {
   #region Public Variable

   // The cached event state that plays loop sounds
   FMOD.Studio.EventInstance playerState;

   // Event that is earchable using this attribute
   [EventRef]
   public string searchableEvent;

   // Loop sound event directories
   public string loopSound1 = "";
   public string loopSound2 = "";

   // One shot sound event directories
   public string onceShotEvent1 = "";
   public string oneShotEvent2 = "";

   #endregion

   public void Start () {
      // Assign the strings real time
      onceShotEvent1 = "event:/SFX/Player/Interactions/Diegetic/Jump";
      oneShotEvent2 = "event:/SFX/Player/Interactions/Diegetic/Land";
      loopSound1 = "event:/SFX/Ambience/Beds/Ocean_Pad_01";
      loopSound2 = "event:/SFX/Ambience/Beds/Ocean_Pad_02";

      // Make sure that the loop sound is attached to this object
      RuntimeManager.AttachInstanceToGameObject(playerState, GetComponent<Transform>(), GetComponent<Rigidbody>());
   }

   public void Update () {
      // Triggers the sfx using the xml id fetched from the web tool
      if (KeyUtils.GetKeyDown(UnityEngine.InputSystem.Key.E)) {
         SoundEffectManager.self.playSoundEffect(3, transform);
      }

      // Plays the first one shot event
      if (KeyUtils.GetKeyDown(UnityEngine.InputSystem.Key.X)) {
         RuntimeManager.PlayOneShot(onceShotEvent1, transform.position);
      }

      // Plays the second one shot event
      if (KeyUtils.GetKeyDown(UnityEngine.InputSystem.Key.Z)) {
         RuntimeManager.PlayOneShot(oneShotEvent2, transform.position);
      }

      // Plays the first loop event
      if (KeyUtils.GetKeyDown(UnityEngine.InputSystem.Key.C)) {
         playerState.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
         playerState = RuntimeManager.CreateInstance(loopSound1);
         playerState.start();
      }

      // Plays the second loop event
      if (KeyUtils.GetKeyDown(UnityEngine.InputSystem.Key.V)) {
         playerState.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
         playerState = RuntimeManager.CreateInstance(loopSound2);
         playerState.start();
      }  

      // Cancels the last loop event being played
      if (KeyUtils.GetKeyDown(UnityEngine.InputSystem.Key.Q)) {
         playerState.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
      }
   }

   #region Private Variables

   #endregion
}
