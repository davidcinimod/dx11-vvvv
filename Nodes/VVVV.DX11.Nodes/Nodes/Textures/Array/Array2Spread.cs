﻿
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.Composition;


using SlimDX;
using SlimDX.Direct3D11;
//using SlimDX.DXGI;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;

using FeralTic.DX11.Geometry;
using FeralTic.DX11.Resources;
using FeralTic.DX11;
using VVVV.DX11.Nodes;
using VVVV.DX11;
using VVVV.DX11.Lib.Rendering;

namespace VVVV.DX11.Nodes
{
    [PluginInfo(Name = "GetSlice", Category = "DX11.TextureArray", Version = "", Author = "sebl")]
    public class Array2Spread : IPluginEvaluate, IDX11ResourceProvider, IDisposable
    {
        [Input("TextureArray In", IsSingle = true)]
        protected Pin<DX11Resource<DX11RenderTextureArray>> FTexIn;

        [Input("Index")]
        protected ISpread<int> FIndex;

        [Output("Textures Out")]
        protected ISpread<DX11Resource<DX11Texture2D>> FTextureOutput;

        int ArrayCount = 1;
        int numSlicesOut;

        public void Evaluate(int SpreadMax)
        {
            if (this.FTexIn.IsConnected)
            {
                //FTextureOutput.SliceCount = ArrayCount;
                this.numSlicesOut = this.FIndex.SliceCount;
                this.FTextureOutput.SliceCount = this.numSlicesOut;

                for (int i = 0; i < numSlicesOut; i++)
                {
                    if (this.FTextureOutput[i] == null)
                    {
                        this.FTextureOutput[i] = new DX11Resource<DX11Texture2D>();
                    }

                }
            }
            else
            {
                for (int i = 0; i < FTextureOutput.SliceCount; i++)
                {
                    this.FTextureOutput[i].Dispose();
                }
                this.ArrayCount = 1;
            }

            this.FTextureOutput.SliceCount = this.numSlicesOut;

            if (this.FTextureOutput.SliceCount > this.numSlicesOut)
            {
                for (int t = numSlicesOut; t < this.FTextureOutput.SliceCount; t++)
                {
                    this.FTextureOutput[t].Dispose();
                }
            }
        }

        
        public void Update(IPluginIO pin, DX11RenderContext context)
        {
            if (this.FTextureOutput.SliceCount == 0 || !FTexIn.IsConnected) { return; }

            if (FTexIn.IsConnected)
            {
                ArrayCount = FTexIn[0][context].ElemCnt;

                //for (int i = 0; i < numSlicesOut; i++)
                foreach( int i in FIndex)
                {
                    int slice = i % ArrayCount;

                    Texture2DDescription descIn;
                    Texture2DDescription descOut;

                    if (this.FTextureOutput[i].Contains(context))
                    {
                        descIn = FTexIn[0][context].Resource.Description;
                        descOut = this.FTextureOutput[i][context].Resource.Description;

                        if (descIn.Format != descOut.Format || descIn.Width != descOut.Width || descIn.Height != descOut.Height)
                        {
                            this.FTextureOutput[i].Dispose(context);
                            this.FTextureOutput[i] = new DX11Resource<DX11Texture2D>();
                            descIn.ArraySize = 1;
                            this.FTextureOutput[i][context] = DX11Texture2D.FromDescription(context, descIn);
                        }
                    }
                    else
                    {
                        this.FTextureOutput[i][context] = new DX11Texture2D();
                    }

                    descIn = this.FTexIn[0][context].Resource.Description;

                    if (this.FTextureOutput[i][context].Resource == null)
                    {
                        this.FTextureOutput[i].Dispose(context);
                        this.FTextureOutput[i] = new DX11Resource<DX11Texture2D>();
                        descIn.ArraySize = 1;
                        this.FTextureOutput[i][context] = DX11Texture2D.FromDescription(context, descIn);
                    }

                    SlimDX.Direct3D11.Resource source = this.FTexIn[0][context].Resource;
                    SlimDX.Direct3D11.Resource destination = this.FTextureOutput[i][context].Resource;

                    int sourceSubres = SlimDX.Direct3D11.Texture2D.CalculateSubresourceIndex(0, slice, descIn.MipLevels);
                    int destinationSubres = SlimDX.Direct3D11.Texture2D.CalculateSubresourceIndex(0, 0, 1);

                    context.CurrentDeviceContext.CopySubresourceRegion(source, sourceSubres, destination, destinationSubres, 0, 0, 0);
                }
            }
        }
        

        public void Dispose()
        {
            if (this.FTextureOutput.SliceCount > 0)
            {
                if (this.FTextureOutput[0] != null)
                {
                    for (int i = 0; i < FTextureOutput.SliceCount; i++)
                    {
                        this.FTextureOutput[i].Dispose();
                    }
                }
            }
        }


        public void Destroy(IPluginIO pin, DX11RenderContext context, bool force)
        {
            for (int i = 0; i < FTextureOutput.SliceCount; i++ )
            {
                this.FTextureOutput[i].Dispose(context);
            }
        }


    }
}