using UnityEngine;

namespace Interactions
{
    public class Phone : MonoBehaviour, IInteractive
    {
        public GameObject phone;
        public AudioSource phoneAudioSource;
        private bool _pickedUp;
        private Transform _defaultPhoneTransform;

        private void Awake()
        {
            _defaultPhoneTransform = phone.transform;
        }

        public string InteractionText => _pickedUp ? "Put down" : "Pick up";
        
        public void Interact(PlayerController player)
        {
            var playerCamera = player.GetComponentInChildren<Camera>();
            
            _pickedUp = !_pickedUp;
            if (_pickedUp)
            {
                // Parent the phone to the camera
                phone.transform.SetParent(playerCamera.transform);
                phone.transform.localPosition = new Vector3(-0.35f, -0.02f, 0.54f);
                phone.transform.localEulerAngles = new Vector3(0, -38f, 0);
                
                phoneAudioSource.Play();
            }
            else
            {
                // Reset everything
                phone.transform.SetParent(transform);
                phone.transform.position = _defaultPhoneTransform.position;
                phone.transform.rotation = _defaultPhoneTransform.rotation;
                
                phoneAudioSource.Stop();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (phone is null) return;
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, phone.transform.position);
        }
    }
}
