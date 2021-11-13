using System;

[Serializable]
public class QuestTimer {
   // Primary key id of the timer data
   public int timerId;

   // Basic info of the quest timer
   public string name, description;

   // The repeat rate of the quest in minutes
   public int repeatRateInMins;

   // The time this quest was deployed
   public double timeDeployed;
}