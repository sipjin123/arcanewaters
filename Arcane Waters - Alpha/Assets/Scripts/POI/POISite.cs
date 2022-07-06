using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class POISite
{
   // The group linked to this site
   public int groupId;

   // The list of group instances that compose this POI site, areaKey to groupInstanceId
   public Dictionary<string, int> groupInstanceSet = new Dictionary<string, int>();

   // The last time at least one group member was present in the site (in one of the POI areas)
   public float lastActiveTime = 0f;

   public POISite (int groupId) {
      this.groupId = groupId;
      lastActiveTime = Time.time;
   }
}