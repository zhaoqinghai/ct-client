﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Threading;
using WixToolset;
using WixToolset.BootstrapperApplicationApi;

using Threading = System.Windows.Threading;

namespace CT.InstallerUI
{
    public class WixBA : BootstrapperApplication
    {
        internal IBootstrapperApplicationData BAManifest { get; private set; } = null!;

        internal IBootstrapperCommand Command { get; private set; } = null!;

        internal IEngine Engine => this.engine;

        /// <summary>
        /// Gets the global model.
        /// </summary>
        public static Model Model { get; private set; } = null!;

        /// <summary>
        /// Gets the global view.
        /// </summary>
        public static RootView View { get; private set; } = null!;

        // TODO: We should refactor things so we dont have a global View.

        /// <summary>
        /// Gets the global dispatcher.
        /// </summary>
        public static Threading.Dispatcher Dispatcher { get; private set; }

        /// <summary>
        /// Launches the default web browser to the provided URI.
        /// </summary>
        /// <param name="uri">URI to open the web browser.</param>
        public static void LaunchUrl(string uri)
        {
            WixBA.UseShellExecute(uri);
        }

        /// <summary>
        /// Open a log file.
        /// </summary>
        /// <param name="uri">URI to a log file.</param>
        internal static void OpenLog(Uri uri)
        {
            WixBA.UseShellExecute(uri.ToString());
        }

        /// <summary>
        /// Open a log folder.
        /// </summary>
        /// <param name="string">path to a log folder.</param>
        internal static void OpenLogFolder(string logFolder)
        {
            WixBA.UseShellExecute(logFolder);
        }

        /// <summary>
        /// Open a log folder.
        /// </summary>
        /// <param name="uri">path to a log folder.</param>
        private static void UseShellExecute(string path)
        {
            // Switch the wait cursor since shellexec can take a second or so.
            System.Windows.Input.Cursor cursor = WixBA.View.Cursor;
            WixBA.View.Cursor = System.Windows.Input.Cursors.Wait;
            Process? process = null;
            try
            {
                process = new Process();
                process.StartInfo.FileName = path;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.Verb = "open";

                process.Start();
            }
            finally
            {
                if (null != process)
                {
                    process.Dispose();
                }
                // back to the original cursor.
                WixBA.View.Cursor = cursor;
            }
        }

        /// <summary>
        /// Starts planning the appropriate action.
        /// </summary>
        /// <param name="action">Action to plan.</param>
        public static void Plan(LaunchAction action)
        {
            WixBA.Model.PlannedAction = action;
            WixBA.Model.Engine.Plan(WixBA.Model.PlannedAction);
        }

        public static void PlanLayout()
        {
            // Either default or set the layout directory
            if (String.IsNullOrEmpty(WixBA.Model.Command.LayoutDirectory))
            {
                WixBA.Model.LayoutDirectory = Directory.GetCurrentDirectory();

                // Ask the user for layout folder if one wasn't provided and we're in full UI mode
                if (WixBA.Model.Command.Display == Display.Full)
                {
                    WixBA.Dispatcher.Invoke((Action)delegate ()
                    {
                        var browserDialog = new OpenFolderDialog();
                        browserDialog.RootDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
                        browserDialog.FolderName = WixBA.Model.LayoutDirectory;
                        var result = browserDialog.ShowDialog();

                        if (result == true)
                        {
                            WixBA.Model.LayoutDirectory = browserDialog.FolderName;
                            WixBA.Plan(WixBA.Model.Command.Action);
                        }
                        else
                        {
                            WixBA.View.Close();
                        }
                    }
                    );
                }
            }
            else
            {
                WixBA.Model.LayoutDirectory = WixBA.Model.Command.LayoutDirectory;
                WixBA.Plan(WixBA.Model.Command.Action);
            }
        }

        /// <summary>
        /// Thread entry point for WiX Toolset Bootstrapper Application.
        /// </summary>
        protected override void Run()
        {
            this.Engine.Log(LogLevel.Verbose, "Running the WiX BA.");

            WixBA.Model = new Model(this);
            WixBA.Dispatcher = Threading.Dispatcher.CurrentDispatcher;
            RootViewModel viewModel = new RootViewModel();
            WixBA.View = new RootView(viewModel);

            // Create a Window to show UI.
            if (WixBA.Model.Command.Display == Display.Passive ||
                WixBA.Model.Command.Display == Display.Full)
            {
                this.Engine.Log(LogLevel.Verbose, "Creating a UI.");
                WixBA.View.Show();
            }
            this.Engine.Detect();

            Threading.Dispatcher.Run();

            this.PostTelemetry();

            var exitCode = WixBA.Model.Result;
            if ((exitCode & 0xFFFF0000) == unchecked(0x80070000))
            {
                exitCode &= 0xFFFF; // return plain old Win32 error, not HRESULT.
            }

            this.Engine.Quit(exitCode);
        }

        protected override void OnCreate(CreateEventArgs args)
        {
            base.OnCreate(args);
            this.Command = args.Command;
            this.BAManifest = new BootstrapperApplicationData();
        }

        private void PostTelemetry()
        {
            string result = String.Concat("0x", WixBA.Model.Result.ToString("x"));

            StringBuilder telemetryData = new StringBuilder();
            foreach (KeyValuePair<string, string> kvp in WixBA.Model.Telemetry)
            {
                telemetryData.AppendFormat("{0}={1}+", kvp.Key, kvp.Value);
            }
            telemetryData.AppendFormat("Result={0}", result);

            byte[] data = Encoding.UTF8.GetBytes(telemetryData.ToString());

            try
            {
                HttpWebRequest post = WixBA.Model.CreateWebRequest(String.Format(WixDistribution.TelemetryUrlFormat, WixBA.Model.Version.ToString(), result));
                post.Method = "POST";
                post.ContentType = "application/x-www-form-urlencoded";
                post.ContentLength = data.Length;

                using (Stream postStream = post.GetRequestStream())
                {
                    postStream.Write(data, 0, data.Length);
                }

                HttpWebResponse response = (HttpWebResponse)post.GetResponse();
            }
            catch (ArgumentException)
            {
            }
            catch (FormatException)
            {
            }
            catch (OverflowException)
            {
            }
            catch (ProtocolViolationException)
            {
            }
            catch (WebException)
            {
            }
        }
    }

    public class Program
    {
        public static int Main(string[] args)
        {
            var app = new WixBA();

            ManagedBootstrapperApplication.Run(app);

            return 0;
        }
    }
}