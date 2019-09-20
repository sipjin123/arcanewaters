using UnityEngine;
using System.Collections;
using System.Xml.Serialization;

public abstract class QuestObjective
{
   #region Public Variables
      
   #endregion

   // Must be called from the background thread!
   public abstract bool canObjectiveBeCompletedDB (int userId);

   // Must be called from the background thread!
   public abstract void completeObjective (int userId);

   // Must be called from the background thread!
   public abstract int getObjectiveProgress (int userId);

   public abstract bool canObjectiveBeCompleted (int progress);

   public abstract string getObjectiveDescription ();

   public abstract Sprite getIcon ();

   public abstract string getProgressString (int progress);

   #region Private Variables

   #endregion
}
