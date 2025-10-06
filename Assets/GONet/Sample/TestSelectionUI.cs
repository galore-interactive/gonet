using GONet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GONet.Sample
{
    /// <summary>
    /// UI component for selecting and starting tests.
    /// Shows a dropdown with all available test files and a Start button.
    /// </summary>
    public class TestSelectionUI : MonoBehaviour
    {
        private TMP_Dropdown testDropdown;
        private Button startButton;
        private Button collapseButton;
        private Button expandButton;
        private TextMeshProUGUI instructionText;
        private GameObject panel;
        private GameObject expandButtonObj;
        private Image panelImage;

        private List<string> availableTests = new List<string>();
        private string selectedTestName = null;
        private bool isCollapsed = true; // Start collapsed

        public delegate void OnTestStartRequested(string testName);
        public event OnTestStartRequested TestStartRequested;

        public void Initialize()
        {
            // Ensure this GameObject has a RectTransform
            RectTransform thisRect = gameObject.GetComponent<RectTransform>();
            if (thisRect == null)
            {
                thisRect = gameObject.AddComponent<RectTransform>();
            }

            // Make this fill the entire canvas
            thisRect.anchorMin = Vector2.zero;
            thisRect.anchorMax = Vector2.one;
            thisRect.offsetMin = Vector2.zero;
            thisRect.offsetMax = Vector2.zero;

            // Create background panel - bottom-right corner
            panel = new GameObject("TestSelectionPanel");
            panel.transform.SetParent(transform, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(1f, 0f);
            panelRect.sizeDelta = new Vector2(600f, 400f);
            panelRect.anchoredPosition = new Vector2(-20f, 20f); // 20px margins from corner

            panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Title text
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(panel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.8f);
            titleRect.anchorMax = new Vector2(1f, 0.95f);
            titleRect.offsetMin = new Vector2(20f, 0f);
            titleRect.offsetMax = new Vector2(-20f, -10f);

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.fontSize = 28;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
            titleText.text = "🧪 GONet Test Runner 🧪";
            titleText.fontStyle = FontStyles.Bold;

            // Dropdown
            GameObject dropdownObj = new GameObject("TestDropdown");
            dropdownObj.transform.SetParent(panel.transform, false);
            RectTransform dropdownRect = dropdownObj.AddComponent<RectTransform>();
            dropdownRect.anchorMin = new Vector2(0.1f, 0.55f);
            dropdownRect.anchorMax = new Vector2(0.9f, 0.7f);
            dropdownRect.offsetMin = Vector2.zero;
            dropdownRect.offsetMax = Vector2.zero;

            // Setup dropdown visuals
            Image dropdownImage = dropdownObj.AddComponent<Image>();
            dropdownImage.color = new Color(0.2f, 0.2f, 0.25f, 1f);

            // Dropdown label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(dropdownObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(10f, 2f);
            labelRect.offsetMax = new Vector2(-25f, -2f);

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.fontSize = 16;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.color = Color.white;

            // Create template for dropdown
            GameObject templateObj = new GameObject("Template");
            templateObj.transform.SetParent(dropdownObj.transform, false);
            RectTransform templateRect = templateObj.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.sizeDelta = new Vector2(0f, 150f);
            templateRect.anchoredPosition = new Vector2(0f, 2f);

            // Template needs a ScrollRect and item
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(templateObj.transform, false);
            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);

            GameObject itemObj = new GameObject("Item");
            itemObj.transform.SetParent(contentObj.transform, false);
            RectTransform itemRect = itemObj.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, 25f);

            // Item background
            Image itemBg = itemObj.AddComponent<Image>();
            itemBg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            // Item checkmark (required for dropdown)
            GameObject checkmarkObj = new GameObject("Item Checkmark");
            checkmarkObj.transform.SetParent(itemObj.transform, false);
            RectTransform checkmarkRect = checkmarkObj.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0f, 0.5f);
            checkmarkRect.anchorMax = new Vector2(0f, 0.5f);
            checkmarkRect.sizeDelta = new Vector2(20f, 20f);
            checkmarkRect.anchoredPosition = new Vector2(10f, 0f);
            Image checkmark = checkmarkObj.AddComponent<Image>();
            checkmark.color = Color.white;

            GameObject itemLabelObj = new GameObject("Item Label");
            itemLabelObj.transform.SetParent(itemObj.transform, false);
            RectTransform itemLabelRect = itemLabelObj.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(25f, 2f);
            itemLabelRect.offsetMax = new Vector2(-5f, -2f);

            TextMeshProUGUI itemLabel = itemLabelObj.AddComponent<TextMeshProUGUI>();
            itemLabel.fontSize = 14;
            itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
            itemLabel.color = Color.white;

            Toggle itemToggle = itemObj.AddComponent<Toggle>();
            itemToggle.targetGraphic = itemBg;
            itemToggle.graphic = checkmark;

            // Add dropdown component AFTER setting up template
            testDropdown = dropdownObj.AddComponent<TMP_Dropdown>();
            testDropdown.captionText = labelText;
            testDropdown.template = templateRect;
            testDropdown.itemText = itemLabel;

            templateObj.SetActive(false);

            // Populate dropdown with available tests
            LoadAvailableTests();

            // Start button
            GameObject buttonObj = new GameObject("StartButton");
            buttonObj.transform.SetParent(panel.transform, false);
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.3f, 0.35f);
            buttonRect.anchorMax = new Vector2(0.7f, 0.5f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            startButton = buttonObj.AddComponent<Button>();
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.7f, 0.3f, 1f);

            // Button text
            GameObject buttonTextObj = new GameObject("Text");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            RectTransform buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.fontSize = 24;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;
            buttonText.text = "▶ START TEST";
            buttonText.fontStyle = FontStyles.Bold;

            startButton.onClick.AddListener(OnStartButtonClicked);

            // Instructions text
            GameObject instructionsObj = new GameObject("InstructionsText");
            instructionsObj.transform.SetParent(panel.transform, false);
            RectTransform instructionsRect = instructionsObj.AddComponent<RectTransform>();
            instructionsRect.anchorMin = new Vector2(0.05f, 0.05f);
            instructionsRect.anchorMax = new Vector2(0.95f, 0.3f);
            instructionsRect.offsetMin = Vector2.zero;
            instructionsRect.offsetMax = Vector2.zero;

            instructionText = instructionsObj.AddComponent<TextMeshProUGUI>();
            instructionText.fontSize = 13;
            instructionText.alignment = TextAlignmentOptions.TopLeft;
            instructionText.color = new Color(0.9f, 0.9f, 1f, 1f);
            instructionText.text = "Select a test from the dropdown above.\n\n" +
                                   "Pre-test setup instructions will appear here.";
            instructionText.enableWordWrapping = true;
            instructionText.overflowMode = TextOverflowModes.Overflow;

            // Collapse button (top-right of panel)
            GameObject collapseButtonObj = new GameObject("CollapseButton");
            collapseButtonObj.transform.SetParent(panel.transform, false);
            RectTransform collapseRect = collapseButtonObj.AddComponent<RectTransform>();
            collapseRect.anchorMin = new Vector2(1f, 1f);
            collapseRect.anchorMax = new Vector2(1f, 1f);
            collapseRect.pivot = new Vector2(1f, 1f);
            collapseRect.sizeDelta = new Vector2(80f, 40f);
            collapseRect.anchoredPosition = new Vector2(-10f, -10f);

            collapseButton = collapseButtonObj.AddComponent<Button>();
            Image collapseImage = collapseButtonObj.AddComponent<Image>();
            collapseImage.color = new Color(0.3f, 0.3f, 0.35f, 1f);

            GameObject collapseTextObj = new GameObject("Text");
            collapseTextObj.transform.SetParent(collapseButtonObj.transform, false);
            RectTransform collapseTextRect = collapseTextObj.AddComponent<RectTransform>();
            collapseTextRect.anchorMin = Vector2.zero;
            collapseTextRect.anchorMax = Vector2.one;
            collapseTextRect.offsetMin = Vector2.zero;
            collapseTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI collapseText = collapseTextObj.AddComponent<TextMeshProUGUI>();
            collapseText.fontSize = 16;
            collapseText.alignment = TextAlignmentOptions.Center;
            collapseText.color = Color.white;
            collapseText.text = "◀ Hide";

            collapseButton.onClick.AddListener(OnCollapseClicked);

            // Create expand button (shown when collapsed)
            CreateExpandButton();

            // Start collapsed
            panel.SetActive(false);
            expandButtonObj.SetActive(true);
        }

        private void CreateExpandButton()
        {
            expandButtonObj = new GameObject("ExpandButton");
            expandButtonObj.transform.SetParent(transform, false);

            RectTransform expandRect = expandButtonObj.AddComponent<RectTransform>();
            expandRect.anchorMin = new Vector2(1f, 0f);
            expandRect.anchorMax = new Vector2(1f, 0f);
            expandRect.pivot = new Vector2(1f, 0f);
            expandRect.sizeDelta = new Vector2(100f, 50f);
            expandRect.anchoredPosition = new Vector2(-20f, 20f);

            expandButton = expandButtonObj.AddComponent<Button>();
            Image expandImage = expandButtonObj.AddComponent<Image>();
            expandImage.color = new Color(0.2f, 0.5f, 0.7f, 0.95f);

            GameObject expandTextObj = new GameObject("Text");
            expandTextObj.transform.SetParent(expandButtonObj.transform, false);
            RectTransform expandTextRect = expandTextObj.AddComponent<RectTransform>();
            expandTextRect.anchorMin = Vector2.zero;
            expandTextRect.anchorMax = Vector2.one;
            expandTextRect.offsetMin = Vector2.zero;
            expandTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI expandText = expandTextObj.AddComponent<TextMeshProUGUI>();
            expandText.fontSize = 16;
            expandText.alignment = TextAlignmentOptions.Center;
            expandText.color = Color.white;
            expandText.text = "🧪 Tests ▶";
            expandText.fontStyle = FontStyles.Bold;

            expandButton.onClick.AddListener(OnExpandClicked);
        }

        private void OnCollapseClicked()
        {
            isCollapsed = true;
            panel.SetActive(false);
            expandButtonObj.SetActive(true);
        }

        private void OnExpandClicked()
        {
            isCollapsed = false;
            panel.SetActive(true);
            expandButtonObj.SetActive(false);
        }

        private void LoadAvailableTests()
        {
            availableTests.Clear();
            testDropdown.options.Clear();

            // Load all TextAsset files from Resources/Tests folder
            TextAsset[] testFiles = Resources.LoadAll<TextAsset>("Tests");

            testDropdown.options.Add(new TMP_Dropdown.OptionData("-- Select a test --"));

            foreach (var testFile in testFiles)
            {
                availableTests.Add(testFile.name);
                testDropdown.options.Add(new TMP_Dropdown.OptionData(testFile.name));
            }

            testDropdown.value = 0;
            testDropdown.RefreshShownValue();

            testDropdown.onValueChanged.AddListener(OnTestSelected);

            GONetLog.Info($"[TestSelectionUI] Loaded {availableTests.Count} test(s)");
        }

        private void OnTestSelected(int index)
        {
            if (index == 0)
            {
                selectedTestName = null;
                instructionText.text = "Select a test from the dropdown above.\n\n" +
                                      "Pre-test setup instructions will appear here.";
                return;
            }

            selectedTestName = availableTests[index - 1];

            // Load the test script to get pre-test conditions
            TextAsset testAsset = Resources.Load<TextAsset>($"Tests/{selectedTestName}");
            if (testAsset != null)
            {
                GONetTestScript script = GONetTestScript.ParseFromString(testAsset.text);
                if (script != null)
                {
                    string preTestConditions = GetPreTestConditions(script);
                    instructionText.text = $"Test: {script.name}\n\n" +
                                          $"Description: {script.description}\n\n" +
                                          $"Required Clients: {script.requireClients}\n\n" +
                                          $"{preTestConditions}";
                }
            }
        }

        private string GetPreTestConditions(GONetTestScript script)
        {
            // Check for pre_condition metadata in script
            if (!string.IsNullOrEmpty(script.preCondition))
            {
                return $"⚠ PRE-TEST SETUP REQUIRED:\n{script.preCondition}";
            }

            // Default conditions based on test requirements
            if (script.requireClients > 0)
            {
                return $"✓ Ready to start.\nTest will wait for {script.requireClients} client(s) to connect.";
            }

            return "✓ Ready to start.";
        }

        private void OnStartButtonClicked()
        {
            if (string.IsNullOrEmpty(selectedTestName))
            {
                GONetLog.Warning("[TestSelectionUI] No test selected");
                return;
            }

            GONetLog.Info($"[TestSelectionUI] Starting test: {selectedTestName}");
            TestStartRequested?.Invoke(selectedTestName);

            // Hide both the panel and expand button when test starts
            panel.SetActive(false);
            expandButtonObj.SetActive(false);
        }

        public void Show()
        {
            if (isCollapsed)
            {
                if (expandButtonObj != null)
                    expandButtonObj.SetActive(true);
                if (panel != null)
                    panel.SetActive(false);
            }
            else
            {
                if (panel != null)
                    panel.SetActive(true);
                if (expandButtonObj != null)
                    expandButtonObj.SetActive(false);
            }
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
            if (expandButtonObj != null)
                expandButtonObj.SetActive(false);
        }
    }
}
