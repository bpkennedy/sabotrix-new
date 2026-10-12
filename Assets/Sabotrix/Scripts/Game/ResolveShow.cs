using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sabotrix.Core;
using Sabotrix.Game.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Sabotrix.Game
{
    // Plays a resolved round back as a short show over the table (GDD 5: "plan it, then watch it play out").
    // First both players' cards are revealed on the table together. Then each phase (Abilities, Attacks,
    // Sabotage) plays out for both players at once: numbers float up from each player and the HUD counts
    // HP and Block down. Click or Space skips to the end.
    // Core is untouched: the show only replays Match.LastRoundEvents, which already happened.
    public sealed class ResolveShow : MonoBehaviour
    {
        private const float RevealSeconds = 1f;
        private const float PhaseSeconds = 1.2f;

        // Matches the CinemachineBrain's default blend, so the first step starts once the camera is overhead.
        private const float CameraBlendSeconds = 0.6f;

        // World-space UI is sized in pixels, then scaled so 1 pixel = 1 millimeter.
        private const float PixelsToMeters = 0.001f;
        private const int CardWidth = 220;
        private const int CardHeight = 300;
        private const float CardSpacing = 0.25f;
        private const float CardRowOffset = 0.22f;
        private const float PopupOffset = 0.3f;
        private const float PopupRowSpacing = 0.12f;
        private const float PopupRise = 0.25f;

        private static readonly Color DamageColor = new Color(1f, 0.35f, 0.3f);
        private static readonly Color HealColor = new Color(0.4f, 1f, 0.5f);
        private static readonly Color BlockColor = new Color(0.5f, 0.8f, 1f);
        private static readonly Color SabotageColor = new Color(1f, 0.85f, 0.2f);
        private static readonly Color BackfireColor = new Color(1f, 0.5f, 0.1f);

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly Text[] _hud = new Text[2];
        private readonly int[] _cardsPlaced = new int[2];
        private Vector3 _tableTop;
        private GameObject _overlay;
        private Text _banner;
        private int[] _hp;
        private int[] _block;
        private bool _playing;
        private bool _skipped;

        private void Awake()
        {
            Transform top = transform.Find("Table/Top");
            _tableTop = top.position + Vector3.up * top.lossyScale.y / 2;
            BuildOverlay();
        }

        private void Update()
        {
            bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool space = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            if (_playing && (click || space))
            {
                _skipped = true;
            }
        }

        // hpBefore, blockBefore: each player's HP and Block before the round resolved, indexed like match.Players.
        public IEnumerator Play(Match match, int[] hpBefore, int[] blockBefore)
        {
            _hp = (int[])hpBefore.Clone();
            _block = (int[])blockBefore.Clone();
            _cardsPlaced[0] = 0;
            _cardsPlaced[1] = 0;
            _banner.text = "";
            _skipped = false;
            _playing = true;
            _overlay.SetActive(true);
            UpdateHud(match);
            yield return Wait(CameraBlendSeconds);

            foreach (MatchEvent played in match.LastRoundEvents.Where(e => e.Kind == MatchEventKind.CardPlayed))
            {
                PlaceCard(PlayerIndex(match, played.Player), played.Card);
            }

            ShowBanner("REVEAL!", Color.white);
            yield return Wait(RevealSeconds);

            foreach (List<MatchEvent> phase in SplitByPhase(match.LastRoundEvents))
            {
                ShowPhase(match, phase);
                UpdateHud(match);
                yield return Wait(PhaseSeconds);
            }

            foreach (GameObject spawned in _spawned)
            {
                Destroy(spawned);
            }

            _spawned.Clear();
            _overlay.SetActive(false);
            _playing = false;
        }

        // After a skip, every wait ends at once and the rest of the show finishes immediately.
        private IEnumerator Wait(float seconds)
        {
            for (float elapsed = 0; elapsed < seconds && !_skipped; elapsed += Time.deltaTime)
            {
                yield return null;
            }
        }

        // Groups the round's events by resolve phase. Each phase starts with its first played card.
        private static List<List<MatchEvent>> SplitByPhase(IReadOnlyList<MatchEvent> events)
        {
            List<List<MatchEvent>> phases = new List<List<MatchEvent>>();
            CardType? currentPhase = null;
            foreach (MatchEvent matchEvent in events)
            {
                if (matchEvent.Kind == MatchEventKind.CardPlayed && matchEvent.Card.Type != currentPhase)
                {
                    currentPhase = matchEvent.Card.Type;
                    phases.Add(new List<MatchEvent>());
                }

                phases[phases.Count - 1].Add(matchEvent);
            }

            return phases;
        }

        // Applies one phase for both players and shows each player's totals at once.
        private void ShowPhase(Match match, List<MatchEvent> phase)
        {
            int[] damage = new int[2];
            int[] blocked = new int[2];
            int[] healed = new int[2];
            int[] blockGained = new int[2];
            List<string> sabotageLines = new List<string>();
            bool backfired = false;

            foreach (MatchEvent matchEvent in phase)
            {
                int index = PlayerIndex(match, matchEvent.Player);
                switch (matchEvent.Kind)
                {
                    case MatchEventKind.Damage:
                        damage[index] += matchEvent.Amount;
                        blocked[index] += matchEvent.Blocked;
                        break;
                    case MatchEventKind.Heal:
                        healed[index] += matchEvent.Amount;
                        break;
                    case MatchEventKind.BlockGained:
                        blockGained[index] += matchEvent.Amount;
                        break;
                    case MatchEventKind.SabotageLanded:
                        sabotageLines.Add($"SABOTAGE! {matchEvent.Card.Name} lands!");
                        break;
                    case MatchEventKind.SabotageBackfired:
                        sabotageLines.Add($"BACKFIRE! {matchEvent.Card.Name} hits {MatchText.PlayerName(match, matchEvent.Player)}!");
                        backfired = true;
                        break;
                }
            }

            string title = PhaseTitle(phase[0].Card.Type);
            Color titleColor = sabotageLines.Count == 0 ? Color.white : backfired ? BackfireColor : SabotageColor;
            ShowBanner(string.Join("\n", new[] { title }.Concat(sabotageLines)), titleColor);

            for (int index = 0; index < 2; index++)
            {
                _hp[index] += healed[index] - damage[index];
                _block[index] += blockGained[index] - blocked[index];

                int row = 0;
                if (damage[index] > 0 || blocked[index] > 0)
                {
                    string blockedText = blocked[index] > 0 ? $" ({blocked[index]} blocked)" : "";
                    Popup(index, row++, $"-{damage[index]}{blockedText}", DamageColor);
                }

                if (healed[index] > 0)
                {
                    Popup(index, row++, $"+{healed[index]}", HealColor);
                }

                if (blockGained[index] > 0)
                {
                    Popup(index, row, $"+{blockGained[index]} Block", BlockColor);
                }
            }
        }

        private static string PhaseTitle(CardType phase)
        {
            switch (phase)
            {
                case CardType.Ability:
                    return "ABILITIES";
                case CardType.Attack:
                    return "ATTACKS";
                default:
                    return "SABOTAGE";
            }
        }

        private void ShowBanner(string text, Color color)
        {
            _banner.text = text;
            _banner.color = color;
        }

        // Cards line up on the table in front of the player who played them.
        private void PlaceCard(int index, CardDefinition card)
        {
            float x = -1.5f * CardSpacing + _cardsPlaced[index] * CardSpacing;
            _cardsPlaced[index]++;
            Vector3 position = _tableTop + new Vector3(x, 0.002f, Side(index) * CardRowOffset);
            Image panel = CreateTablePanel(card.Name, position, new Vector2(CardWidth, CardHeight), MatchScreen.CardColor(card));
            Text label = UiBuilder.AddText("Label", panel.transform, 26, TextAnchor.MiddleCenter, 0);
            UiBuilder.Stretch(label.rectTransform, 10);
            label.text = MatchText.CardFace(card, card.Cost);
        }

        // A number on the player's side of the table that rises toward the overhead camera and fades out.
        // Each extra row for the same player sits a bit closer to the middle of the table.
        private void Popup(int index, int row, string text, Color color)
        {
            Vector3 position = _tableTop + new Vector3(0, 0.1f, Side(index) * (PopupOffset - row * PopupRowSpacing));
            Image panel = CreateTablePanel("Popup", position, new Vector2(600, 160), Color.clear);
            Text label = UiBuilder.AddText("Label", panel.transform, 96, TextAnchor.MiddleCenter, 0);
            UiBuilder.Stretch(label.rectTransform);
            label.text = text;
            label.color = color;
            label.fontStyle = FontStyle.Bold;
            label.gameObject.AddComponent<Outline>().effectColor = Color.black;
            StartCoroutine(Rise(panel.transform, label));
        }

        private IEnumerator Rise(Transform popup, Text label)
        {
            Vector3 start = popup.position;
            Color color = label.color;
            for (float elapsed = 0; elapsed < PhaseSeconds && popup != null; elapsed += Time.deltaTime)
            {
                float progress = elapsed / PhaseSeconds;
                popup.position = start + Vector3.up * PopupRise * progress;
                color.a = 1f - progress * progress;
                label.color = color;
                yield return null;
            }
        }

        // A world-space uGUI panel lying flat on the table, readable from the overhead camera.
        private Image CreateTablePanel(string name, Vector3 position, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            RectTransform rect = (RectTransform)panel.transform;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one * PixelsToMeters;
            rect.position = position;
            rect.rotation = Quaternion.Euler(90, 0, 0);

            Image image = panel.AddComponent<Image>();
            image.color = color;
            _spawned.Add(panel);
            return image;
        }

        // Overhead, Player 2 is at the top of the screen and Player 1 at the bottom, so the HUD matches.
        private void BuildOverlay()
        {
            _overlay = new GameObject();
            RectTransform background = UiBuilder.CreateCanvas("ResolveOverlay", _overlay);
            background.GetComponent<Image>().enabled = false;
            VerticalLayoutGroup rows = UiBuilder.AddLayout<VerticalLayoutGroup>(background.gameObject, 16, 20);
            rows.childForceExpandHeight = false;

            _hud[1] = AddHudRow("Player2", background);
            Transform middle = UiBuilder.AddRow("Middle", background, 0, vertical: true, flexible: true);
            _banner = UiBuilder.AddText("Banner", middle, 48, TextAnchor.MiddleCenter, 180);
            _banner.gameObject.AddComponent<Outline>().effectColor = Color.black;
            _hud[0] = AddHudRow("Player1", background);
            UiBuilder.AddText("SkipHint", background, 20, TextAnchor.MiddleRight, 28).text = "Click or press Space to skip";

            _overlay.SetActive(false);
        }

        private static Text AddHudRow(string name, Transform parent)
        {
            Transform row = UiBuilder.AddRow(name, parent, 56, vertical: true);
            row.gameObject.AddComponent<Image>().color = UiBuilder.OverlayColor;
            return UiBuilder.AddText("Stats", row, 32, TextAnchor.MiddleCenter, 56);
        }

        private void UpdateHud(Match match)
        {
            for (int i = 0; i < _hud.Length; i++)
            {
                PlayerState player = match.Players[i];
                _hud[i].text = $"{MatchText.PlayerName(match, player)}   HP {_hp[i]}/{player.Hero.MaxHp}   Block {_block[i]}";
            }
        }

        private static int PlayerIndex(Match match, PlayerState player) => match.Players[0] == player ? 0 : 1;

        // Player 1 sits on the -Z side of the table, Player 2 on the +Z side.
        private static float Side(int index) => index == 0 ? -1f : 1f;
    }
}
