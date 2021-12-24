using Mirror;
using UnityEngine;

[AddComponentMenu("Network/Experimental/NetworkLerpRigidbody2D")]
[HelpURL("https://mirror-networking.com/docs/Components/NetworkLerpRigidbody.html")]
public class NetworkLerpRigidbody2D : NetworkBehaviour
{
   [Header("Settings")]
   [SerializeField] internal Rigidbody2D target = null;
   [Tooltip("How quickly current velocity approaches target velocity")]
   [SerializeField] float lerpVelocityAmount = 0.5f;
   [Tooltip("How quickly current position approaches target position")]
   [SerializeField] float lerpPositionAmount = 0.5f;
   [Tooltip("The distance threshold above which the target is teleported. A value of 0 disables teleporting.")]
   [SerializeField] public float snapPositionThreshold = 0;

   [Tooltip("Set to true if moves come from owner client, set to false if moves always come from server")]
   [SerializeField] bool clientAuthority = false;

   float nextSyncTime;

   [SyncVar()]
   Vector3 targetVelocity;

   [SyncVar()]
   Vector3 targetPosition;

   /// <summary>
   /// Ignore value if is host or client with Authority
   /// </summary>
   /// <returns></returns>
   bool IgnoreSync => isServer || ClientWithAuthority;

   bool ClientWithAuthority => clientAuthority && hasAuthority;

   // The time at which we last had any sort of rigidbody velocity
   public double lastVelocityTime;

   [ExecuteInEditMode]
   void OnValidate () {
      if (target == null) {
         target = GetComponent<Rigidbody2D>();
      }
   }

   void Update () {
      if (isServer) {
         SyncToClients();
      } else if (ClientWithAuthority) {
         SendToServer();
      }

      // Keep track of any times at which we're applying velocity
      if (targetVelocity.magnitude > .00001f) {
         lastVelocityTime = NetworkTime.time;
      }
   }

   private void SyncToClients () {
      targetVelocity = target.velocity;
      targetPosition = target.position;
   }

   private void SendToServer () {
      float now = Time.time;
      if (now > nextSyncTime) {
         nextSyncTime = now + syncInterval;
         CmdSendState(target.velocity, target.position);
      }
   }

   [Command]
   private void CmdSendState (Vector3 velocity, Vector3 position) {
      target.velocity = velocity;
      target.position = position;
      targetVelocity = velocity;
      targetPosition = position;
   }

   void FixedUpdate () {
      if (IgnoreSync) { return; }

      if (snapPositionThreshold > 0 && Vector3.Distance(target.position, targetPosition) > snapPositionThreshold) {
         target.velocity = Vector3.zero;
         target.position = targetPosition;
         return;
      }

      target.velocity = Vector3.Lerp(target.velocity, targetVelocity, lerpVelocityAmount);
      target.position = Vector3.Lerp(target.position, targetPosition, lerpPositionAmount);

      // add velocity to position as position would have moved on server at that velocity
      targetPosition += (Vector3) target.velocity * Time.fixedDeltaTime;
   }
}
