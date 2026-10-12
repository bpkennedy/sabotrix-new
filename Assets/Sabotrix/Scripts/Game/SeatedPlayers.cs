using UnityEngine;

namespace Sabotrix.Game
{
    // The two players sitting at the table (Patron_P1, Patron_P2 on the Bar).
    // Your own patron is hidden in first person, since the seat camera is inside their head.
    // The other patron holds one face-down card per card in their hand.
    public sealed class SeatedPlayers : MonoBehaviour
    {
        private const int PlayerCount = 2;

        private readonly Transform[] _patrons = new Transform[PlayerCount];

        private void Awake()
        {
            for (int i = 0; i < PlayerCount; i++)
            {
                _patrons[i] = transform.Find($"Patron_P{i + 1}");
            }
        }

        public void Show(int activePlayerIndex, int opponentHandCount)
        {
            for (int i = 0; i < PlayerCount; i++)
            {
                _patrons[i].gameObject.SetActive(i != activePlayerIndex);

                Transform hand = _patrons[i].Find("Hand");
                for (int card = 0; card < hand.childCount; card++)
                {
                    hand.GetChild(card).gameObject.SetActive(card < opponentHandCount);
                }
            }
        }
    }
}
