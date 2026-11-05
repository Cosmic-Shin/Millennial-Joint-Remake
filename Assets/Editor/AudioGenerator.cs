using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Synthesizes the kitchen sound effects and the original "Cosmic Kitchen" gameplay track
// into Assets/Audio/Generated. Tweak a recipe below, then run Tools > Generate Extra Audio.
public static class AudioGenerator
{
    const int SR = 44100;
    const string Dir = "Assets/Audio/Generated/";
    static System.Random rng;

    [MenuItem("Tools/Generate Extra Audio")]
    public static void Generate()
    {
        rng = new System.Random(7);
        Save("fry_sizzle", FrySizzle(), 0.6f);
        Save("stove_ignite", StoveIgnite(), 0.6f);
        Save("pour", Pour(), 0.7f);
        Save("cup_clink", CupClink(), 0.6f);
        Save("box_rustle", BoxRustle(), 0.6f);
        Save("chili_bubble", ChiliBubble(), 0.7f);
        Save("pad_whoosh", PadWhoosh(), 0.7f);
        Save("powerup_chime", PowerUpChime(), 0.7f);
        Save("trash_thunk", TrashThunk(), 0.8f);
        Save("cosmic_kitchen_bgm", Music(), 0.85f);
        AssetDatabase.Refresh();

        // Long track: stream it compressed instead of holding ~6 MB of PCM in memory.
        var music = (AudioImporter)AssetImporter.GetAtPath(Dir + "cosmic_kitchen_bgm.wav");
        var s = music.defaultSampleSettings;
        s.loadType = AudioClipLoadType.Streaming;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = 0.6f;
        music.defaultSampleSettings = s;
        music.SaveAndReimport();
    }

    // ---------- sound effects ----------

    static float[] FrySizzle()
    {
        var b = New(0.9f);
        Sweep(b, 0f, 220f, 90f, 0.09f, 0.5f);          // the "plop" into hot oil
        float lp = 0f, pop = 0f;
        for (int i = 0; i < b.Length; i++)
        {
            float t = T(i);
            float n = Noise();
            float hiss = n - Lp(ref lp, n, 3000f);       // high-passed noise
            if (rng.NextDouble() < 0.004) pop = 1f;       // random oil crackles
            pop *= 0.995f;
            b[i] += hiss * (0.25f + pop * 0.8f) * Fade(t, 0.03f, 0.9f, 0.35f);
        }
        return b;
    }

    static float[] StoveIgnite()
    {
        var b = New(0.7f);
        foreach (float c in new[] { 0f, 0.06f, 0.12f })   // igniter ticks
            for (int i = 0; i < 200; i++)
                b[At(c) + i] += Noise() * Mathf.Exp(-i / 40f) * 0.9f;
        float lp = 0f;
        for (int i = At(0.14f); i < b.Length; i++)       // gas catching: soft "whoomp"
        {
            float t = T(i) - 0.14f;
            b[i] += Lp(ref lp, Noise(), 300f + 900f * Mathf.Exp(-t * 6f)) * 1.6f * Fade(t, 0.06f, 0.56f, 0.3f);
        }
        return b;
    }

    static float[] Pour()
    {
        var b = New(0.85f);
        float lp = 0f;
        for (int i = 0; i < b.Length; i++)
            b[i] += Lp(ref lp, Noise(), 1500f) * 0.25f * Fade(T(i), 0.05f, 0.85f, 0.2f);
        for (float t = 0.02f; t < 0.7f; t += 0.018f + (float)rng.NextDouble() * 0.02f)
        {
            float f = Mathf.Lerp(420f, 950f, t / 0.7f) * (0.85f + (float)rng.NextDouble() * 0.3f); // cup filling up
            Sweep(b, t, f, f * 1.6f, 0.025f, 0.22f);
        }
        return b;
    }

    static float[] CupClink()
    {
        var b = New(0.45f);
        Bell(b, 0f, 2350f, 0.12f, 0.5f, 1.53f, 2.2f);
        Bell(b, 0.07f, 2350f * 1.06f, 0.08f, 0.25f, 1.53f, 2.2f);
        return b;
    }

