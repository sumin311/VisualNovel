using System.Collections;
using UnityEngine;

namespace VN
{
    /// <summary>BGM: Assets/Resources/Audio/BGM/,  효과음: Assets/Resources/Audio/SE/</summary>
    public class VNAudio : MonoBehaviour
    {
        public AudioSource bgmSource;
        public AudioSource seSource;

        public string CurrentBgm { get; private set; }

        Coroutine fadeRoutine;

        public void PlayBgm(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (name == CurrentBgm && bgmSource.isPlaying) return;
            CurrentBgm = name;

            if (fadeRoutine != null) { StopCoroutine(fadeRoutine); fadeRoutine = null; }
            var clip = Resources.Load<AudioClip>("Audio/BGM/" + name);
            if (clip == null)
            {
                Debug.LogWarning($"[VN] BGM을 찾을 수 없습니다: Assets/Resources/Audio/BGM/{name}");
                bgmSource.Stop();
                return;
            }
            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.volume = VNSettings.BgmVolume;
            bgmSource.Play();
        }

        public void StopBgm(float fade)
        {
            CurrentBgm = null;
            if (fadeRoutine != null) { StopCoroutine(fadeRoutine); fadeRoutine = null; }
            if (fade <= 0f || !bgmSource.isPlaying) { bgmSource.Stop(); return; }
            fadeRoutine = StartCoroutine(FadeOut(fade));
        }

        IEnumerator FadeOut(float d)
        {
            float start = bgmSource.volume;
            for (float t = 0f; t < d; t += Time.deltaTime)
            {
                bgmSource.volume = Mathf.Lerp(start, 0f, t / d);
                yield return null;
            }
            bgmSource.Stop();
            bgmSource.volume = VNSettings.BgmVolume;
            fadeRoutine = null;
        }

        public void PlaySe(string name)
        {
            var clip = Resources.Load<AudioClip>("Audio/SE/" + name);
            if (clip == null)
            {
                Debug.LogWarning($"[VN] 효과음을 찾을 수 없습니다: Assets/Resources/Audio/SE/{name}");
                return;
            }
            seSource.PlayOneShot(clip);
        }

        public void ApplyVolume()
        {
            if (fadeRoutine == null) bgmSource.volume = VNSettings.BgmVolume;
            seSource.volume = VNSettings.SeVolume;
        }
    }
}
