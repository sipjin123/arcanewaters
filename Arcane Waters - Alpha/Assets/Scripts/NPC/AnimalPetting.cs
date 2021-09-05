using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AnimalPetting : MonoBehaviour {
   #region Public Variables

   public enum ReactionType
   {
      None = 0,
      Hearts = 1,
      Angry = 2,
      Confused = 3
   }

   #endregion

   public void playAnimalAnimation(NPC npc, ReactionType reactionType) {
      _npc = npc;
      _initialPosition = transform.position;
      _currentPosition = _initialPosition;

      // Hide all position of animal petting
      foreach (GameObject spot in _npc.animalPettingPositions) {
         spot.SetActive(false);
      }
      StopAllCoroutines();

      switch (reactionType) {
         case ReactionType.None:
            break;
         case ReactionType.Hearts:
            StartCoroutine(CO_PlayAnimalJumpingAnimation());
            break;
         case ReactionType.Angry:
            StartCoroutine(CO_PlayAnimalAngryAnimation());
            break;
         case ReactionType.Confused:
            StartCoroutine(CO_PlayAnimalConfusedAnimation());
            break;
      }
   }

   public void hideSpotsAfterTime (NPC npc) {
      _npc = npc;
      StartCoroutine(CO_HideSpots());
   }

   private void Update () {
      // Do not process the animation altering for server to prevent modifying client position via smooth sync
      if (_npc.isServer) {
         return;
      } 

      if (_isAnimalAnimationPlaying) {
         transform.position = Vector3.Lerp(_currentPosition, _initialPosition + _additionalPosition, _currentTransitionTime / _maxTransitionTime);
         _currentPosition = transform.position;
         _currentTransitionTime += Time.deltaTime;
      }
   }

   private IEnumerator CO_HideSpots () {
      yield return new WaitForSeconds(5.0f);
      foreach (GameObject spot in _npc.animalPettingPositions) {
         spot.SetActive(false);
      }
   }

   private IEnumerator CO_PlayAnimalJumpingAnimation () {
      yield return new WaitForSeconds(1.4f);
      _npc.finishAnimalPetting();
      createAnimator("playHearts");
      _isAnimalAnimationPlaying = true;

      // SFX
      SoundEffectManager.self.playFmodSfx(SoundEffectManager.AFFECTION_EMOTE, this.transform);
      SoundEffectManager.self.playAnimalCry(_npc.spritePath, this.transform);

      for (int i = 0; i < 2; i++) {
         _additionalPosition = new Vector3(0, 0.02f, 0);
         _maxTransitionTime = 0.05f;
         _currentTransitionTime = 0.0f;
         yield return new WaitForSeconds(0.05f);

         _additionalPosition = new Vector3(0, 0.05f, 0);
         _maxTransitionTime = 0.1f;
         _currentTransitionTime = 0.0f;
         yield return new WaitForSeconds(0.1f);

         _additionalPosition = new Vector3(0, 0.02f, 0);
         _maxTransitionTime = 0.05f;
         _currentTransitionTime = 0.0f;
         yield return new WaitForSeconds(0.05f);

         _additionalPosition = new Vector3(0, 0.0f, 0);
         _maxTransitionTime = 0.05f;
         _currentTransitionTime = 0.0f;
         yield return new WaitForSeconds(0.05f);

         yield return new WaitForSeconds(0.07f);
      }

      _isAnimalAnimationPlaying = false;
      _npc.finishAnimalReaction();
   }

   private IEnumerator CO_PlayAnimalAngryAnimation () {
      yield return new WaitForSeconds(1.4f);
      _npc.finishAnimalPetting();
      createAnimator("playAngry");
      _isAnimalAnimationPlaying = true;

      const float timePerFrame = 0.04f;
      _maxTransitionTime = timePerFrame;

      // SFX
      SoundEffectManager.self.playFmodSfx(SoundEffectManager.ANGER_EMOTE, this.transform);
      SoundEffectManager.self.playAnimalCry(_npc.spritePath, this.transform);

      for (int i = 0; i < 2; i++) {
         _additionalPosition = new Vector3(-0.01f, 0, 0);
         _currentTransitionTime = 0.0f;
         yield return new WaitForSeconds(timePerFrame);

         _additionalPosition = new Vector3(0.0f, 0, 0);
         _currentTransitionTime = 0.0f;
         yield return new WaitForSeconds(timePerFrame);

         _additionalPosition = new Vector3(0.01f, 0, 0);
         _currentTransitionTime = 0.0f;
         yield return new WaitForSeconds(timePerFrame);

         _additionalPosition = new Vector3(0.0f, 0, 0);
         _currentTransitionTime = 0.0f;
         yield return new WaitForSeconds(timePerFrame);

         _additionalPosition = new Vector3(-0.01f, 0, 0);
         _currentTransitionTime = 0.0f;
         yield return new WaitForSeconds(timePerFrame);

         _additionalPosition = new Vector3(0.0f, 0, 0);
         _currentTransitionTime = 0.0f;
         yield return new WaitForSeconds(timePerFrame);

         _additionalPosition = new Vector3(0.01f, 0, 0);
         _currentTransitionTime = 0.0f;
         yield return new WaitForSeconds(timePerFrame);

         _additionalPosition = new Vector3(0.0f, 0, 0);
         _currentTransitionTime = 0.0f;
         yield return new WaitForSeconds(timePerFrame);
      }

      _isAnimalAnimationPlaying = false;
      _npc.finishAnimalReaction();
   }

   private IEnumerator CO_PlayAnimalConfusedAnimation () {
      yield return new WaitForSeconds(1.4f);
      _npc.finishAnimalPetting();
      createAnimator("playConfused");

      // SFX
      SoundEffectManager.self.playFmodSfx(SoundEffectManager.QUESTION_EMOTE, this.transform);
      SoundEffectManager.self.playAnimalCry(_npc.spritePath, this.transform);

      yield return new WaitForSeconds(1.0f);
      _npc.finishAnimalReaction();
   }

   private void createAnimator (string stateToPlay) {
      _pettingAnimator = Instantiate(PrefabsManager.self.pettingAnimatorPrefab).GetComponent<Animator>();
      _pettingAnimator.gameObject.SetActive(true);
      _pettingAnimator.transform.SetParent(this.transform);
      _pettingAnimator.transform.localPosition = new Vector3(0, -GetComponent<ZSnap>().offsetZ * 2.0f, -1e-5f);
      _pettingAnimator.SetBool(stateToPlay, true);
      _pettingAnimator.GetComponent<DestroyAfterAnimation>().enabled = true;
   }

   #region Private Variables

   // Component playing animation after petting animal
   protected Animator _pettingAnimator;

   // Original position of NPC stored to restore after animation is finished
   private Vector3 _initialPosition;

   // Current NPC's position after applying animation translation
   private Vector3 _currentPosition;

   // Position to add in this animation frame
   private Vector3 _additionalPosition;

   // Time of this animation frame
   private float _maxTransitionTime;

   // Increasing time of animation frame
   private float _currentTransitionTime;

   // Indicates if animal animation is currently playing
   private bool _isAnimalAnimationPlaying = false;

   // Reference to NPC script to callback after finishing all animations
   private NPC _npc;

   #endregion
}