    static float[] BoxRustle()
    {
        var b = New(0.35f);
        float lp = 0f, lp2 = 0f;
        foreach (float c in new[] { 0f, 0.07f, 0.15f })
            for (int i = At(c); i < At(c + 0.12f); i++)
            {
                float n = Noise();
                float band = Lp(ref lp, n, 4500f) - Lp(ref lp2, n, 1200f); // cardboard-ish band
                float k = (T(i) - c) / 0.12f;
                b[i] += band * Mathf.Sin(Mathf.PI * k) * (1f - c * 3f);
            }
        return b;
    }

    static float[] ChiliBubble()
    {
        var b = New(0.9f);
        for (float t = 0.01f; t < 0.75f; t += 0.05f + (float)rng.NextDouble() * 0.08f)
        {
            float f = 140f + (float)rng.NextDouble() * 160f;
            Sweep(b, t, f, f * 2.4f, 0.06f, 0.5f);         // a bubble pops with rising pitch
        }
        float lp = 0f;
        for (int i = 0; i < b.Length; i++)
            b[i] += Lp(ref lp, Noise(), 400f) * 0.5f * Fade(T(i), 0.05f, 0.9f, 0.3f);
        return b;
    }

    static float[] PadWhoosh()
    {
        var b = New(0.75f);
        double ph = 0, ph2 = 0;
        float lp = 0f;
        for (int i = 0; i < b.Length; i++)
        {
            float t = T(i);
            float k = t / 0.75f;
            float f = 180f * Mathf.Pow(6f, k);              // rising zero-g sweep
            ph += 2 * Math.PI * f / SR;
            ph2 += 2 * Math.PI * (1800f + 60f * Mathf.Sin(t * 40f)) / SR;
            float env = Mathf.Sin(Mathf.PI * Mathf.Pow(k, 0.6f));
            float air = Lp(ref lp, Noise(), 300f + 2500f * k);
            b[i] += env * ((float)Math.Sin(ph) * 0.35f + air * 0.9f + (float)Math.Sin(ph2) * 0.06f * k);
        }
        return b;
    }

    static float[] PowerUpChime()
    {
        var b = New(0.9f);
        int[] notes = { 84, 88, 91, 96 };                  // C6 E6 G6 C7
        for (int n = 0; n < notes.Length; n++)
            Bell(b, n * 0.06f, Midi(notes[n]), 0.35f, 0.4f, 2f, 3f);
        for (int s = 0; s < 6; s++)                        // sparkle tail
            Bell(b, 0.25f + s * 0.07f, Midi(96 + rng.Next(0, 12)), 0.12f, 0.08f, 2f, 3f);
        return b;
    }

    static float[] TrashThunk()
    {
        var b = New(0.45f);
        Sweep(b, 0f, 110f, 45f, 0.25f, 1f, true);
        float lp = 0f;
        for (int i = 0; i < At(0.08f); i++)
            b[i] += Lp(ref lp, Noise(), 900f) * Mathf.Exp(-T(i) * 40f) * 1.5f;
        foreach (float c in new[] { 0.11f, 0.17f, 0.21f })  // a little rattle in the bin
            Bell(b, c, 900f + (float)rng.NextDouble() * 500f, 0.03f, 0.12f, 1.7f, 2.6f);
        return b;
    }

    // ---------- music: "Cosmic Kitchen", 112 bpm, 32 bars, A minor ----------

