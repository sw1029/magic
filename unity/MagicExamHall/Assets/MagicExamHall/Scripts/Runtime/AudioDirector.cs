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
        private readonly Dictionary<SpellFamily, AudioClip> familyLayerClips = new();
        private readonly Dictionary<CustomSpellEffectKind, AudioClip> customEffectLayerClips = new();
        private readonly Dictionary<CustomShapeEventKind, AudioClip> customShapeEventLayerClips = new();
        private readonly Dictionary<BgmCue, AudioClip> bgmClips = new();
        private AudioSource sfxSource = null!;
        private AudioSource familyLayerSource = null!;
        private AudioSource customEffectLayerSource = null!;
        private AudioSource customShapeEventLayerSource = null!;
        private AudioSource bgmSource = null!;
        private BgmCue currentBgm = BgmCue.None;

        public BgmCue CurrentBgmForTests => currentBgm;
        public int SfxClipCountForTests => sfxClips.Count;
        public int FamilyLayerClipCountForTests => familyLayerClips.Count;
        public int CustomEffectLayerClipCountForTests => customEffectLayerClips.Count;
        public int CustomShapeEventLayerClipCountForTests => customShapeEventLayerClips.Count;
        public int BgmClipCountForTests => bgmClips.Count;
        public bool HasFamilyLayerClipForTests(SpellFamily family) => familyLayerClips.ContainsKey(family);
        public bool HasCustomEffectLayerClipForTests(CustomSpellEffectKind effect) => customEffectLayerClips.ContainsKey(effect);
        public bool HasCustomShapeEventLayerClipForTests(CustomShapeEventKind eventKind) => customShapeEventLayerClips.ContainsKey(eventKind);

        public void Initialize()
        {
            if (sfxSource != null &&
                familyLayerSource != null &&
                customEffectLayerSource != null &&
                customShapeEventLayerSource != null &&
                bgmSource != null)
            {
                return;
            }

            sfxSource = gameObject.AddComponent<AudioSource>();
            ConfigureSfxSource(sfxSource);

            familyLayerSource = gameObject.AddComponent<AudioSource>();
            ConfigureSfxSource(familyLayerSource);

            customEffectLayerSource = gameObject.AddComponent<AudioSource>();
            ConfigureSfxSource(customEffectLayerSource);

            customShapeEventLayerSource = gameObject.AddComponent<AudioSource>();
            ConfigureSfxSource(customShapeEventLayerSource);

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
            var pitch = FamilyPitch(family);
            var averageQuality = quality.Average();
            PlaySfx(AudioCue.CastBaseSuccess, averageQuality < 0.58f ? 0.50f : 0.82f, pitch);
            PlayFamilyLayer(family, averageQuality < 0.58f ? 0.28f : 0.44f, pitch);
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

        public void PlayCustomSpellEffect(CustomSpellEffectKind effect, SpellFamily baseFamily)
        {
            if (effect == CustomSpellEffectKind.None)
            {
                return;
            }

            var familyPitch = FamilyPitch(baseFamily);
            var effectPitch = CustomEffectPitch(effect);
            PlaySfx(AudioCue.CastOverlaySuccess, 0.52f, Mathf.Lerp(familyPitch, effectPitch, 0.35f));
            PlayFamilyLayer(baseFamily, 0.32f, familyPitch);
            PlayCustomEffectLayer(effect, 0.64f, effectPitch);
        }

        public void PlayCustomShapeEvent(CustomShapeEventKind eventKind)
        {
            if (eventKind == CustomShapeEventKind.None)
            {
                return;
            }

            PlayCustomShapeEventLayer(eventKind, 0.46f, CustomShapeEventPitch(eventKind));
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

        private void PlayFamilyLayer(SpellFamily family, float volume, float pitch)
        {
            Initialize();
            if (familyLayerClips.TryGetValue(family, out var clip))
            {
                PlayLayerClip(familyLayerSource, clip, volume, pitch);
            }
        }

        private void PlayCustomEffectLayer(CustomSpellEffectKind effect, float volume, float pitch)
        {
            Initialize();
            if (customEffectLayerClips.TryGetValue(effect, out var clip))
            {
                PlayLayerClip(customEffectLayerSource, clip, volume, pitch);
            }
        }

        private void PlayCustomShapeEventLayer(CustomShapeEventKind eventKind, float volume, float pitch)
        {
            Initialize();
            if (customShapeEventLayerClips.TryGetValue(eventKind, out var clip))
            {
                PlayLayerClip(customShapeEventLayerSource, clip, volume, pitch);
            }
        }

        private static void PlayLayerClip(AudioSource source, AudioClip clip, float volume, float pitch)
        {
            source.pitch = Mathf.Clamp(pitch, 0.5f, 1.6f);
            source.PlayOneShot(clip, Mathf.Clamp01(volume) * MagicExamSettings.SfxVolume);
            source.pitch = 1f;
        }

        private void ApplyVolumes()
        {
            if (sfxSource != null)
            {
                sfxSource.volume = MagicExamSettings.SfxVolume;
            }
            if (familyLayerSource != null)
            {
                familyLayerSource.volume = MagicExamSettings.SfxVolume;
            }
            if (customEffectLayerSource != null)
            {
                customEffectLayerSource.volume = MagicExamSettings.SfxVolume;
            }
            if (customShapeEventLayerSource != null)
            {
                customShapeEventLayerSource.volume = MagicExamSettings.SfxVolume;
            }
            if (bgmSource != null)
            {
                bgmSource.volume = MagicExamSettings.BgmVolume * 0.55f;
            }
        }

        private void BuildClips()
        {
            if (sfxClips.Count == 0)
            {
                BuildCoreSfxClips();
            }

            if (familyLayerClips.Count == 0)
            {
                BuildFamilyLayerClips();
            }

            if (customEffectLayerClips.Count == 0)
            {
                BuildCustomEffectLayerClips();
            }

            if (customShapeEventLayerClips.Count == 0)
            {
                BuildCustomShapeEventLayerClips();
            }

            if (bgmClips.Count == 0)
            {
                BuildBgmClips();
            }
        }

        private void BuildCoreSfxClips()
        {
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
        }

        private void BuildFamilyLayerClips()
        {
            familyLayerClips[SpellFamily.Fire] = ExternalSfx("element_fire") ?? Tone("element_fire", 0.24f, 880f, 1320f, Wave.Triangle, 0.08f);
            familyLayerClips[SpellFamily.Water] = ExternalSfx("element_water") ?? Noise("element_water", 0.28f, 0.08f);
            familyLayerClips[SpellFamily.Wind] = ExternalSfx("element_wind") ?? Tone("element_wind", 0.32f, 720f, 1080f, Wave.Sine, 0.07f);
            familyLayerClips[SpellFamily.Earth] = ExternalSfx("element_earth") ?? Tone("element_earth", 0.30f, 180f, 240f, Wave.Square, 0.06f);
            familyLayerClips[SpellFamily.Life] = ExternalSfx("element_life") ?? Tone("element_life", 0.34f, 440f, 660f, Wave.Triangle, 0.07f);
        }

        private void BuildCustomEffectLayerClips()
        {
            customEffectLayerClips[CustomSpellEffectKind.Ice] = ExternalSfx("custom_ice") ?? Tone("custom_ice", 0.28f, 988f, 1480f, Wave.Triangle, 0.08f);
            customEffectLayerClips[CustomSpellEffectKind.Electric] = ExternalSfx("custom_electric") ?? Noise("custom_electric", 0.20f, 0.12f);
            customEffectLayerClips[CustomSpellEffectKind.Cleanse] = ExternalSfx("custom_cleanse") ?? Tone("custom_cleanse", 0.38f, 523f, 880f, Wave.Sine, 0.07f);
            customEffectLayerClips[CustomSpellEffectKind.Focus] = ExternalSfx("custom_focus") ?? Tone("custom_focus", 0.26f, 660f, 1320f, Wave.Sine, 0.09f);
            customEffectLayerClips[CustomSpellEffectKind.Flow] = ExternalSfx("custom_flow") ?? Tone("custom_flow", 0.34f, 620f, 760f, Wave.Sine, 0.07f);
            customEffectLayerClips[CustomSpellEffectKind.Connection] = ExternalSfx("custom_connection") ?? Tone("custom_connection", 0.32f, 392f, 784f, Wave.Triangle, 0.07f);
            customEffectLayerClips[CustomSpellEffectKind.Steel] = ExternalSfx("custom_steel") ?? Tone("custom_steel", 0.30f, 246f, 370f, Wave.Square, 0.06f);
            customEffectLayerClips[CustomSpellEffectKind.Stability] = ExternalSfx("custom_stability") ?? Tone("custom_stability", 0.30f, 196f, 294f, Wave.Square, 0.06f);
            customEffectLayerClips[CustomSpellEffectKind.LivingBridge] = ExternalSfx("custom_living_bridge") ?? Tone("custom_living_bridge", 0.36f, 330f, 660f, Wave.Triangle, 0.07f);
            customEffectLayerClips[CustomSpellEffectKind.WindPlatform] = ExternalSfx("custom_wind_platform") ?? Tone("custom_wind_platform", 0.30f, 740f, 1110f, Wave.Sine, 0.07f);
        }

        private void BuildCustomShapeEventLayerClips()
        {
            customShapeEventLayerClips[CustomShapeEventKind.SlashDamage] = ExternalSfx("event_slash_damage") ?? Tone("event_slash_damage", 0.18f, 520f, 260f, Wave.Square, 0.08f);
            customShapeEventLayerClips[CustomShapeEventKind.DirectionalProjectile] = ExternalSfx("event_directional_projectile") ?? Tone("event_directional_projectile", 0.24f, 620f, 980f, Wave.Triangle, 0.07f);
            customShapeEventLayerClips[CustomShapeEventKind.WallEntity] = ExternalSfx("event_wall_entity") ?? Tone("event_wall_entity", 0.26f, 190f, 160f, Wave.Square, 0.07f);
            customShapeEventLayerClips[CustomShapeEventKind.Barrier] = ExternalSfx("event_barrier") ?? Tone("event_barrier", 0.30f, 392f, 784f, Wave.Sine, 0.07f);
            customShapeEventLayerClips[CustomShapeEventKind.Trap] = ExternalSfx("event_trap") ?? Noise("event_trap", 0.20f, 0.08f);
            customShapeEventLayerClips[CustomShapeEventKind.Stun] = ExternalSfx("event_stun") ?? Tone("event_stun", 0.20f, 988f, 494f, Wave.Triangle, 0.08f);
            customShapeEventLayerClips[CustomShapeEventKind.MagicAmplify] = ExternalSfx("event_magic_amplify") ?? Tone("event_magic_amplify", 0.26f, 660f, 1320f, Wave.Sine, 0.08f);
            customShapeEventLayerClips[CustomShapeEventKind.AttackBuff] = ExternalSfx("event_attack_buff") ?? Tone("event_attack_buff", 0.24f, 440f, 880f, Wave.Triangle, 0.07f);
            customShapeEventLayerClips[CustomShapeEventKind.MoveSpeedBuff] = ExternalSfx("event_move_speed_buff") ?? Tone("event_move_speed_buff", 0.24f, 740f, 980f, Wave.Sine, 0.07f);
            customShapeEventLayerClips[CustomShapeEventKind.SpecialAttackBoost] = ExternalSfx("event_special_attack_boost") ?? Tone("event_special_attack_boost", 0.24f, 740f, 1480f, Wave.Triangle, 0.08f);
            customShapeEventLayerClips[CustomShapeEventKind.BuffDispel] = ExternalSfx("event_buff_dispel") ?? Noise("event_buff_dispel", 0.22f, 0.08f);
            customShapeEventLayerClips[CustomShapeEventKind.EventBlock] = ExternalSfx("event_event_block") ?? Tone("event_event_block", 0.22f, 294f, 147f, Wave.Square, 0.07f);
            customShapeEventLayerClips[CustomShapeEventKind.AttributeLaser] = ExternalSfx("event_attribute_laser") ?? Tone("event_attribute_laser", 0.22f, 880f, 1760f, Wave.Sine, 0.08f);
            customShapeEventLayerClips[CustomShapeEventKind.RandomBuffDispel] = ExternalSfx("event_random_buff_dispel") ?? Noise("event_random_buff_dispel", 0.24f, 0.08f);
            customShapeEventLayerClips[CustomShapeEventKind.PiercingMark] = ExternalSfx("event_piercing_mark") ?? Tone("event_piercing_mark", 0.22f, 520f, 1040f, Wave.Triangle, 0.07f);
            customShapeEventLayerClips[CustomShapeEventKind.GuardBuff] = ExternalSfx("event_guard_buff") ?? Tone("event_guard_buff", 0.26f, 330f, 495f, Wave.Triangle, 0.07f);
            customShapeEventLayerClips[CustomShapeEventKind.CurveProjectile] = ExternalSfx("event_curve_projectile") ?? Tone("event_curve_projectile", 0.24f, 620f, 840f, Wave.Sine, 0.07f);
        }

        private void BuildBgmClips()
        {
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

        private static void ConfigureSfxSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
        }

        private static float FamilyPitch(SpellFamily family)
        {
            return family switch
            {
                SpellFamily.Fire => 1.12f,
                SpellFamily.Water => 0.94f,
                SpellFamily.Wind => 1.06f,
                SpellFamily.Earth => 0.88f,
                SpellFamily.Life => 1.0f,
                _ => 1f
            };
        }

        private static float CustomEffectPitch(CustomSpellEffectKind effect)
        {
            return effect switch
            {
                CustomSpellEffectKind.Ice => 1.12f,
                CustomSpellEffectKind.Electric => 1.24f,
                CustomSpellEffectKind.Cleanse => 0.98f,
                CustomSpellEffectKind.Focus => 1.16f,
                CustomSpellEffectKind.Flow => 1.04f,
                CustomSpellEffectKind.Connection => 0.96f,
                CustomSpellEffectKind.Steel => 0.90f,
                CustomSpellEffectKind.Stability => 0.86f,
                CustomSpellEffectKind.LivingBridge => 0.92f,
                CustomSpellEffectKind.WindPlatform => 1.10f,
                _ => 1f
            };
        }

        private static float CustomShapeEventPitch(CustomShapeEventKind eventKind)
        {
            return eventKind switch
            {
                CustomShapeEventKind.SlashDamage => 1.10f,
                CustomShapeEventKind.DirectionalProjectile => 1.08f,
                CustomShapeEventKind.WallEntity => 0.86f,
                CustomShapeEventKind.Barrier => 0.96f,
                CustomShapeEventKind.Trap => 0.92f,
                CustomShapeEventKind.Stun => 1.14f,
                CustomShapeEventKind.MagicAmplify => 1.18f,
                CustomShapeEventKind.AttackBuff => 1.06f,
                CustomShapeEventKind.MoveSpeedBuff => 1.16f,
                CustomShapeEventKind.SpecialAttackBoost => 1.20f,
                CustomShapeEventKind.BuffDispel => 0.90f,
                CustomShapeEventKind.EventBlock => 0.84f,
                CustomShapeEventKind.AttributeLaser => 1.22f,
                CustomShapeEventKind.RandomBuffDispel => 0.94f,
                CustomShapeEventKind.PiercingMark => 1.02f,
                CustomShapeEventKind.GuardBuff => 0.88f,
                CustomShapeEventKind.CurveProjectile => 1.12f,
                _ => 1f
            };
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
