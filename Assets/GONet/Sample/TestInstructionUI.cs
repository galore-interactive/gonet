using UnityEngine;
using UnityEngine.UI;

namespace GONet.Sample
{
    /// <summary>
    /// Simple UI overlay that displays test instructions to the human operator.
    /// Shows large, centered text for clear visibility.
    /// </summary>
    public class TestInstructionUI : MonoBehaviour
    {
        private Text instructionText;
        private GameObject panel;
        private Image panelImage;

        public void Initialize()
        {
            // Ensure this GameObject has a RectTransform (required for Unity UI hierarchy)
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

            // Create background panel - positioned in lower-right corner
            panel = new GameObject("InstructionPanel");
            panel.transform.SetParent(transform, false);

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            // Anchor to bottom-right corner
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(1f, 0f);
            // Size and position
            panelRect.sizeDelta = new Vector2(550f, 250f);
            panelRect.anchoredPosition = new Vector2(-20f, 20f); // 20px margins from corner

            panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.9f); // Darker, more opaque

            // Create text
            GameObject textObj = new GameObject("InstructionText");
            textObj.transform.SetParent(panel.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(15f, 15f);
            textRect.offsetMax = new Vector2(-15f, -15f);

            instructionText = textObj.AddComponent<Text>();
            instructionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            instructionText.fontSize = 20; // Larger font
            instructionText.alignment = TextAnchor.MiddleCenter;
            instructionText.color = Color.white;
            instructionText.text = "";
            instructionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            instructionText.verticalOverflow = VerticalWrapMode.Overflow;

            panel.SetActive(false);
        }

        public void SetInstruction(string instruction, bool isHumanActionRequired = false)
        {
            if (instructionText == null)
                return;

            if (string.IsNullOrEmpty(instruction))
            {
                panel.SetActive(false);
            }
            else
            {
                instructionText.text = instruction;

                // Color coding:
                // - Human action required: Bright magenta/pink background with yellow text
                // - Status/waiting: Dark background with white text
                if (isHumanActionRequired)
                {
                    panelImage.color = new Color(0.8f, 0f, 0.6f, 0.95f); // Bright magenta
                    instructionText.color = Color.yellow;
                }
                else
                {
                    panelImage.color = new Color(0f, 0f, 0f, 0.85f); // Dark background
                    instructionText.color = Color.white;
                }

                panel.SetActive(true);
            }
        }
    }
}
