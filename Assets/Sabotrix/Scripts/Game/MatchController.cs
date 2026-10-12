using System;
using System.Collections;
using System.Collections.Generic;
using Sabotrix.Core;
using Sabotrix.Game.Data;
using Sabotrix.Game.UI;
using UnityEngine;

namespace Sabotrix.Game
{
    // Runs one hot-seat match: builds it from the data assets, turns clicks into rule calls, and redraws the screen.
    public sealed class MatchController : MonoBehaviour
    {
        private const int MaxLogLines = 24;
        private const float BlindRevealSeconds = 1.5f;

        [Header("Player 1")]
        [SerializeField] private HeroData _heroOne;
        [SerializeField] private DeckData _deckOne;

        [Header("Player 2")]
        [SerializeField] private HeroData _heroTwo;
        [SerializeField] private DeckData _deckTwo;

        [Tooltip("0 = different shuffle every match. Any other number replays the same shuffle and backfire rolls.")]
        [SerializeField] private int _seed;

        private readonly List<string> _log = new List<string>();
        private Match _match;
        private MatchScreen _screen;
        private CardInstance _revealedCard;

        private void Start()
        {
            int seed = _seed != 0 ? _seed : Environment.TickCount;
            _match = Match.Create(
                _heroOne.ToDefinition(), _deckOne.ToDefinition(),
                _heroTwo.ToDefinition(), _deckTwo.ToDefinition(),
                new SeededRandom(seed));

            _screen = MatchScreen.Create();
            _screen.CardClicked += OnCardClicked;
            _screen.EndTurnClicked += OnEndTurnClicked;

            AddLog($"Match start. {MatchText.PlayerName(_match, _match.ActivePlayer)} goes first.");
            Render();
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

            AddLog($"--- Turn {turn} ---");
            foreach (MatchEvent matchEvent in _match.LastTurnEvents)
            {
                AddLog(MatchText.Describe(_match, matchEvent));
            }

            Render();
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

        private void Render() => _screen.Render(_match, _log, _revealedCard);
    }
}
