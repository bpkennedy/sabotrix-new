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
    // Runs the hot-seat game loop: Title -> Setup -> per round (Handoff -> pick, Handoff -> pick, Resolve show) -> Results.
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

        [Tooltip("The ResolveShow on the Bar. Plays each turn back over the table.")]
        [SerializeField] private ResolveShow _resolveShow;

        [Tooltip("The BlackoutEffect on the Bar. Darkens the room when Blackout resolves.")]
        [SerializeField] private BlackoutEffect _blackout;

        // Setup picks per player, as indexes into _heroes and _decks.
        private readonly int[] _heroPicks = { 0, 1 };
        private readonly int[] _deckPicks = { 0, 1 };

        private readonly List<string> _log = new List<string>();
        private MenuScreen _menu;
        private MatchScreen _matchScreen;
        private Match _match;
        private HashSet<string> _cutawayCardIds;
        private CardInstance _revealedCard;

        private void Start()
        {
            _menu = MenuScreen.Create();
            _matchScreen = MatchScreen.Create();
            _matchScreen.CardClicked += OnCardClicked;
            _matchScreen.EndTurnClicked += OnEndTurnClicked;
            _cutawayCardIds = new HashSet<string>(_decks.SelectMany(deck => deck.Cards).Where(card => card.Cutaway).Select(card => card.name));

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

            _blackout.Restore();
            _log.Clear();
            string firstPlayer = MatchText.PlayerName(_match, _match.ActivePlayer);
            AddLog($"Match start. {firstPlayer} picks first each round.");
            ShowHandoff($"{firstPlayer} picks first each round (higher Initiative, or a coin flip on a tie).");
        }

        // Hides the hands between picks so the next player can take the seat without peeking.
        // Meanwhile the camera moves to the next player's seat behind the handoff screen.
        private void ShowHandoff(string body)
        {
            _matchScreen.Hide();
            _cameras.Show(CameraDirector.SeatName(_match.ActivePlayerIndex));
            _seatedPlayers.Show(_match.Players, _match.ActivePlayerIndex);
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

        private void ShowResults(string lastRound)
        {
            _matchScreen.Hide();
            string title = _match.Winner == null ? "DRAW!" : $"{MatchText.PlayerName(_match, _match.Winner).ToUpperInvariant()} WINS!";
            string finalHp = string.Join("\n", _match.Players.Select(player =>
                $"{MatchText.PlayerName(_match, player)}: {player.Hp}/{player.Hero.MaxHp} HP"));

            _menu.Show(title, $"{lastRound}\n\n{finalHp}\nRounds played: {_match.RoundNumber}", new[]
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

            string picker = MatchText.PlayerName(_match, _match.ActivePlayer);
            int round = _match.RoundNumber;
            int[] hpBefore = _match.Players.Select(player => player.Hp).ToArray();
            int[] blockBefore = _match.Players.Select(player => player.Block).ToArray();
            _match.EndTurn();

            // First lock-in of the round: the other player picks next. Nothing has resolved yet.
            if (!_match.IsOver && !_match.IsFirstPick)
            {
                ShowHandoff($"{picker} has locked in their cards.");
                return;
            }

            List<string> roundLines = _match.LastRoundEvents.Select(matchEvent => MatchText.Describe(_match, matchEvent)).ToList();
            AddLog($"--- Round {round} ---");
            roundLines.ForEach(AddLog);
            string lastRound = $"Round {round}:\n{string.Join("\n", roundLines)}";
            StartCoroutine(PlayRound(lastRound, hpBefore, blockBefore));
        }

        // Both players have picked: watch the round play out from overhead, then pass the seat (or show the winner).
        private IEnumerator PlayRound(string lastRound, int[] hpBefore, int[] blockBefore)
        {
            _matchScreen.Hide();
            _seatedPlayers.Show(_match.Players, SeatedPlayers.NoneHidden);
            _cameras.Show(CameraDirector.Overhead);

            // The dark round after a Blackout has now resolved, so the lights come back on.
            if (_blackout.IsDark)
            {
                _blackout.Restore();
            }

            yield return _resolveShow.Play(_match, hpBefore, blockBefore, _cutawayCardIds);

            if (_match.IsOver)
            {
                ShowResults(lastRound);
            }
            else
            {
                ShowHandoff(lastRound);
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
