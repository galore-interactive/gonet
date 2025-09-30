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
        private Text timeText;
        private Text roleText;
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
            panelRect.sizeDelta = new Vector2(320, 95);

            Image panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f);

            // Create time text
            timeText = CreateText(panelGO.transform, "TimeText", "Time: 0.00", 24, new Vector2(10, -10), new Vector2(280, 35));

            // Create role text
            roleText = CreateText(panelGO.transform, "RoleText", "Role: Connecting...", 24, new Vector2(10, -50), new Vector2(280, 35));
        }

        private Text CreateText(Transform parent, string name, string initialText, int fontSize, Vector2 position, Vector2 size)
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
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return text;
        }

        private void Update()
        {
            if (timeText != null)
            {
                timeText.text = $"Time: {GONetMain.Time.ElapsedSeconds:F2}s";
            }

            if (roleText != null)
            {
                string role = "Connecting...";

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

                roleText.text = $"Role: {role}";
            }
        }
    }
}
