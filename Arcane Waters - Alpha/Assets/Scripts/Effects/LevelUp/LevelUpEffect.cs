using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;

[RequireComponent(typeof(SpriteRenderer))]
public class LevelUpEffect : MonoBehaviour {
   #region Public Variables

   // The gameobjects that should be hidden while the effect is visible
   public GameObject[] objectsToHide;

   // Event called when the effect ends
   public UnityEvent onEffectEnded;

   // Speed (seconds per frame)
   public float timePerFrame = 0.25f;

   // The first frame of the animation
   public int startIndex = 0;

   #endregion

   private void OnEnable () {
      _renderer = GetComponent<SpriteRenderer>();

      if (_activeObjects == null) {
         _activeObjects = new List<GameObject>();
      }
   }

   private void Update () {
      if (_renderer != null) {
         _renderer.flipX = false;
      }
   }

   public bool isVisible () {
      return _isVisible;
   }

   public void play(Jobs.Type jobType) {
      Sprite[] foundSprites = getSpritesForJobType(jobType);

      if (foundSprites == null) {
         D.debug($"Couldn't play the level up effect for job type {jobType}. Effect texture not found.");
         return;
      }

      _index = Mathf.Max(0, startIndex);
      _jobType = jobType;
      _sprites = foundSprites;

      this.gameObject.SetActive(true);
      _activeObjects.Clear();

      // Filter the items that were actually active before the effect played
      foreach (GameObject activeGameObject in objectsToHide) {
         if (activeGameObject.activeSelf) {
            _activeObjects.Add(activeGameObject);
         }
      }

      toggleGameObjects(show: false);
      InvokeRepeating(nameof(changeSprite), 0f, timePerFrame);
   }

   public void stop () {
      CancelInvoke(nameof(changeSprite));
      
      // Disable the effect
      this.gameObject.SetActive(false);

      // Display the objects that were hidden
      toggleGameObjects(show: true);

      _isVisible = false;

      // Report listeners
      if (onEffectEnded != null) {
         onEffectEnded.Invoke();
      }
   }

   protected void changeSprite () {
      _isVisible = true;

      if (_index >= _sprites.Length) {
         stop();
         return;
      }

      _renderer.sprite = _sprites[_index];
      _index++;
   }

   private Sprite[] getSpritesForJobType(Jobs.Type jobType) {
      // Load our sprites
      string path = "Effects/LevelUp/level_up_effect_" + jobType.ToString().ToLower();
      Sprite[] sprites = ImageManager.getSprites(path);

      if (sprites == null || sprites.Length == 0) {
         return null;
      }

      return sprites;
   }

   private void toggleGameObjects (bool show = true) {
      if (_activeObjects == null || _activeObjects.Count == 0) {
         return;
      }

      foreach (GameObject gameObject in _activeObjects) {
         gameObject.SetActive(show);
      }
   }

   #region Private Variables

   // Current index
   private int _index;

   // Is the effect visible;
   private bool _isVisible;

   // Current job type
   private Jobs.Type _jobType;

   // Reference to the renderer
   private SpriteRenderer _renderer;

   // Set of sprites
   private Sprite[] _sprites;

   // Gameobjects that were active before the effect
   private List<GameObject> _activeObjects;

   #endregion
}
