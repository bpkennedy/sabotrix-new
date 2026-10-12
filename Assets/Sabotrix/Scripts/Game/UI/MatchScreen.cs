using System;
using System.Collections.Generic;
using Sabotrix.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Sabotrix.Game.UI
{
    // Gray-box match screen, built entirely from code with uGUI. It sits over the 3D bar.
    // Top: opponent. Middle: turn status (left) and resolve log (right), with the bar showing between them.
    // Bottom: the active player's hand and buttons.
    public sealed class MatchScreen : MonoBehaviour
    {
        private const int CardWidth = 200;
        private const int CardHeight = 260;
        private const int CardBackWidth = 60;
        private const int CardBackHeight = 84;
        private const float UnplayableAlpha = 0.35f;
        private const int StatusPanelWidth = 420;
        private const int LogPanelWidth = 520;

        // See-through so the bar shows behind the HUD.
        private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.55f);

        private static readonly Color AttackColor = new Color(0.7f, 0.25f, 0.25f);
        private static readonly Color SabotageColor = new Color(0.8f, 0.65f, 0.1f);
        private static readonly Color AbilityColor = new Color(0.85f, 0.55f, 0.15f);
        private static readonly Color BasicAttackColor = new Color(0.45f, 0.45f, 0.45f);
        private static readonly Color FaceDownColor = new Color(0.25f, 0.25f, 0.32f);

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
            GameObject root = new GameObject();
            RectTransform background = UiBuilder.CreateCanvas("MatchScreen", root);
            background.GetComponent<Image>().enabled = false;
            MatchScreen screen = root.AddComponent<MatchScreen>();
            screen.Build(background);
            return screen;
        }

        public void Show() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);

        // revealedCard: a blind pick that failed, shown face-up for a moment.
        public void Render(Match match, IReadOnlyList<string> log, CardInstance revealedCard)
        {
            PlayerState player = match.ActivePlayer;
            PlayerState opponent = match.Opponent;

            _opponentStats.text = MatchText.Stats(match, opponent);
            UiBuilder.Clear(_opponentHand);
            for (int i = 0; i < opponent.Hand.Count; i++)
            {
                AddCardBack(_opponentHand);
            }

            _status.text = StatusText(match);
            _log.text = string.Join("\n", log);
            _playerStats.text = MatchText.Stats(match, player);

            UiBuilder.Clear(_slots);
            AddCard(_slots, match, player.Ability, faceDown: false);
            AddCard(_slots, match, player.BasicAttack, faceDown: false);

            UiBuilder.Clear(_hand);
            foreach (CardInstance card in player.Hand)
            {
                AddCard(_hand, match, card, faceDown: player.IsBlind && card != revealedCard);
            }

            _endTurn.interactable = match.CanEndTurn();
        }

        private static string StatusText(Match match)
        {
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

        private void Build(RectTransform background)
        {
            VerticalLayoutGroup rows = UiBuilder.AddLayout<VerticalLayoutGroup>(background.gameObject, 16, 20);
            rows.childForceExpandHeight = false;

            // Opponent row
            Transform opponentRow = UiBuilder.AddRow("Opponent", background, 160, vertical: true);
            opponentRow.gameObject.AddComponent<Image>().color = OverlayColor;
            _opponentStats = UiBuilder.AddText("OpponentStats", opponentRow, 28, TextAnchor.MiddleLeft, 40);
            _opponentHand = UiBuilder.AddRow("OpponentHand", opponentRow, CardBackHeight, vertical: false);

            // Middle row: status on the left, log on the right, an empty gap in between to see the bar
            Transform middleRow = UiBuilder.AddRow("Middle", background, 0, vertical: false, flexible: true);
            Image statusPanel = UiBuilder.AddImage("StatusPanel", middleRow, OverlayColor);
            statusPanel.gameObject.AddComponent<LayoutElement>().preferredWidth = StatusPanelWidth;
            _status = UiBuilder.AddText("Status", statusPanel.transform, 28, TextAnchor.MiddleCenter, 0);
            UiBuilder.Stretch(_status.rectTransform, 16);
            GameObject gap = new GameObject("Gap", typeof(RectTransform));
            gap.transform.SetParent(middleRow, false);
            UiBuilder.Flexible(gap, 1);
            Image logPanel = UiBuilder.AddImage("LogPanel", middleRow, OverlayColor);
            logPanel.gameObject.AddComponent<LayoutElement>().preferredWidth = LogPanelWidth;
            _log = UiBuilder.AddText("Log", logPanel.transform, 22, TextAnchor.LowerLeft, 0);
            UiBuilder.Stretch(_log.rectTransform, 16);

            // Player row
            Transform playerRow = UiBuilder.AddRow("Player", background, 340, vertical: true);
            playerRow.gameObject.AddComponent<Image>().color = OverlayColor;
            _playerStats = UiBuilder.AddText("PlayerStats", playerRow, 28, TextAnchor.MiddleLeft, 40);
            Transform cardsRow = UiBuilder.AddRow("Cards", playerRow, CardHeight, vertical: false);
            _slots = UiBuilder.AddRow("Slots", cardsRow, CardHeight, vertical: false);
            _hand = UiBuilder.AddRow("Hand", cardsRow, CardHeight, vertical: false);
            UiBuilder.Flexible(_hand.gameObject, 1);
            _endTurn = UiBuilder.AddButton("EndTurn", cardsRow, UiBuilder.ButtonColor, "END TURN", 200, CardHeight, () => EndTurnClicked?.Invoke());
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

            Button button = UiBuilder.AddButton(card.Definition.Name, parent, color, label, CardWidth, CardHeight, () => CardClicked?.Invoke(card));
            if (selected)
            {
                Outline outline = button.gameObject.AddComponent<Outline>();
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

        private static void AddCardBack(Transform parent)
        {
            Image back = UiBuilder.AddImage("CardBack", parent, FaceDownColor);
            LayoutElement layout = back.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = CardBackWidth;
            layout.preferredHeight = CardBackHeight;
        }
    }
}
