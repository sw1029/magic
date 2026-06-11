using System;
using System.Collections.Generic;
using UnityEngine;

namespace MagicExamHall
{
    public enum AudioCue
    {
        CastBaseSuccess,
        CastOverlaySuccess,
        CastFinalEffect,
        CastInvalid,
        CastIncomplete,
        CastDependencyMissing,
        GoalSatisfied,
        FloorComplete,
        NoteUnlock,
        HazardReset,
        NpcAppear,
        EndingReportOpened
    }

    public enum BgmCue
    {
        None,
        AmbientTower,
        ClimaxSeal
    }

    public sealed class AudioDirector : MonoBehaviour
    {
        private const int SampleRate = 44100;

        private readonly Dictionary<AudioCue, AudioClip> sfxClips = new();
        private readonly Dictionary<BgmCue, AudioClip> bgmClips = new();
        private AudioSource sfxSource = null!;
        private AudioSource bgmSource = null!;
        private BgmCue currentBgm = BgmCue.None;

        public BgmCue CurrentBgmForTests => currentBgm;
        public int SfxClipCountForTests => sfxClips.Count;
        public int BgmClipCountForTests => bgmClips.Count;

        public void Initialize()
        {
            if (sfxSource != null && bgmSource != null)
            {
                return;
            }

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.ignoreListenerPause = true;

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.spatialBlend = 0f;
            bgmSource.ignoreListenerPause = true;

            BuildClips();
            ApplyVolumes();
        }

        private void Update()
        {
            ApplyVolumes();
        }

        public void PlayForFloor(int floorNumber)
        {
            if (floorNumber == 4)
            {
                PlayBgm(BgmCue.None);
                return;
            }

            PlayBgm(floorNumber >= 5 ? BgmCue.ClimaxSeal : BgmCue.AmbientTower);
        }

        public void PlayBgm(BgmCue cue)
        {
            Initialize();
            if (currentBgm == cue)
            {
                return;
            }

            currentBgm = cue;
            if (cue == BgmCue.None)
            {
                bgmSource.Stop();
                bgmSource.clip = null;
                return;
            }

            bgmSource.clip = bgmClips[cue];
            bgmSource.time = 0f;
            bgmSource.Play();
        }

        public void PlayBaseSuccess(SpellFamily family, QualityVector quality)
        {
            var pitch = family switch
            {
                SpellFamily.Fire => 1.12f,
                SpellFamily.Water => 0.94f,
                SpellFamily.Wind => 1.06f,
                SpellFamily.Earth => 0.88f,
                SpellFamily.Life => 1.0f,
                _ => 1f
            };
            PlaySfx(AudioCue.CastBaseSuccess, quality.Average() < 0.58f ? 0.50f : 0.82f, pitch);
        }

        public void PlayOverlaySuccess(OverlayOperator op)
        {
            var pitch = op switch
            {
                OverlayOperator.SteelBrace => 0.92f,
                OverlayOperator.ElectricFork => 1.16f,
                OverlayOperator.IceBar => 1.02f,
                OverlayOperator.SoulDot => 1.08f,
                OverlayOperator.VoidCut => 0.86f,
                OverlayOperator.MartialAxis => 1.20f,
                _ => 1f
            };
            PlaySfx(AudioCue.CastOverlaySuccess, 0.78f, pitch);
        }

