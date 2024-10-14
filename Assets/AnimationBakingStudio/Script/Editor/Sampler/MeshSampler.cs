using UnityEngine;
using UnityEditor;

namespace ABS
{
	public class MeshSampler : Sampler
	{
		private readonly MeshModel meshModel;

		private readonly MeshAnimation animation;

		private readonly Vector3 modelBaseSize; // for dynamic simple shadow
		private readonly Vector3 simpleShadowBaseScale;

		public MeshSampler(Model model, MeshAnimation animation, Studio studio)
			: base(model, studio)
		{
			this.animation = animation;

			meshModel = model as MeshModel;

			if (studio.shadow.type == ShadowType.Simple && studio.shadow.simple.isDynamicScale)
			{
				modelBaseSize = model.GetSize();
				simpleShadowBaseScale = studio.shadow.obj.transform.localScale;
			}
		}

		protected override void Initialize_More()
		{
			if (studio.output.toMakeNormalMap)
				CapturingHelper.SetupNormalPixelsLists();
		}

		protected override void StopAndPlayModel()
		{
			meshModel.StopAndPlay(animation);
		}

		protected override float GetFrameInterval()
		{
			return animation.clip.length / studio.filming.numOfFrames;
		}

		protected override void SimulateModel()
		{
			float frameTime = GetFrameTime(frameIndex);
			meshModel.Simulate(animation, new Frame(frameIndex, frameTime));
		}

		protected override void OnCaptureFrame_()
		{
			if (studio.shadow.type == ShadowType.Simple && studio.shadow.simple.isDynamicScale)
				ShadowHelper.ScaleSimpleShadowDynamically(modelBaseSize, simpleShadowBaseScale, meshModel, studio);

			CapturingHelper.ReadAndKeepRawPixelsManagingShadow(meshModel, studio);
			if (studio.output.toMakeNormalMap)
			{
				float rotX = studio.view.slopeAngle;
				float rotY = studio.appliedSubViewTurnAngle;
				CapturingHelper.ReadAndKeepRawPixelsForNormal_AllFrames(meshModel, rotX, rotY, studio.shadow.obj);
			}
		}

		protected override void CreateSampling_More(int numOfFrames)
		{
			if (studio.output.toMakeNormalMap)
				studio.sampling.rawNormalTextures = new Texture2D[numOfFrames];
		}

		protected override void ExtractValidPixels_More(int frameIndex)
		{
			if (studio.output.toMakeNormalMap)
				studio.sampling.rawNormalTextures[frameIndex] = CapturingHelper.ExtractValidPixelsForNormal_OneFrame(frameIndex);
		}

		protected override float GetFrameTime(int fi)
		{
			float frameRatio = 0.0f;
			if (fi > 0 && fi < studio.filming.numOfFrames)
				frameRatio = (float)fi / (float)(studio.filming.numOfFrames - 1);

			return meshModel.GetTimeForRatio(animation.clip, frameRatio);
		}

		protected override void Finish_More()
		{
			if (studio.output.toMakeNormalMap)
				CapturingHelper.ClearNormalPixelsLists();
		}
	}
}