    static float[] Music()
    {
        const float beat = 60f / 112f, bar = beat * 4f;
        const int bars = 32;
        var mix = New(bars * bar + 2f);
        var echo = new float[mix.Length];         // arp + melody go through a spacey delay

        int[] roots = { 45, 41, 48, 43 };                  // Am F C G
        int[][] chords = { new[] { 57, 60, 64, 67 }, new[] { 57, 60, 64, 65 }, new[] { 55, 59, 60, 64 }, new[] { 55, 59, 62, 64 } };
        // 4-bar melody: (beat, midi) pairs
        float[][] melody =
        {
            new float[] { 0, 76, 1.5f, 74, 2, 72, 3, 69 },
            new float[] { 0, 72, 1, 74, 2.5f, 76, 3.5f, 79 },
            new float[] { 0, 79, 1, 76, 2, 74, 3, 72 },
            new float[] { 0, 74, 2, 71, 3, 69 },
        };

        for (int bi = 0; bi < bars; bi++)
        {
            float t0 = bi * bar;
            int c = bi % 4;
            bool intro = bi < 4, breakdown = bi >= 16 && bi < 24, full = !intro && !breakdown;

            foreach (int m in chords[c])                   // soft pad
            {
                PadNote(mix, t0, bar + 0.3f, Midi(m), intro || breakdown ? 0.07f : 0.05f);
                PadNote(mix, t0, bar + 0.3f, Midi(m) * 1.003f, 0.03f);
            }

            if (!intro)                                     // bass: 8ths, octave pop on the offbeats
                for (int e = 0; e < 8; e++)
                {
                    if (breakdown && e % 2 == 1) continue;
                    float f = Midi(roots[c] + (e == 3 || e == 7 ? 12 : 0));
                    Pluck(mix, t0 + e * beat / 2f, f, 0.2f, 0.3f);
                }

            for (int s = 0; s < 16; s++)                    // arp
            {
                int[] pattern = { 0, 1, 2, 3, 2, 1, 2, 3 };
                float f = Midi(chords[c][pattern[s % 8]] + 12);
                SoftSquare(echo, t0 + s * beat / 4f, f, 0.09f, intro ? 0.035f : 0.055f);
            }

            if (bi >= 16)                                   // melody in the second half
            {
                float[] ph = melody[c];
                for (int n = 0; n < ph.Length; n += 2)
                    Bell(echo, t0 + ph[n] * beat, Midi((int)ph[n + 1]), 0.45f, bi >= 24 ? 0.16f : 0.12f, 2f, 3f);
            }

            if (intro || breakdown)                         // twinkles
                for (int k = 0; k < 3; k++)
                    Bell(mix, t0 + (float)rng.NextDouble() * bar, Midi(chords[c][rng.Next(4)] + 24), 0.3f, 0.03f, 2f, 3f);

            for (int q = 0; q < 4; q++)                     // drums
            {
                float bt = t0 + q * beat;
                if (full && (q == 0 || q == 2)) Kick(mix, bt);
                if (full && bi % 4 == 3 && q == 2) Kick(mix, bt + beat / 2f);
                if (full && bi >= 8 && (q == 1 || q == 3)) Snare(mix, bt);
                if (!intro) Hat(mix, bt + beat / 2f, breakdown ? 0.04f : 0.07f);
            }
        }

        int d = At(beat * 0.75f);                           // dotted-8th echo
        for (int i = d; i < echo.Length; i++)
            echo[i] += echo[i - d] * 0.38f;
        for (int i = 0; i < mix.Length; i++)
        {
            float t = T(i);
            float fade = Mathf.Clamp01(t / 0.5f) * Mathf.Clamp01((mix.Length / (float)SR - t) / 2f);
            mix[i] = (float)Math.Tanh((mix[i] + echo[i]) * 1.2f) * fade;
        }
        return mix;
    }

    static void PadNote(float[] b, float t0, float dur, float f, float gain)
    {
        double ph = 0;
        for (int i = 0, s = At(t0); i < At(dur) && s + i < b.Length; i++)
        {
            float t = T(i);
            ph += 2 * Math.PI * f / SR;
            float env = Mathf.Clamp01(t / 0.4f) * Mathf.Clamp01((dur - t) / 0.3f);
            b[s + i] += gain * env * ((float)Math.Sin(ph) + 0.3f * (float)Math.Sin(ph * 0.5));
        }
    }

    static void Pluck(float[] b, float t0, float f, float decay, float gain)
    {
        double ph = 0;
        for (int i = 0, s = At(t0); i < At(decay * 4f) && s + i < b.Length; i++)
        {
            ph += f / SR;
            float tri = 4f * Mathf.Abs((float)(ph - Math.Floor(ph + 0.5))) - 1f;
            b[s + i] += gain * tri * Mathf.Clamp01(T(i) / 0.005f) * Mathf.Exp(-T(i) / decay);
        }
    }

