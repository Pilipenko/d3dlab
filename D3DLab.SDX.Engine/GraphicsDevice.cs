﻿using D3DLab.SDX.Engine.Shader;
using D3DLab.Std.Engine.Core;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using System;
using System.Diagnostics;

namespace D3DLab.SDX.Engine {

    public class SynchronizedGraphics : ISynchronizationContext {
        struct Size {
            public float Width;
            public float Height;
        }
        internal event Action<GraphicsDevice> Changed;
        internal GraphicsDevice Device;
        readonly IAppWindow window;
        readonly SynchronizationContext<SynchronizedGraphics, Size> synchronizer;

        public SynchronizedGraphics(IAppWindow window) {
            Device = new GraphicsDevice(window);
            window.Resized += OnResized;
            this.window = window;
            synchronizer = new SynchronizationContext<SynchronizedGraphics, Size>(this);
        }

        private void OnResized() {
            synchronizer.Add((_this, size) => {
                _this.Device.Resize(size.Width, size.Height);
                Changed(_this.Device);
            }, new Size { Height = window.Height, Width = window.Width });
        }

        internal void Dispose() {
            Device.Dispose();
        }

        public void Synchronize() {
            synchronizer.Synchronize();
        }
    }

    public sealed class AdapterFactory {
        public static event Func<Adapter[], int, Adapter> SelectAdapter;

        public static Adapter GetBestAdapter(global::SharpDX.DXGI.Factory f) {
            Adapter bestAdapter = null;
            var bestLevel = global::SharpDX.Direct3D.FeatureLevel.Level_11_1;

            var selectedId = -1;
            for (int i = 0; i < f.Adapters.Length; i++) {
                Adapter adapter = f.Adapters[i];
                var level = global::SharpDX.Direct3D11.Device.GetSupportedFeatureLevel(adapter);
                if (bestAdapter == null || level > bestLevel) {
                    selectedId = adapter.Description.DeviceId;
                    bestAdapter = adapter;
                    bestLevel = level;
                    break;
                }
            }

            if (SelectAdapter != null) {
                bestAdapter = SelectAdapter(f.Adapters, selectedId) ?? bestAdapter;
            }

            return bestAdapter;
        }
    }

    internal class GraphicsFrame : IDisposable {
        public readonly GraphicsDevice Graphics;
        readonly Stopwatch sw;
        TimeSpan spendTime;

        public GraphicsFrame(GraphicsDevice graphics) {
            this.Graphics = graphics;
            sw = new Stopwatch();
            sw.Start();
            graphics.Refresh();
        }


        public void Dispose() {
            Graphics.Present();
            sw.Stop();
            spendTime = sw.Elapsed;
        }
    }

    internal class GraphicsDevice {
        public readonly D3DShaderCompilator Compilator;

        internal SharpDX.Direct3D11.Device D3DDevice { get; private set; }
        internal DeviceContext ImmediateContext { get; private set; }
        RenderTargetView renderTargetView;
        DepthStencilView depthStencilView;

        readonly SwapChain swapChain;
        readonly IntPtr handle;

        public GraphicsDevice(IAppWindow window) {
            this.handle = window.Handle;

            Compilator = new D3DShaderCompilator();
            Compilator.AddIncludeMapping("Game", "D3DLab.SDX.Engine.Rendering.Shaders.Game.hlsl");
            Compilator.AddIncludeMapping("Light", "D3DLab.SDX.Engine.Rendering.Shaders.Light.hlsl");
            Compilator.AddIncludeMapping("Math", "D3DLab.SDX.Engine.Rendering.Shaders.Math.hlsl");

            var width = (int)window.Width;
            var height = (int)window.Height;

            var backBufferDesc = new ModeDescription(width, height, new Rational(60, 1), Format.R8G8B8A8_UNorm);

            // Descriptor for the swap chain
            var swapChainDesc = new SwapChainDescription() {
                ModeDescription = backBufferDesc,
                SampleDescription = new SampleDescription(1, 0),
                Usage = Usage.RenderTargetOutput,
                BufferCount = 2,
                OutputHandle = handle,
                IsWindowed = true
            };

            var factory = new Factory1();
            var adapter = AdapterFactory.GetBestAdapter(factory);

            // Create device and swap chain
            SharpDX.Direct3D11.Device.CreateWithSwapChain(adapter, DeviceCreationFlags.Debug, swapChainDesc, out var d3dDevice, out var sch);

            swapChain = sch.QueryInterface<SwapChain4>();
            D3DDevice = d3dDevice.QueryInterface<Device5>();
            
            ImmediateContext = d3dDevice.ImmediateContext;

            CreateBuffers(width, height);

            //TODO: Динамический оверлей. Direct3D 11.2 https://habr.com/company/microsoft/blog/199380/
            //swapChain.SetSourceSize
            //DContext = new DeviceContext(D3DDevice);
        }

