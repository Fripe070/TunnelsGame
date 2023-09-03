namespace Interactions
{
    public interface IInteractive
    {
        public string InteractionText { get; }

        public void Interact(PlayerController player);
    }
}