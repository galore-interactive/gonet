using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace GONet.Sample
{
    /// <summary>
    /// Persistent UI that shows an "Exit" button in the upper-right corner when not in the menu scene.
    /// Uses DontDestroyOnLoad to persist across scene changes.
    /// Triggers the same scene change request flow as SceneSelectionUI (client requests, server approves/denies).
    /// </summary>
    public class ExitButtonUI : MonoBehaviour
    {
        private static ExitButtonUI instance;

        private Canvas canvas;
        private GameObject buttonContainer;
        private Button exitButton;
        private GameObject clientResponsePanel;
        private TextMeshProUGUI clientResponseMessageText;
        private Button clientResponseCloseButton;
        private bool isAwaitingResponse = false;

        // Server approval UI (for when SceneSelectionUI isn't present)
        private GameObject approvalPanel;
        private TextMeshProUGUI approvalMessageText;
        private Button approveButton;
        private Button denyButton;

        // Pending request info (server-side tracking)
        private string pendingSceneName;
        private LoadSceneMode pendingLoadMode;
        private ushort pendingRequestingAuthority;
        private bool hasPendingRequest = false;

        private bool hasSetupAsyncApproval = false;

        [Header("Configuration")]
        [SerializeField] private string menuSceneName = "GONetSample";
        [SerializeField] private int buttonWidth = 100;
        [SerializeField] private int buttonHeight = 40;
        [SerializeField] private int marginFromEdge = 20;

        private void Awake()
        {
            // Singleton pattern - only allow one instance
            if (instance != null && instance != this)
            {
                GONetLog.Debug($"[ExitButtonUI] Duplicate instance detected - destroying self");
                Destroy(gameObject);
                return;
            }

            instance = this;

            // Make this persist across scenes
            DontDestroyOnLoad(gameObject);

            // Create canvas if it doesn't exist
            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100; // Render on top

                // Add CanvasScaler for resolution independence
                CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                // Add GraphicRaycaster for button clicks
                gameObject.AddComponent<GraphicRaycaster>();
            }

            BuildUI();
        }

        private void EnsureEventSystemExists()
        {
            // Find all EventSystems (both in scene and DontDestroyOnLoad)
            EventSystem[] allEventSystems = FindObjectsOfType<EventSystem>();

            if (allEventSystems.Length == 0)
            {
                // No EventSystem at all - create a persistent one
                GONetLog.Debug("[ExitButtonUI] No EventSystem found - creating persistent one");
                GameObject eventSystemGO = new GameObject("EventSystem_Persistent");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();
                DontDestroyOnLoad(eventSystemGO);
                return;
            }

            EventSystem persistentEventSystem = null;
            List<EventSystem> sceneEventSystems = new List<EventSystem>();

            // Categorize EventSystems
            foreach (var es in allEventSystems)
            {
                // Objects in DontDestroyOnLoad have scene.name == "DontDestroyOnLoad"
                if (es.gameObject.scene.name == "DontDestroyOnLoad")
                {
                    persistentEventSystem = es;
                }
                else
                {
                    sceneEventSystems.Add(es);
                }
            }

            // If we have a persistent one, destroy ALL scene-based ones
            if (persistentEventSystem != null)
            {
                foreach (var sceneES in sceneEventSystems)
                {
                    GONetLog.Debug($"[ExitButtonUI] Found scene-based EventSystem '{sceneES.gameObject.name}' while persistent exists - destroying it");
                    Destroy(sceneES.gameObject);
                }
                return;
            }

            // No persistent EventSystem - make the first scene-based one persistent, destroy the rest
            if (sceneEventSystems.Count > 0)
            {
                EventSystem firstSceneES = sceneEventSystems[0];
                GONetLog.Debug($"[ExitButtonUI] Making scene-based EventSystem '{firstSceneES.gameObject.name}' persistent");
                DontDestroyOnLoad(firstSceneES.gameObject);

                // Destroy any other scene-based EventSystems
                for (int i = 1; i < sceneEventSystems.Count; i++)
                {
                    GONetLog.Debug($"[ExitButtonUI] Destroying duplicate scene-based EventSystem '{sceneEventSystems[i].gameObject.name}'");
                    Destroy(sceneEventSystems[i].gameObject);
                }
            }
        }

        private void Start()
        {
            // Subscribe to scene events including validation (needed since we persist across scenes)
            if (GONetMain.SceneManager != null)
            {
                GONetMain.SceneManager.OnValidateSceneLoad += ValidateSceneLoad;
                GONetMain.SceneManager.OnSceneRequestResponse += OnSceneRequestResponseReceived;
                GONetMain.SceneManager.OnSceneLoadCompleted += OnSceneLoadCompleted;
            }

            UpdateButtonVisibility();
        }

        private void Update()
        {
            // Enable async approval mode once server is detected (only once)
            if (!hasSetupAsyncApproval && GONetMain.IsServer && GONetMain.SceneManager != null)
            {
                GONetLog.Debug($"[ExitButtonUI] Setting RequiresAsyncApproval = true");
                GONetMain.SceneManager.RequiresAsyncApproval = true;
                hasSetupAsyncApproval = true;
            }
        }

        private void OnDestroy()
        {
            // Clear singleton reference
            if (instance == this)
            {
                instance = null;
            }

            // Cleanup event subscriptions
            if (GONetMain.SceneManager != null)
            {
                GONetMain.SceneManager.OnValidateSceneLoad -= ValidateSceneLoad;
                GONetMain.SceneManager.OnSceneRequestResponse -= OnSceneRequestResponseReceived;
                GONetMain.SceneManager.OnSceneLoadCompleted -= OnSceneLoadCompleted;
            }
        }

        private void OnEnable()
        {
            // Subscribe to scene loaded event to update button visibility
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            GONetLog.Debug($"[ExitButtonUI] OnSceneLoaded: {scene.name}");

            // Ensure EventSystem still exists after scene load
            // Use Invoke to delay slightly so scene objects are fully instantiated
            Invoke(nameof(EnsureEventSystemExistsDelayed), 0.1f);

            UpdateButtonVisibility();
        }

        private void EnsureEventSystemExistsDelayed()
        {
            EnsureEventSystemExists();
        }

        private void BuildUI()
        {
            // Create button container (upper-right corner)
            buttonContainer = new GameObject("ExitButtonContainer");
            buttonContainer.transform.SetParent(canvas.transform, false);

            RectTransform containerRect = buttonContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(1, 1); // Upper-right anchor
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(1, 1);
            containerRect.anchoredPosition = new Vector2(-marginFromEdge, -marginFromEdge);
            containerRect.sizeDelta = new Vector2(buttonWidth, buttonHeight);

            // Create button
            GameObject buttonGO = new GameObject("ExitButton");
            buttonGO.transform.SetParent(buttonContainer.transform, false);

            RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = Vector2.zero;
            buttonRect.anchorMax = Vector2.one;
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            Image buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);

            exitButton = buttonGO.AddComponent<Button>();
            ColorBlock colors = exitButton.colors;
            colors.normalColor = new Color(0.8f, 0.2f, 0.2f, 0.9f);
            colors.highlightedColor = new Color(0.9f, 0.3f, 0.3f, 1f);
            colors.pressedColor = new Color(0.7f, 0.15f, 0.15f, 1f);
            exitButton.colors = colors;
            exitButton.onClick.AddListener(OnExitClicked);

            // Button label
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(buttonGO.transform, false);

            RectTransform labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = "Exit";
            label.fontSize = 18;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            // Build server approval panel
            BuildApprovalPanel();

            // Build client response panel (approval/denial feedback)
            BuildClientResponsePanel();
        }

        private void BuildClientResponsePanel()
        {
            // Create client response panel (centered)
            clientResponsePanel = new GameObject("ClientResponsePanel");
            clientResponsePanel.transform.SetParent(canvas.transform, false);

            RectTransform responseRect = clientResponsePanel.AddComponent<RectTransform>();
            responseRect.anchorMin = new Vector2(0.25f, 0.3f);
            responseRect.anchorMax = new Vector2(0.75f, 0.7f);
            responseRect.offsetMin = Vector2.zero;
            responseRect.offsetMax = Vector2.zero;

            Image responseImage = clientResponsePanel.AddComponent<Image>();
            responseImage.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            // Response message
            GameObject msgGO = new GameObject("ResponseMessage");
            msgGO.transform.SetParent(clientResponsePanel.transform, false);

            RectTransform msgRect = msgGO.AddComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0.1f, 0.35f);
            msgRect.anchorMax = new Vector2(0.9f, 0.85f);
            msgRect.offsetMin = Vector2.zero;
            msgRect.offsetMax = Vector2.zero;

            clientResponseMessageText = msgGO.AddComponent<TextMeshProUGUI>();
            clientResponseMessageText.text = "Awaiting server approval...";
            clientResponseMessageText.fontSize = 24;
            clientResponseMessageText.alignment = TextAlignmentOptions.Center;
            clientResponseMessageText.color = Color.white;
            clientResponseMessageText.enableAutoSizing = true;
            clientResponseMessageText.fontSizeMin = 16;
            clientResponseMessageText.fontSizeMax = 28;

            // Close button
            GameObject closeButtonGO = new GameObject("CloseButton");
            closeButtonGO.transform.SetParent(clientResponsePanel.transform, false);

            RectTransform closeRect = closeButtonGO.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.3f, 0.08f);
            closeRect.anchorMax = new Vector2(0.7f, 0.25f);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = Vector2.zero;

            Image closeImage = closeButtonGO.AddComponent<Image>();
            closeImage.color = new Color(0.6f, 0.2f, 0.2f, 1f);

            clientResponseCloseButton = closeButtonGO.AddComponent<Button>();
            ColorBlock closeColors = clientResponseCloseButton.colors;
            closeColors.normalColor = new Color(0.6f, 0.2f, 0.2f, 1f);
            closeColors.highlightedColor = new Color(0.7f, 0.3f, 0.3f, 1f);
            closeColors.pressedColor = new Color(0.5f, 0.15f, 0.15f, 1f);
            clientResponseCloseButton.colors = closeColors;
            clientResponseCloseButton.onClick.AddListener(OnClientResponseCloseClicked);

            // Close button label
            GameObject closeLabelGO = new GameObject("Label");
            closeLabelGO.transform.SetParent(closeButtonGO.transform, false);

            RectTransform closeLabelRect = closeLabelGO.AddComponent<RectTransform>();
            closeLabelRect.anchorMin = Vector2.zero;
            closeLabelRect.anchorMax = Vector2.one;
            closeLabelRect.offsetMin = Vector2.zero;
            closeLabelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI closeLabel = closeLabelGO.AddComponent<TextMeshProUGUI>();
            closeLabel.text = "Close";
            closeLabel.fontSize = 18;
            closeLabel.alignment = TextAlignmentOptions.Center;
            closeLabel.color = Color.white;
            closeLabel.enableAutoSizing = true;
            closeLabel.fontSizeMin = 10;
            closeLabel.fontSizeMax = 20;

            // Initially hide close button (only show for denials)
            clientResponseCloseButton.gameObject.SetActive(false);

            // Start hidden
            clientResponsePanel.SetActive(false);
        }

        private void UpdateButtonVisibility()
        {
            if (buttonContainer == null) return;

            string currentScene = SceneManager.GetActiveScene().name;
            bool shouldShow = currentScene != menuSceneName;

            buttonContainer.SetActive(shouldShow);

            GONetLog.Debug($"[ExitButtonUI] Current scene: '{currentScene}', showing Exit button: {shouldShow}");
        }

        private void OnExitClicked()
        {
            GONetLog.Info($"[ExitButtonUI] OnExitClicked called! IsServer: {GONetMain.IsServer}, IsClient: {GONetMain.IsClient}");

            if (GONetMain.SceneManager == null)
            {
                GONetLog.Warning("[ExitButtonUI] GONetMain.SceneManager not initialized");
                return;
            }

            GONetLog.Info($"[ExitButtonUI] Exit button clicked, requesting return to menu scene: {menuSceneName}");

            if (GONetMain.IsServer)
            {
                // Server loads directly
                GONetMain.SceneManager.LoadSceneFromBuildSettings(menuSceneName, LoadSceneMode.Single);
            }
            else if (GONetMain.IsClient)
            {
                // Client requests through RPC - show "awaiting approval" UI
                GONetMain.SceneManager.RequestLoadScene(menuSceneName, LoadSceneMode.Single);

                // Show awaiting approval message
                if (clientResponsePanel != null && clientResponseMessageText != null)
                {
                    clientResponseMessageText.text = $"Awaiting server approval to return to menu...";
                    clientResponseCloseButton.gameObject.SetActive(false); // No close button while waiting
                    clientResponsePanel.SetActive(true);
                    isAwaitingResponse = true;
                }
            }
            else
            {
                GONetLog.Warning("[ExitButtonUI] Not connected as server or client");
            }
        }

        private void OnClientResponseCloseClicked()
        {
            if (clientResponsePanel != null)
            {
                clientResponsePanel.SetActive(false);
                isAwaitingResponse = false;
            }
        }

        private void OnSceneRequestResponseReceived(bool approved, string sceneName, string denialReason)
        {
            // Only show response UI if THIS client is awaiting a response
            if (!isAwaitingResponse || clientResponsePanel == null || clientResponseMessageText == null)
                return;

            GONetLog.Info($"[ExitButtonUI] Scene request response - Approved: {approved}, Scene: '{sceneName}', Reason: '{denialReason}'");

            if (approved)
            {
                // Approval - scene will load, hide the panel
                clientResponsePanel.SetActive(false);
                isAwaitingResponse = false;
            }
            else
            {
                // Denial - show denial message with close button
                clientResponseMessageText.text = $"Request DENIED:\n\nReturn to '{sceneName}'\n\n{denialReason}";
                clientResponseCloseButton.gameObject.SetActive(true);
                clientResponsePanel.SetActive(true);
            }
        }

        private void OnSceneLoadCompleted(string sceneName, LoadSceneMode mode)
        {
            // Hide client response panel when scene loads (approval was granted)
            if (clientResponsePanel != null && clientResponsePanel.activeSelf)
            {
                clientResponsePanel.SetActive(false);
                isAwaitingResponse = false;
            }

            // Hide approval panel if it was showing
            if (approvalPanel != null && approvalPanel.activeSelf)
            {
                approvalPanel.SetActive(false);
                hasPendingRequest = false;
            }
        }

        private void BuildApprovalPanel()
        {
            // Create approval panel (centered, similar to client response panel)
            approvalPanel = new GameObject("ApprovalPanel_ExitButton");
            approvalPanel.transform.SetParent(canvas.transform, false);

            RectTransform approvalRect = approvalPanel.AddComponent<RectTransform>();
            approvalRect.anchorMin = new Vector2(0.25f, 0.3f);
            approvalRect.anchorMax = new Vector2(0.75f, 0.7f);
            approvalRect.offsetMin = Vector2.zero;
            approvalRect.offsetMax = Vector2.zero;

            Image approvalImage = approvalPanel.AddComponent<Image>();
            approvalImage.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            // Title
            GameObject titleGO = new GameObject("Title");
            titleGO.transform.SetParent(approvalPanel.transform, false);
            RectTransform titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.75f);
            titleRect.anchorMax = new Vector2(0.9f, 0.9f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            TextMeshProUGUI titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "Scene Change Request";
            titleText.fontSize = 24;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            // Message
            GameObject msgGO = new GameObject("Message");
            msgGO.transform.SetParent(approvalPanel.transform, false);
            RectTransform msgRect = msgGO.AddComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0.1f, 0.4f);
            msgRect.anchorMax = new Vector2(0.9f, 0.7f);
            msgRect.offsetMin = Vector2.zero;
            msgRect.offsetMax = Vector2.zero;
            approvalMessageText = msgGO.AddComponent<TextMeshProUGUI>();
            approvalMessageText.text = "Client requesting scene change...";
            approvalMessageText.fontSize = 20;
            approvalMessageText.alignment = TextAlignmentOptions.Center;
            approvalMessageText.color = Color.white;
            approvalMessageText.enableAutoSizing = true;
            approvalMessageText.fontSizeMin = 14;
            approvalMessageText.fontSizeMax = 24;

            // Approve button (left side)
            GameObject approveGO = new GameObject("ApproveButton");
            approveGO.transform.SetParent(approvalPanel.transform, false);
            RectTransform approveRect = approveGO.AddComponent<RectTransform>();
            approveRect.anchorMin = new Vector2(0.1f, 0.15f);
            approveRect.anchorMax = new Vector2(0.45f, 0.3f);
            approveRect.offsetMin = Vector2.zero;
            approveRect.offsetMax = Vector2.zero;
            Image approveImage = approveGO.AddComponent<Image>();
            approveImage.color = new Color(0.2f, 0.7f, 0.2f, 1f);
            approveButton = approveGO.AddComponent<Button>();
            approveButton.onClick.AddListener(OnApproveClicked);

            GameObject approveLabelGO = new GameObject("Label");
            approveLabelGO.transform.SetParent(approveGO.transform, false);
            RectTransform approveLabelRect = approveLabelGO.AddComponent<RectTransform>();
            approveLabelRect.anchorMin = Vector2.zero;
            approveLabelRect.anchorMax = Vector2.one;
            approveLabelRect.offsetMin = Vector2.zero;
            approveLabelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI approveLabel = approveLabelGO.AddComponent<TextMeshProUGUI>();
            approveLabel.text = "APPROVE";
            approveLabel.fontSize = 20;
            approveLabel.alignment = TextAlignmentOptions.Center;
            approveLabel.color = Color.white;

            // Deny button (right side)
            GameObject denyGO = new GameObject("DenyButton");
            denyGO.transform.SetParent(approvalPanel.transform, false);
            RectTransform denyRect = denyGO.AddComponent<RectTransform>();
            denyRect.anchorMin = new Vector2(0.55f, 0.15f);
            denyRect.anchorMax = new Vector2(0.9f, 0.3f);
            denyRect.offsetMin = Vector2.zero;
            denyRect.offsetMax = Vector2.zero;
            Image denyImage = denyGO.AddComponent<Image>();
            denyImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            denyButton = denyGO.AddComponent<Button>();
            denyButton.onClick.AddListener(OnDenyClicked);

            GameObject denyLabelGO = new GameObject("Label");
            denyLabelGO.transform.SetParent(denyGO.transform, false);
            RectTransform denyLabelRect = denyLabelGO.AddComponent<RectTransform>();
            denyLabelRect.anchorMin = Vector2.zero;
            denyLabelRect.anchorMax = Vector2.one;
            denyLabelRect.offsetMin = Vector2.zero;
            denyLabelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI denyLabel = denyLabelGO.AddComponent<TextMeshProUGUI>();
            denyLabel.text = "DENY";
            denyLabel.fontSize = 20;
            denyLabel.alignment = TextAlignmentOptions.Center;
            denyLabel.color = Color.white;

            // Start hidden
            approvalPanel.SetActive(false);
        }

        private bool ValidateSceneLoad(string sceneName, LoadSceneMode mode, ushort requestingAuthority)
        {
            GONetLog.Info($"[ExitButtonUI] ValidateSceneLoad: scene='{sceneName}', authority={requestingAuthority}, MyAuthority={GONetMain.MyAuthorityId}, IsServer={GONetMain.IsServer}");

            // Server's own requests are auto-approved (don't show UI)
            if (GONetMain.IsServer && requestingAuthority == GONetMain.MyAuthorityId)
            {
                GONetLog.Info($"[ExitButtonUI] Server's own request - auto-approved (no UI)");
                return true;
            }

            // Client request on server - check if SceneSelectionUI exists first
            if (GONetMain.IsServer && requestingAuthority != GONetMain.MyAuthorityId)
            {
                // Check if SceneSelectionUI exists in the scene - if so, let IT handle the approval UI
                SceneSelectionUI sceneSelectionUI = FindObjectOfType<SceneSelectionUI>();
                if (sceneSelectionUI != null)
                {
                    GONetLog.Info($"[ExitButtonUI] SceneSelectionUI exists - deferring to it for approval UI");
                    return true; // Let SceneSelectionUI handle the approval UI
                }

                // No SceneSelectionUI - we need to show approval dialog
                // Check if another request is already pending
                if (hasPendingRequest)
                {
                    GONetLog.Warning($"[ExitButtonUI] DENIED - another request pending");
                    GONetMain.SceneManager.SendSceneRequestResponse(requestingAuthority, false, sceneName,
                        "Another scene change request is already pending approval. Please wait.");
                    return false;
                }

                // Show our approval dialog
                GONetLog.Info($"[ExitButtonUI] No SceneSelectionUI - showing ExitButtonUI approval UI");
                pendingSceneName = sceneName;
                pendingLoadMode = mode;
                pendingRequestingAuthority = requestingAuthority;
                hasPendingRequest = true;

                if (approvalPanel != null && approvalMessageText != null)
                {
                    approvalMessageText.text = $"Client (Authority {requestingAuthority})\nrequests:\n\n'{sceneName}'\n\nApprove?";
                    approvalPanel.SetActive(true);
                }

                return true; // Allow RPC through - async approval will send response later
            }

            // Client's own requests - always allow
            return true;
        }

        private void OnApproveClicked()
        {
            GONetLog.Info($"[ExitButtonUI] Server APPROVED scene change to '{pendingSceneName}'");
            approvalPanel.SetActive(false);
            hasPendingRequest = false;

            // Server loads the scene
            if (GONetMain.SceneManager != null)
            {
                GONetMain.SceneManager.LoadSceneFromBuildSettings(pendingSceneName, pendingLoadMode);
            }

            // Send approval response to client
            GONetMain.SceneManager.SendSceneRequestResponse(pendingRequestingAuthority, true, pendingSceneName, "");
        }

        private void OnDenyClicked()
        {
            GONetLog.Info($"[ExitButtonUI] Server DENIED scene change to '{pendingSceneName}'");
            approvalPanel.SetActive(false);
            hasPendingRequest = false;

            // Send denial response to client
            GONetMain.SceneManager.SendSceneRequestResponse(pendingRequestingAuthority, false, pendingSceneName, "Server denied the request");
        }
    }
}