        public void Dispose() {
            renderTargetView.Dispose();
            D3DDevice.Dispose();
            swapChain.Dispose();
        }

        public void Resize(float w, float h) {
            var width = (int)w;
            var height = (int)h;

            renderTargetView.Dispose();
            depthStencilView.Dispose();

            swapChain.ResizeBuffers(2, width, height, Format.R8G8B8A8_UNorm, SwapChainFlags.None);

            CreateBuffers(width, height);
        }

        void CreateBuffers(int width, int height) {
            using (Texture2D backBuffer = swapChain.GetBackBuffer<Texture2D>(0)) {
                renderTargetView = new RenderTargetView(D3DDevice, backBuffer);
            }

            var zBufferTextureDescription = new Texture2DDescription {
                Format = Format.D16_UNorm,
                ArraySize = 1,
                MipLevels = 1,
                Width = width,
                Height = height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            };

            using (var zBufferTexture = new Texture2D(D3DDevice, zBufferTextureDescription)) {
                depthStencilView = new DepthStencilView(D3DDevice, zBufferTexture);
            }
            var depthDisabledStencilDesc = new DepthStencilStateDescription() {
                // true - correct overlap objects based on depth 
                // false - overlap based on rendering order
                IsDepthEnabled = true,

                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.Less,
                IsStencilEnabled = true,
                StencilReadMask = 0xFF,
                StencilWriteMask = 0xFF,
                // Stencil operation if pixel front-facing.
                FrontFace = new DepthStencilOperationDescription() {
                    FailOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Increment,
                    PassOperation = StencilOperation.Keep,
                    Comparison = Comparison.Always
                },
                // Stencil operation if pixel is back-facing.
                BackFace = new DepthStencilOperationDescription() {
                    FailOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Decrement,
                    PassOperation = StencilOperation.Keep,
                    Comparison = Comparison.Always
                }
            };
            var depthDisabledStencilState = new DepthStencilState(D3DDevice, depthDisabledStencilDesc);

            var viewport = new Viewport(0, 0, width, height);
            ImmediateContext.Rasterizer.SetViewport(viewport);
            ImmediateContext.OutputMerger.SetTargets(depthStencilView, renderTargetView);

            //no zbuffer and DepthStencil
            //ImmediateContext.OutputMerger.SetRenderTargets(renderTargetView);

            //with zbuffer / DepthStencil
            ImmediateContext.OutputMerger.SetDepthStencilState(depthDisabledStencilState, 0);

            //var blendStateDesc = new BlendStateDescription();
            //blendStateDesc.RenderTarget[0].IsBlendEnabled = true;
            //blendStateDesc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
            //blendStateDesc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
            //blendStateDesc.RenderTarget[0].BlendOperation = BlendOperation.Add;
            //blendStateDesc.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
            //blendStateDesc.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
            //blendStateDesc.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            //blendStateDesc.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
            //var blend = new BlendState(Device, blendStateDesc);

            //var blendFactor = new Color4(0, 0, 0, 0);
            //Device.ImmediateContext.OutputMerger.SetBlendState(blend, blendFactor, -1);
        }

        public GraphicsFrame FrameBegin() {
            return new GraphicsFrame(this);
        }

        internal void UpdateRasterizerState(RasterizerStateDescription descr) {
            ImmediateContext.Rasterizer.State = new RasterizerState(D3DDevice, descr);
        }

