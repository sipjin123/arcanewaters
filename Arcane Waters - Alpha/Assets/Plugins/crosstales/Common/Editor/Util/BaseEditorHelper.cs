using UnityEngine;
using UnityEditor;

namespace Crosstales.Common.EditorUtil
{
    /// <summary>Base for various Editor helper functions.</summary>
    public abstract class BaseEditorHelper : Util.BaseHelper
    {
        #region Public methods

        /// <summary>Restart Unity.</summary>
        /// <param name="executeMethod">Executed method after the restart (optional)</param>
        public static void RestartUnity(string executeMethod = "")
        {
            UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            bool success = false;
            string scriptfile = string.Empty;

            using (System.Diagnostics.Process process = new System.Diagnostics.Process())
            {
                try
                {
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;

                    if (Application.platform == RuntimePlatform.WindowsEditor)
                    {
                        scriptfile = System.IO.Path.GetTempPath() + "RestartUnity-" + System.Guid.NewGuid() + ".cmd";

                        System.IO.File.WriteAllText(scriptfile, generateWindowsRestartScript(executeMethod));

                        process.StartInfo.FileName = "cmd.exe";
                        process.StartInfo.Arguments = "/c start  \"\" " + '"' + scriptfile + '"';
                    }
                    else if (Application.platform == RuntimePlatform.OSXEditor)
                    {
                        scriptfile = System.IO.Path.GetTempPath() + "RestartUnity-" + System.Guid.NewGuid() + ".sh";

                        System.IO.File.WriteAllText(scriptfile, generateMacRestartScript(executeMethod));

                        process.StartInfo.FileName = "/bin/sh";
                        process.StartInfo.Arguments = '"' + scriptfile + "\" &";
                    }
                    else if (Application.platform == RuntimePlatform.LinuxEditor)
                    {
                        scriptfile = System.IO.Path.GetTempPath() + "RestartUnity-" + System.Guid.NewGuid() + ".sh";

                        System.IO.File.WriteAllText(scriptfile, generateLinuxRestartScript(executeMethod));

                        process.StartInfo.FileName = "/bin/sh";
                        process.StartInfo.Arguments = '"' + scriptfile + "\" &";
                    }
                    else
                    {
                        Debug.LogError("Unsupported Unity Editor: " + Application.platform);
                        return;
                    }

                    process.Start();

                    if (isWindowsPlatform)
                        process.WaitForExit(Util.BaseConstants.PROCESS_KILL_TIME);

                    success = true;
                }
                catch (System.Exception ex)
                {
                    string errorMessage = "Could restart Unity!" + System.Environment.NewLine + ex;
                    Debug.LogError(errorMessage);
                }
            }

            if (success)
                EditorApplication.Exit(0);
        }

        #endregion


        #region Private methods

        private static string generateWindowsRestartScript(string executeMethod)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // setup
            sb.AppendLine("@echo off");
            sb.AppendLine("cls");

            // title
            sb.Append("title Restart of ");
            sb.Append(Application.productName);
            sb.AppendLine(" - DO NOT CLOSE THIS WINDOW!");

            // header
            sb.AppendLine("echo ##############################################################################");
            sb.AppendLine("echo #                                                                            #");
            sb.AppendLine("echo #  Common 2019.1.0 - Linux                                                   #");
            sb.AppendLine("echo #  Copyright 2018-2019 by www.crosstales.com                                 #");
            sb.AppendLine("echo #                                                                            #");
            sb.AppendLine("echo #  This script restarts Unity.                                               #");
            sb.AppendLine("echo #  This will take some time, so please be patient and DON'T CLOSE THIS       #");
            sb.AppendLine("echo #  WINDOW before the process is finished!                                    #");
            sb.AppendLine("echo #                                                                            #");
            sb.AppendLine("echo ##############################################################################");
            sb.AppendLine("echo " + Application.productName);
            sb.AppendLine("echo.");
            sb.AppendLine("echo.");

            // check if Unity is closed
            sb.AppendLine(":waitloop");
            sb.Append("if not exist \"");
            sb.Append(Util.BaseConstants.APPLICATION_PATH);
            sb.Append("Temp\\UnityLockfile\" goto waitloopend");
            sb.AppendLine();
            sb.AppendLine("echo.");
            sb.AppendLine("echo Waiting for Unity to close...");
            sb.AppendLine("timeout /t 3");
            /*
#if UNITY_2018_2_OR_NEWER
                        sb.Append("del \"");
                        sb.Append(Constants.PATH);
                        sb.Append("Temp\\UnityLockfile\" /q");
                        sb.AppendLine();
#endif
            */
            sb.AppendLine("goto waitloop");
            sb.AppendLine(":waitloopend");

