using System;

[Serializable]
public class RefinementData {
   public RefinementData () { }
   
   // The xml id
   public int xmlId;

   // The item requirements of the refinement
   public Item[] combinationRequirements;
}
