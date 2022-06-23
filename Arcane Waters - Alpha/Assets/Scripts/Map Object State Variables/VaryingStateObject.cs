using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using MapCreationTool.Serialization;
using MapObjectStateVariables;

public class VaryingStateObject : NetworkBehaviour, IMapEditorDataReceiver, IObserver
{
   #region Public Variables

   // Can this object be interacted with by the user
   public bool canUserInteractWith = false;

   // The state this object currently has
   [SyncVar(hook = nameof(stateSyncVarChanged))]
   public string state = "";

   // The instance this object belongs to
   [SyncVar, Space(10)]
   public int instanceId;

   // The area key this object belongs to
   [SyncVar]
   public string areaKey;

   // The local position to an area
   [SyncVar]
   public Vector2 localPosition;

   // Unique ID for a prefab (only within it's area) provided by map editor
   public int mapEditorId;

   // To which objects' ids does this object potentially react
   public readonly List<int> reactsTo = new List<int>();

   // Which objects react to this object's state changes
   public readonly List<int> triggersObjects = new List<int>();

   #endregion

   private void Awake () {
      if (NetworkClient.active) {
         _outline = GetComponent<SpriteOutline>();
         _haveOutline = _outline != null;

         if (_haveOutline) {
            _outline.setNewColor(Color.white);
            _outline.setVisibility(false);
         }
      }
   }

   private void Start () {
      StartCoroutine(CO_SetAreaParent());
   }

   protected IEnumerator CO_SetAreaParent () {
      // Wait until we have finished instantiating the area
      Area area = null;
      if (AreaManager.self == null) {
         yield return null;
      }
      while (!AreaManager.self.tryGetArea(areaKey, out area)) {
         yield return 0;
      }

      transform.parent = area.prefabParent.transform;
      transform.localPosition = localPosition;
      if (TryGetComponent(out ZSnap snap)) {
         snap.snapZ();
      }
   }

   private void stateSyncVarChanged (string oldVal, string newVal) {
      onStateChanged(newVal);
   }

   protected virtual void onStateChanged (string state) { }
   protected virtual void clientInteract () { }

   public bool clientTriesInteracting (Vector2 mouseWorldPos) {
      if (Global.player == null) {
         return false;
      }

      if (!canUserInteractWith) {
         return false;
      }

      // Only works when the player is close enough
      if (!Util.distanceLessThan2D(transform.position, Global.player.transform.position, NPC.TALK_DISTANCE)) {
         FloatingCanvas.instantiateAt(mouseWorldPos).asTooFar();
         return true;
      }

      clientInteract();

      return true;
   }

   public override void OnStartClient () {
      if (TryGetComponent(out ZSnap zSnap)) {
         zSnap.snapZ();
      }
   }

   public override void OnStartServer () {
      if (TryGetComponent(out ZSnap zSnap)) {
         zSnap.snapZ();
      }
   }

   [Client]
   protected void requestStateState (string state) {
      if (Time.frameCount == _lastStateChangeFrame) {
         return;
      }
      _lastStateChangeFrame = Time.frameCount;

      Cmd_RequestSetState(state);
   }

   [Command(ignoreAuthority = true)]
   private void Cmd_RequestSetState (string state) {
      if (InstanceManager.self.tryGetInstance(instanceId, out Instance instance)) {
         instance.requestChangeStateCascading(this, state);
      }
   }

   [Server]
   public void initializeState () {
      if (_stateModel == null) {
         D.error("Object " + name + " has a null state model");
         return;
      }

      state = _stateModel.state;
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.Equals(DataField.MAP_OBJECT_STATE_MODEL_KEY)) {
            _stateModel = ObjectStateModel.deserializeOrDefault(field.v);
         } else if (field.k.Equals(DataField.PLACED_PREFAB_ID)) {
            field.tryGetIntValue(out mapEditorId);
         }
      }

      receiveMapEditorData(dataFields);
   }

   protected virtual void receiveMapEditorData (DataField[] dataFields) { }

   public int getInstanceId () {
      return instanceId;
   }

   public ObjectStateModel getStateModel () {
      return _stateModel;
   }

   public HashSet<int> findDependantObjectIds () {
      if (_stateModel != null) {
         return _stateModel.stateExpression.findDependantObjectIds();
      }
      return new HashSet<int>();
   }

   public void onPointerExit () {
      _hovered = false;

      if (_haveOutline) {
         _outline.setVisibility(false);
      }
   }

   public void onPointerEnter () {
      _hovered = true;

      if (canUserInteractWith && _haveOutline) {
         // Don't use outlines on-pointer anymore
         //_outline.setVisibility(true);
      }
   }

   #region Private Variables

   // Describes how the state of this object can change
   private ObjectStateModel _stateModel;

   // Last time we tried to change the state on client
   private int _lastStateChangeFrame = 0;

   // Outline componenet, if we have it
   private SpriteOutline _outline;
   private bool _haveOutline = false;

   // Are we hovered right now
   private bool _hovered = false;

   #endregion
}