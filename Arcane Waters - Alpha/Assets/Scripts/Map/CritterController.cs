using UnityEngine;
using System.Collections;
using MapCreationTool.Serialization;

public class CritterController : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   // The speed at which we move
   public static float SPEED = .6f;

   #endregion

   private void Start () {
      _animator = GetComponent<Animator>();
      _renderer = GetComponent<SpriteRenderer>();
      _rigidbody = GetComponent<Rigidbody2D>();

      // Start off invisible
      _renderer.enabled = false;

      // Note our start position
      _startPos = this.transform.position;

      // Appear every now and then
      InvokeRepeating("appear", Random.Range(0f, 10f), Random.Range(10f, 20f));
   }

   private void Update () {
      // Note whether or not we're moving
      _animator.SetBool("isMoving", _rigidbody.velocity.magnitude > 0f);
   }

   private void appear () {
      // Handle it in a coroutine so that we can wait
      StartCoroutine(CO_Appear());
   }

   private IEnumerator CO_Appear () {
      // Restart our animation state
      _animator.Rebind();

      // Make sure we're visible now
      _renderer.enabled = true;

      // Play a sound
      SoundManager.create3dSound("gopher_" + Random.Range(1, 4), this.transform.position);

      // Wait for our appear animation to finish
      yield return new WaitForSeconds(1.25f);

      // Add some velocity
      _rigidbody.velocity = this.transform.localScale * Vector2.right * SPEED;

      // Give us some time to move
      yield return new WaitForSeconds(.85f);

      // Remove the velocity
      _rigidbody.velocity = Vector2.zero;

      // Wait for our disappear animation to finish
      yield return new WaitForSeconds(3f);

      // Move back to our starting position
      this.transform.position = _startPos;
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField dataField in dataFields) {
         if (dataField.k.Trim(' ').CompareTo(DataField.CRITTER_RUN_DIRECTION_KEY) == 0) {
            if (dataField.v.CompareTo("left") == 0) {
               transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            } else if (dataField.v.CompareTo("right") == 0) {
               transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
         }
      }
   }

   #region Private Variables

   // Our animator
   protected Animator _animator;

   // Our Sprite Renderer
   protected SpriteRenderer _renderer;

   // Our rigid body
   protected Rigidbody2D _rigidbody;

   // The position we started at
   protected Vector2 _startPos;

   #endregion
}
