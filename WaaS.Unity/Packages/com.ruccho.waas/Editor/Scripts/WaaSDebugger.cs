#nullable enable
using System;
using System.IO;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;
using WaaS.Runtime.Debugging;

namespace WaaS.Unity.Editor
{
    /// <summary>
    ///     Runs the server of the Debug Adapter Protocol in the editor, to which IDEs attach to debug the scripts.
    ///     It is enabled with <c>Assets &gt; Enable Debugger for WaaS</c>, and listens on <see cref="Port" /> of the local
    ///     host. The server is restarted after each domain reload, which disconnects the client.
    /// </summary>
    [InitializeOnLoad]
    public static class WaaSDebugger
    {
        private const string MenuPath = "Assets/Enable Debugger for WaaS";
        private const string EnabledPreference = "WaaS.Debugger.Enabled";
        private const string PortPreference = "WaaS.Debugger.Port";
        private const int DefaultPort = 4711;

        private static DebugAdapterServer? server;

        static WaaSDebugger()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            EditorApplication.quitting += Stop;
            if (IsEnabled) Start();
        }

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool(EnabledPreference, false);
            set
            {
                EditorPrefs.SetBool(EnabledPreference, value);
                if (value) Start();
                else Stop();
            }
        }

        /// <summary>
        ///     The port of the server. Changing it restarts the server.
        /// </summary>
        public static int Port
        {
            get => EditorPrefs.GetInt(PortPreference, DefaultPort);
            set
            {
                EditorPrefs.SetInt(PortPreference, value);
                if (!IsEnabled) return;
                Stop();
                Start();
            }
        }

        private static void Start()
        {
            if (server != null) return;
            try
            {
                // the relative paths of the sources in the line tables are relative to the project, where cargo runs
                server = new DebugAdapterServer(Port, Directory.GetCurrentDirectory());
            }
            catch (SocketException ex)
            {
                Debug.LogWarning($"[WaaS] The debugger cannot listen on the port {Port}: {ex.Message}");
            }
        }

        private static void Stop()
        {
            server?.Dispose();
            server = null;
        }

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            IsEnabled = !IsEnabled;
            if (IsEnabled && !RustImporterDebugInformation)
                Debug.Log(
                    "[WaaS] The debugger is enabled. Enable Assets > Build Rust Scripts with Debug Information for WaaS to set breakpoints in Rust scripts.");
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateToggle()
        {
            Menu.SetChecked(MenuPath, IsEnabled);
            return true;
        }

        private static bool RustImporterDebugInformation
        {
            get
            {
#if !WAAS_DISABLE_RUST_IMPORTER
                return Rust.RustImporter.BuildWithDebugInformation;
#else
                return false;
#endif
            }
        }
    }
}
