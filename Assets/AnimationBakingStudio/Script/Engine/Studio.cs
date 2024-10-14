using System;
using System.Collections.Generic;
using UnityEngine;

namespace ABS
{
    [DisallowMultipleComponent]
    public class Studio : MonoBehaviour
    {
        public ModelProperty model = new ModelProperty();
        public CameraProperty cam = new CameraProperty();
        public LightProperty lit = new LightProperty();
        public ViewProperty view = new ViewProperty();
        public ShadowProperty shadow = new ShadowProperty();
        public ExtractionProperty extraction = new ExtractionProperty();
        public PreviewProperty preview = new PreviewProperty();
        public FilmingProperty filming = new FilmingProperty();
        public TrimmingProperty triming = new TrimmingProperty();
        public PackingProperty packing = new PackingProperty();
        public OutputProperty output = new OutputProperty();
        public DirectoryProperty dir = new DirectoryProperty();
        public NamingProperty naming = new NamingProperty();

        public float appliedSubViewTurnAngle = 0;
        public string appliedSubViewName = "";

        [NonSerialized]
        public string[] atlasSizes = new string[] { "128", "256", "512", "1024", "2048", "4096", "8192" };

        [NonSerialized]
        public bool isSamplingReady = false;

        [NonSerialized]
        public Sampling sampling = null;

        [NonSerialized]
        public bool isBakingReady = false;
        [NonSerialized]
        public bool isDirectSaving = false;

        public bool IsSideView()
        {
            return view.slopeAngle == 0f;
        }

        public void AddModel(Model model)
        {
            bool assigned = false;
            for (int i = 0; i < this.model.list.Count; ++i)
            {
                if (this.model.list[i] == null)
                {
                    this.model.list[i] = model;
                    assigned = true;
                    break;
                }
            }

            if (!assigned)
                this.model.list.Add(model);
        }
    }
}