            // Restart Unity
            sb.AppendLine("echo.");
            sb.AppendLine("echo ##############################################################################");
            sb.AppendLine("echo #  Restarting Unity                                                          #");
            sb.AppendLine("echo ##############################################################################");
            sb.Append("start \"\" \"");
            sb.Append(ValidatePath(EditorApplication.applicationPath, false));
            sb.Append("\" -projectPath \"");
            sb.Append(Util.BaseConstants.APPLICATION_PATH.Substring(0, Util.BaseConstants.APPLICATION_PATH.Length - 1));
            sb.Append("\"");

            if (!string.IsNullOrEmpty(executeMethod))
            {
                sb.Append(" -executeMethod ");
                sb.Append(executeMethod);
            }

            sb.AppendLine();
            sb.AppendLine("echo.");

            // check if Unity is started
            sb.AppendLine(":waitloop2");
            sb.Append("if exist \"");
            sb.Append(Util.BaseConstants.APPLICATION_PATH);
            sb.Append("Temp\\UnityLockfile\" goto waitloopend2");
            sb.AppendLine();
            sb.AppendLine("echo Waiting for Unity to start...");
            sb.AppendLine("timeout /t 3");
            sb.AppendLine("goto waitloop2");
            sb.AppendLine(":waitloopend2");
            sb.AppendLine("echo.");
            sb.AppendLine("echo Bye!");
            sb.AppendLine("timeout /t 1");
            sb.AppendLine("exit");

            return sb.ToString();
        }

        private static string generateMacRestartScript(string executeMethod)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // setup
            sb.AppendLine("#!/bin/bash");
            sb.AppendLine("set +v");
            sb.AppendLine("clear");

            // title
            sb.Append("title='Relaunch of ");
            sb.Append(Application.productName);
            sb.AppendLine(" - DO NOT CLOSE THIS WINDOW!'");
            sb.AppendLine("echo -n -e \"\\033]0;$title\\007\"");

            // header
            sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
            sb.AppendLine("echo \"¦                                                                            ¦\"");
            sb.AppendLine("echo \"¦  Common 2019.1.0 - Linux                                                   ¦\"");
            sb.AppendLine("echo \"¦  Copyright 2018-2019 by www.crosstales.com                                 ¦\"");
            sb.AppendLine("echo \"¦                                                                            ¦\"");
            sb.AppendLine("echo \"¦  This script restarts Unity.                                               ¦\"");
            sb.AppendLine("echo \"¦  This will take some time, so please be patient and DON'T CLOSE THIS       ¦\"");
            sb.AppendLine("echo \"¦  WINDOW before the process is finished!                                    ¦\"");
            sb.AppendLine("echo \"¦                                                                            ¦\"");
            sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
            sb.AppendLine("echo \"" + Application.productName + "\"");
            sb.AppendLine("echo");
            sb.AppendLine("echo");

            // check if Unity is closed
            sb.Append("while [ -f \"");
            sb.Append(Util.BaseConstants.APPLICATION_PATH);
            sb.Append("Temp/UnityLockfile\" ]");
            sb.AppendLine();
            sb.AppendLine("do");
            sb.AppendLine("  echo \"Waiting for Unity to close...\"");
            sb.AppendLine("  sleep 3");
            /*
#if UNITY_2018_2_OR_NEWER
                        sb.Append("  rm \"");
                        sb.Append(Constants.PATH);
                        sb.Append("Temp/UnityLockfile\"");
                        sb.AppendLine();
#endif
            */
            sb.AppendLine("done");

            // Restart Unity
            sb.AppendLine("echo");
            sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
            sb.AppendLine("echo \"¦  Restarting Unity                                                          ¦\"");
            sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
            sb.Append("open -a \"");
            sb.Append(EditorApplication.applicationPath);
            sb.Append("\" --args -projectPath \"");
            sb.Append(Util.BaseConstants.APPLICATION_PATH);
            sb.Append("\"");

            if (!string.IsNullOrEmpty(executeMethod))
            {
                sb.Append(" -executeMethod ");
                sb.Append(executeMethod);
            }

            sb.AppendLine();

            //check if Unity is started
            sb.AppendLine("echo");
            sb.Append("while [ ! -f \"");
            sb.Append(Util.BaseConstants.APPLICATION_PATH);
            sb.Append("Temp/UnityLockfile\" ]");
            sb.AppendLine();
            sb.AppendLine("do");
            sb.AppendLine("  echo \"Waiting for Unity to start...\"");
            sb.AppendLine("  sleep 3");
            sb.AppendLine("done");
            sb.AppendLine("echo");
            sb.AppendLine("echo \"Bye!\"");
            sb.AppendLine("sleep 1");
            sb.AppendLine("exit");

