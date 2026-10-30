using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

public class TVPostEffectTests
{
    [Test]
    public void VolumeDefaultsOffAndEitherEffectCanActivate()
    {
        var volume = ScriptableObject.CreateInstance<TVPostEffectVolume>();
        try
        {
            Assert.IsFalse(volume.IsActive());
            volume.scanlineIntensity.value = 0.3f;
            Assert.IsTrue(volume.IsActive());
            volume.scanlineIntensity.value = 0f;
            volume.chromaticAberration.value = 2f;
            Assert.IsTrue(volume.IsActive());
        }
        finally { Object.DestroyImmediate(volume); }
    }

    [Test]
    public void ShaderPreservesIdentityDrawsScanlinesAndSeparatesChannels()
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            Assert.Ignore("GPU描画検証は-EnableGraphicsで実行してください。");
        var shader = Shader.Find("Hidden/GcRobot/TVPostEffect");
        Assert.IsNotNull(shader);
        Assert.IsTrue(shader.isSupported);
        var material = new Material(shader);
        var source = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
        source.wrapMode = TextureWrapMode.Clamp;
        var output = new RenderTexture(64, 64, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        output.Create();
        var readback = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
        var oldTarget = RenderTexture.active;
        try
        {
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++) source.SetPixel(x, y, Color.white * (x / 63f));
            source.Apply();
            Draw(Vector4.zero + new Vector4(0, 4, 0, 0));
            Assert.AreEqual(source.GetPixel(48, 32).r, readback.GetPixel(48, 32).r, 0.025f);
            Draw(new Vector4(0, 4, 4, 0));
            var split = readback.GetPixel(48, 32);
            Assert.Greater(Mathf.Abs(split.r - split.b), 0.03f);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++) source.SetPixel(x, y, Color.white);
            source.Apply();
            Draw(new Vector4(0.5f, 4, 0, 0));
            float min = 1, max = 0;
            for (int y = 0; y < 64; y++)
            {
                float value = readback.GetPixel(32, y).r;
                min = Mathf.Min(min, value); max = Mathf.Max(max, value);
            }
            Assert.Less(min, 0.7f);
            Assert.Greater(max, 0.85f);
        }
        finally
        {
            RenderTexture.active = oldTarget;
            Object.DestroyImmediate(material); Object.DestroyImmediate(source);
            Object.DestroyImmediate(readback); output.Release(); Object.DestroyImmediate(output);
        }

        void Draw(Vector4 values)
        {
            material.SetVector("_TVParameters", values);
            using var cmd = new CommandBuffer();
            cmd.SetRenderTarget(output);
            cmd.SetViewport(new Rect(0, 0, 64, 64));
            cmd.SetGlobalTexture("_BlitTexture", source);
            cmd.SetGlobalVector("_BlitTexture_TexelSize", new Vector4(1f/64, 1f/64, 64, 64));
            cmd.SetGlobalVector("_BlitScaleBias", new Vector4(1, 1, 0, 0));
            cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
            Graphics.ExecuteCommandBuffer(cmd);
            RenderTexture.active = output;
            readback.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
            readback.Apply();
        }
    }
}
