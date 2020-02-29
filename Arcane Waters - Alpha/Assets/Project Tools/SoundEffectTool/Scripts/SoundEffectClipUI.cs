using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SoundEffectClipUI : MonoBehaviour
{
   #region Public Variables

   // The name that will be shown in the SoundEffects list
   public Text clipNameText;

   // The AudioClip linked with this entry
   [HideInInspector]
   public AudioClip clip;

   #endregion

   public void init (AudioClip clip) {
      this.clip = clip;

      clipNameText.text = clip.name;
   }

   #region Private Variables

   #endregion
}