        public void PlaySfx(AudioCue cue, float volume = 0.82f, float pitch = 1f)
        {
            Initialize();
            if (!sfxClips.TryGetValue(cue, out var clip))
            {
                return;
            }

            sfxSource.pitch = Mathf.Clamp(pitch, 0.5f, 1.6f);
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume) * MagicExamSettings.SfxVolume);
            sfxSource.pitch = 1f;
        }

        private void ApplyVolumes()
        {
            if (sfxSource != null)
            {
                sfxSource.volume = MagicExamSettings.SfxVolume;
            }
            if (bgmSource != null)
            {
                bgmSource.volume = MagicExamSettings.BgmVolume * 0.55f;
            }
        }

        private void BuildClips()
        {
            if (sfxClips.Count > 0)
            {
                return;
            }

            sfxClips[AudioCue.CastBaseSuccess] = Tone("cast_base_success", 0.40f, 523f, 784f, Wave.Sine, 0.12f);
            sfxClips[AudioCue.CastOverlaySuccess] = Tone("cast_overlay_success", 0.30f, 660f, 990f, Wave.Triangle, 0.10f);
            sfxClips[AudioCue.CastFinalEffect] = Tone("cast_final_effect", 0.80f, 392f, 1175f, Wave.Sine, 0.18f);
            sfxClips[AudioCue.CastInvalid] = Tone("cast_invalid", 0.20f, 180f, 120f, Wave.Square, 0.10f);
            sfxClips[AudioCue.CastIncomplete] = Tone("cast_incomplete", 0.30f, 240f, 190f, Wave.Triangle, 0.10f);
            sfxClips[AudioCue.CastDependencyMissing] = Tone("cast_dependency_missing", 0.25f, 120f, 90f, Wave.Square, 0.16f);
            sfxClips[AudioCue.GoalSatisfied] = Tone("goal_satisfied", 0.60f, 440f, 880f, Wave.Sine, 0.14f);
            sfxClips[AudioCue.FloorComplete] = Tone("floor_complete", 1.20f, 330f, 990f, Wave.Sine, 0.20f);
            sfxClips[AudioCue.NoteUnlock] = Tone("note_unlock", 0.30f, 740f, 555f, Wave.Triangle, 0.08f);
            sfxClips[AudioCue.HazardReset] = Noise("hazard_reset", 0.50f, 0.18f);
            sfxClips[AudioCue.NpcAppear] = Tone("npc_appear", 0.40f, 620f, 930f, Wave.Sine, 0.10f);
            sfxClips[AudioCue.EndingReportOpened] = Tone("ending_report_opened", 0.70f, 294f, 880f, Wave.Sine, 0.16f);
            bgmClips[BgmCue.AmbientTower] = Loop("ambient_tower", 16f, 110f, 146.83f, 220f);
            bgmClips[BgmCue.ClimaxSeal] = Loop("climax_seal", 12f, 146.83f, 220f, 440f);
        }

        private static AudioClip Tone(string name, float seconds, float startHz, float endHz, Wave wave, float amplitude)
        {
            var samples = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[samples];
            var phase = 0f;
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)Mathf.Max(1, samples - 1);
                var hz = Mathf.Lerp(startHz, endHz, Mathf.SmoothStep(0f, 1f, t));
                phase += hz / SampleRate;
                var envelope = Mathf.Sin(t * Mathf.PI);
                data[i] = Sample(wave, phase) * envelope * amplitude;
            }

            return BuildClip(name, data);
        }

        private static AudioClip Noise(string name, float seconds, float amplitude)
        {
            var samples = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[samples];
            var random = new System.Random(7319);
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)Mathf.Max(1, samples - 1);
                var envelope = Mathf.Sin(t * Mathf.PI);
                data[i] = ((float)random.NextDouble() * 2f - 1f) * envelope * amplitude;
            }

            return BuildClip(name, data);
        }

        private static AudioClip Loop(string name, float seconds, params float[] notes)
        {
            var samples = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[samples];
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)SampleRate;
                var value = 0f;
                for (var n = 0; n < notes.Length; n++)
                {
                    var phase = t * notes[n] * Mathf.PI * 2f;
                    value += Mathf.Sin(phase) * (0.035f / (n + 1));
                }

                var shimmer = Mathf.Sin(t * 0.25f * Mathf.PI * 2f) * 0.018f;
                data[i] = value + shimmer;
            }

            return BuildClip(name, data);
        }

        private static AudioClip BuildClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Sample(Wave wave, float phase)
        {
            var normalized = phase - Mathf.Floor(phase);
            return wave switch
            {
                Wave.Square => normalized < 0.5f ? 1f : -1f,
                Wave.Triangle => Mathf.Abs(normalized * 4f - 2f) - 1f,
                _ => Mathf.Sin(normalized * Mathf.PI * 2f)
            };
        }

        private enum Wave
        {
            Sine,
            Square,
            Triangle
        }
    }
}
