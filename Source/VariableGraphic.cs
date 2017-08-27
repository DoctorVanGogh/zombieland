using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace ZombieLand
{

    public class DynamicMaterial : Material, IDisposable {

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                Destroy(this);
                disposedValue = true;
            }
        }
        
        ~DynamicMaterial() {          
           Dispose(false);
        }
       
        public void Dispose() {
           
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        public DynamicMaterial(string contents) : base(contents) {
        }

        public DynamicMaterial(Shader shader) : base(shader) {
        }

        public DynamicMaterial(Material source) : base(source) {
        }
    }


    public class PreparedMaterial : IDisposable
	{
        DynamicMaterial material;
		MaterialRequest req;
		ColorData data;

	    private bool disposed;

	    public PreparedMaterial(MaterialRequest req, ColorData data)
		{
			this.req = req;
			this.data = data;
		}

		public Material GetMaterial
		{
			get {
			    if (disposed)
			        throw new ObjectDisposedException(ToString());

				if (material == null)
				{
					var mainTex = data.ToTexture();
					data = null;
					material = new DynamicMaterial(req.shader)
					{
						name = req.shader.name + "_" + mainTex.name,
						mainTexture = mainTex,
						color = req.color
					};
					if (req.maskTex != null)
					{
						material.SetTexture(ShaderPropertyIDs.MaskTex, req.maskTex);
						material.SetColor(ShaderPropertyIDs.ColorTwo, req.colorTwo);
					}
				}
				return material;
			}
		}

	    public void Dispose() {
	        Dispose(true);
	    }

        private void Dispose(bool v) {
            if (!disposed) {
                material?.Dispose();
                material = null;
            }

            disposed = true;
        }
    }

	public class VariableGraphic : Graphic, IDisposable
	{
		private PreparedMaterial[] mats = new PreparedMaterial[3];
		private int hash;

		public string GraphicPath => path;
		public override Material MatSingle => mats[2].GetMaterial;
		public override Material MatFront => mats[2].GetMaterial;
		public override Material MatSide => mats[1].GetMaterial;
		public override Material MatBack => mats[0].GetMaterial;
		public override bool ShouldDrawRotated => MatSide == MatBack;

		public override void Init(GraphicRequest req)
		{
			data = req.graphicData;
			path = req.path;
			color = req.color;
			colorTwo = req.colorTwo;
			drawSize = req.drawSize;

			hash = Gen.HashCombine(hash, path);
			hash = Gen.HashCombineStruct(hash, color);
			hash = Gen.HashCombineStruct(hash, colorTwo);

			var bodyColor = GraphicToolbox.RandomSkinColorString();
			mats = new ColorData[]
			{
				GraphicsDatabase.GetColorData(req.path + "_back", bodyColor, true),
				GraphicsDatabase.GetColorData(req.path + "_side", bodyColor, true),
				GraphicsDatabase.GetColorData(req.path + "_front", bodyColor, true)
			}
			.Select(data =>
			{
				var points = ZombieStains.maxStainPoints;
				while (points > 0)
				{
					var stain = ZombieStains.GetRandom(points, req.path.Contains("Naked"));
					data.ApplyStains(stain.Key, Rand.Bool, Rand.Bool);
					points -= stain.Value;

					hash = Gen.HashCombine(hash, stain);
				}

				var request = new MaterialRequest
				{
					mainTex = null, // will be calculated lazy from 'data'
					shader = req.shader,
					color = color,
					colorTwo = colorTwo,
					maskTex = null
				};
				return new PreparedMaterial(request, data);
			})
			.ToArray();
		}

		public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
		{
			return this;
		}

		public override string ToString()
		{
			return string.Concat(new object[]
			{
				"Multi(initPath=",
				path,
				", color=",
				color,
				", colorTwo=",
				colorTwo,
				")"
			});
		}

		public override int GetHashCode()
		{
			return hash;
		}

	    public void Dispose() {
            Dispose(true);

	    }

        private void Dispose(bool v) {
            if (mats != null) {
                // clean way would need to trigger objectdisposedexceptions on use after calling tis - but what the hell.. running into an empty array should error too
                foreach (var preparedMaterial in mats) {
                    preparedMaterial.Dispose();
                }
                mats = null;
            }
        }
    }
}