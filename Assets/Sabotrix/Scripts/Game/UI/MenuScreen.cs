using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sabotrix.Game.UI
{
    public sealed class MenuButton
    {
        public string Label { get; }
        public Action OnClick { get; }

        public MenuButton(string label, Action onClick)
        {
            Label = label;
            OnClick = onClick;
        }
    }

    // One reusable screen: a big title, some text, and a column of buttons.
    // Used for the title, match setup, hot-seat handoff, and results screens.
    public sealed class MenuScreen : MonoBehaviour
    {
        private const int ButtonWidth = 640;
        private const int ButtonHeight = 80;

        private Text _title;
        private Text _body;
        private Transform _buttons;

        public static MenuScreen Create()
        {
            GameObject root = new GameObject();
            RectTransform background = UiBuilder.CreateCanvas("MenuScreen", root);
            MenuScreen screen = root.AddComponent<MenuScreen>();
            screen.Build(background);
            return screen;
        }

        public void Show(string title, string body, IReadOnlyList<MenuButton> buttons)
        {
            _title.text = title;
            _body.text = body;

            UiBuilder.Clear(_buttons);
            foreach (MenuButton button in buttons)
            {
                UiBuilder.AddButton(button.Label, _buttons, UiBuilder.ButtonColor, button.Label, ButtonWidth, ButtonHeight, button.OnClick);
            }

            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        private void Build(RectTransform background)
        {
            VerticalLayoutGroup column = UiBuilder.AddLayout<VerticalLayoutGroup>(background.gameObject, 24, 80);
            column.childAlignment = TextAnchor.MiddleCenter;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            _title = UiBuilder.AddText("Title", background, 72, TextAnchor.MiddleCenter, 120);
            _body = UiBuilder.AddText("Body", background, 28, TextAnchor.MiddleCenter, 420);

            _buttons = UiBuilder.AddRow("Buttons", background, 0, vertical: true, flexible: true);
            VerticalLayoutGroup buttonColumn = _buttons.GetComponent<VerticalLayoutGroup>();
            buttonColumn.childAlignment = TextAnchor.UpperCenter;
            buttonColumn.childForceExpandWidth = false;
            buttonColumn.childForceExpandHeight = false;
        }
    }
}
