using GONet;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GONet.Sample.RpcTests
{
    /// <summary>
    /// Center-screen toggle UI for selecting and running RPC tests.
    ///
    /// Usage:
    /// - Press Shift+R to toggle UI on/off
    /// - Select test category from dropdown
    /// - Select specific test from second dropdown
    /// - Click "▶ RUN TEST" to execute
    ///
    /// Features:
    /// - Machine-aware filtering (only shows applicable tests)
    /// - Test descriptions and expected results
    /// - Execution result feedback
    ///
    /// Setup:
    /// - Requires GONetParticipant component (uses OnGONetReady lifecycle)
    /// - Add to GameObject with GONetParticipant in scene
    /// </summary>
    [RequireComponent(typeof(GONetParticipant))]
    public class RpcTestRunnerUI : GONetParticipantCompanionBehaviour
    {
        #region UI References

        private Canvas canvas;
        private GameObject panel;
        private TMP_Dropdown categoryDropdown;
        private TMP_Dropdown testDropdown;
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI descriptionText;
        private TextMeshProUGUI resultsText;
        private Button runButton;
        private TMP_FontAsset defaultFont;

        #endregion

        #region State

        private bool isVisible = false;
        private RpcTestRegistry.TestCategory? selectedCategory = null;
        private RpcTestRegistry.TestDescriptor selectedTest = null;

        #endregion

        #region Configuration

        [Header("Toggle Key")]
        [SerializeField] private KeyCode toggleKey = KeyCode.R;
        [SerializeField] private bool requireShift = true;

        [Header("UI Dimensions (90% screen coverage)")]
        [Tooltip("Percentage of screen width to use (0-1)")]
        [SerializeField] private float screenCoverageWidth = 0.90f;
        [Tooltip("Percentage of screen height to use (0-1)")]
        [SerializeField] private float screenCoverageHeight = 0.90f;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            
            BuildUI();
        }

        public override void OnGONetReady()
        {
            base.OnGONetReady();

            // GONet is fully initialized - populate UI
            PopulateCategoryDropdown();
            UpdateStatusText();
        }

        internal override void UpdateAfterGONetReady()
        {
            base.UpdateAfterGONetReady();

            // Toggle UI with Shift+R
            bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool shouldToggle = requireShift ? (shiftPressed && Input.GetKeyDown(toggleKey)) : Input.GetKeyDown(toggleKey);

            if (shouldToggle)
            {
                ToggleUI();
            }
        }

        #endregion

        #region UI Construction

        private void BuildUI()
        {
            // Load default TMP font (LiberationSans SDF) for crisp rendering
            defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (defaultFont == null)
            {
                GONetLog.Warning("[RpcTestRunnerUI] Could not load LiberationSans SDF font - text may not be crisp. Using TMP default.");
            }

            // CRITICAL: Create completely separate Canvas GameObject to avoid interference with GONet stats UI
            // This canvas persists across scenes and uses very high sorting order
            GameObject canvasObject = new GameObject("RpcTestRunnerCanvas_Isolated");
            DontDestroyOnLoad(canvasObject); // Persist across scene changes

            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // VERY HIGH - render on top of everything including GONet stats UI
            canvas.pixelPerfect = true; // CRITICAL: Enable pixel-perfect rendering for sharp text

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f; // Balance between width/height scaling
            scaler.referencePixelsPerUnit = 100f; // Standard Unity UI value

            canvasObject.AddComponent<GraphicRaycaster>();

            // Create main panel (CENTER SCREEN, 90% coverage)
            panel = new GameObject("RpcTestRunnerPanel");
            panel.transform.SetParent(canvasObject.transform, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            // Use anchors for percentage-based sizing (90% screen coverage)
            float marginX = (1f - screenCoverageWidth) / 2f;
            float marginY = (1f - screenCoverageHeight) / 2f;
            panelRect.anchorMin = new Vector2(marginX, marginY);
            panelRect.anchorMax = new Vector2(1f - marginX, 1f - marginY);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.05f, 0.1f, 0.15f, 0.98f);

            // Add subtle border
            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.2f, 0.5f, 0.7f, 1f);
            outline.effectDistance = new Vector2(3, -3);

            // Title bar (larger, more prominent)
            TextMeshProUGUI titleText = CreateText(panel.transform, "Title", "🧪 GONet RPC Test Runner 🧪", 38, TextAlignmentOptions.Center);
            titleText.fontStyle = FontStyles.Bold;
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.92f);
            titleRect.anchorMax = new Vector2(0.95f, 0.98f);

            // Instructions (how to close)
            TextMeshProUGUI instructionsText = CreateText(panel.transform, "Instructions", "Press Shift+R to close", 16, TextAlignmentOptions.Center);
            instructionsText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            RectTransform instructionsRect = instructionsText.GetComponent<RectTransform>();
            instructionsRect.anchorMin = new Vector2(0.05f, 0.88f);
            instructionsRect.anchorMax = new Vector2(0.95f, 0.92f);

            // Status text (machine info)
            statusText = CreateText(panel.transform, "Status", "Status: Initializing...", 18, TextAlignmentOptions.Center);
            RectTransform statusRect = statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.05f, 0.82f);
            statusRect.anchorMax = new Vector2(0.95f, 0.87f);

            // Separator line
            GameObject separator1 = CreateSeparator(panel.transform, 0.81f);

            // Category label
            TextMeshProUGUI categoryLabel = CreateText(panel.transform, "CategoryLabel", "Test Category:", 20, TextAlignmentOptions.MidlineLeft);
            categoryLabel.fontStyle = FontStyles.Bold;
            RectTransform categoryLabelRect = categoryLabel.GetComponent<RectTransform>();
            categoryLabelRect.anchorMin = new Vector2(0.05f, 0.74f);
            categoryLabelRect.anchorMax = new Vector2(0.95f, 0.78f);

            // Category dropdown
            categoryDropdown = CreateDropdown(panel.transform, "CategoryDropdown", 0.68f, 0.74f, 18);
            categoryDropdown.onValueChanged.AddListener(OnCategorySelected);

            // Test label
            TextMeshProUGUI testLabel = CreateText(panel.transform, "TestLabel", "Specific Test:", 20, TextAlignmentOptions.MidlineLeft);
            testLabel.fontStyle = FontStyles.Bold;
            RectTransform testLabelRect = testLabel.GetComponent<RectTransform>();
            testLabelRect.anchorMin = new Vector2(0.05f, 0.61f);
            testLabelRect.anchorMax = new Vector2(0.95f, 0.65f);

            // Test dropdown
            testDropdown = CreateDropdown(panel.transform, "TestDropdown", 0.55f, 0.61f, 18);
            testDropdown.onValueChanged.AddListener(OnTestSelected);

            // Separator line
            GameObject separator2 = CreateSeparator(panel.transform, 0.54f);

            // Description text (larger for readability)
            descriptionText = CreateText(panel.transform, "Description", "Select a test category and test to view details.", 16, TextAlignmentOptions.TopLeft);
            descriptionText.enableWordWrapping = true;
            descriptionText.color = new Color(0.9f, 0.9f, 0.95f, 1f);
            RectTransform descRect = descriptionText.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.05f, 0.28f);
            descRect.anchorMax = new Vector2(0.95f, 0.52f);

            // Separator line
            GameObject separator3 = CreateSeparator(panel.transform, 0.27f);

            // Run button (larger text)
            runButton = CreateButton(panel.transform, "RunButton", "▶ RUN TEST", 0.16f, 0.25f);
            runButton.onClick.AddListener(OnRunButtonClicked);
            runButton.interactable = false;

            // Results text (larger for readability)
            resultsText = CreateText(panel.transform, "Results", "", 15, TextAlignmentOptions.TopLeft);
            resultsText.enableWordWrapping = true;
            resultsText.color = new Color(0.7f, 1f, 0.7f, 1f);
            RectTransform resultsRect = resultsText.GetComponent<RectTransform>();
            resultsRect.anchorMin = new Vector2(0.05f, 0.02f);
            resultsRect.anchorMax = new Vector2(0.95f, 0.14f);

            // Start hidden
            panel.SetActive(false);
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI textComp = go.AddComponent<TextMeshProUGUI>();

            // Explicitly assign font for consistent rendering
            if (defaultFont != null)
            {
                textComp.font = defaultFont;
            }

            textComp.text = text;
            textComp.fontSize = fontSize;
            textComp.alignment = alignment;
            textComp.color = Color.white;

            // CRITICAL: Settings for crisp, sharp rendering
            textComp.enableAutoSizing = false;
            textComp.fontStyle = FontStyles.Normal;
            textComp.raycastTarget = true;

            // Disable word wrapping by default (unless enabled later)
            textComp.enableWordWrapping = false;
            textComp.overflowMode = TextOverflowModes.Overflow;

            // Use pixel-perfect rendering (very important for sharpness)
            textComp.extraPadding = false;
            textComp.margin = Vector4.zero;

            // Ensure proper rendering mode (important for crisp text)
            textComp.renderMode = TextRenderFlags.Render;
            textComp.parseCtrlCharacters = true;

            return textComp;
        }

        private TMP_Dropdown CreateDropdown(Transform parent, string name, float yMin, float yMax, int fontSize)
        {
            GameObject dropdownObj = new GameObject(name);
            dropdownObj.transform.SetParent(parent, false);

            RectTransform dropdownRect = dropdownObj.AddComponent<RectTransform>();
            dropdownRect.anchorMin = new Vector2(0.05f, yMin);
            dropdownRect.anchorMax = new Vector2(0.95f, yMax);
            dropdownRect.offsetMin = Vector2.zero;
            dropdownRect.offsetMax = Vector2.zero;

            Image dropdownImage = dropdownObj.AddComponent<Image>();
            dropdownImage.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(dropdownObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(15f, 5f);
            labelRect.offsetMax = new Vector2(-30f, -5f);

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.fontSize = fontSize;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.color = Color.white;

            // Template setup (larger dropdown menu for readability)
            GameObject templateObj = new GameObject("Template");
            templateObj.transform.SetParent(dropdownObj.transform, false);
            RectTransform templateRect = templateObj.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.sizeDelta = new Vector2(0f, 300f); // Larger dropdown menu
            templateRect.anchoredPosition = new Vector2(0f, 2f);

            Image templateBg = templateObj.AddComponent<Image>();
            templateBg.color = new Color(0.1f, 0.1f, 0.15f, 1f);

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(templateObj.transform, false);
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);

            GameObject itemObj = new GameObject("Item");
            itemObj.transform.SetParent(contentObj.transform, false);
            RectTransform itemRect = itemObj.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, 35f); // Taller dropdown items

            Image itemBg = itemObj.AddComponent<Image>();
            itemBg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            GameObject itemLabelObj = new GameObject("Item Label");
            itemLabelObj.transform.SetParent(itemObj.transform, false);
            RectTransform itemLabelRect = itemLabelObj.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(15f, 5f);
            itemLabelRect.offsetMax = new Vector2(-10f, -5f);

            TextMeshProUGUI itemLabel = itemLabelObj.AddComponent<TextMeshProUGUI>();
            itemLabel.fontSize = fontSize;
            itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
            itemLabel.color = Color.white;

            Toggle itemToggle = itemObj.AddComponent<Toggle>();
            itemToggle.targetGraphic = itemBg;

            TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
            dropdown.captionText = labelText;
            dropdown.template = templateRect;
            dropdown.itemText = itemLabel;

            templateObj.SetActive(false);

            return dropdown;
        }

        private Button CreateButton(Transform parent, string name, string labelText, float yMin, float yMax)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);

            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.25f, yMin);
            buttonRect.anchorMax = new Vector2(0.75f, yMax);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            Button button = buttonObj.AddComponent<Button>();
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.7f, 0.3f, 1f);

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.7f, 0.3f, 1f);
            colors.highlightedColor = new Color(0.25f, 0.8f, 0.35f, 1f);
            colors.pressedColor = new Color(0.15f, 0.6f, 0.25f, 1f);
            colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            button.colors = colors;

            GameObject buttonTextObj = new GameObject("Text");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            RectTransform buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.fontSize = 28; // Larger button text
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;
            buttonText.text = labelText;
            buttonText.fontStyle = FontStyles.Bold;

            return button;
        }

        private GameObject CreateSeparator(Transform parent, float yPosition)
        {
            GameObject separator = new GameObject("Separator");
            separator.transform.SetParent(parent, false);

            RectTransform rect = separator.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, yPosition);
            rect.anchorMax = new Vector2(0.95f, yPosition);
            rect.sizeDelta = new Vector2(0f, 2f);
            rect.anchoredPosition = Vector2.zero;

            Image image = separator.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.35f, 0.8f);

            return separator;
        }

        #endregion

        #region UI Interaction

        private void ToggleUI()
        {
            isVisible = !isVisible;
            panel.SetActive(isVisible);

            if (isVisible)
            {
                // Refresh status when opening
                UpdateStatusText();
            }
        }

        private void OnCategorySelected(int index)
        {
            if (index == 0)
            {
                selectedCategory = null;
                testDropdown.options.Clear();
                testDropdown.options.Add(new TMP_Dropdown.OptionData("-- Select a category first --"));
                testDropdown.value = 0;
                testDropdown.RefreshShownValue();
                descriptionText.text = "Select a test category to view available tests.";
                runButton.interactable = false;
                resultsText.text = "";
                return;
            }

            selectedCategory = (RpcTestRegistry.TestCategory)(index - 1);
            PopulateTestDropdown();
        }

        private void OnTestSelected(int index)
        {
            if (!selectedCategory.HasValue || index == 0)
            {
                selectedTest = null;
                descriptionText.text = "Select a test to view details.";
                runButton.interactable = false;
                resultsText.text = "";
                return;
            }

            List<RpcTestRegistry.TestDescriptor> applicableTests = RpcTestRegistry.GetApplicableTests(selectedCategory.Value);
            selectedTest = applicableTests[index - 1];

            // Display test details
            descriptionText.text = $"<b>{selectedTest.Name}</b>\n\n" +
                                  $"<b>Description:</b>\n{selectedTest.Description}\n\n" +
                                  $"<b>Expected Result:</b>\n{selectedTest.ExpectedResult}\n\n" +
                                  $"<b>Applicable Machines:</b> {selectedTest.ApplicableMachines}";

            runButton.interactable = true;
            resultsText.text = "";
        }

        private void OnRunButtonClicked()
        {
            if (selectedTest == null)
            {
                GONetLog.Warning("[RpcTestRunnerUI] No test selected");
                return;
            }

            GONetLog.Info($"[RpcTestRunnerUI] Running test: {selectedTest.Name}");
            resultsText.text = $"<b>Running test:</b> {selectedTest.Name}...\n(Check logs for execution details)";

            // Invoke the test
            try
            {
                selectedTest.InvokeTest?.Invoke();
                resultsText.text = $"<b>Last Run Results:</b>\n✓ Test invoked successfully: {selectedTest.Name}\n\nCheck logs for RPC execution tracking (Shift+K to dump summary).";
                resultsText.color = new Color(0.7f, 1f, 0.7f, 1f);
            }
            catch (Exception ex)
            {
                resultsText.text = $"<b>Last Run Results:</b>\n❌ Test FAILED with exception:\n{ex.Message}";
                resultsText.color = new Color(1f, 0.5f, 0.5f, 1f);
                GONetLog.Error($"[RpcTestRunnerUI] Test failed: {ex}");
            }
        }

        #endregion

        #region Population

        private void PopulateCategoryDropdown()
        {
            categoryDropdown.options.Clear();
            categoryDropdown.options.Add(new TMP_Dropdown.OptionData("-- Select Test Category --"));

            // Add all test categories
            foreach (RpcTestRegistry.TestCategory category in Enum.GetValues(typeof(RpcTestRegistry.TestCategory)))
            {
                categoryDropdown.options.Add(new TMP_Dropdown.OptionData(RpcTestRegistry.GetCategoryDisplayName(category)));
            }

            categoryDropdown.value = 0;
            categoryDropdown.RefreshShownValue();
        }

        private void PopulateTestDropdown()
        {
            if (!selectedCategory.HasValue)
                return;

            testDropdown.options.Clear();
            testDropdown.options.Add(new TMP_Dropdown.OptionData("-- Select Test --"));

            // Get only tests applicable to this machine
            List<RpcTestRegistry.TestDescriptor> applicableTests = RpcTestRegistry.GetApplicableTests(selectedCategory.Value);

            foreach (var test in applicableTests)
            {
                testDropdown.options.Add(new TMP_Dropdown.OptionData(test.Name));
            }

            testDropdown.value = 0;
            testDropdown.RefreshShownValue();

            if (applicableTests.Count == 0)
            {
                descriptionText.text = $"<b>No tests available</b>\n\n" +
                                      $"Category: {RpcTestRegistry.GetCategoryDisplayName(selectedCategory.Value)}\n\n" +
                                      $"No tests in this category are applicable to this machine.\n\n" +
                                      $"Current machine: {(GONetMain.IsServer ? "Server" : $"Client:{GONetMain.MyAuthorityId}")}";
            }
            else
            {
                descriptionText.text = $"{applicableTests.Count} test(s) available for this machine.";
            }

            runButton.interactable = false;
            resultsText.text = "";
        }

        private void UpdateStatusText()
        {
            string machine = GONetMain.IsServer ? "Server" : $"Client:{GONetMain.MyAuthorityId}";
            int connectedClients = GONetMain.IsServer && GONetMain.gonetServer != null ? GONetMain.gonetServer.remoteClients.Count : 0;

            statusText.text = $"Machine: {machine} | ";

            if (GONetMain.IsServer)
            {
                statusText.text += $"Connected Clients: {connectedClients} | ";
            }

            statusText.text += "Ready";
        }

        #endregion

        #region Public API

        /// <summary>
        /// Programmatically show the UI (alternative to keyboard toggle).
        /// </summary>
        public void Show()
        {
            if (!isVisible)
            {
                ToggleUI();
            }
        }

        /// <summary>
        /// Programmatically hide the UI (alternative to keyboard toggle).
        /// </summary>
        public void Hide()
        {
            if (isVisible)
            {
                ToggleUI();
            }
        }

        #endregion
    }
}
