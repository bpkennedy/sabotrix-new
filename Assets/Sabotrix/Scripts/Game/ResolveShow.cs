using System.Collections;
using System.Collections.Generic;
using Sabotrix.Core;
using Sabotrix.Game.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Sabotrix.Game
{
    // Plays a finished turn back as a short show over the table (GDD 5: "plan it, then watch it play out").
    // Walks Match.LastTurnEvents one step at a time: played cards land on the table, numbers float up from
    // the player they hit, and the HUD counts HP and Block down. Click or Space skips to the end.
    // Core is untouched: the show only replays events that already happened.
    public sealed class ResolveShow : MonoBehaviour
    {
        private const float StepSeconds = 0.8f;

        // Matches the CinemachineBrain's default blend, so the first step starts once the camera is overhead.
        private const float CameraBlendSeconds = 0.6f;

        // World-space UI is sized in pixels, then scaled so 1 pixel = 1 millimeter.
        private const float PixelsToMeters = 0.001f;
        private const int CardWidth = 220;
        private const int CardHeight = 300;
        private const float CardSpacing = 0.25f;
        private const float CardRowOffset = 0.22f;
        private const float PopupOffset = 0.3f;
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

        // hpBefore, blockBefore: each player's HP and Block before the turn resolved, indexed like match.Players.
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
            foreach (MatchEvent matchEvent in match.LastTurnEvents)
            {
                Show(match, matchEvent);
                UpdateHud(match);
                yield return Wait(StepSeconds);
            }

            yield return Wait(StepSeconds);

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

        private void Show(Match match, MatchEvent matchEvent)
        {
            int index = PlayerIndex(match, matchEvent.Player);
            _banner.text = MatchText.Describe(match, matchEvent).Trim();
            _banner.color = Color.white;

            switch (matchEvent.Kind)
            {
                case MatchEventKind.CardPlayed:
                    PlaceCard(index, matchEvent.Card);
                    break;
                case MatchEventKind.Damage:
                    _hp[index] -= matchEvent.Amount;
                    _block[index] -= matchEvent.Blocked;
                    string blocked = matchEvent.Blocked > 0 ? $" ({matchEvent.Blocked} blocked)" : "";
                    Popup(index, $"-{matchEvent.Amount}{blocked}", DamageColor);
                    break;
                case MatchEventKind.Heal:
                    _hp[index] += matchEvent.Amount;
                    Popup(index, $"+{matchEvent.Amount}", HealColor);
                    break;
                case MatchEventKind.BlockGained:
                    _block[index] += matchEvent.Amount;
                    Popup(index, $"+{matchEvent.Amount} Block", BlockColor);
                    break;
                case MatchEventKind.SabotageLanded:
                    _banner.text = $"SABOTAGE! {matchEvent.Card.Name} lands!";
                    _banner.color = SabotageColor;
                    break;
                case MatchEventKind.SabotageBackfired:
                    _banner.text = $"BACKFIRE! {matchEvent.Card.Name} hits {MatchText.PlayerName(match, matchEvent.Player)}!";
                    _banner.color = BackfireColor;
                    break;
            }
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

        // A number at the player's edge of the table that rises toward the overhead camera and fades out.
        private void Popup(int index, string text, Color color)
        {
            Vector3 position = _tableTop + new Vector3(0, 0.1f, Side(index) * PopupOffset);
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
            for (float elapsed = 0; elapsed < StepSeconds && popup != null; elapsed += Time.deltaTime)
            {
                float progress = elapsed / StepSeconds;
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
            _banner = UiBuilder.AddText("Banner", middle, 48, TextAnchor.MiddleCenter, 80);
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
