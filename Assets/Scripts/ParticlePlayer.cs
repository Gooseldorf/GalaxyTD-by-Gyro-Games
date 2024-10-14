using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ParticlePlayer : MonoBehaviour
{
    private bool canPlay => particleSystem != null;
    private ParticleSystem particleSystem;
    [SerializeField] private List<ParticleSystem> particleSystems;
    [SerializeField] private List<float> durations;
    private Camera camera;

    [SerializeField]
    private string pathForScreenShot;
    [SerializeField]
    private float duration;
    [SerializeField]
    private int framesCount;

    private int width, height;

    private void Start()
    {
        particleSystem = particleSystems[0];
        camera = Camera.main;
        width = Screen.width;
        height = Screen.height;
    }

    [Button, ShowIf("canPlay")]
    private async void MakeSprites()
    {
        for (int k = 0; k < particleSystems.Count; k++)
        {
            particleSystem = particleSystems[k];
            duration = durations[0];
            float time = 1f/60f;
            framesCount = (int)(duration / time);

            SetSeed(UnityEngine.Random.Range(-100000, 100000));

            Debug.LogError("Start!");

            for (int i = 1; i < framesCount + 1; i++)
            {
                MakeSprite(time * i, (particleSystem.name + "_" + i));
                await System.Threading.Tasks.Task.Delay(1000);
            }
            Debug.LogError("Done!");
        }
    }

    private void SetSeed(int seed)
    {
        var allParticles = GetComponentsInChildren<ParticleSystem>();

        foreach (var p in allParticles)
        {
            p.randomSeed = (uint)seed;
        }
    }

    [Button, ShowIf("canPlay")]
    private void MakeSprite(float time, string fileName)
    {
        particleSystem.Simulate(time);
        string filename = string.Format($"{pathForScreenShot}/{fileName}.png");
        CaptureScreenshot.CaptureTransparentScreenshot(camera, width, height, filename);
    }
    public static class CaptureScreenshot
    {
        public static void CaptureTransparentScreenshot(Camera cam, int width, int height, string screengrabfile_path)
        {
            // This is slower, but seems more reliable.
            var bak_cam_targetTexture = cam.targetTexture;
            var bak_cam_clearFlags = cam.clearFlags;
            var bak_RenderTexture_active = RenderTexture.active;

            var tex_white = new Texture2D(width, height, TextureFormat.ARGB32, false);
            var tex_black = new Texture2D(width, height, TextureFormat.ARGB32, false);
            var tex_transparent = new Texture2D(width, height, TextureFormat.ARGB32, false);
            // Must use 24-bit depth buffer to be able to fill background.
            var render_texture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var grab_area = new Rect(0, 0, width, height);

            RenderTexture.active = render_texture;
            cam.targetTexture = render_texture;
            cam.clearFlags = CameraClearFlags.SolidColor;

            cam.backgroundColor = Color.black;
            cam.Render();
            tex_black.ReadPixels(grab_area, 0, 0);
            tex_black.Apply();

            cam.backgroundColor = Color.white;
            cam.Render();
            tex_white.ReadPixels(grab_area, 0, 0);
            tex_white.Apply();

            // Create Alpha from the difference between black and white camera renders
            for (int y = 0; y < tex_transparent.height; ++y)
            {
                for (int x = 0; x < tex_transparent.width; ++x)
                {
                    float alpha = tex_white.GetPixel(x, y).r - tex_black.GetPixel(x, y).r;
                    alpha = 1.0f - alpha;
                    Color color;
                    if (alpha == 0)
                    {
                        color = Color.clear;
                    }
                    else
                    {
                        color = tex_black.GetPixel(x, y) / alpha;
                    }
                    color.a = alpha;
                    tex_transparent.SetPixel(x, y, color);
                }
            }

            // Encode the resulting output texture to a byte array then write to the file
            byte[] pngShot = ImageConversion.EncodeToPNG(tex_transparent);
            File.WriteAllBytes(screengrabfile_path, pngShot);

            cam.clearFlags = bak_cam_clearFlags;
            cam.targetTexture = bak_cam_targetTexture;
            RenderTexture.active = bak_RenderTexture_active;
            RenderTexture.ReleaseTemporary(render_texture);

            Texture2D.Destroy(tex_black);
            Texture2D.Destroy(tex_white);
            Texture2D.Destroy(tex_transparent);
        }

        public static void SimpleCaptureTransparentScreenshot(Camera cam, int width, int height, string screengrabfile_path)
        {
            // Depending on your render pipeline, this may not work.
            var bak_cam_targetTexture = cam.targetTexture;
            var bak_cam_clearFlags = cam.clearFlags;
            var bak_RenderTexture_active = RenderTexture.active;

            var tex_transparent = new Texture2D(width, height, TextureFormat.ARGB32, false);
            // Must use 24-bit depth buffer to be able to fill background.
            var render_texture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var grab_area = new Rect(0, 0, width, height);

            RenderTexture.active = render_texture;
            cam.targetTexture = render_texture;
            cam.clearFlags = CameraClearFlags.SolidColor;

            // Simple: use a clear background
            cam.backgroundColor = Color.clear;
            cam.Render();
            tex_transparent.ReadPixels(grab_area, 0, 0);
            tex_transparent.Apply();

            // Encode the resulting output texture to a byte array then write to the file
            byte[] pngShot = ImageConversion.EncodeToPNG(tex_transparent);
            File.WriteAllBytes(screengrabfile_path, pngShot);

            cam.clearFlags = bak_cam_clearFlags;
            cam.targetTexture = bak_cam_targetTexture;
            RenderTexture.active = bak_RenderTexture_active;
            RenderTexture.ReleaseTemporary(render_texture);

            Texture2D.Destroy(tex_transparent);
        }
    }
}