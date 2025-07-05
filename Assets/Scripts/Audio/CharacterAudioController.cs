using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class CharacterAudioController : MonoBehaviour
    {
        [SerializeField] private List<AudioClip> footstepSounds;
        [SerializeField] private AudioSource baseAudioSource;
        [SerializeField] private AudioSource footstepAudioSource;
        

        private void Awake()
        {
            CopyValuesFromBaseAudioSource();
        }

        public void CopyValuesFromBaseAudioSource()
        {
            if (!baseAudioSource) return;
            // copy settings from base audio source
            footstepAudioSource.volume = baseAudioSource.volume;
            footstepAudioSource.pitch = baseAudioSource.pitch;
            footstepAudioSource.loop = baseAudioSource.loop;
            if (baseAudioSource.outputAudioMixerGroup)
            {
                footstepAudioSource.outputAudioMixerGroup = baseAudioSource.outputAudioMixerGroup;
            }
            footstepAudioSource.playOnAwake = baseAudioSource.playOnAwake;
            footstepAudioSource.spatialBlend = baseAudioSource.spatialBlend;
            footstepAudioSource.dopplerLevel = baseAudioSource.dopplerLevel;
            footstepAudioSource.minDistance = baseAudioSource.minDistance;
            footstepAudioSource.maxDistance = baseAudioSource.maxDistance;
            footstepAudioSource.rolloffMode = baseAudioSource.rolloffMode;
            footstepAudioSource.bypassEffects = baseAudioSource.bypassEffects;
            footstepAudioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, baseAudioSource.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
            footstepAudioSource.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, baseAudioSource.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix));
            footstepAudioSource.SetCustomCurve(AudioSourceCurveType.SpatialBlend, baseAudioSource.GetCustomCurve(AudioSourceCurveType.SpatialBlend));
            footstepAudioSource.SetCustomCurve(AudioSourceCurveType.Spread, baseAudioSource.GetCustomCurve(AudioSourceCurveType.Spread));
        }
        
        public void PlayFootstepSound()
        {
            if (footstepSounds.Count == 0) return;
            
            var randomIndex = Random.Range(0, footstepSounds.Count);
            footstepAudioSource.PlayOneShot(footstepSounds[randomIndex]);
        }

    }
    
#if UNITY_EDITOR

    [CustomEditor(typeof(CharacterAudioController))]
    public class CharacterAudioControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        
            var controller = (CharacterAudioController)target;
            if (GUILayout.Button("Play Footstep Sound"))
            {
                controller.PlayFootstepSound();
            }
            if (GUILayout.Button("Copy Settings from Base Audio Source"))
            {
                controller.CopyValuesFromBaseAudioSource();
            }
        }
    }
#endif
}

