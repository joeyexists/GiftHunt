using MelonLoader;
using System.Collections;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Color = UnityEngine.Color;

namespace GiftHunt
{
    internal static class UITextManager
    {
        private static Canvas canvas = null;
        private static float scale = Screen.height / 1440f;

        public static void Initialize()
        {
            if (UnityEngine.Object.FindObjectOfType<Canvas>() is not Canvas _canvas)
            {
                MelonLogger.Warning("Text initialization failed: canvas not found.");
                return;
            }

            canvas = _canvas;

            PopupText.Initialize();
            TimeText.Initialize();
        }

        private static (GameObject, TextMeshProUGUI) CreateTextObj(
            string name, Transform parent, float fontSize, Color outlineColor, float outlineWidth, Color textColor, Vector2 sizeDelta, float yOffset = 0f)
        {
            GameObject obj = new(name);
            obj.transform.SetParent(parent, false);

            TextMeshProUGUI mesh = obj.AddComponent<TextMeshProUGUI>();
            mesh.alignment = TextAlignmentOptions.Center;
            mesh.fontSize = fontSize * scale;
            mesh.outlineColor = outlineColor;
            mesh.outlineWidth = outlineWidth;
            mesh.color = textColor;

            RectTransform rectTransform = mesh.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.8f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.8f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, yOffset * scale);
            rectTransform.sizeDelta = sizeDelta * scale;

            return (obj, mesh);
        }

        private static IEnumerator FadeOutText(TextMeshProUGUI text, Color startColor, float fadeDuration, float displayDuration = 0f)
        {
            text.color = startColor;

            if (displayDuration > 0f)
                yield return new WaitForSecondsRealtime(displayDuration);

            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
        }

        private static void RestartFadeOut(ref object coroutine, IEnumerator routine)
        {
            if (coroutine != null)
                MelonCoroutines.Stop(coroutine);

            coroutine = MelonCoroutines.Start(routine);
        }

        private static void ResizeText(TextMeshProUGUI mesh, float fontSize, Vector2 sizeDelta, float yOffset = 0f)
        {
            mesh.fontSize = fontSize * scale;

            RectTransform rectTransform = mesh.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(0f, yOffset * scale);
            rectTransform.sizeDelta = sizeDelta * scale;
        }

        public static class PopupText
        {
            private static GameObject obj = null;
            private static TextMeshProUGUI mesh = null;

            private static object fadeOutCoroutine;

            private const string Name = "GiftHunt_PopupText";

            private const float DefaultDisplayDuration = 0.5f;
            private const float FadeDuration = 1f;

            private static readonly Color TextColor = Color.yellow;
            private static readonly Color OutlineColor = new(0.20f, 0.18f, 0f);

            private const float BaseFontSize = 38f;
            private const float OutlineWidth = 0.16f;

            private static readonly Vector2 BaseSizeDelta = new(800f, 50f);
            private const float yOffset = -42f;

            public static void Initialize()
            {
                if (canvas == null)
                    return;

                (obj, mesh) = CreateTextObj(
                    Name, canvas.transform, BaseFontSize, OutlineColor, OutlineWidth, TextColor, BaseSizeDelta, yOffset);
            }

            public static void DisplayMessage(string message, float displayDuration = DefaultDisplayDuration)
            {
                if (obj == null || mesh == null)
                    return;

                mesh.text = message;

                RestartFadeOut(ref fadeOutCoroutine, FadeOutText(mesh, TextColor, FadeDuration, displayDuration));
            }

            public static void Resize()
            {
                ResizeText(mesh, BaseFontSize, BaseSizeDelta, yOffset);
            }
        }

        public static class TimeText
        {
            private static GameObject obj = null;
            private static TextMeshProUGUI mesh = null;

            public static bool visible = false;

            private static object fadeOutCoroutine;

            private const string Name = "GiftHunt_TimeText";

            private const float FadeDuration = 0.3f;

            private static readonly Color TextColor = Color.cyan;
            private static readonly Color OutlineColor = new(0f, 0.19f, 0.19f);

            private const float FontSize = 32f;
            private const float OutlineWidth = 0.16f;

            private static readonly Vector2 SizeDelta = new Vector2(200f, 50f);

            public static void Initialize()
            {
                if (canvas == null)
                    return;

                (obj, mesh) = CreateTextObj(
                    Name, canvas.transform, FontSize, OutlineColor, OutlineWidth, TextColor, SizeDelta);
            }

            public static void DisplayTime(string timeStr)
            {
                if (obj == null || mesh == null)
                    return;

                if (fadeOutCoroutine != null)
                    MelonCoroutines.Stop(fadeOutCoroutine);

                mesh.text = timeStr;
                mesh.color = TextColor;
                visible = true;
            }

            public static void FadeOut()
            {
                if (obj == null || mesh == null)
                    return;

                RestartFadeOut(ref fadeOutCoroutine, FadeOutText(mesh, TextColor, FadeDuration));

                visible = false;
            }

            public static void Resize()
            {
                ResizeText(mesh, FontSize, SizeDelta);
            }
        }

        [HarmonyPatch(typeof(MenuScreenOptionsPanel), "ApplyChanges")]
        public static class PostApplyChanges
        {
            [HarmonyPostfix]
            static void Postfix()
            {
                // resize text on resolution change
                scale = GameDataManager.prefs.resHeight / 1440f;

                PopupText.Resize();
                TimeText.Resize();
            }
        }
    }
}