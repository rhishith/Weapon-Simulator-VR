using UnityEngine;

namespace VRWeaponSimulator
{
    /// <summary>
    /// Generates and manages procedural mechanical sound effects for weapon interactions.
    /// Used automatically when custom audio clips are not assigned in the Inspector.
    /// </summary>
    public static class WeaponSoundManager
    {
        private static AudioClip _snapClip;
        private static AudioClip _rackBackClip;
        private static AudioClip _rackForwardClip;
        private static AudioClip _magEjectClip;
        private static AudioClip _grabClip;

        public static AudioClip SnapClip => _snapClip ??= GenerateSnapSound();
        public static AudioClip RackBackClip => _rackBackClip ??= GenerateRackBackSound();
        public static AudioClip RackForwardClip => _rackForwardClip ??= GenerateRackForwardSound();
        public static AudioClip MagEjectClip => _magEjectClip ??= GenerateMagEjectSound();
        public static AudioClip GrabClip => _grabClip ??= GenerateGrabSound();

        public static void PlayAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip != null)
                AudioSource.PlayClipAtPoint(clip, position, volume);
        }

        private static AudioClip GenerateSnapSound()
        {
            int sampleRate = 44100;
            float duration = 0.08f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 60f);
                float noise = (Random.value * 2f - 1f) * 0.4f;
                float tone1 = Mathf.Sin(2f * Mathf.PI * 1800f * t) * 0.6f;
                float tone2 = Mathf.Sin(2f * Mathf.PI * 850f * t) * 0.4f;
                data[i] = (noise + tone1 + tone2) * decay;
            }

            AudioClip clip = AudioClip.Create("Procedural_Snap", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip GenerateRackBackSound()
        {
            int sampleRate = 44100;
            float duration = 0.12f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 30f);
                float friction = (Random.value * 2f - 1f) * 0.35f * Mathf.Sin(t * 120f);
                float click = (i > samples * 0.7f) ? Mathf.Sin(2f * Mathf.PI * 2200f * t) * 0.7f : 0f;
                data[i] = (friction + click) * decay;
            }

            AudioClip clip = AudioClip.Create("Procedural_RackBack", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip GenerateRackForwardSound()
        {
            int sampleRate = 44100;
            float duration = 0.10f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 45f);
                float impact = Mathf.Sin(2f * Mathf.PI * 650f * t) * 0.7f + Mathf.Sin(2f * Mathf.PI * 1400f * t) * 0.5f;
                float noise = (Random.value * 2f - 1f) * 0.3f;
                data[i] = (impact + noise) * decay;
            }

            AudioClip clip = AudioClip.Create("Procedural_RackForward", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip GenerateMagEjectSound()
        {
            int sampleRate = 44100;
            float duration = 0.09f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 50f);
                float springPop = Mathf.Sin(2f * Mathf.PI * 420f * t) * 0.8f + Mathf.Sin(2f * Mathf.PI * 1900f * t) * 0.3f;
                data[i] = springPop * decay;
            }

            AudioClip clip = AudioClip.Create("Procedural_MagEject", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip GenerateGrabSound()
        {
            int sampleRate = 44100;
            float duration = 0.04f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 90f);
                float click = (Random.value * 2f - 1f) * 0.4f + Mathf.Sin(2f * Mathf.PI * 1200f * t) * 0.4f;
                data[i] = click * decay;
            }

            AudioClip clip = AudioClip.Create("Procedural_Grab", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
