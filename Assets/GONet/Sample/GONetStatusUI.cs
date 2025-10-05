/* GONet (TM, serial number 88592370), Copyright (c) 2019-2023 Galore Interactive LLC - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential, email: contactus@galoreinteractive.com
 *
 *
 * Authorized use is explicitly limited to the following:
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products and whereas the products are interactive multi-player video games and cannot be viewed as a product competitive to GONet
 */

using GONet;
using UnityEngine;
using UnityEngine.UI;

namespace GONet.Sample
{
    /// <summary>
    /// Persistent UI overlay showing GONet status information.
    /// Displays server/client role and synchronized time.
    /// This component is designed to persist across scene changes (via DontDestroyOnLoad on parent).
    /// </summary>
    public class GONetStatusUI : MonoBehaviour
    {
        private Text headerText;
        private Text timeText;
        private Text roleText;
        private Text connectionStatusText;
        private bool wasServerSpawned;

        private void Awake()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            // Create canvas for status UI
            GameObject canvasGO = new GameObject("GONetStatusCanvas");
            canvasGO.transform.SetParent(transform, false);

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Ensure it renders on top

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // Create panel for status info (top-left corner)
            GameObject panelGO = new GameObject("StatusPanel");
            panelGO.transform.SetParent(canvasGO.transform, false);

            RectTransform panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1); // Top-left
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta = new Vector2(600, 225); // Increased width for Authority ID on same line

            Image panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f);

            // Create header text - larger and bold to distinguish it
            headerText = CreateText(panelGO.transform, "HeaderText", "GONet Info", 56, new Vector2(10, -10), new Vector2(500, 50), FontStyle.Bold);

            // Create time text - consistent spacing
            timeText = CreateText(panelGO.transform, "TimeText", "Sync'd Time: 0.00", 48, new Vector2(10, -70), new Vector2(580, 45));

            // Create role text - consistent spacing (includes Authority ID on same line)
            roleText = CreateText(panelGO.transform, "RoleText", "Role: Unknown", 48, new Vector2(10, -125), new Vector2(580, 45));

            // Create connection status text - consistent spacing
            connectionStatusText = CreateText(panelGO.transform, "ConnectionStatusText", "Connection: Not Connected", 48, new Vector2(10, -180), new Vector2(580, 45));
        }

        private Text CreateText(Transform parent, string name, string initialText, int fontSize, Vector2 position, Vector2 size, FontStyle fontStyle = FontStyle.Normal)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);

            RectTransform rect = textGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Text text = textGO.AddComponent<Text>();
            text.text = initialText;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = fontSize;

            return text;
        }

        private void Update()
        {
            if (timeText != null)
            {
                timeText.text = $"Sync'd Time: {GONetMain.Time.ElapsedSeconds:F3}s";
            }

            if (roleText != null)
            {
                string role = "Unknown";

                if (GONetMain.IsServer && GONetMain.IsClient)
                {
                    role = "SERVER + CLIENT";
                }
                else if (GONetMain.IsServer)
                {
                    const string SERVER_GO = "GONetSampleServer(Clone)";
                    wasServerSpawned |= GameObject.Find(SERVER_GO) != null;
                    role = wasServerSpawned ? "SERVER" : "SERVER (starting...)";
                }
                else if (GONetMain.IsClient)
                {
                    role = "CLIENT";
                }

                // Include Authority ID on the same line as Role
                ushort authorityId = GONetMain.MyAuthorityId;

                if (GONetMain.IsServer)
                {
                    roleText.text = $"Role: {role} (Authority:{authorityId})";
                }
                else if (authorityId == 0)
                {
                    // Client hasn't received authority ID yet
                    roleText.text = $"Role: {role} (Authority:--)";
                }
                else
                {
                    // Client with assigned authority ID
                    roleText.text = $"Role: {role} (Authority:{authorityId})";
                }
            }

            if (connectionStatusText != null)
            {
                string connectionStatus = "Not Connected";
                Color statusColor = Color.red; // Red by default

                if (GONetMain.IsServer)
                {
                    // Server: Show number of connected clients
                    uint clientCount = GONetMain.gonetServer != null ? GONetMain.gonetServer.numConnections : 0;
                    connectionStatus = clientCount == 1 ? "1 Client" : $"{clientCount} Clients";
                    statusColor = clientCount == 0 ? Color.yellow : Color.white; // Yellow if no clients, white otherwise
                }
                else if (GONetMain.IsClient && GONetMain.GONetClient != null)
                {
                    // Client: Show connection state
                    var state = GONetMain.GONetClient.ConnectionState;
                    connectionStatus = state switch
                    {
                        NetcodeIO.NET.ClientState.Connected => "Connected",
                        NetcodeIO.NET.ClientState.SendingConnectionRequest => "Connecting",
                        NetcodeIO.NET.ClientState.SendingChallengeResponse => "Connecting",
                        NetcodeIO.NET.ClientState.Disconnected => "Disconnected",
                        NetcodeIO.NET.ClientState.ConnectionDenied => "Connection Denied",
                        NetcodeIO.NET.ClientState.ConnectionRequestTimedOut => "Connection Timeout",
                        NetcodeIO.NET.ClientState.ChallengeResponseTimedOut => "Connection Timeout",
                        NetcodeIO.NET.ClientState.ConnectionTimedOut => "Connection Timeout",
                        NetcodeIO.NET.ClientState.InvalidConnectToken => "Invalid Token",
                        NetcodeIO.NET.ClientState.ConnectTokenExpired => "Token Expired",
                        _ => "Unknown"
                    };

                    // Color coding: Green (initialized), Yellow (connecting/initializing), Red (errors/disconnected)
                    if (state == NetcodeIO.NET.ClientState.Connected)
                    {
                        // Check if client is fully initialized with server
                        bool isInitialized = GONetMain.GONetClient.IsInitializedWithServer;

                        // NOTE: Once IsInitializedWithServer is true, time sync has already converged to acceptable levels
                        // The EffectiveOffsetTicks represents the total historical offset (e.g., 8 seconds if client started 8 seconds after server)
                        // but the actual time sync error is very small (< 100ms) because it's been corrected via interpolation.
                        // We don't have a direct API to get the "remaining sync error", so we just show initialized status.

                        if (isInitialized)
                        {
                            connectionStatus = $"Connected (Initialized)";
                            statusColor = Color.green;
                        }
                        else
                        {
                            connectionStatus = "Connected (Initializing...)";
                            statusColor = Color.yellow;
                        }
                    }
                    else if (state == NetcodeIO.NET.ClientState.SendingConnectionRequest ||
                             state == NetcodeIO.NET.ClientState.SendingChallengeResponse)
                    {
                        statusColor = Color.yellow;
                    }
                    else
                    {
                        statusColor = Color.red;
                    }
                }

                connectionStatusText.text = $"Connection: {connectionStatus}";
                connectionStatusText.color = statusColor;
            }
        }
    }
}
