using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    public class AudioManager : MonoBehaviour
    {
        public Sound[] twoDSounds;
        private static AudioManager _instance;
        public AudioMixer mainAudioMixer;

        public static AudioManager Instance => _instance;

        private void Awake () {

            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        
            foreach (Sound s in twoDSounds)
            {
                s.source = gameObject.AddComponent<AudioSource>();
                s.source.clip = s.clip;
                s.source.playOnAwake = false;
                s.source.volume = s.volume;
                s.source.pitch = s.pitch;

                if (s.audioMixerGroup)
                {
                    s.source.outputAudioMixerGroup = s.audioMixerGroup;
                }

                s.source.loop = s.loop;
            }
        }

        public void Play(string name, bool oneShot = false) {
            var s = Array.Find(twoDSounds, sound => sound.name == name);
            if (s == null) {
                Debug.LogWarning("Sound: " + name + "not found!");
                return;
            }
            if (!oneShot)
                s.source.Play();
            else 
                s.source.PlayOneShot(s.source.clip);
        }

        public void Stop(string name)
        {
            var s = Array.Find(twoDSounds, sound => sound.name == name);
            if (s == null) {
                Debug.LogWarning("Sound: " + name + "not found!");
                return;
            }
            s.source.Stop();
        }

        public AudioSource GetAudioSource(string name)
        {
            return Array.Find(twoDSounds, sound => sound.name == name).source;
        }
    }
}