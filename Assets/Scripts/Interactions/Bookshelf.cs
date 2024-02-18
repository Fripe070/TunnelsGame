using UnityEngine;

namespace Interactions
{
    public class Bookshelf : MonoBehaviour, IInteractive
    {
        public string text = "A bookshelf...";
    
        public string InteractionText => text;

        public void Interact(PlayerController player)
        {
        }
    }
}