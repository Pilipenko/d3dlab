using D3DLab.ECS;
using D3DLab.ECS.Common;
using D3DLab.SDX.Engine.D2;
using D3DLab.SDX.Engine.Shader;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace D3DLab.SDX.Engine {
    public class GraphicsDeviceException : Exception {
        GraphicsDeviceException(string mess) : base(mess) { }
        public static Exception ResourseSlotAlreadyUsed(int slot) { return new GraphicsDeviceException($"Resourse Slot '{slot}' is already used."); }
        public static Exception ShaderAddedTwice() { return new GraphicsDeviceException($"Shader war added twise to DeviceContext."); }
    }

    sealed class AdapterFactory {
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

    public class GraphicsFrame : IDisposable {
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

    public class GraphicsDevice {
        class ResourseRegistrHash {
            readonly Dictionary<int, HashSet<int>> shaders;

            public ResourseRegistrHash() {
                shaders = new Dictionary<int, HashSet<int>>();
            }

            public void RegisterShader(int ptr) {
                if (shaders.ContainsKey(ptr)) {
                    throw GraphicsDeviceException.ShaderAddedTwice();
                }
                shaders.Add(ptr, new HashSet<int>());
            }
            public void RegisterResourseSlot(int ptr, int slot) {
                if (shaders[ptr].Contains(slot)) {
                    throw GraphicsDeviceException.ResourseSlotAlreadyUsed(slot);
                }
                shaders[ptr].Add(slot);
            }
            public void Clear() {
                shaders.Clear();
            }
        }

        public readonly D3DShaderCompilator Compilator;

        public TextureLoader TexturedLoader { get; }
        public SharpDX.Direct3D11.Device D3DDevice { get; private set; }
        public DeviceContext ImmediateContext { get; private set; }
        public string VideoCardDescription { get; }

        RenderTargetView renderTargetView;
        DepthStencilView depthStencilView;

        readonly SwapChain swapChain;
        readonly IntPtr handle;

        readonly ResourseRegistrHash resourseHash;

        public GraphicsDevice(IAppWindow window) {
            resourseHash = new ResourseRegistrHash();

            this.handle = window.Handle;

            Compilator = new D3DShaderCompilator();           

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

            VideoCardDescription = adapter.Description.Description.Trim('\0');


            /*
             * 
             *  DeviceCreationFlags.Debug - not supported by default, need to install the optional feature Graphics Tools
             * 
             */

            // Create device and swap chain
            SharpDX.Direct3D11.Device.CreateWithSwapChain(adapter, DeviceCreationFlags.None, swapChainDesc, out var d3dDevice, out var sch);

            swapChain = sch.QueryInterface<SwapChain4>();
            D3DDevice = d3dDevice.QueryInterface<Device5>();

            ImmediateContext = d3dDevice.ImmediateContext;

            CreateBuffers(width, height);

            //TODO: Динамический оверлей. Direct3D 11.2 https://habr.com/company/microsoft/blog/199380/
            //swapChain.SetSourceSize
            //DContext = new DeviceContext(D3DDevice);

            TexturedLoader = new TextureLoader(D3DDevice);

        }

        public void Dispose() {
            renderTargetView.Dispose();

            ImmediateContext.ClearState();
            ImmediateContext.Flush();
            ImmediateContext.Dispose();

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

            var depthEnabledStencilState = new DepthStencilState(D3DDevice, D3DDepthStencilStateDescriptions.DepthEnabled);

            var viewport = new SharpDX.Viewport(0, 0, width, height);
            ImmediateContext.Rasterizer.SetViewport(viewport);
            ImmediateContext.OutputMerger.SetTargets(depthStencilView, renderTargetView);

            //no zbuffer and DepthStencil
            //ImmediateContext.OutputMerger.SetRenderTargets(renderTargetView);

            //with zbuffer / DepthStencil
            ImmediateContext.OutputMerger.SetDepthStencilState(depthEnabledStencilState, 0);

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
            resourseHash.Clear();
            return new GraphicsFrame(this);
        }

        public void UpdateRasterizerState(RasterizerStateDescription descr) {
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

        public System.Drawing.Bitmap CopyBackBufferTexture() {
            using (var stream = new MemoryStream()) {
                using (var tex = swapChain.GetBackBuffer<Texture2D>(0)) {
                    Copy(tex, stream, D3DDevice);
                    stream.Position = 0;
                    var bmp = new System.Drawing.Bitmap(stream);
                    return bmp;
                }                
            }
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

        static void Copy(Texture2D texture, Stream stream, SharpDX.Direct3D11.Device device) {
            var desc = new Texture2DDescription {
                Width = (int)texture.Description.Width,
                Height = (int)texture.Description.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = texture.Description.Format,
                Usage = ResourceUsage.Staging,
                SampleDescription = new SampleDescription(1, 0),
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                OptionFlags = ResourceOptionFlags.None
            };

            using (var factory = new SharpDX.WIC.ImagingFactory()) {
                using (var textureCopy = new Texture2D(device, desc)) {
                    device.ImmediateContext.CopyResource(texture, textureCopy);

                    var dataBox = device.ImmediateContext.MapSubresource(
                        textureCopy,
                        0,
                        0,
                        MapMode.Read,
                        global::SharpDX.Direct3D11.MapFlags.None,
                        out SharpDX.DataStream dataStream);
                    using (dataStream) {
                        var t = dataStream.ReadByte(); //ReadFloat();

                        var dataRectangle = new SharpDX.DataRectangle {
                            DataPointer = dataStream.DataPointer,
                            Pitch = dataBox.RowPitch
                        };

                        using (var bitmap = new SharpDX.WIC.Bitmap(factory, textureCopy.Description.Width, textureCopy.Description.Height,
                            SharpDX.WIC.PixelFormat.Format32bppRGBA, dataRectangle, 0)) {

                            stream.Position = 0;
                            using (var bitmapEncoder = new SharpDX.WIC.PngBitmapEncoder(factory, stream)) {
                                using (var bitmapFrameEncode = new SharpDX.WIC.BitmapFrameEncode(bitmapEncoder)) {
                                    bitmapFrameEncode.Initialize();
                                    bitmapFrameEncode.SetSize(bitmap.Size.Width, bitmap.Size.Height);
                                    var pixelFormat = SharpDX.WIC.PixelFormat.FormatDontCare;
                                    bitmapFrameEncode.SetPixelFormat(ref pixelFormat);
                                    bitmapFrameEncode.WriteSource(bitmap);
                                    bitmapFrameEncode.Commit();
                                    bitmapEncoder.Commit();
                                }
                            }

                        }
                        device.ImmediateContext.UnmapSubresource(textureCopy, 0);
                    }
                }
            }
        }


        public SharpDX.Direct3D11.Buffer CreateBuffer<T>(BindFlags flags, ref T range)
            where T : struct {
            BufferStaticVerifications.CheckSizeInBytes<T>();
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
        public SharpDX.Direct3D11.Buffer CreateBuffer<T>(ref T data, BufferDescription desc)
         where T : struct {
            BufferStaticVerifications.CheckSizeInBytes(desc.SizeInBytes);
            return SharpDX.Direct3D11.Buffer.Create(D3DDevice, ref data, desc);
        }

        public SharpDX.Direct3D11.Buffer CreateDynamicBuffer<T>(ref T range, int sizeInBytes)
        where T : struct {
            BufferStaticVerifications.CheckSizeInBytes(sizeInBytes);

             var des = new BufferDescription() {
                Usage = ResourceUsage.Dynamic,
                SizeInBytes = sizeInBytes,
                BindFlags = BindFlags.ConstantBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                StructureByteStride = 0
            };
            return SharpDX.Direct3D11.Buffer.Create(D3DDevice, ref range, des);
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

        public void UpdateDynamicBuffer<T>(ref T newdata, SharpDX.Direct3D11.Buffer buffer) where T : struct {
            ImmediateContext.MapSubresource(buffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out var mappedResource);
            using (mappedResource) {
                mappedResource.Write(newdata);
                ImmediateContext.UnmapSubresource(buffer, 0);//TODO: remove slot value
            }
        }

        public void UpdateArraySubresource<T>(T[] data, SharpDX.Direct3D11.Buffer buff) where T : struct {
            ImmediateContext.UpdateSubresource(data, buff);
        }
        public void UpdateSubresource<T>(ref T data, SharpDX.Direct3D11.Buffer buff, int subresource) where T : struct {
            ImmediateContext.UpdateSubresource(ref data, buff, subresource);
        }
        public void UpdateSubresource<T>(ref T data, SharpDX.Direct3D11.Buffer buff) where T : struct {
            ImmediateContext.UpdateSubresource(ref data, buff);
        }

        public void RegisterConstantBuffer(CommonShaderStage stage, int slot, SharpDX.Direct3D11.Buffer buff) {
            stage.SetConstantBuffer(slot, buff);
            resourseHash.RegisterResourseSlot(stage.GetHashCode(), slot);
        }
        public void RegisterConstantBuffer(CommonShaderStage stage, int slot, DisposableSetter<SharpDX.Direct3D11.Buffer> buff) {
            RegisterConstantBuffer(stage, slot, buff.Get());
        }

        public void SetVertexShader(DisposableSetter<VertexShader> shader) {
            ImmediateContext.VertexShader.Set(shader.Get());
            resourseHash.RegisterShader(ImmediateContext.VertexShader.GetHashCode());
        }
        public void SetPixelShader(DisposableSetter<PixelShader> shader) {
            ImmediateContext.PixelShader.Set(shader.Get());
            resourseHash.RegisterShader(ImmediateContext.PixelShader.GetHashCode());
        }

        public SamplerState CreateSampler(SamplerStateDescription desc) {
            return new SamplerState(D3DDevice, desc);
        }


    }
}
