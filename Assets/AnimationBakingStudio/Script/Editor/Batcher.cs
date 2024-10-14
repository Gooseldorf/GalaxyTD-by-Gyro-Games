using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ABS
{
	public class Batcher
	{
		private readonly List<Model> modelList;
		private readonly Studio studio;
		private readonly bool isDirectSaving;

		private CompletionCallback completion;

		public int ModelIndex { get; set; }

		public Baker CurrentBaker { get; set; }

		private string rootFolderPath = "";

		private bool toPassFinalUpdate = true;

		public Batcher(List<Model> modelList, Studio studio, bool isDirectSaving)
		{
			this.modelList = modelList;
			this.studio = studio;
			this.isDirectSaving = isDirectSaving;
		}

		public void Batch(CompletionCallback completion)
        {
			this.completion = completion;

			Debug.Assert(modelList.Count > 0);

			if (modelList.Count > 1)
            {
                int assetRootIndex = studio.dir.exportPath.IndexOf("Assets");
				string dirPath = studio.dir.exportPath.Substring(assetRootIndex);
                rootFolderPath = Path.Combine(dirPath, PathHelper.MakeDateTimeString());
                Directory.CreateDirectory(rootFolderPath);
            }

			ModelIndex = 0;

			EditorApplication.update -= UpdateState;
			EditorApplication.update += UpdateState;

			EditorUtility.DisplayProgressBar("Progress...", "Ready...", 0.0f);
		}

		private Model NextModel()
        {
			for (int i = ModelIndex; i < modelList.Count; ++i)
            {
				if (modelList[i] != null)
				{
					ModelIndex = i;
					return modelList[i];
                }
            }
			return null;
        }

		public void UpdateState()
		{
			try
			{
				if (CurrentBaker != null)
                {
                    if (CurrentBaker.IsInProgress())
					{
						CurrentBaker.UpdateState();

						if (CurrentBaker.IsCancelled)
							throw new Exception("Cancelled");
						return;
					}
					else
					{
						EditorUtility.ClearProgressBar();

						ModelIndex++;
					}
				}

				Model model = NextModel();
				if (model != null)
                {
					string sIndex = "";
					if (modelList.Count > 1)
						sIndex = ModelIndex.ToString().PadLeft((modelList.Count + 1).ToString().Length, '0');

					for (int i = 0; i < modelList.Count; ++i)
						modelList[i].gameObject.SetActive(false);
					model.gameObject.SetActive(true);

					if (isDirectSaving)
                    {
						CurrentBaker = new RuntimeBaker(model, studio, rootFolderPath);
                    }
					else
					{
						if (Model.IsMeshModel(model))
						{
							MeshModel meshModel = Model.AsMeshModel(model);
							List<MeshAnimation> animations = meshModel.GetValidAnimations();

							if (animations.Count > 0)
								CurrentBaker = new MeshBaker(model, animations, studio, sIndex, rootFolderPath);
							else
								CurrentBaker = new StaticBaker(model, studio, sIndex, rootFolderPath);
						}
						else if (Model.IsParticleModel(model))
						{
							CurrentBaker = new ParticleBaker(model, studio, sIndex, rootFolderPath);
						}
					}
				}
				else
				{
					EditorUtility.FocusProjectWindow();

					if (toPassFinalUpdate)
					{
						toPassFinalUpdate = false;
						return;
					}

					Finish();
				}
			}
			catch (Exception e)
            {
				Debug.LogException(e);
				Finish();
			}
		}

		public void Finish()
        {
			EditorApplication.update -= UpdateState;

			EditorUtility.ClearProgressBar();

			AssetDatabase.Refresh();

			CurrentBaker = null;

			completion();
		}
	}
}
