using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class VoyageManager : MonoBehaviour {

   #region Public Variables

   // The area and spawn the user is warped to when leaving a voyage group
   public static string RETURN_AREA_KEY = "StartingTown";
   public static string RETURN_SPAWN_KEY = "ForestTownDock";

   // Self
   public static VoyageManager self;

   #endregion

   void Awake () {
      self = this;

      // Hardcoded data for testing purposes
      store(new Voyage("SeaMiddle", Voyage.Difficulty.Easy, true));
      store(new Voyage("SeaTop", Voyage.Difficulty.Medium, false));
      store(new Voyage("SeaBottom", Voyage.Difficulty.Hard, true));
   }

   public void Start () {
      InvokeRepeating("updateVoyageGroupMembers", 0f, 2f);
   }

   public void store (Voyage voyage) {
      _allVoyages.Add(voyage.areaKey, voyage);
   }

   public Voyage getVoyage (string areaKey) {
      if (_allVoyages.ContainsKey(areaKey)) {
         return _allVoyages[areaKey];
      } else {
         D.error("No voyage is defined for area " + areaKey);
         return null;
      }
   }

   public List<Voyage> getAllVoyages () {
      List<Voyage> voyageList = new List<Voyage>(_allVoyages.Values);
      return voyageList;
   }

   public bool isVoyageArea (string areaKey) {
      foreach(Voyage voyage in _allVoyages.Values) {
         if (string.Equals(voyage.areaKey, areaKey)) {
            return true;
         }
      }
      return false;
   }

   public static bool isInVoyage (NetEntity entity) {
      return entity != null && entity.voyageGroupId != -1;
   }

   public void handleInviteCommand (string inputString) {
      // Split things apart based on spaces
      string[] sections = inputString.Split(' ');

      // Get the user name
      string userName = sections[0];

      // Send the invite request to the server
      Global.player.rpc.Cmd_SendVoyageGroupInvitationToUser(userName);
   }

   public void acceptVoyageInvitation () {
      if (Global.player == null || Global.player.isInBattle()) {
         return;
      }

      // Retrieve the voyage group id from the invite panel
      int voyageGroupId = PanelManager.self.voyageInviteScreen.getVoyageGroupId();

      // Hide the invite panel
      PanelManager.self.voyageInviteScreen.hide();

      // Send the join request to the server
      Global.player.rpc.Cmd_AddUserToVoyageGroup(voyageGroupId);
   }

   private void updateVoyageGroupMembers () {
      if (Global.player == null || !Global.player.isLocalPlayer) {
         return;
      }

      // Check if the player is not in a group
      if (!isInVoyage(Global.player)) {
         // If the player just left his group, request an update from the server
         if (_visibleGroupMembers.Count > 0) {
            _visibleGroupMembers.Clear();
            Global.player.rpc.Cmd_RequestVoyageGroupMembersFromServer();
         }
         return;
      }

      // Get the list of all entities (visible by this client)
      List<NetEntity> allEntities = EntityManager.self.getAllEntities();

      // Retrieve the group members that we can see
      List<int> visibleGroupMembers = new List<int>();
      foreach (NetEntity entity in allEntities) {
         if (entity != null && entity.voyageGroupId == Global.player.voyageGroupId) {
            visibleGroupMembers.Add(entity.userId);
         }
      }

      // Compare this new list with the latest
      bool hasNewGroupMembers = visibleGroupMembers.Except(_visibleGroupMembers).Any();
      bool hasMissingGroupMembers = _visibleGroupMembers.Except(visibleGroupMembers).Any();

      // If there are differences, request a full group members update from the server
      if (hasNewGroupMembers || hasMissingGroupMembers) {
         Global.player.rpc.Cmd_RequestVoyageGroupMembersFromServer();
      }

      // Save this list for future comparisons
      _visibleGroupMembers = visibleGroupMembers;
   }

   #region Private Variables

   // Keeps track of the voyages, accessible by areaKey
   private Dictionary<string, Voyage> _allVoyages = new Dictionary<string, Voyage>();

   // Keeps track of the group members visible by this client
   private List<int> _visibleGroupMembers = new List<int>();

   #endregion
}
