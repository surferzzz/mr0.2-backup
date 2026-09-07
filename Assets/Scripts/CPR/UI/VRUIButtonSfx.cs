using UnityEngine;
using UnityEngine.UI;

namespace CPRSimulation
{
    [DisallowMultipleComponent]
    public sealed class VRUIButtonSfx : MonoBehaviour
    {
        [SerializeField] private float volume = 0.32f;

        private Button _button;
        private AudioClip _clip;
        private AudioSource _source;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _source = GetComponentInParent<AudioSource>();
            _clip = VRTrainingUIStyle.LoadClickClip();
        }

        private void OnEnable()
        {
            EnsureRegistered();
        }

        public void EnsureRegistered()
        {
            if (_button == null)
                _button = GetComponent<Button>();
            if (_source == null)
                _source = GetComponentInParent<AudioSource>();
            if (_clip == null)
                _clip = VRTrainingUIStyle.LoadClickClip();

            if (_button != null)
            {
                _button.onClick.RemoveListener(Play);
                _button.onClick.AddListener(Play);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(Play);
        }

        private void Play()
        {
            if (_clip == null)
                _clip = VRTrainingUIStyle.LoadClickClip();
            if (_clip == null)
                return;

            if (_source == null)
                _source = GetComponentInParent<AudioSource>();

            if (_source != null)
                _source.PlayOneShot(_clip, volume);
            else
                AudioSource.PlayClipAtPoint(_clip, transform.position, volume);
        }
    }
}
