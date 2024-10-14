using UnityEngine;
using UnityEditor;

namespace ABS
{
	public class ParticleSampler : Sampler
	{
		private readonly ParticleModel particleModel;

		private Projectile projectile;

		public ParticleSampler(Model model, Studio studio)
			: base(model, studio)
		{
			particleModel = model as ParticleModel;
		}

		protected override void Initialize_More()
		{
			if (EditorApplication.isPlaying && particleModel.isProjectile)
			{
				projectile = particleModel.gameObject.AddComponent<Projectile>();
				projectile.MovingVector = particleModel.projectileVector;
			}
		}

		protected override void StopAndPlayModel()
		{
			particleModel.StopAndPlay();
		}

		protected override float GetFrameInterval()
		{
			return particleModel.duration / studio.filming.numOfFrames;
		}

		protected override void SimulateModel()
		{
			particleModel.Simulate(frameIndex == 0, studio.filming.numOfFrames);
		}

		protected override void OnCaptureFrame_()
		{
			CapturingHelper.ReadAndKeepRawPixelsManagingShadow(particleModel, studio);
		}

		protected override void Finish_More()
		{
			if (EditorApplication.isPlaying && particleModel.isProjectile)
				GameObject.Destroy(projectile);
		}
	}
}
