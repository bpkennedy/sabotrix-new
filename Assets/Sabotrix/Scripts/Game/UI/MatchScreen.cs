using System;
using System.Collections.Generic;
using Sabotrix.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Sabotrix.Game.UI
{
    // Gray-box match screen, built entirely from code with uGUI.
    // Top: opponent. Middle: turn status and resolve log. Bottom: the active player's hand and buttons.
    public sealed class MatchScreen : MonoBehaviour
    {
        private const int CardWidth = 200;
        private const int CardHeight = 260;
        private const int CardBackWidth = 60;
        private const int CardBackHeight = 84;
        private const float UnplayableAlpha = 0.35f;

        private static readonly Color BackgroundColor = new Color(0.12f, 0.1f, 0.1f);
        private static readonly Color PanelColor = new Color(0.18f, 0.16f, 0.16f);
        private static readonly Color AttackColor = new Color(0.7f, 0.25f, 0.25f);
        private static readonly Color SabotageColor = new Color(0.8f, 0.65f, 0.1f);
        private static readonly Color AbilityColor = new Color(0.85f, 0.55f, 0.15f);
        private static readonly Color BasicAttackColor = new Color(0.45f, 0.45f, 0.45f);
        private static readonly Color FaceDownColor = new Color(0.25f, 0.25f, 0.32f);
        private static readonly Color EndTurnColor = new Color(0.2f, 0.5f, 0.3f);

        private Font _font;
        private Text _opponentStats;
        private Transform _opponentHand;
        private Text _status;
        private Text _log;
        private Text _playerStats;
        private Transform _slots;
        private Transform _hand;
        private Button _endTurn;

        public event Action<CardInstance> CardClicked;
        public event Action EndTurnClicked;

        public static MatchScreen Create()
        {
            EnsureEventSystem();

            GameObject root = new GameObject("MatchScreen", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            MatchScreen screen = root.AddComponent<MatchScreen>();
            screen.Build();
            return screen;
        }

        // revealedCard: a blind pick that failed, shown face-up for a moment.
        public void Render(Match match, IReadOnlyList<string> log, CardInstance revealedCard)
        {
            PlayerState player = match.ActivePlayer;
            PlayerState opponent = match.Opponent;

            _opponentStats.text = MatchText.Stats(match, opponent);
            Clear(_opponentHand);
            for (int i = 0; i < opponent.Hand.Count; i++)
            {
                AddCardBack(_opponentHand);
            }

            _status.text = StatusText(match);
            _log.text = string.Join("\n", log);
            _playerStats.text = MatchText.Stats(match, player);

            Clear(_slots);
            AddCard(_slots, match, player.Ability, faceDown: false);
            AddCard(_slots, match, player.BasicAttack, faceDown: false);

            Clear(_hand);
            foreach (CardInstance card in player.Hand)
            {
                AddCard(_hand, match, card, faceDown: player.IsBlind && card != revealedCard);
            }

            _endTurn.interactable = match.CanEndTurn();
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static string StatusText(Match match)
        {
            if (match.IsOver)
            {
                return match.Winner == null ? "DRAW!" : $"{MatchText.PlayerName(match, match.Winner)} WINS!";
            }

            PlayerState player = match.ActivePlayer;
            List<string> lines = new List<string>
            {
                $"Turn {match.TurnNumber}/{Rules.TurnLimit}",
                $"{MatchText.PlayerName(match, player)}'s turn"
            };

            if (player.IsBlind)
            {
                lines.Add("BLACKOUT! You can't see your cards.");
            }

            if (player.AttackCostIncrease > 0)
            {
                lines.Add($"SPILLED DRINK! Your Attacks cost +{player.AttackCostIncrease}.");
            }

            lines.Add("Click cards to select or withdraw. Pick at least one Attack.");
            return string.Join("\n", lines);
        }

        private void Build()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            RectTransform background = AddImage("Background", transform, BackgroundColor).rectTransform;
            Stretch(background);
            VerticalLayoutGroup rows = AddLayout<VerticalLayoutGroup>(background.gameObject, 16, 20);
            rows.childForceExpandHeight = false;

            // Opponent row
            Transform opponentRow = AddRow("Opponent", background, 160, vertical: true);
            _opponentStats = AddText("OpponentStats", opponentRow, 28, TextAnchor.MiddleLeft, 40);
            _opponentHand = AddRow("OpponentHand", opponentRow, CardBackHeight, vertical: false);

            // Middle row: status on the left, log on the right
            Transform middleRow = AddRow("Middle", background, 0, vertical: false, flexible: true);
            Image statusPanel = AddImage("StatusPanel", middleRow, PanelColor);
            Flexible(statusPanel.gameObject, 1);
            _status = AddText("Status", statusPanel.transform, 32, TextAnchor.MiddleCenter, 0);
            Stretch(_status.rectTransform, 16);
            Image logPanel = AddImage("LogPanel", middleRow, PanelColor);
            Flexible(logPanel.gameObject, 1);
            _log = AddText("Log", logPanel.transform, 22, TextAnchor.LowerLeft, 0);
            Stretch(_log.rectTransform, 16);

            // Player row
            Transform playerRow = AddRow("Player", background, 340, vertical: true);
            _playerStats = AddText("PlayerStats", playerRow, 28, TextAnchor.MiddleLeft, 40);
            Transform cardsRow = AddRow("Cards", playerRow, CardHeight, vertical: false);
            _slots = AddRow("Slots", cardsRow, CardHeight, vertical: false);
            _hand = AddRow("Hand", cardsRow, CardHeight, vertical: false);
            Flexible(_hand.gameObject, 1);
            _endTurn = AddButton("EndTurn", cardsRow, EndTurnColor, "END TURN", 200, CardHeight, () => EndTurnClicked?.Invoke()).GetComponent<Button>();
        }

        private void AddCard(Transform parent, Match match, CardInstance card, bool faceDown)
        {
            PlayerState player = match.ActivePlayer;
            bool selected = player.IsSelected(card);
            bool playable = selected || match.CanSelect(card);

            Color color = faceDown ? FaceDownColor : CardColor(player, card);
            if (!playable && !faceDown)
            {
                color.a = UnplayableAlpha;
            }

            string label = faceDown ? "?" : MatchText.CardFace(card.Definition, card == player.Ability ? card.Definition.Cost : match.CostOf(card));
            if (selected)
            {
                label = "[ SELECTED ]\n\n" + label;
            }

            GameObject button = AddButton(card.Definition.Name, parent, color, label, CardWidth, CardHeight, () => CardClicked?.Invoke(card));
            if (selected)
            {
                Outline outline = button.AddComponent<Outline>();
                outline.effectColor = Color.white;
                outline.effectDistance = new Vector2(6, 6);
            }
        }

        private static Color CardColor(PlayerState player, CardInstance card)
        {
            if (card == player.BasicAttack)
            {
                return BasicAttackColor;
            }

            switch (card.Definition.Type)
            {
                case CardType.Ability:
                    return AbilityColor;
                case CardType.Sabotage:
                    return SabotageColor;
                default:
                    return AttackColor;
            }
        }

        private void AddCardBack(Transform parent)
        {
            Image back = AddImage("CardBack", parent, FaceDownColor);
            LayoutElement layout = back.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = CardBackWidth;
            layout.preferredHeight = CardBackHeight;
        }

        private GameObject AddButton(string name, Transform parent, Color color, string label, int width, int height, Action onClick)
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
            return image.gameObject;
        }

        private Transform AddRow(string name, Transform parent, int height, bool vertical, bool flexible = false)
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

        private Text AddText(string name, Transform parent, int fontSize, TextAnchor alignment, int height)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.AddComponent<Text>();
            text.font = _font;
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

        private static Image AddImage(string name, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform));
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static T AddLayout<T>(GameObject target, int spacing, int padding) where T : HorizontalOrVerticalLayoutGroup
        {
            T group = target.AddComponent<T>();
            group.spacing = spacing;
            group.padding = new RectOffset(padding, padding, padding, padding);
            group.childControlWidth = true;
            group.childControlHeight = true;
            return group;
        }

        private static void Flexible(GameObject target, int flexibleWidth)
        {
            // Unity objects don't work with ??, so check for null explicitly.
            LayoutElement layout = target.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = target.AddComponent<LayoutElement>();
            }

            layout.flexibleWidth = flexibleWidth;
        }

        private static void Stretch(RectTransform rect, int inset = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void Clear(Transform parent)
        {
            foreach (Transform child in parent)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
