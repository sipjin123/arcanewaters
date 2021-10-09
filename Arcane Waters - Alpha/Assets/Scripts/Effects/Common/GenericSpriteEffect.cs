using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using DG.Tweening;

public class GenericSpriteEffect : MonoBehaviour
{
   #region Public Variables

   // The gameobjects that should be hidden while the effect is visible
   public GameObject[] objectsToHide;

   // Event called when the effect ends
   public UnityEvent onEffectEnded;

   // Start delay
   public float startDelay = 0.5f;

   // Animation speed
   public float secondsPerFrame = 0.25f;

   // The first frame of the animation
   public int startIndex = 0;

   // The time that the the effect will wait before disappearing
   public float holdTime = 5.0f;

   // Should the effect disappear with a fade or instantly?
   public bool shouldFadeOut = false;

   // The duration of the fade
   public float fadeDuration = 3.0f;

   // Should the effect cause the containing object to be destryed?
   public bool destroyOnEnd = false;

   // Reference to the texture that holds the frames of the effect
   public Texture2D texture;

   // Reference to the Sprite renderer
   public SpriteRenderer spriteRenderer;

   #endregion

   private void OnEnable () {
      if (_activeObjects == null) {
         _activeObjects = new List<GameObject>();
      }
   }

   private void Update () {
      if (spriteRenderer != null) {
         spriteRenderer.flipX = false;
      }
   }

   public bool isVisible () {
      return _isVisible;
   }

   public void play () {
      Sprite[] foundSprites = ImageManager.getSprites(texture);

      if (foundSprites == null) {
         D.debug($"Couldn't play the effect because the texture could not found.");
         return;
      }

      _index = Mathf.Max(0, startIndex);
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

      // Reset opacity
      Color oldColor = spriteRenderer.color;
      spriteRenderer.color = new Color(oldColor.r, oldColor.g, oldColor.b, 1.0f);

      InvokeRepeating(nameof(changeSprite), startDelay, secondsPerFrame);
   }

   public void stop () {
      CancelInvoke(nameof(changeSprite));
      hold();
   }

   private void hold () {
      Invoke(nameof(onHoldEnded), holdTime);
   }

   private void onHoldEnded () {
      if (!shouldFadeOut) {
         hide();
         return;
      }

      spriteRenderer.DOFade(0.0f, fadeDuration).OnComplete(hide);
   }

   private void hide () {
      // Disable the effect
      this.gameObject.SetActive(false);

      // Display the objects that were hidden
      toggleGameObjects(show: true);

      _isVisible = false;
      spriteRenderer.DOKill();

      // Report listeners
      if (onEffectEnded != null) {
         onEffectEnded.Invoke();
      }

      // Destroy the gameobject if necessary
      if (destroyOnEnd) {
         Destroy(gameObject);
      }
   }

   protected void changeSprite () {
      _isVisible = true;

      if (_index >= _sprites.Length) {
         stop();
         return;
      }

      spriteRenderer.sprite = _sprites[_index];
      _index++;
   }

   private Sprite[] getEffectSprites (string path) {
      // Load our sprites
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

   // Set of sprites
   private Sprite[] _sprites;

   // Gameobjects that were active before the effect
   private List<GameObject> _activeObjects;

   #endregion
}