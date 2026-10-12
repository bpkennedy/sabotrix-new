namespace Sabotrix.Core
{
    // One physical copy of a card in a match. Decks can hold duplicates (Jab x2),
    // so each copy gets its own InstanceId to select or withdraw a specific one.
    public sealed class CardInstance
    {
        public int InstanceId { get; }
        public CardDefinition Definition { get; }

        public CardInstance(int instanceId, CardDefinition definition)
        {
            InstanceId = instanceId;
            Definition = definition;
        }
    }
}
