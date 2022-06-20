using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class POISite
{
   // The group linked to this site
   public int groupId;

   // The list of voyage instances that compose this POI site, areaKey to voyageId
   public Dictionary<string, int> voyageInstanceSet = new Dictionary<string, int>();

   public POISite () {

   }
}