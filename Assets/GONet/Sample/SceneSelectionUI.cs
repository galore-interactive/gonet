using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        [SerializeField] private Text statusText;
        [SerializeField] private Text instructionsText;
        [SerializeField] private Text titleText;
        [SerializeField] private Button projectileTestButton;
        [SerializeField] private Button rpcPlaygroundButton;
        [SerializeField] private Button backToMenuButton;

        [Header("Scene Names")]
        [SerializeField] private string projectileTestSceneName = "ProjectileTest";
        [SerializeField] private string rpcPlaygroundSceneName = "JustAnotherScene";
        [SerializeField] private string menuSceneName = "GONetSample";

        [Header("Auto-Build UI")]
        [SerializeField] private bool autoBuildUI = true;

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

            // Setup validation hook to allow all requests in this demo
            if (GONetMain.SceneManager != null)
            {
                GONetMain.SceneManager.OnValidateSceneLoad += ValidateSceneLoad;

                // Subscribe to scene events for status updates
                GONetMain.SceneManager.OnSceneLoadStarted += OnSceneLoadStarted;
                GONetMain.SceneManager.OnSceneLoadCompleted += OnSceneLoadCompleted;
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

            // Create panel
            GameObject panel = new GameObject("SceneSelectionPanel");
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.2f, 0.2f);
            panelRect.anchorMax = new Vector2(0.8f, 0.8f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            // Title
            titleText = CreateText(panel.transform, "Title", "GONet Scene Management Demo", 24, TextAnchor.UpperCenter);
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.85f);
            titleRect.anchorMax = new Vector2(0.9f, 0.95f);

            // Status text
            statusText = CreateText(panel.transform, "Status", "Connecting...", 16, TextAnchor.UpperLeft);
            RectTransform statusRect = statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.1f, 0.65f);
            statusRect.anchorMax = new Vector2(0.9f, 0.8f);

            // Instructions text
            instructionsText = CreateText(panel.transform, "Instructions", "Select a scene to load:", 14, TextAnchor.MiddleLeft);
            RectTransform instrRect = instructionsText.GetComponent<RectTransform>();
            instrRect.anchorMin = new Vector2(0.1f, 0.52f);
            instrRect.anchorMax = new Vector2(0.9f, 0.62f);

            // Buttons
            projectileTestButton = CreateButton(panel.transform, "ProjectileTestButton", "Load Projectile Test Scene", 0.4f);
            rpcPlaygroundButton = CreateButton(panel.transform, "RPCPlaygroundButton", "Load RPC Playground (Chat)", 0.25f);
            backToMenuButton = CreateButton(panel.transform, "BackButton", "Back to Menu", 0.1f);
        }

        private Text CreateText(Transform parent, string name, string text, int fontSize, TextAnchor alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text textComp = go.AddComponent<Text>();
            textComp.text = text;
            textComp.fontSize = fontSize;
            textComp.alignment = alignment;
            textComp.color = Color.white;
            textComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
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

            Text label = labelGO.AddComponent<Text>();
            label.text = labelText;
            label.fontSize = 18;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            return button;
        }

        private void OnDestroy()
        {
            // Cleanup
            if (GONetMain.SceneManager != null)
            {
                GONetMain.SceneManager.OnValidateSceneLoad -= ValidateSceneLoad;
                GONetMain.SceneManager.OnSceneLoadStarted -= OnSceneLoadStarted;
                GONetMain.SceneManager.OnSceneLoadCompleted -= OnSceneLoadCompleted;
            }
        }

        private void Update()
        {
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
                // Server loads directly
                GONetMain.SceneManager.LoadSceneFromBuildSettings(sceneName, LoadSceneMode.Single);
            }
            else if (GONetMain.IsClient)
            {
                // Client requests through RPC
                GONetMain.SceneManager.RequestLoadScene(sceneName, LoadSceneMode.Single, SceneLoadType.BuildSettings);
            }
            else
            {
                GONetLog.Warning("[SceneSelectionUI] Not connected as server or client");
            }
        }

        /// <summary>
        /// Validation hook - in this demo, we allow all scene load requests.
        /// In production, you would add permission checks here.
        /// </summary>
        private bool ValidateSceneLoad(string sceneName, LoadSceneMode mode, ushort requestingAuthority)
        {
            GONetLog.Info($"[SceneSelectionUI] Validating scene load request: '{sceneName}' from authority {requestingAuthority}");

            // For this demo, allow all requests
            return true;
        }

        private void OnSceneLoadStarted(string sceneName, LoadSceneMode mode)
        {
            GONetLog.Info($"[SceneSelectionUI] Scene load started: {sceneName}");
        }

        private void OnSceneLoadCompleted(string sceneName, LoadSceneMode mode)
        {
            GONetLog.Info($"[SceneSelectionUI] Scene load completed: {sceneName}");
        }
    }
}
