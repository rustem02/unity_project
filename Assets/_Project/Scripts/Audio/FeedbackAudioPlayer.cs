using UnityEngine;
using VRTraining.Core.Events;

namespace VRTraining.Audio
{
    /// <summary>
    /// Plays success / failure / sequence cues from FeedbackRequestEvent.
    /// Clips can be assigned; otherwise procedural tones are generated at runtime.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class FeedbackAudioPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip successClip;
        [SerializeField] private AudioClip failureClip;
        [SerializeField] private AudioClip sequenceViolationClip;
        [SerializeField] private AudioClip groupStartClip;

        private AudioClip _procSuccess;
        private AudioClip _procFailure;
        private AudioClip _procSequence;
        private AudioClip _procGroup;

        private void Awake()
        {
            if (source == null)
                source = GetComponent<AudioSource>();

            source.playOnAwake = false;
            source.spatialBlend = 0f;

            _procSuccess = ToneUtility.CreateTone("proc_success", 880f, 0.18f);
            _procFailure = ToneUtility.CreateTone("proc_fail", 220f, 0.28f, 0.45f);
            _procSequence = ToneUtility.CreateTone("proc_seq", 160f, 0.4f, 0.55f);
            _procGroup = ToneUtility.CreateTone("proc_group", 523f, 0.15f);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<FeedbackRequestEvent>(OnFeedback);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<FeedbackRequestEvent>(OnFeedback);
        }

        private void OnFeedback(FeedbackRequestEvent evt)
        {
            switch (evt.Kind)
            {
                case FeedbackKind.Success:
                    source.PlayOneShot(successClip != null ? successClip : _procSuccess);
                    break;
                case FeedbackKind.Failure:
                    source.PlayOneShot(failureClip != null ? failureClip : _procFailure);
                    break;
                case FeedbackKind.SequenceViolation:
                    source.PlayOneShot(sequenceViolationClip != null ? sequenceViolationClip : _procSequence);
                    break;
                case FeedbackKind.GroupStart:
                    source.PlayOneShot(groupStartClip != null ? groupStartClip : _procGroup);
                    break;
            }
        }
    }

    internal static class ToneUtility
    {
        public static AudioClip CreateTone(string name, float frequency, float duration, float volume = 0.35f)
        {
            const int sampleRate = 44100;
            var sampleCount = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = 1f - (t / duration);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * volume * envelope;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
