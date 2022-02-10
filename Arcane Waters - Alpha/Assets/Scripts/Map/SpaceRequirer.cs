using UnityEngine;

public class SpaceRequirer : MonoBehaviour
{
   #region Public Variables

   // The layer we use to determine if space is taken
   public const string REQUIRER_LAYER_NAME = "SpaceRequirer";

   // Whether to use a custom mask for raycasts
   public bool useCustomMask = false;
   public LayerMask customMask;

   // Whose child objects to ignore while checking collisions
   public Transform ignoreChildrenOf;
   #endregion

   private void Start () {
      PolygonCollider2D[] polys = GetComponentsInChildren<PolygonCollider2D>(true);

      if (_spaceBoxColliders.Length == 0 && _spaceCircleColliders.Length == 0 && polys.Length == 0) {
         D.warning("Space Requirer " + name + " does not have any cooliders.");
         Destroy(this);
      }

      foreach (Collider2D col in _spaceCircleColliders) {
         if (col.gameObject.layer != LayerMask.NameToLayer(REQUIRER_LAYER_NAME)) {
            D.error("Space Requirer " + name + " collider " + col.name + " has incorrect layer " + LayerMask.LayerToName(col.gameObject.layer) + ". Updating...");
            col.gameObject.layer = LayerMask.NameToLayer(REQUIRER_LAYER_NAME);
         }
      }

      foreach (Collider2D col in _spaceBoxColliders) {
         if (col.gameObject.layer != LayerMask.NameToLayer(REQUIRER_LAYER_NAME)) {
            D.error("Space Requirer " + name + " collider " + col.name + " has incorrect layer " + LayerMask.LayerToName(col.gameObject.layer) + ". Updating...");
            col.gameObject.layer = LayerMask.NameToLayer(REQUIRER_LAYER_NAME);
         }
      }

      foreach (Collider2D col in polys) {
         if (col.gameObject.layer != LayerMask.NameToLayer(REQUIRER_LAYER_NAME)) {
            D.error("Space Requirer " + name + " collider " + col.name + " has incorrect layer " + LayerMask.LayerToName(col.gameObject.layer) + ". Updating...");
            col.gameObject.layer = LayerMask.NameToLayer(REQUIRER_LAYER_NAME);
         }
      }
   }

   private void OnEnable () {
      foreach (BoxCollider2D col in _spaceBoxColliders) {
         col.enabled = true;
      }

      foreach (CircleCollider2D col in _spaceCircleColliders) {
         col.enabled = true;
      }
   }

   private void OnDisable () {
      foreach (BoxCollider2D col in _spaceBoxColliders) {
         col.enabled = false;
      }

      foreach (CircleCollider2D col in _spaceCircleColliders) {
         col.enabled = false;
      }
   }

   public bool wouldHaveSpace (Vector2 position) {
      // If we are disabled, we don't require space
      if (!enabled) {
         return true;
      }

      foreach (BoxCollider2D col in _spaceBoxColliders) {
         if (Util.overlapOrEncapsulateAny(col, position, getContactFilter(), ignoreChildrenOf)) {
            return false;
         }
      }

      foreach (CircleCollider2D col in _spaceCircleColliders) {
         if (Util.overlapOrEncapsulateAny(col, position, getContactFilter(), ignoreChildrenOf)) {
            return false;
         }
      }

      return true;
   }

   public bool hasSpace () {
      return wouldHaveSpace(transform.position);
   }

   private ContactFilter2D getContactFilter () {
      if (useCustomMask) {
         return new ContactFilter2D {
            useTriggers = true,
            useLayerMask = true,
            layerMask = customMask
         };
      }

      return new ContactFilter2D {
         useTriggers = true,
         useLayerMask = true,
         layerMask = LayerMask.GetMask(REQUIRER_LAYER_NAME)
      };
   }

   #region Private Variables

   // Polygon colliders can be used on objects that are created together with the area.
   // You can check with other objects against polygon colliders, but not vise-versa
   [Header("Object can still have polygon colliders, but they will not be checked before it is placed.")]
   [SerializeField, Tooltip("Box colliders, which define what space this object needs")]
   private BoxCollider2D[] _spaceBoxColliders = new BoxCollider2D[0];

   [SerializeField, Tooltip("Circle colliders, which define what space this object needs")]
   private CircleCollider2D[] _spaceCircleColliders = new CircleCollider2D[0];

   #endregion
}
