using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class SoundTests
{
    [Test]
    public void PreparedSoundsHaveCorrectDurationAndLoopBoundary()
    {
        foreach (string name in new[] { "moveLoop", "VacuumLoop", "DashStart" })
        {
            var clip = Resources.Load<AudioClip>("Sound/Prepared/" + name);
            Assert.IsNotNull(clip, name);
            Assert.AreEqual(name == "DashStart" ? 0.6f : 7.88f, clip.length, 0.001f);
            clip.LoadAudioData();
            var data = new float[clip.samples * clip.channels];
            Assert.IsTrue(clip.GetData(data, 0));
            if (name != "DashStart")
                for (int c = 0; c < clip.channels; c++)
                    Assert.Less(Mathf.Abs(data[c] - data[data.Length - clip.channels + c]), 0.1f, name);
        }
    }

    [Test, Explicit("音源の更新時だけ実行するオフライン素材作成")]
    public void PrepareSoundPlaybackAssets()
    {
        string root = System.IO.Path.GetFullPath(Application.dataPath + "/../../..");
        string folder = System.IO.Path.Combine(root, "Assets/SRC/Sound/Prepared");
        System.IO.Directory.CreateDirectory(folder);
        foreach (string name in new[] { "move", "Vacuum", "Dash" })
        {
            var clip = Resources.Load<AudioClip>("Sound/" + name);
            clip.LoadAudioData();
            var source = new float[clip.samples * clip.channels];
            Assert.IsTrue(clip.GetData(source, 0));
            int start = Mathf.RoundToInt((name == "Dash" ? 0.06f : 2f) * clip.frequency);
            int frames = Mathf.RoundToInt((name == "Dash" ? 0.60f : 8f) * clip.frequency);
            int fade = name == "Dash" ? 0 : Mathf.RoundToInt(0.12f * clip.frequency);
            int count = frames - fade;
            var samples = new float[count * clip.channels];
            for (int i = 0; i < count; i++)
                for (int c = 0; c < clip.channels; c++)
                {
                    float value = source[(start + fade + i) * clip.channels + c];
                    if (fade > 0 && i >= count - fade)
                    {
                        int j = i - (count - fade);
                        value = Mathf.Lerp(value, source[(start + j) * clip.channels + c], j / (float)fade);
                    }
                    if (name == "Dash")
                        value *= Mathf.Clamp01(i / (0.003f * clip.frequency)) * Mathf.Clamp01((count - 1 - i) / (0.01f * clip.frequency));
                    samples[i * clip.channels + c] = value;
                }
            string path = System.IO.Path.Combine(folder, name + (fade > 0 ? "Loop" : "Start") + ".wav");
            using var writer = new System.IO.BinaryWriter(System.IO.File.Create(path));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + samples.Length * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16); writer.Write((short)1); writer.Write((short)clip.channels);
            writer.Write(clip.frequency); writer.Write(clip.frequency * clip.channels * 2);
            writer.Write((short)(clip.channels * 2)); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples.Length * 2);
            foreach (float value in samples) writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(value, -1f, 1f) * 32767));
            Debug.Log("Prepared audio: " + path);
        }
    }

    [UnityTest, Explicit("元音源の解析用。BGMは解析時にDecompress On Loadへ設定する")]
    public IEnumerator InspectImportedSoundClips()
    {
        foreach (string name in new[] { "BGM", "Dash", "move", "Vacuum" })
        {
            var clip = Resources.Load<AudioClip>("Sound/" + name);
            Assert.IsNotNull(clip, name);
            clip.LoadAudioData();
            while (clip.loadState == AudioDataLoadState.Loading) yield return null;
            var data = new float[clip.samples * clip.channels];
            Assert.IsTrue(clip.GetData(data, 0), name);
            float peak = 0; double sum = 0;
            foreach (float x in data) { peak = Mathf.Max(peak, Mathf.Abs(x)); sum += x * x; }
            int start = 0, end = data.Length - 1;
            while (start < end && Mathf.Abs(data[start]) < peak * 0.03f) start++;
            while (end > start && Mathf.Abs(data[end]) < peak * 0.03f) end--;
            Debug.Log($"SOUND {name}: seconds={clip.length:F3}, channels={clip.channels}, rate={clip.frequency}, peak={peak:F3}, rms={System.Math.Sqrt(sum/data.Length):F3}, onset={start/(float)(clip.frequency*clip.channels):F3}, tail={(data.Length-1-end)/(float)(clip.frequency*clip.channels):F3}");
            var bins = new System.Text.StringBuilder();
            int stride = clip.frequency * clip.channels / 10;
            for (int offset = 0; offset < data.Length; offset += stride)
            {
                double energy = 0; int count = System.Math.Min(stride, data.Length-offset);
                for (int j=0;j<count;j++) energy += data[offset+j]*data[offset+j];
                if(offset < stride*15 || offset > data.Length-stride*15)
                    bins.Append($"{offset/(float)(clip.frequency*clip.channels):F1}:{System.Math.Sqrt(energy/count):F3} ");
            }
            Debug.Log("SOUND envelope " + name + " " + bins);
        }
    }
}
