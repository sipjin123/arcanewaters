// This class contains the information about the actions that are sent across the network to all battlers
public class QueuedRpcAction
{
   // The serialized action data
   public string[] actionSerialized;

   // The type of battle action
   public BattleActionType battleActionType;

   // If action is to be cancelled
   public bool isCancelAction;

   // The time the action should end
   public double actionEndTime;
}