using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System;

public class GUI_SoundEffects : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerDownHandler {
   #region Public Variables

   #endregion

   void Start () {
      // Look up the audio source
      if (_audioSource == null) {
         _audioSource = GameObject.FindGameObjectWithTag("GUI Audio Source").GetComponent<AudioSource>();
      }
      _button = GetComponent<Button>();
   }

   public virtual void OnPointerEnter (PointerEventData eventData) {
      if (_button && _button.IsInteractable()) {
         play("Sound/Effects/ui_hover");
      }
   }

   public virtual void OnPointerClick (PointerEventData eventData) {
      
   }

   public virtual void OnPointerDown (PointerEventData eventData) {
      if (_button && _button.IsInteractable()) {
         play("Sound/Effects/GUI_Press");
      }
   }

   protected void play (string path) {
      _audioSource.volume = SoundManager.effectsVolume;

      // Don't play it if not enough time has passed
      if (_lastPlayTime.ContainsKey(path) && Time.time - _lastPlayTime[path] < .10f) {
         return;
      }

      // Make note of the time
      _lastPlayTime[path] = Time.time;

      // Check if we've cached the clip
      AudioClip clip = _cachedClips.ContainsKey(path) ? _cachedClips[path] : Resources.Load<AudioClip>(path);

      // Cache it for later
      _cachedClips[path] = clip;

      // Play a button hover sound effect
      _audioSource.PlayOneShot(clip);
   }

   #region Private Variables

   // Button that is causing sound after hovering/pressing
   protected Button _button;

   // Our Audio Source
   protected static AudioSource _audioSource;

   // The time at which we last played a specified clip
   protected static Dictionary<string, float> _lastPlayTime = new Dictionary<string, float>();

   // Clips that we've cached for later reuse
   protected static Dictionary<string, AudioClip> _cachedClips = new Dictionary<string, AudioClip>();

   #endregion
}
