using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace GONet.Sample
{
    /// <summary>
    /// UI controller for demonstrating GONet scene management.
    /// Shows available scenes and allows both server and client to request scene loads.
    /// Builds UI programmatically if references are not set.
    /// </summary>
    public class SceneSelectionUI : MonoBehaviour
    {
        [Header("UI References (Auto-created if null)")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI instructionsText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Button projectileTestButton;
        [SerializeField] private Button rpcPlaygroundButton;
        [SerializeField] private Button backToMenuButton;

        // Server approval UI
        private GameObject approvalPanel;
        private TextMeshProUGUI approvalMessageText;
        private Button approveButton;
        private Button denyButton;

        // Client waiting/response UI
        private GameObject clientResponsePanel;
        private TextMeshProUGUI clientResponseMessageText;
        private Button clientResponseCloseButton;
        private bool isAwaitingResponse = false; // Track if THIS client is waiting for a response

        // Pending request info (server-side tracking)
        private string pendingSceneName;
        private LoadSceneMode pendingLoadMode;
        private ushort pendingRequestingAuthority;
        private bool hasPendingRequest = false;

        [Header("Scene Names")]
        [SerializeField] private string projectileTestSceneName = "ProjectileTest";
        [SerializeField] private string rpcPlaygroundSceneName = "RpcPlayground";
        [SerializeField] private string menuSceneName = "GONetSample";

        [Header("Scene Loading Configuration")]
        [SerializeField] private bool rpcPlaygroundIsAddressable = true;

        [Header("Auto-Build UI")]
        [SerializeField] private bool autoBuildUI = true;

        private bool hasSetupAsyncApproval = false;

        private void Awake()
        {
            if (autoBuildUI && (statusText == null || projectileTestButton == null))
            {
                BuildUI();
            }
        }

        private void Start()
        {
            // Setup button listeners
            if (projectileTestButton != null)
                projectileTestButton.onClick.AddListener(() => RequestLoadScene(projectileTestSceneName));

            if (rpcPlaygroundButton != null)
                rpcPlaygroundButton.onClick.AddListener(() => RequestLoadScene(rpcPlaygroundSceneName));

            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.AddListener(() => RequestLoadScene(menuSceneName));
                backToMenuButton.gameObject.SetActive(false); // Hidden in menu scene
            }

            // Setup validation hook and event subscriptions
            if (GONetMain.SceneManager != null)
            {
                GONetMain.SceneManager.OnValidateSceneLoad += ValidateSceneLoad;

                // Subscribe to scene events for status updates
                GONetMain.SceneManager.OnSceneLoadStarted += OnSceneLoadStarted;
                GONetMain.SceneManager.OnSceneLoadCompleted += OnSceneLoadCompleted;

                // Subscribe to scene request response for client feedback
                GONetMain.SceneManager.OnSceneRequestResponse += OnSceneRequestResponseReceived;
            }
            else
            {
                GONetLog.Warning($"[SceneSelectionUI] GONetMain.SceneManager is NULL in Start()!");
            }

            UpdateUI();
        }

        private void BuildUI()
        {
            // Find or create canvas
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                GONetLog.Error("[SceneSelectionUI] No Canvas found in parent hierarchy");
                return;
            }

            // Create main panel
            GameObject panel = new GameObject("SceneSelectionPanel");
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.2f, 0.2f);
            panelRect.anchorMax = new Vector2(0.8f, 0.8f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            // Create approval panel (initially hidden)
            BuildApprovalPanel(canvas);

            // Create client response panel (initially hidden)
            BuildClientResponsePanel(canvas);

            // Title
            titleText = CreateText(panel.transform, "Title", "GONet Scene Management Demo", 24, TextAlignmentOptions.Top);
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.85f);
            titleRect.anchorMax = new Vector2(0.9f, 0.95f);

            // Status text
            statusText = CreateText(panel.transform, "Status", "Connecting...", 16, TextAlignmentOptions.TopLeft);
            RectTransform statusRect = statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.1f, 0.65f);
            statusRect.anchorMax = new Vector2(0.9f, 0.8f);

            // Instructions text
            instructionsText = CreateText(panel.transform, "Instructions", "Select a scene to load:", 14, TextAlignmentOptions.MidlineLeft);
            RectTransform instrRect = instructionsText.GetComponent<RectTransform>();
            instrRect.anchorMin = new Vector2(0.1f, 0.52f);
            instrRect.anchorMax = new Vector2(0.9f, 0.62f);

            // Buttons
            projectileTestButton = CreateButton(panel.transform, "ProjectileTestButton", "Load Projectile Test Scene", 0.4f);
            rpcPlaygroundButton = CreateButton(panel.transform, "RPCPlaygroundButton", "Load RPC Playground (Chat)", 0.25f);
            backToMenuButton = CreateButton(panel.transform, "BackButton", "Back to Menu", 0.1f);
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            TextMeshProUGUI textComp = go.AddComponent<TextMeshProUGUI>();
            textComp.text = text;
            textComp.fontSize = fontSize;
            textComp.alignment = alignment;
            textComp.color = Color.white;
            return textComp;
        }

        private Button CreateButton(Transform parent, string name, string labelText, float yAnchor)
        {
            GameObject buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(parent, false);

            RectTransform rect = buttonGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, yAnchor);
            rect.anchorMax = new Vector2(0.8f, yAnchor + 0.08f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.2f, 0.5f, 0.8f, 1f);

            Button button = buttonGO.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.5f, 0.8f, 1f);
            colors.highlightedColor = new Color(0.3f, 0.6f, 0.9f, 1f);
            colors.pressedColor = new Color(0.15f, 0.4f, 0.7f, 1f);
            button.colors = colors;

            // Button label
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(buttonGO.transform, false);
            RectTransform labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.fontSize = 18;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            return button;
        }

        private void BuildApprovalPanel(Canvas canvas)
        {
            // Create approval panel (centered, smaller than main panel)
            approvalPanel = new GameObject("ApprovalPanel");
            approvalPanel.transform.SetParent(canvas.transform, false);
            RectTransform approvalRect = approvalPanel.AddComponent<RectTransform>();
            approvalRect.anchorMin = new Vector2(0.3f, 0.35f);
            approvalRect.anchorMax = new Vector2(0.7f, 0.65f);
            approvalRect.offsetMin = Vector2.zero;
            approvalRect.offsetMax = Vector2.zero;
            Image approvalImage = approvalPanel.AddComponent<Image>();
            approvalImage.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            // Approval title
            TextMeshProUGUI titleText = CreateText(approvalPanel.transform, "ApprovalTitle", "Scene Change Request", 20, TextAlignmentOptions.Top);
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.75f);
            titleRect.anchorMax = new Vector2(0.9f, 0.9f);

            // Approval message
            approvalMessageText = CreateText(approvalPanel.transform, "ApprovalMessage", "Client requesting scene change...", 16, TextAlignmentOptions.Center);
            RectTransform msgRect = approvalMessageText.GetComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0.1f, 0.4f);
            msgRect.anchorMax = new Vector2(0.9f, 0.7f);

            // Approve button
            approveButton = CreateButton(approvalPanel.transform, "ApproveButton", "APPROVE", 0.15f);
            RectTransform approveRect = approveButton.GetComponent<RectTransform>();
            approveRect.anchorMin = new Vector2(0.1f, 0.15f);
            approveRect.anchorMax = new Vector2(0.45f, 0.3f);
            approveButton.GetComponent<Image>().color = new Color(0.2f, 0.7f, 0.2f, 1f);
            approveButton.onClick.AddListener(OnApproveClicked);

            // Deny button
            denyButton = CreateButton(approvalPanel.transform, "DenyButton", "DENY", 0.15f);
            RectTransform denyRect = denyButton.GetComponent<RectTransform>();
            denyRect.anchorMin = new Vector2(0.55f, 0.15f);
            denyRect.anchorMax = new Vector2(0.9f, 0.3f);
            denyButton.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f, 1f);
            denyButton.onClick.AddListener(OnDenyClicked);

            // Start hidden
            approvalPanel.SetActive(false);
        }

        private void BuildClientResponsePanel(Canvas canvas)
        {
            // Create client response panel (centered, larger to accommodate longer messages)
            clientResponsePanel = new GameObject("ClientResponsePanel");
            clientResponsePanel.transform.SetParent(canvas.transform, false);
            RectTransform responseRect = clientResponsePanel.AddComponent<RectTransform>();
            responseRect.anchorMin = new Vector2(0.25f, 0.3f);
            responseRect.anchorMax = new Vector2(0.75f, 0.7f);
            responseRect.offsetMin = Vector2.zero;
            responseRect.offsetMax = Vector2.zero;
            Image responseImage = clientResponsePanel.AddComponent<Image>();
            responseImage.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            // Response message (leave more room at bottom for button)
            clientResponseMessageText = CreateText(clientResponsePanel.transform, "ResponseMessage", "Awaiting server approval...", 16, TextAlignmentOptions.Center);
            RectTransform msgRect = clientResponseMessageText.GetComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0.1f, 0.35f);
            msgRect.anchorMax = new Vector2(0.9f, 0.85f);

            // Close button (bottom center, bigger)
            clientResponseCloseButton = CreateButton(clientResponsePanel.transform, "CloseButton", "Close", 0.25f);
            RectTransform closeRect = clientResponseCloseButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.3f, 0.08f);
            closeRect.anchorMax = new Vector2(0.7f, 0.25f);
            clientResponseCloseButton.GetComponent<Image>().color = new Color(0.6f, 0.2f, 0.2f, 1f);
            clientResponseCloseButton.onClick.AddListener(OnClientResponseCloseClicked);

            // Enable Best Fit on the button text
            TextMeshProUGUI buttonText = clientResponseCloseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.enableAutoSizing = true;
                buttonText.fontSizeMin = 10;
                buttonText.fontSizeMax = 20;
            }

            // Initially hide the close button (only show for denials)
            clientResponseCloseButton.gameObject.SetActive(false);

            // Start hidden
            clientResponsePanel.SetActive(false);
        }

        private void OnDestroy()
        {
            // Cleanup
            if (GONetMain.SceneManager != null)
            {
                GONetMain.SceneManager.OnValidateSceneLoad -= ValidateSceneLoad;
                GONetMain.SceneManager.OnSceneLoadStarted -= OnSceneLoadStarted;
                GONetMain.SceneManager.OnSceneLoadCompleted -= OnSceneLoadCompleted;
                GONetMain.SceneManager.OnSceneRequestResponse -= OnSceneRequestResponseReceived;
            }
        }

        private void Update()
        {
            // Enable async approval mode once server is detected
            // This must be done in Update because GONetMain.IsServer may not be set yet in Start()
            if (!hasSetupAsyncApproval && GONetMain.IsServer && GONetMain.SceneManager != null)
            {
                GONetLog.Info($"[SceneSelectionUI] Setting RequiresAsyncApproval = true (IsServer: {GONetMain.IsServer})");
                GONetMain.SceneManager.RequiresAsyncApproval = true;
                hasSetupAsyncApproval = true;
            }

            // Update UI every frame to show current state
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (statusText != null)
            {
                string role = GONetMain.IsServer ? "SERVER" : (GONetMain.IsClient ? "CLIENT" : "NOT CONNECTED");
                string currentScene = SceneManager.GetActiveScene().name;

                statusText.text = $"Role: {role}\nCurrent Scene: {currentScene}";

                // Add loading state info
                if (GONetMain.SceneManager != null)
                {
                    if (GONetMain.SceneManager.IsSceneLoading(projectileTestSceneName))
                        statusText.text += $"\nLoading {projectileTestSceneName}...";
                    else if (GONetMain.SceneManager.IsSceneLoading(rpcPlaygroundSceneName))
                        statusText.text += $"\nLoading {rpcPlaygroundSceneName}...";
                    else if (GONetMain.SceneManager.IsSceneLoading(menuSceneName))
                        statusText.text += $"\nLoading {menuSceneName}...";
                }
            }

            if (instructionsText != null)
            {
                instructionsText.text = GONetMain.IsServer
                    ? "SERVER: Select a scene to load for all clients"
                    : "CLIENT: Request server to load a scene\n(Server must approve the request)";
            }

            // Show back button only when not in menu scene
            if (backToMenuButton != null)
            {
                string currentScene = SceneManager.GetActiveScene().name;
                backToMenuButton.gameObject.SetActive(currentScene != menuSceneName);
            }
        }

        private void RequestLoadScene(string sceneName)
        {
            if (GONetMain.SceneManager == null)
            {
                GONetLog.Warning("[SceneSelectionUI] GONetMain.SceneManager not initialized");
                return;
            }

            GONetLog.Info($"[SceneSelectionUI] Requesting scene load: {sceneName}");

            if (GONetMain.IsServer)
            {
                // Server loads directly using appropriate method
                LoadScene(sceneName, LoadSceneMode.Single);
            }
            else if (GONetMain.IsClient)
            {
                // Client requests through RPC - use appropriate request method
                bool isAddressable = (sceneName == rpcPlaygroundSceneName && rpcPlaygroundIsAddressable);

                if (isAddressable)
                {
#if ADDRESSABLES_AVAILABLE
                    GONetMain.SceneManager.RequestLoadAddressablesScene(sceneName, LoadSceneMode.Single);
#else
                    GONetLog.Error($"Unable to load Addressable scene {sceneName} until you add precompiler directive to define ADDRESSABLES_AVAILABLE.");
#endif
                }
                else
                    {
                    GONetMain.SceneManager.RequestLoadScene(sceneName, LoadSceneMode.Single);
                }

                // Show awaiting approval message
                if (clientResponsePanel != null && clientResponseMessageText != null)
                {
                    clientResponseMessageText.text = $"Awaiting server approval for:\n\n'{sceneName}'";
                    clientResponseCloseButton.gameObject.SetActive(false); // No close button while waiting
                    clientResponsePanel.SetActive(true);
                    isAwaitingResponse = true; // Mark this client as awaiting a response
                }
            }
            else
            {
                GONetLog.Warning("[SceneSelectionUI] Not connected as server or client");
            }
        }

        /// <summary>
        /// Helper method to load a scene using the appropriate method (Build Settings or Addressables)
        /// </summary>
        private void LoadScene(string sceneName, LoadSceneMode mode)
        {
            // Check if this scene should be loaded from Addressables
            bool isAddressable = (sceneName == rpcPlaygroundSceneName && rpcPlaygroundIsAddressable);

            if (isAddressable)
            {
#if ADDRESSABLES_AVAILABLE
                GONetLog.Info($"[SceneSelectionUI] Loading scene '{sceneName}' from Addressables");
                GONetMain.SceneManager.LoadSceneFromAddressables(sceneName, mode);
#else
                GONetLog.Error($"Unable to load Addressable scene {sceneName} until you add precompiler directive to define ADDRESSABLES_AVAILABLE.");
#endif
            }
            else
            {
                GONetLog.Info($"[SceneSelectionUI] Loading scene '{sceneName}' from Build Settings");
                GONetMain.SceneManager.LoadSceneFromBuildSettings(sceneName, mode);
            }
        }

        private void OnClientResponseCloseClicked()
        {
            if (clientResponsePanel != null)
            {
                clientResponsePanel.SetActive(false);
                isAwaitingResponse = false; // Clear the flag
            }
        }

        /// <summary>
        /// Validation hook - shows approval UI for client requests on server.
        /// Server's own requests are auto-approved.
        /// </summary>
        private bool ValidateSceneLoad(string sceneName, LoadSceneMode mode, ushort requestingAuthority)
        {
            GONetLog.Info($"[SceneSelectionUI] Validating scene load request: '{sceneName}' from authority {requestingAuthority} (MyAuthorityId: {GONetMain.MyAuthorityId})");

            // Server's own requests are auto-approved
            if (GONetMain.IsServer && requestingAuthority == GONetMain.MyAuthorityId)
            {
                GONetLog.Info($"[SceneSelectionUI] Server's own request - auto-approved");
                return true;
            }

            // Client request on server - check for conflicts before showing approval dialog
            if (GONetMain.IsServer)
            {
                // Check if another request is already pending
                if (hasPendingRequest)
                {
                    GONetLog.Warning($"[SceneSelectionUI] Request from authority {requestingAuthority} DENIED - another request is already pending approval");
                    GONetMain.SceneManager.SendSceneRequestResponse(requestingAuthority, false, sceneName,
                        "Another scene change request is already pending approval. Please wait.");
                    return false; // Deny this request
                }

                // Check if a scene is currently loading
                if (GONetMain.SceneManager != null && GONetMain.SceneManager.IsSceneLoading(sceneName))
                {
                    GONetLog.Warning($"[SceneSelectionUI] Request from authority {requestingAuthority} DENIED - scene '{sceneName}' is already loading");
                    GONetMain.SceneManager.SendSceneRequestResponse(requestingAuthority, false, sceneName,
                        $"Scene '{sceneName}' is already loading. Please wait.");
                    return false; // Deny this request
                }

                // Check if any scene is loading (for different scene requests)
                if (GONetMain.SceneManager != null &&
                    (GONetMain.SceneManager.IsSceneLoading(projectileTestSceneName) ||
                     GONetMain.SceneManager.IsSceneLoading(rpcPlaygroundSceneName) ||
                     GONetMain.SceneManager.IsSceneLoading(menuSceneName)))
                {
                    GONetLog.Warning($"[SceneSelectionUI] Request from authority {requestingAuthority} DENIED - a scene change is already in progress");
                    GONetMain.SceneManager.SendSceneRequestResponse(requestingAuthority, false, sceneName,
                        "A scene change is already in progress. Please wait.");
                    return false; // Deny this request
                }

                // No conflicts - show approval dialog
                GONetLog.Info($"[SceneSelectionUI] Client request - showing approval dialog (requestingAuthority={requestingAuthority} != MyAuthorityId={GONetMain.MyAuthorityId})");
                pendingSceneName = sceneName;
                pendingLoadMode = mode;
                pendingRequestingAuthority = requestingAuthority;
                hasPendingRequest = true; // Mark request as pending

                if (approvalPanel != null && approvalMessageText != null)
                {
                    approvalMessageText.text = $"Client (Authority {requestingAuthority}) requests:\n\n'{sceneName}'\n\nApprove this scene change?";
                    approvalPanel.SetActive(true);
                }

                // Return true to allow RPC through - async approval will send response later
                // ExpectFollowOnResponse flag will be set automatically since RequiresAsyncApproval is true
                return true;
            }

            // Client's own requests - this validation runs on client side, always allow
            return true;
        }

        private void OnApproveClicked()
        {
            GONetLog.Info($"[SceneSelectionUI] Server APPROVED scene change to '{pendingSceneName}'");
            approvalPanel.SetActive(false);
            hasPendingRequest = false; // Clear pending request flag

            // Server loads the scene using appropriate method
            if (GONetMain.SceneManager != null)
            {
                LoadScene(pendingSceneName, pendingLoadMode);
            }

            // Send approval response to client via GONetSceneManager public API
            GONetMain.SceneManager.SendSceneRequestResponse(pendingRequestingAuthority, true, pendingSceneName, "");
        }

        private void OnDenyClicked()
        {
            GONetLog.Info($"[SceneSelectionUI] Server DENIED scene change to '{pendingSceneName}'");
            approvalPanel.SetActive(false);
            hasPendingRequest = false; // Clear pending request flag

            // Send denial response to client via GONetSceneManager public API
            GONetMain.SceneManager.SendSceneRequestResponse(pendingRequestingAuthority, false, pendingSceneName, "Server denied the request");
        }

        private void OnSceneLoadStarted(string sceneName, LoadSceneMode mode)
        {
            GONetLog.Info($"[SceneSelectionUI] Scene load started: {sceneName}");
        }

        private void OnSceneLoadCompleted(string sceneName, LoadSceneMode mode)
        {
            GONetLog.Info($"[SceneSelectionUI] Scene load completed: {sceneName}");

            // Hide client response panel when scene loads (approval was granted)
            if (clientResponsePanel != null && clientResponsePanel.activeSelf)
            {
                clientResponsePanel.SetActive(false);
                isAwaitingResponse = false; // Clear the flag
            }
        }

        private void OnSceneRequestResponseReceived(bool approved, string sceneName, string denialReason)
        {
            GONetLog.Info($"[SceneSelectionUI] Scene request response received - Approved: {approved}, Scene: '{sceneName}', Reason: '{denialReason}', IsAwaitingResponse: {isAwaitingResponse}");

            // Only show response UI if THIS client is awaiting a response
            if (!isAwaitingResponse || clientResponsePanel == null || clientResponseMessageText == null)
                return;

            if (approved)
            {
                // Approval - scene will load, so just hide the "awaiting approval" panel
                // The OnSceneLoadCompleted handler will also hide it when scene actually loads
                clientResponsePanel.SetActive(false);
                isAwaitingResponse = false; // Clear the flag
                GONetLog.Info($"[SceneSelectionUI] Client: Server approved scene '{sceneName}' - scene will load");
            }
            else
            {
                // Denial - show denial message with close button
                clientResponseMessageText.text = $"Request DENIED:\n\n'{sceneName}'\n\n{denialReason}";
                clientResponseCloseButton.gameObject.SetActive(true); // Show Close button
                clientResponsePanel.SetActive(true);
                // Don't clear isAwaitingResponse yet - wait for user to close the panel
                GONetLog.Warning($"[SceneSelectionUI] Client: Server denied scene '{sceneName}' - Reason: {denialReason}");
            }
        }
    }
}