    static void SoftSquare(float[] b, float t0, float f, float decay, float gain)
    {
        double ph = 0;
        for (int i = 0, s = At(t0); i < At(decay * 4f) && s + i < b.Length; i++)
        {
            ph += 2 * Math.PI * f / SR;
            b[s + i] += gain * (float)Math.Tanh(2.5 * Math.Sin(ph)) * Mathf.Clamp01(T(i) / 0.004f) * Mathf.Exp(-T(i) / decay);
        }
    }

    static void Kick(float[] b, float t0) => Sweep(b, t0, 130f, 45f, 0.18f, 0.7f, true);

    static void Snare(float[] b, float t0)
    {
        float lp = 0f;
        Sweep(b, t0, 200f, 160f, 0.08f, 0.2f, true);
        for (int i = 0, s = At(t0); i < At(0.18f) && s + i < b.Length; i++)
        {
            float n = Noise();
            b[s + i] += (n - Lp(ref lp, n, 1500f)) * 0.3f * Mathf.Exp(-T(i) / 0.05f);
        }
    }

    static void Hat(float[] b, float t0, float gain)
    {
        float lp = 0f;
        for (int i = 0, s = At(t0); i < At(0.06f) && s + i < b.Length; i++)
        {
            float n = Noise();
            b[s + i] += (n - Lp(ref lp, n, 7000f)) * gain * Mathf.Exp(-T(i) / 0.015f);
        }
    }

    // ---------- building blocks ----------

    // Sine whose pitch glides f0 -> f1. decayEnv = punchy hit; otherwise a smooth bump.
    static void Sweep(float[] b, float t0, float f0, float f1, float dur, float gain, bool decayEnv = false)
    {
        double ph = 0;
        int n = At(dur);
        for (int i = 0, s = At(t0); i < n && s + i < b.Length; i++)
        {
            float k = (float)i / n;
            ph += 2 * Math.PI * f0 * Mathf.Pow(f1 / f0, k) / SR;
            float env = decayEnv ? Mathf.Clamp01(i / 40f) * Mathf.Exp(-k * 5f) : Mathf.Sin(Mathf.PI * k);
            b[s + i] += gain * env * (float)Math.Sin(ph);
        }
    }

    // Metallic ping: fundamental plus two inharmonic partials.
    static void Bell(float[] b, float t0, float f, float decay, float gain, float p2, float p3)
    {
        for (int i = 0, s = At(t0); i < At(decay * 5f) && s + i < b.Length; i++)
        {
            float t = T(i);
            double w = 2 * Math.PI * f * t;
            float v = (float)(Math.Sin(w) + 0.4 * Math.Sin(w * p2) * Math.Exp(-t / (decay * 0.5)) + 0.2 * Math.Sin(w * p3) * Math.Exp(-t / (decay * 0.3)));
            b[s + i] += gain * v * Mathf.Clamp01(i / 30f) * Mathf.Exp(-t / decay);
        }
    }

    static float Lp(ref float state, float x, float fc)
    {
        state += (1f - Mathf.Exp(-2f * Mathf.PI * fc / SR)) * (x - state);
        return state;
    }

    static float Fade(float t, float attack, float length, float release) =>
        Mathf.Clamp01(t / attack) * Mathf.Clamp01((length - t) / release);

    static float Midi(int m) => 440f * Mathf.Pow(2f, (m - 69) / 12f);
    static float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);
    static float[] New(float seconds) => new float[At(seconds)];
    static int At(float seconds) => (int)(seconds * SR);
    static float T(int sample) => sample / (float)SR;

    static void Save(string name, float[] data, float peakTarget)
    {
        float peak = 1e-4f;
        foreach (float v in data) peak = Mathf.Max(peak, Mathf.Abs(v));
        float g = peakTarget / peak;

        Directory.CreateDirectory(Dir);
        using var w = new BinaryWriter(File.Create(Dir + name + ".wav"));
        int bytes = data.Length * 2;
        w.Write("RIFF".ToCharArray()); w.Write(36 + bytes);
        w.Write("WAVEfmt ".ToCharArray()); w.Write(16);
        w.Write((short)1); w.Write((short)1); w.Write(SR); w.Write(SR * 2); w.Write((short)2); w.Write((short)16);
        w.Write("data".ToCharArray()); w.Write(bytes);
        foreach (float v in data) w.Write((short)(Mathf.Clamp(v * g, -1f, 1f) * 32767f));
    }
}