            return sb.ToString();
        }

        private static string generateLinuxRestartScript(string executeMethod)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // setup
            sb.AppendLine("#!/bin/bash");
            sb.AppendLine("set +v");
            sb.AppendLine("clear");

            // title
            sb.Append("title='Relaunch of ");
            sb.Append(Application.productName);
            sb.AppendLine(" - DO NOT CLOSE THIS WINDOW!'");
            sb.AppendLine("echo -n -e \"\\033]0;$title\\007\"");

            // header
            sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
            sb.AppendLine("echo \"¦                                                                            ¦\"");
            sb.AppendLine("echo \"¦  Common 2019.1.0 - Linux                                                   ¦\"");
            sb.AppendLine("echo \"¦  Copyright 2018-2019 by www.crosstales.com                                 ¦\"");
            sb.AppendLine("echo \"¦                                                                            ¦\"");
            sb.AppendLine("echo \"¦  This script restarts Unity.                                               ¦\"");
            sb.AppendLine("echo \"¦  This will take some time, so please be patient and DON'T CLOSE THIS       ¦\"");
            sb.AppendLine("echo \"¦  WINDOW before the process is finished!                                    ¦\"");
            sb.AppendLine("echo \"¦                                                                            ¦\"");
            sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
            sb.AppendLine("echo \"" + Application.productName + "\"");
            sb.AppendLine("echo");
            sb.AppendLine("echo");

            // check if Unity is closed
            sb.Append("while [ -f \"");
            sb.Append(Util.BaseConstants.APPLICATION_PATH);
            sb.Append("Temp/UnityLockfile\" ]");
            sb.AppendLine();
            sb.AppendLine("do");
            sb.AppendLine("  echo \"Waiting for Unity to close...\"");
            sb.AppendLine("  sleep 3");
            /*
#if UNITY_2018_2_OR_NEWER
                        sb.Append("  rm \"");
                        sb.Append(Constants.PATH);
                        sb.Append("Temp/UnityLockfile\"");
                        sb.AppendLine();
#endif
            */
            sb.AppendLine("done");

            // Restart Unity
            sb.AppendLine("echo");
            sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
            sb.AppendLine("echo \"¦  Restarting Unity                                                          ¦\"");
            sb.AppendLine("echo \"+----------------------------------------------------------------------------+\"");
            sb.Append('"');
            sb.Append(EditorApplication.applicationPath);
            sb.Append("\" --args -projectPath \"");
            sb.Append(Util.BaseConstants.APPLICATION_PATH);
            sb.Append("\"");

            if (!string.IsNullOrEmpty(executeMethod))
            {
                sb.Append(" -executeMethod ");
                sb.Append(executeMethod);
            }

            sb.Append(" &");
            sb.AppendLine();

            // check if Unity is started
            sb.AppendLine("echo");
            sb.Append("while [ ! -f \"");
            sb.Append(Util.BaseConstants.APPLICATION_PATH);
            sb.Append("Temp/UnityLockfile\" ]");
            sb.AppendLine();
            sb.AppendLine("do");
            sb.AppendLine("  echo \"Waiting for Unity to start...\"");
            sb.AppendLine("  sleep 3");
            sb.AppendLine("done");
            sb.AppendLine("echo");
            sb.AppendLine("echo \"Bye!\"");
            sb.AppendLine("sleep 1");
            sb.AppendLine("exit");

            return sb.ToString();
        }

        #endregion


        /*
// compress the folder into a ZIP file, uses https://github.com/r2d2rigo/dotnetzip-for-unity
static void CompressDirectory(string directory, string zipFileOutputPath)
{
    Debug.Log("attempting to compress " + directory + " into " + zipFileOutputPath);
    // display fake percentage, I can't get zip.SaveProgress event handler to work for some reason, whatever
    EditorUtility.DisplayProgressBar("COMPRESSING... please wait", zipFileOutputPath, 0.38f);
    using (ZipFile zip = new ZipFile())
    {
        zip.ParallelDeflateThreshold = -1; // DotNetZip bugfix that corrupts DLLs / binaries http://stackoverflow.com/questions/15337186/dotnetzip-badreadexception-on-extract
        zip.AddDirectory(directory);
        zip.Save(zipFileOutputPath);
    }
    EditorUtility.ClearProgressBar();
}
*/
    }
}
// © 2018-2019 crosstales LLC (https://www.crosstales.com)