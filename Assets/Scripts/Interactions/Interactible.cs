namespace Interactions
{
    public interface IInteractible
    {
        public string InteractionText { get; }
        
        public void Interact(PlayerController player);
    }
}