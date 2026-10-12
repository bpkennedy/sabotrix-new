using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sabotrix.Core;
using Sabotrix.Game.Data;
using Sabotrix.Game.UI;
using UnityEngine;

namespace Sabotrix.Game
{
    // Runs the hot-seat game loop: Title -> Setup -> (Handoff -> Match) per turn -> Results.
    public sealed class GameController : MonoBehaviour
    {
        private const int MaxLogLines = 24;
        private const float BlindRevealSeconds = 1.5f;
        private const int PlayerCount = 2;

        [Tooltip("Heroes players can pick on the setup screen.")]
        [SerializeField] private HeroData[] _heroes;

        [Tooltip("Prebuilt decks players can pick on the setup screen.")]
        [SerializeField] private DeckData[] _decks;

        [Tooltip("0 = different shuffle every match. Any other number replays the same shuffle and backfire rolls.")]
        [SerializeField] private int _seed;

        [Tooltip("The CameraDirector on the Bar. Picks the seat camera for each turn.")]
        [SerializeField] private CameraDirector _cameras;

        [Tooltip("The SeatedPlayers on the Bar. Shows the opponent holding their cards.")]
        [SerializeField] private SeatedPlayers _seatedPlayers;

        // Setup picks per player, as indexes into _heroes and _decks.
        private readonly int[] _heroPicks = { 0, 1 };
        private readonly int[] _deckPicks = { 0, 1 };

        private readonly List<string> _log = new List<string>();
        private MenuScreen _menu;
        private MatchScreen _matchScreen;
        private Match _match;
        private CardInstance _revealedCard;

        private void Start()
        {
            _menu = MenuScreen.Create();
            _matchScreen = MatchScreen.Create();
            _matchScreen.CardClicked += OnCardClicked;
            _matchScreen.EndTurnClicked += OnEndTurnClicked;

            ShowTitle();
        }

        private void ShowTitle()
        {
            _matchScreen.Hide();
            _menu.Show("SABOTRIX", "A card battle at the bar, where sabotage messes with the room.", new[]
            {
                new MenuButton("PLAY", ShowSetup),
                new MenuButton("QUIT", Application.Quit)
            });
        }

        private void ShowSetup()
        {
            List<MenuButton> buttons = new List<MenuButton>();
            for (int player = 0; player < PlayerCount; player++)
            {
                int index = player;
                buttons.Add(new MenuButton($"P{index + 1} Hero: {_heroes[_heroPicks[index]].DisplayName}  >", () => CycleHero(index)));
                buttons.Add(new MenuButton($"P{index + 1} Deck: {_decks[_deckPicks[index]].DisplayName}  >", () => CycleDeck(index)));
            }

            buttons.Add(new MenuButton("START MATCH", StartMatch));
            buttons.Add(new MenuButton("BACK", ShowTitle));

            _menu.Show("MATCH SETUP", "Click a hero or deck to change it.", buttons);
        }

        private void CycleHero(int player)
        {
            _heroPicks[player] = (_heroPicks[player] + 1) % _heroes.Length;
            ShowSetup();
        }

        private void CycleDeck(int player)
        {
            _deckPicks[player] = (_deckPicks[player] + 1) % _decks.Length;
            ShowSetup();
        }

        private void StartMatch()
        {
            int seed = _seed != 0 ? _seed : Environment.TickCount;
            _match = Match.Create(
                _heroes[_heroPicks[0]].ToDefinition(), _decks[_deckPicks[0]].ToDefinition(),
                _heroes[_heroPicks[1]].ToDefinition(), _decks[_deckPicks[1]].ToDefinition(),
                new SeededRandom(seed));

            _log.Clear();
            string firstPlayer = MatchText.PlayerName(_match, _match.ActivePlayer);
            AddLog($"Match start. {firstPlayer} goes first.");
            ShowHandoff($"{firstPlayer} goes first (higher Initiative, or a coin flip on a tie).");
        }

        // Hides the hands between turns so the next player can take the seat without peeking.
        // Meanwhile the camera moves to the next player's seat behind the handoff screen.
        private void ShowHandoff(string body)
        {
            _matchScreen.Hide();
            _cameras.Show(CameraDirector.SeatName(_match.ActivePlayerIndex));
            _seatedPlayers.Show(_match.ActivePlayerIndex, _match.Opponent.Hand.Count);
            _menu.Show($"PASS TO {MatchText.PlayerName(_match, _match.ActivePlayer).ToUpperInvariant()}", body, new[]
            {
                new MenuButton("READY", ShowMatch)
            });
        }

        private void ShowMatch()
        {
            _menu.Hide();
            _matchScreen.Show();
            Render();
        }

        private void ShowResults(string lastTurn)
        {
            _matchScreen.Hide();
            string title = _match.Winner == null ? "DRAW!" : $"{MatchText.PlayerName(_match, _match.Winner).ToUpperInvariant()} WINS!";
            string finalHp = string.Join("\n", _match.Players.Select(player =>
                $"{MatchText.PlayerName(_match, player)}: {player.Hp}/{player.Hero.MaxHp} HP"));

            _menu.Show(title, $"{lastTurn}\n\n{finalHp}\nTurns played: {_match.TurnNumber}", new[]
            {
                new MenuButton("REMATCH", StartMatch),
                new MenuButton("MAIN MENU", ShowTitle)
            });
        }

        private void OnCardClicked(CardInstance card)
        {
            if (_match.ActivePlayer.IsSelected(card))
            {
                _match.TryWithdraw(card);
            }
            else if (!_match.TrySelect(card) && _match.ActivePlayer.IsBlind)
            {
                // Blind pick that isn't allowed: flash the card face-up, then hide it again.
                StartCoroutine(RevealBriefly(card));
                return;
            }

            Render();
        }

        private void OnEndTurnClicked()
        {
            if (!_match.CanEndTurn())
            {
                return;
            }

            int turn = _match.TurnNumber;
            _match.EndTurn();

            List<string> turnLines = _match.LastTurnEvents.Select(matchEvent => MatchText.Describe(_match, matchEvent)).ToList();
            AddLog($"--- Turn {turn} ---");
            turnLines.ForEach(AddLog);
            string lastTurn = $"Turn {turn}:\n{string.Join("\n", turnLines)}";

            if (_match.IsOver)
            {
                ShowResults(lastTurn);
            }
            else
            {
                ShowHandoff(lastTurn);
            }
        }

        private IEnumerator RevealBriefly(CardInstance card)
        {
            _revealedCard = card;
            Render();
            yield return new WaitForSeconds(BlindRevealSeconds);
            _revealedCard = null;
            Render();
        }

        private void AddLog(string line)
        {
            _log.Add(line);
            if (_log.Count > MaxLogLines)
            {
                _log.RemoveAt(0);
            }
        }

        private void Render() => _matchScreen.Render(_match, _log, _revealedCard);
    }
}
