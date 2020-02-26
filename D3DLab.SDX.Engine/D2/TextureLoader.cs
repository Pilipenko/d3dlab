using SharpDX.Direct3D11;
using SharpDX.WIC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace D3DLab.SDX.Engine.D2 {
    public class TextureLoader {
        readonly Device device;
        public TextureLoader(Device device) {
            this.device = device;
        }
        public ShaderResourceView LoadShaderResource(FileInfo file) {
            ShaderResourceView res = null;
            try {
                using (var texture = LoadFromFile(device, new SharpDX.WIC.ImagingFactory(), file.FullName)) {
                    var srvDesc = new ShaderResourceViewDescription() {
                        Format = texture.Description.Format,
                        Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D,
                    };
                    srvDesc.Texture2D.MostDetailedMip = 0;
                    srvDesc.Texture2D.MipLevels = -1;

                    res = new ShaderResourceView(device, texture, srvDesc);
                    device.ImmediateContext.GenerateMips(res);
                }
                // TextureResource = ShaderResourceView.FromFile(device, fileName);
            } catch (Exception ex) {
                System.Diagnostics.Trace.WriteLine($"TexturedLoader {ex.Message}");
            }
            return res;
        }

        public ShaderResourceView LoadBitmapShaderResource(System.Drawing.Bitmap btm) {
            ShaderResourceView res = null;
            try {
                var ms = new MemoryStream();
                btm.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;

                var factory = new SharpDX.WIC.ImagingFactory();
                var bitmapDecoder = new BitmapDecoder(factory, ms, DecodeOptions.CacheOnDemand);
                var result = new FormatConverter(factory);

                result.Initialize(bitmapDecoder.GetFrame(0), PixelFormat.Format32bppPRGBA, BitmapDitherType.None,
                    null, 0.0, BitmapPaletteType.Custom);

                using (var texture = CreateTexture2DFromBitmap(device, result)) {
                    var srvDesc = new ShaderResourceViewDescription() {
                        Format = texture.Description.Format,
                        Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D,
                    };
                    srvDesc.Texture2D.MostDetailedMip = 0;
                    srvDesc.Texture2D.MipLevels = -1;

                    res = new ShaderResourceView(device, texture, srvDesc);
                    device.ImmediateContext.GenerateMips(res);
                }
                // TextureResource = ShaderResourceView.FromFile(device, fileName);
            } catch (Exception ex) {
                System.Diagnostics.Trace.WriteLine($"TexturedLoader {ex.Message}");
            }
            return res;
        }

        Texture2D LoadFromFile(Device device, ImagingFactory factory, string fileName) {
            using (var bs = LoadBitmap(factory, fileName)) {
                return CreateTexture2DFromBitmap(device, bs);
            }
        }

        BitmapSource LoadBitmap(ImagingFactory factory, string filename) {
            var bitmapDecoder = new BitmapDecoder(factory, filename, DecodeOptions.CacheOnDemand);
            var result = new FormatConverter(factory);
            result.Initialize(bitmapDecoder.GetFrame(0), PixelFormat.Format32bppPRGBA, BitmapDitherType.None,
                null, 0.0, BitmapPaletteType.Custom);

            return result;
        }
        Texture2D CreateTexture2DFromBitmap(Device device, BitmapSource bitmapSource) {
            // Allocate DataStream to receive the WIC image pixels
            int stride = bitmapSource.Size.Width * 4;
            using (var buffer = new SharpDX.DataStream(bitmapSource.Size.Height * stride, true, true)) {
                // Copy the content of the WIC to the buffer
                bitmapSource.CopyPixels(stride, buffer);
                return new SharpDX.Direct3D11.Texture2D(device, new SharpDX.Direct3D11.Texture2DDescription() {
                    Width = bitmapSource.Size.Width,
                    Height = bitmapSource.Size.Height,
                    ArraySize = 1,
                    BindFlags = SharpDX.Direct3D11.BindFlags.ShaderResource | BindFlags.RenderTarget,
                    Usage = SharpDX.Direct3D11.ResourceUsage.Default,
                    CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None,
                    Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                    MipLevels = 1,
                    OptionFlags = ResourceOptionFlags.GenerateMipMaps, // ResourceOptionFlags.GenerateMipMap
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                }, new SharpDX.DataRectangle(buffer.DataPointer, stride));
            }
        }
       
    }
}
