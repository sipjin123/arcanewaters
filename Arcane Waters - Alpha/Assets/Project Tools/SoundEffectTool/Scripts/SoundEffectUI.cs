using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SoundEffectUI : MonoBehaviour
{
   #region Public Variables

   // The name that will be shown in the SoundEffectPreviews list
   public Text nameText;

   // The SoundEffect data linked with this entry
   [HideInInspector]
   public SoundEffect effect;

   #endregion

   public void init (SoundEffect effect) {
      this.effect = effect;

      onSoundEffectUpdated();
   }

   public void onSoundEffectUpdated () {
      nameText.text = effect.name;

      validateSoundEffectState();
   }

   private void validateSoundEffectState () {
      if (string.IsNullOrEmpty(effect.name) || effect.name.Contains(SoundEffectTool.DUPLICATE)) {
         addErrorToName(INVALIDNAMING);
      } else if (string.IsNullOrEmpty(effect.clipName)) {
         addErrorToName(ASSIGNCLIP);
      } else if (!string.IsNullOrEmpty(effect.clipName) && effect.clip == null) {
         addErrorToName(BROKENCLIPLINK);
      }
   }

   private void addErrorToName (string error) {
      nameText.text = nameText.text + " (<color=red>" + error + "</color>)";
   }

   #region Private Variables

   // Will be appended to the displayed SoundEffect preview Name(ui only) when the Name is invalid(empty, null or contains (duplicate))
   private const string INVALIDNAMING = "Assign proper Name";

   // Will be appended to the displayed SoundEffect preview Name(ui only) when the ClipName is not assigned(which also means it doesn't have a linked AudioClip)
   private const string ASSIGNCLIP = "Missing AudioClip";

   // Will be appended to the displayed SoundEffect preview Name(ui only) when the ClipName is assigned but the SoundEffect doesn't have an attached AudioClip from the project
   private const string BROKENCLIPLINK = "Broken AudioClip link";

   #endregion
}
