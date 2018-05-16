namespace Veldrid.MTL
{
    // A fake Texture object representing swapchain Textures.
    internal class MTLPlaceholderTexture : Texture
    {
        private uint _width;
        private uint _height;

        public override PixelFormat Format { get; }

        public override uint Width => _width;

        public override uint Height => _height;

        public override uint Depth => 1;

        public override uint MipLevels => 1;

        public override uint ArrayLayers => 1;

        public override TextureUsage Usage => TextureUsage.RenderTarget;

        public override TextureType Type => TextureType.Texture2D;

        public override TextureSampleCount SampleCount => TextureSampleCount.Count1;

        public override string Name { get; set; }

        public void Resize(uint width, uint height)
        {
            _width = width;
            _height = height;
        }

        public override void Dispose()
        {
        }
    }
}