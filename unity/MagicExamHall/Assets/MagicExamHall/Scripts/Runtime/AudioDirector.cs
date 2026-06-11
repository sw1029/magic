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

            sfxClips[AudioCue.CastBaseSuccess] = ExternalSfx("cast_base_success") ?? Tone("cast_base_success", 0.40f, 523f, 784f, Wave.Sine, 0.12f);
            sfxClips[AudioCue.CastOverlaySuccess] = ExternalSfx("cast_overlay_success") ?? Tone("cast_overlay_success", 0.30f, 660f, 990f, Wave.Triangle, 0.10f);
            sfxClips[AudioCue.CastFinalEffect] = ExternalSfx("cast_final_effect") ?? Tone("cast_final_effect", 0.80f, 392f, 1175f, Wave.Sine, 0.18f);
            sfxClips[AudioCue.CastInvalid] = ExternalSfx("cast_invalid") ?? Tone("cast_invalid", 0.20f, 180f, 120f, Wave.Square, 0.10f);
            sfxClips[AudioCue.CastIncomplete] = ExternalSfx("cast_incomplete") ?? Tone("cast_incomplete", 0.30f, 240f, 190f, Wave.Triangle, 0.10f);
            sfxClips[AudioCue.CastDependencyMissing] = ExternalSfx("cast_dependency_missing") ?? Tone("cast_dependency_missing", 0.25f, 120f, 90f, Wave.Square, 0.16f);
            sfxClips[AudioCue.GoalSatisfied] = ExternalSfx("goal_satisfied") ?? Tone("goal_satisfied", 0.60f, 440f, 880f, Wave.Sine, 0.14f);
            sfxClips[AudioCue.FloorComplete] = ExternalSfx("floor_complete") ?? Tone("floor_complete", 1.20f, 330f, 990f, Wave.Sine, 0.20f);
            sfxClips[AudioCue.NoteUnlock] = ExternalSfx("note_unlock") ?? Tone("note_unlock", 0.30f, 740f, 555f, Wave.Triangle, 0.08f);
            sfxClips[AudioCue.HazardReset] = ExternalSfx("hazard_reset") ?? Noise("hazard_reset", 0.50f, 0.18f);
            sfxClips[AudioCue.NpcAppear] = ExternalSfx("npc_appear") ?? Tone("npc_appear", 0.40f, 620f, 930f, Wave.Sine, 0.10f);
            sfxClips[AudioCue.EndingReportOpened] = ExternalSfx("ending_report_opened") ?? Tone("ending_report_opened", 0.70f, 294f, 880f, Wave.Sine, 0.16f);
            bgmClips[BgmCue.AmbientTower] = ExternalBgm("ambient_tower") ?? PadLoop(
                "ambient_tower",
                32f,
                new[] { 110f, 164.81f, 220f, 261.63f },
                new[] { 98f, 146.83f, 196f, 246.94f },
                pulseCycles: 0);
            bgmClips[BgmCue.ClimaxSeal] = ExternalBgm("climax_seal") ?? PadLoop(
                "climax_seal",
                24f,
                new[] { 146.83f, 220f, 293.66f, 440f },
                new[] { 130.81f, 196f, 261.63f, 392f },
                pulseCycles: 12);
        }

        /// <summary>
        /// File-first lookup: drop a licensed clip at
        /// <c>Resources/Sfx/&lt;name&gt;</c> or <c>Resources/Bgm/&lt;name&gt;</c>
        /// and it replaces the procedural fallback without code changes.
        /// </summary>
        private static AudioClip ExternalSfx(string name)
        {
            return Resources.Load<AudioClip>("Sfx/" + name);
        }

        private static AudioClip ExternalBgm(string name)
        {
            return Resources.Load<AudioClip>("Bgm/" + name);
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

        /// <summary>
        /// Seamless two-chord ambient pad. Every frequency is quantized to a
        /// whole number of cycles per loop so the clip loops without clicks;
        /// the chords crossfade twice per loop for a slow harmonic drift.
        /// <paramref name="pulseCycles"/> &gt; 0 adds a gentle amplitude pulse
        /// (used by the climax track for a sense of forward motion).
        /// </summary>
        private static AudioClip PadLoop(string name, float seconds, float[] chordA, float[] chordB, int pulseCycles)
        {
            var samples = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[samples];
            var quantizedA = Quantize(chordA, seconds);
            var quantizedB = Quantize(chordB, seconds);
            var sub = Mathf.Round(55f * seconds) / seconds;
            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)SampleRate;
                // 0 -> 1 -> 0 -> 1 -> 0 over one loop: smooth chord alternation.
                var blend = 0.5f - 0.5f * Mathf.Cos(t / seconds * Mathf.PI * 4f);
                var value = 0f;
                for (var n = 0; n < quantizedA.Length; n++)
                {
                    var amp = 0.030f / (n + 1);
                    value += Mathf.Sin(t * quantizedA[n] * Mathf.PI * 2f) * amp * (1f - blend);
                    value += Mathf.Sin(t * quantizedB[n] * Mathf.PI * 2f) * amp * blend;
                }

                value += Mathf.Sin(t * sub * Mathf.PI * 2f) * 0.020f;
                var swell = 0.88f + 0.12f * Mathf.Sin(t / seconds * Mathf.PI * 6f);
                if (pulseCycles > 0)
                {
                    swell *= 0.86f + 0.14f * Mathf.Sin(t / seconds * Mathf.PI * 2f * pulseCycles);
                }

                data[i] = value * swell;
            }

            return BuildClip(name, data);
        }

        private static float[] Quantize(float[] frequencies, float loopSeconds)
        {
            var quantized = new float[frequencies.Length];
            for (var i = 0; i < frequencies.Length; i++)
            {
                quantized[i] = Mathf.Max(1f, Mathf.Round(frequencies[i] * loopSeconds)) / loopSeconds;
            }

            return quantized;
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
