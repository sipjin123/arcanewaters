using System.Collections.Generic;
using System;
namespace CloudBuildDataFetch
{
   [Serializable]
   public class Author
   {
      public string absoluteUrl;
      public string fullName;
   }

   [Serializable]
   public class Changeset
   {
      public string commitId;
      public string message;
      public DateTime timestamp;
      public string _id;
      public Author author;
      public int numAffectedFiles;
   }

   [Serializable]
   public class ProjectVersion
   {
      public string name;
      public string filename;
      public string projectName;
      public string platform;
      public int size;
      public DateTime created;
      public DateTime lastMod;
      public IList<object> udids;
   }

   [Serializable]
   public class Self
   {
      public string method;
      public string href;
   }

   [Serializable]
   public class Log
   {
      public string method;
      public string href;
   }

   [Serializable]
   public class Auditlog
   {
      public string method;
      public string href;
   }

   [Serializable]
   public class CreateShare
   {
      public string method;
      public string href;
   }

   [Serializable]
   public class RevokeShare
   {
      public string method;
      public string href;
   }

   [Serializable]
   public class Icon
   {
      public string method;
      public string href;
   }

   [Serializable]
   public class Meta
   {
      public string type;
   }

   [Serializable]
   public class DownloadPrimary
   {
      public string method;
      public string href;
      public Meta meta;
   }

   [Serializable]
   public class File
   {
      public string filename;
      public int size;
      public bool resumable;
      public string md5sum;
      public string href;
   }

   [Serializable]
   public class Artifact
   {
      public string key;
      public string name;
      public bool primary;
      public bool show_download;
      public IList<File> files;
   }

   [Serializable]
   public class Links
   {
      public Self self;
      public Log log;
      public Auditlog auditlog;
      public CreateShare create_share;
      public RevokeShare revoke_share;
      public Icon icon;
      public DownloadPrimary download_primary;
      public IList<Artifact> artifacts;
   }

   [Serializable]
   public class BuildReport
   {
      public int errors;
      public int warnings;
   }

   [Serializable]
   public class Root
   {
      public int build;
      public string buildtargetid;
      public string buildTargetName;
      public string buildGUID;
      public string buildStatus;
      public bool cleanBuild;
      public string platform;
      public int workspaceSize;
      public string created;
      public string finished;
      public string checkoutStartTime;
      public int checkoutTimeInSeconds;
      public string buildStartTime;
      public double buildTimeInSeconds;
      public string publishStartTime;
      public double publishTimeInSeconds;
      public double totalTimeInSeconds;
      public string lastBuiltRevision;
      public Changeset[] changeset;
      public bool favorited;
      public bool deleted;
      public bool headless;
      public bool credentialsOutdated;
      public string queuedReason;
      public DateTime cooldownDate;
      public string scmBranch;
      public string unityVersion;
      public int auditChanges;
      public ProjectVersion projectVersion;
      public string projectName;
      public string projectId;
      public string projectGuid;
      public string orgId;
      public string orgFk;
      public string filetoken;
      public Links links;
      public BuildReport buildReport;
   }
}