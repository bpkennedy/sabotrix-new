using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Sabotrix.Game.UI
{
    // Shared helpers for building gray-box uGUI screens in code.
    public static class UiBuilder
    {
        public static readonly Color BackgroundColor = new Color(0.12f, 0.1f, 0.1f);
        public static readonly Color PanelColor = new Color(0.18f, 0.16f, 0.16f);
        public static readonly Color ButtonColor = new Color(0.2f, 0.5f, 0.3f);

        private static Font _font;

        private static Font Font
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                return _font;
            }
        }

        // A full-screen canvas scaled for 1920x1080, with a stretched background panel.
        public static RectTransform CreateCanvas(string name, GameObject owner)
        {
            EnsureEventSystem();

            owner.name = name;
            owner.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = owner.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            owner.AddComponent<GraphicRaycaster>();

            RectTransform background = AddImage("Background", owner.transform, BackgroundColor).rectTransform;
            Stretch(background);
            return background;
        }

        public static Image AddImage(string name, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform));
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        // height > 0 gives the text a fixed height inside a layout group.
        public static Text AddText(string name, Transform parent, int fontSize, TextAnchor alignment, int height)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            if (height > 0)
            {
                textObject.AddComponent<LayoutElement>().preferredHeight = height;
            }

            return text;
        }

        public static Button AddButton(string name, Transform parent, Color color, string label, int width, int height, Action onClick)
        {
            Image image = AddImage(name, parent, color);
            Button button = image.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => onClick());

            LayoutElement layout = image.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;

            Text text = AddText("Label", image.transform, 20, TextAnchor.MiddleCenter, 0);
            text.text = label;
            Stretch(text.rectTransform, 10);
            return button;
        }

        // A child that lays out its own children in a row (horizontal) or column (vertical).
        public static Transform AddRow(string name, Transform parent, int height, bool vertical, bool flexible = false)
        {
            GameObject row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);

            LayoutElement layout = row.AddComponent<LayoutElement>();
            if (flexible)
            {
                layout.flexibleHeight = 1;
            }
            else
            {
                layout.preferredHeight = height;
            }

            HorizontalOrVerticalLayoutGroup group = vertical
                ? (HorizontalOrVerticalLayoutGroup)AddLayout<VerticalLayoutGroup>(row, 8, 0)
                : AddLayout<HorizontalLayoutGroup>(row, 12, 0);
            group.childForceExpandWidth = vertical;
            group.childForceExpandHeight = flexible;
            return row.transform;
        }

        public static T AddLayout<T>(GameObject target, int spacing, int padding) where T : HorizontalOrVerticalLayoutGroup
        {
            T group = target.AddComponent<T>();
            group.spacing = spacing;
            group.padding = new RectOffset(padding, padding, padding, padding);
            group.childControlWidth = true;
            group.childControlHeight = true;
            return group;
        }

        public static void Flexible(GameObject target, int flexibleWidth)
        {
            // Unity objects don't work with ??, so check for null explicitly.
            LayoutElement layout = target.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = target.AddComponent<LayoutElement>();
            }

            layout.flexibleWidth = flexibleWidth;
        }

        public static void Stretch(RectTransform rect, int inset = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        public static void Clear(Transform parent)
        {
            foreach (Transform child in parent)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
}
