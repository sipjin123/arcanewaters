using System;

[Serializable]
public class CloudBuildData {
   // The build number
   public int buildId;

   // The commit logs
   public string buildMessage;

   // The date time
   public string buildDateTime;
}