        public void Refresh() {
            ImmediateContext.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth, 1f, 0);
            ImmediateContext.ClearRenderTargetView(renderTargetView, new RawColor4(0, 0, 0, 0));
        }

        public static bool IsDirectX11Supported() {
            return global::SharpDX.Direct3D11.Device.GetSupportedFeatureLevel() == FeatureLevel.Level_11_0;
        }

        public void Present() {
            swapChain.Present(1, PresentFlags.None);
            //swapChain.Present(1, PresentFlags.None, new PresentParameters());
        }

        void Resize(uint width, uint height) {
            float _pixelScale = 1;
            uint actualWidth = (uint)(width * _pixelScale);
            uint actualHeight = (uint)(height * _pixelScale);
            swapChain.ResizeBuffers(2, (int)actualWidth, (int)actualHeight, Format.B8G8R8A8_UNorm, SwapChainFlags.None);

            // Get the backbuffer from the swapchain
            //using (Texture2D backBufferTexture = swapChain.GetBackBuffer<Texture2D>(0)) {
            //    if (_depthFormat != null) {
            //        TextureDescription depthDesc = new TextureDescription(
            //            actualWidth, actualHeight, 1, 1, 1,
            //            _depthFormat.Value,
            //            TextureUsage.DepthStencil,
            //            TextureType.Texture2D);
            //        _depthTexture = new D3D11Texture(_device, ref depthDesc);
            //    }

            //    D3D11Texture backBufferVdTexture = new D3D11Texture(backBufferTexture);
            //    FramebufferDescription desc = new FramebufferDescription(_depthTexture, backBufferVdTexture);
            //    _framebuffer = new D3D11Framebuffer(_device, ref desc);
            //    _framebuffer.Swapchain = this;
            //}
        }



        public SharpDX.Direct3D11.Buffer CreateBuffer<T>(BindFlags flags, ref T range)
            where T : struct {
            return SharpDX.Direct3D11.Buffer.Create(D3DDevice, flags, ref range);
        }
        public SharpDX.Direct3D11.Buffer CreateBuffer<T>(BindFlags flags, T[] range)
           where T : struct {
            return SharpDX.Direct3D11.Buffer.Create(D3DDevice, flags, range);
        }
        public SharpDX.Direct3D11.Buffer CreateBuffer<T>(T[] range, BufferDescription desc)
          where T : struct {
            return SharpDX.Direct3D11.Buffer.Create(D3DDevice, range, desc);
        }
        /// <summary>
        /// For referrence types and any arrays
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="range"></param>
        /// <param name="structureByteStride"></param>
        /// <param name="sizeInBytes"></param>
        /// <returns></returns>
        public SharpDX.Direct3D11.Buffer CreateDynamicBuffer<T>(T[] range, int sizeInBytes)
         where T : struct {
            return SharpDX.Direct3D11.Buffer.Create(D3DDevice, range, new BufferDescription {
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                Usage = ResourceUsage.Dynamic,
                //  StructureByteStride = structureByteStride,
                SizeInBytes = sizeInBytes
            });
        }
        //SharpDX.DataBox src = context.MapSubresource(lightDataBuffer, 1, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out SharpDX.DataStream stream);
        //stream.Write(light.GetStructLayoutResource());
        //context.UnmapSubresource(lightDataBuffer, LightStructLayout.RegisterResourceSlot);

        public void UpdateDynamicBuffer<T>(T[] newdata, SharpDX.Direct3D11.Buffer buffer, int slot) where T : struct {
            SharpDX.DataStream stream = null;
            try {
                SharpDX.DataBox src = ImmediateContext.MapSubresource(buffer, slot, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);
                stream.WriteRange(newdata);
                ImmediateContext.UnmapSubresource(buffer, slot);
            } finally {

            }
        }

        public void UpdateSubresource<T>(ref T data, SharpDX.Direct3D11.Buffer buff, int subresource) where T : struct {
            ImmediateContext.UpdateSubresource(ref data, buff, subresource);
        }


    }
}
