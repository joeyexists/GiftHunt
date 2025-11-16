using MelonLoader;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace GiftHunt.UI
{
    internal static class PopupManager
    {
        public static bool IsInitialized { get; private set; }

        public static List<PopupText> PopupTexts { get; } = [];

        public static PopupText InfoText { get; private set; }
        public static PopupText TimeText { get; private set; }
        public static PopupText DiffText { get; private set; }
        public static PopupText DevTimeText { get; private set; }

        private static Canvas canvas;
        private static CanvasScaler canvasScaler;

        public static void Initialize()
        {
            if (IsInitialized) return;

            CreateCanvas();

            InfoText = new PopupText("InfoText", 38f, Color.yellow, new Color32(51, 46, 0, 255), 0.16f,
                new Vector2(800f, 50f), new Vector2(0.5f, 0.8f), -112f);
            TimeText = new PopupText("TimeText", 34f, Color.cyan, new Color32(0, 48, 48, 255),
                0.16f, new Vector2(600f, 50f), new Vector2(0.5f, 0.8f), -36f);
            DiffText = new PopupText("DiffText", 32f, new Color32(50, 255, 50, 255), new Color32(0, 48, 0, 255),
                0.16f, new Vector2(600f, 50f), new Vector2(0.5f, 0.8f), 0f);
            DevTimeText = new PopupText("DevTimeText", 32f, new Color32(255, 144, 0, 255), new Color32(73, 35, 0, 255),
                0.16f, new Vector2(600f, 50f), new Vector2(0.5f, 0.8f), -74f);

            IsInitialized = true;
        }

        private static void CreateCanvas()
        {
            if (canvas != null) return;

            var modName = GiftHunt.ModInstance.Info.Name;
            var go = new GameObject($"{modName}.PopupCanvas");

            UnityEngine.Object.DontDestroyOnLoad(go);

            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvasScaler = go.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(2560f, 1440f);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
        }

        public static void FadeOutAll(float fadeDuration = 0.3f)
        {
            foreach (var popup in PopupTexts)
                popup?.FadeOut(fadeDuration);
        }

        public static void Deinitialize()
        {
            if (!IsInitialized) return;

            foreach (var popup in PopupTexts)
                popup?.StopFadeOut(); // stop coroutines

            if (canvas != null)
            {
                // destroy canvas and child popups
                UnityEngine.Object.Destroy(canvas.gameObject);
                canvas = null;
                canvasScaler = null;
            }

            PopupTexts.Clear();
            InfoText = null;
            TimeText = null;
            DevTimeText = null;
            DiffText = null;

            IsInitialized = false;
        }

        public class PopupText
        {
            public GameObject GameObject { get; private set; }
            public TextMeshProUGUI Mesh { get; private set; }

            private Color textColor;
            private object fadeCoroutine;

            public PopupText(string name, float fontSize, Color textColor, Color outlineColor, float outlineWidth,
                             Vector2 sizeDelta, Vector2 anchor, float yOffset)
            {
                this.textColor = textColor;

                var modName = GiftHunt.ModInstance.Info.Name;
                GameObject = new GameObject($"{modName}.{name}");
                GameObject.transform.SetParent(canvas.transform, false);

                Mesh = GameObject.AddComponent<TextMeshProUGUI>();
                Mesh.alignment = TextAlignmentOptions.Center;
                Mesh.fontSize = fontSize;
                Mesh.outlineColor = outlineColor;
                Mesh.outlineWidth = outlineWidth;
                Mesh.color = textColor;

                RectTransform rect = Mesh.rectTransform;
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, yOffset);
                rect.sizeDelta = sizeDelta;

                PopupTexts.Add(this);
            }

            public void DisplayMessage(string message, float fadeDuration = 1f, float displayDuration = 0.5f)
            {
                Mesh.text = message;
                RestartFadeOut(MelonCoroutines.Start(FadeOutRoutine(fadeDuration, displayDuration)));
            }

            public void DisplayMessagePersistent(string message) 
            { 
                StopFadeOut(); 
                Mesh.text = message;
                Mesh.color = textColor; 
            }

            public void FadeOut(float fadeDuration = 0.3f)
            {
                if (Mesh.color.a <= 0f) return;
                RestartFadeOut(MelonCoroutines.Start(FadeOutRoutine(fadeDuration, 0f)));
            }

            public void StopFadeOut()
            {
                if (fadeCoroutine != null)
                    MelonCoroutines.Stop(fadeCoroutine);
                fadeCoroutine = null;
            }

            private IEnumerator FadeOutRoutine(float fadeDuration, float displayDuration)
            {
                Mesh.color = textColor;

                if (displayDuration > 0f)
                    yield return new WaitForSecondsRealtime(displayDuration);

                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                    Mesh.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
                    yield return null;
                }
            }

            private void RestartFadeOut(object newRoutine)
            {
                if (fadeCoroutine != null)
                    MelonCoroutines.Stop(fadeCoroutine);
                fadeCoroutine = newRoutine;
            }

            public void SetColor(Color textColor, Color outlineColor)
            {
                this.textColor = textColor;

                Mesh.color = textColor;
                Mesh.outlineColor = outlineColor;
            }
        }
    }
}
