using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D3DLab.Wpf.Engine.App.Systems {
    using System.Collections.Immutable;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Threading.Tasks;
    using D3DLab.ECS;
    using D3DLab.ECS.Components;
    using D3DLab.ECS.Ext;
    using D3DLab.Std.Engine.Core;
    using D3DLab.Std.Engine.Core.Common;
    using D3DLab.Std.Engine.Core.Components;
    using D3DLab.Std.Engine.Core.Ext;
    using D3DLab.Std.Engine.Core.Systems;
    using D3DLab.Std.Engine.Core.Utilities;
    using D3DLab.Wpf.Engine.App.D3D.Components;
    using SharpNoise;
    using SharpNoise.Builders;
    using SharpNoise.Modules;
    using SharpNoise.Utilities.Imaging;

    public interface ITerrainComponent : IGraphicComponent { }

    public class TerrainGeneratorSystem : BaseEntitySystem, IGraphicSystem, IGraphicSystemContextDependent {
        public IContextState ContextState { get; set; }

        protected override void Executing(ISceneSnapshot ss) {
            var snapshot = (SceneSnapshot)ss;
            var emanager = ContextState.GetEntityManager();
            foreach (var entity in emanager.GetEntities()) {
                var coms = entity.GetComponents<ITerrainComponent>();
                if (!coms.Any()) {
                    continue;
                }
                var conf = coms.OfType<TerrainConfigurationComponent>().Single();
                if (conf.IsModified) {
                    entity.RemoveComponents<TerrainGeneratorComponent>();
                    conf.IsModified = false;
                    var generator = new TerrainGeneratorComponent(conf.Width, conf.Height, conf);
                    generator.StartGeneratingAsync();
                    entity.AddComponent(generator);
                    continue;
                }
                var generating = coms.OfType<TerrainGeneratorComponent>().SingleOrDefault();
                if (generating.IsNull()) {
                    continue;
                }
                if (generating.IsGenerated) {
                    entity.RemoveComponent(generating);

                    var geo = entity.GetComponents<TerrainGeometryCellsComponent>();
                    if (geo.Any()) {
                        entity.RemoveComponent(geo.First());
                    }

                    var newgeo = new TerrainGeometryCellsComponent();

                    newgeo.MaxHeight = generating.MaxHeight;
                    newgeo.MinHeight = generating.MinHeight;

                    BuildGeometry(generating.HeightMap, conf, newgeo);

                    entity.AddComponent(newgeo);

                    conf.Texture = generating.Texture;

                    var box = BoundingBox.CreateFromVertices(newgeo.Positions.ToArray());

                    entity.UpdateComponent(TransformComponent.Create(Matrix4x4.CreateTranslation(-box.GetCenter())));

                    entity.GetComponent<IRenderableComponent>().CanRender = true;

                    BuildCellsAsync(newgeo, conf);

                    break;
                }
            }
        }

        static void BuildGeometry(Vector3[] HeightMap, TerrainConfigurationComponent config, TerrainGeometryCellsComponent geometry) {
            var width = config.Width;
            var height = config.Height;
            var count = (width - 1) * (height - 1) * 6;
            var indices = new int[count];
            var vertices = HeightMap;//.ToArray();
            var index = 0;
            for (int j = 0; j < (height - 1); j++) {
                var row = height * j;
                var row2 = height * (j + 1);
                for (int i = 0; i < (width - 1); i++) {
                    var indx1 = row + i;
                    var indx2 = row + i + 1;
                    var indx3 = row2 + i;
                    var indx4 = row2 + i + 1;

                    indices[index++] = indx1;
                    indices[index++] = indx2;
                    indices[index++] = indx3;

                    indices[index++] = indx2;
                    indices[index++] = indx4;
                    indices[index++] = indx3;
                }
            }
            var normals = vertices.CalculateNormals(indices);
            geometry.Indices = indices.ToImmutableArray();
            geometry.Positions = vertices.ToImmutableArray();
            geometry.Normals = normals.ToImmutableArray();
            CalculateTextureCoordinates(HeightMap, width, height, config.TextureRepeat, out var t1, out var t2);
            geometry.TextureCoordinates = t1.ToImmutableArray();
            geometry.NormalMapTexCoordinates = t2;
            geometry.Color = V4Colors.White;

            ComputeTangents(geometry);

            //Light.SetAmbientColor(0.05f, 0.05f, 0.05f, 1.0f);
            //Light.SetDiffuseColor(1.0f, 1.0f, 1.0f, 1.0f);
        }
        static void CalculateTextureCoordinates(Vector3[] HeightMap, int width, int height, int textureRepeat,
            out Vector2[] texture, out Vector2[] texture2) {
            // Calculate how much to increment the texture coordinates by.
            float incrementValue = (float)textureRepeat / (float)width;

            // Calculate how many times to repeat the texture.
            int incrementCount = width / textureRepeat;

            // Initialize the tu and tv coordinate values.
            float tuCoordinate = 0.0f;
            float tvCoordinate = 0.0f;

            // Initialize the tu and tv coordinate indexes.
            int tuCount = 0;
            int tvCount = 0;

            texture = new Vector2[HeightMap.Length];
            texture2 = new Vector2[HeightMap.Length];

            var tvIncrementValue = incrementValue;
            var tuIncrementValue = incrementValue;


            //for (int j = 0; j < height; j++) {
            //    for (int i = 0; i < width; i++) {
            //        texture[i + width * j] = new Vector2(tuCoordinate, tvCoordinate);
            //        tuCoordinate += tuIncrementValue;
            //    }
            //    tvCoordinate += tvIncrementValue;
            //    tuCoordinate = 0;
            //}
            //return texture;

            // Setup the increment size for the second set of textures.
            // This is a fixed 33x33 vertex array per cell so there will be 32 rows of quads in a cell.
            float quadsConvered = 32.0f;
            float incrementSize = 1.0f / quadsConvered;

            float tu2Left = 0.0f;
            float tu2Right = incrementSize;
            float tv2Top = 0.0f;
            float tv2Bottom = incrementSize;

            // Loop through the entire height map and calculate the tu and tv texture coordinates for each vertex.
            for (int j = 0; j < height; j++) {
                for (int i = 0; i < width; i++) {
                    // Store the texture coordinate in the height map.
                    texture[(height * j) + i] = new Vector2(tuCoordinate, tvCoordinate);
                    texture2[(height * j) + i] = new Vector2(tu2Left, tv2Top);

                   // texture[(height * j) + i] = new Vector2(i,j);

                    // Increment the tu texture coordinate by the increment value and increment the index by one.
                    tuCoordinate += tuIncrementValue;
                    tuCount++;

                    // Check if at the far right end of the texture and if so then start at the beginning again.
                    if (tuCount == incrementCount) {
                        tuIncrementValue *= -1;
                        //tuCoordinate = 0.0f;
                        tuCount = 0;
                    }

                    // Increment the second tu texture coords.
                    tu2Left += incrementSize;
                    tu2Right += incrementSize;

                    // Reset the second tu texture coordinate increments.
                    if (tu2Right > 1.0f) {
                        tu2Left = 0.0f;
                        tu2Right = incrementSize;
                    }
                }
                // Increment the tv texture coordinate by the increment value and increment the index by one.
                tvCoordinate -= tvIncrementValue;
                tvCount++;

                // Check if at the top of the texture and if so then start at the bottom again.
                if (tvCount == incrementCount) {
                    tvIncrementValue *= -1;
                    //tvCoordinate = 1.0f;
                    tvCount = 0;
                }

                // Increment the second tv texture coords.
                tv2Top += incrementSize;
                tv2Bottom += incrementSize;

                // Reset the second tu texture coordinate increments.
                if (tv2Bottom > 1.0f) {
                    tv2Top = 0.0f;
                    tv2Bottom = incrementSize;
                }
            }
        }
        static void ComputeTangents(GeometryComponent geo) {
            var positions = geo.Positions;
            var triangleIndices = geo.Indices;
            var textureCoordinates = geo.TextureCoordinates;
            var normals = geo.Normals;

            var tan1 = new Vector3[positions.Length];
            //var tan2 = new Vector3[positions.Count];
            for (int t = 0; t < triangleIndices.Length; t += 3) {
                var i1 = triangleIndices[t];
                var i2 = triangleIndices[t + 1];
                var i3 = triangleIndices[t + 2];

                var v1 = positions[i1];
                var v2 = positions[i2];
                var v3 = positions[i3];

                var w1 = textureCoordinates[i1];
                var w2 = textureCoordinates[i2];
                var w3 = textureCoordinates[i3];

                float x1 = v2.X - v1.X;
                float x2 = v3.X - v1.X;
                float y1 = v2.Y - v1.Y;
                float y2 = v3.Y - v1.Y;
                float z1 = v2.Z - v1.Z;
                float z2 = v3.Z - v1.Z;

                float s1 = w2.X - w1.X;
                float s2 = w3.X - w1.X;
                float t1 = w2.Y - w1.Y;
                float t2 = w3.Y - w1.Y;

                float r = 1.0f / (s1 * t2 - s2 * t1);
                var udir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                //var vdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] += udir;
                tan1[i2] += udir;
                tan1[i3] += udir;

                //tan2[i1] += vdir;
                //tan2[i2] += vdir;
                //tan2[i3] += vdir;
            }

            geo.Tangents = new List<Vector3>();
            geo.Binormal = new List<Vector3>();
            for (int i = 0; i < positions.Length; i++) {
                var n = normals[i];
                var t = tan1[i];
                t = (t - n * Vector3.Dot(n, t));
                t.Normalize();
                var b = Vector3.Cross(n, t);
                geo.Tangents.Add(t);
                geo.Binormal.Add(b);
            }
        }


        Task BuildCellsAsync(TerrainGeometryCellsComponent geo, TerrainConfigurationComponent config) {

            // Set the height and width of each terrain cell to a fixed 33x33 vertex array.
            int cellHeight = 33;
            int cellWidth = 33;

            // Calculate the number of cells needed to store the terrain data.
            int cellRowCount = (config.Width - 1) / (cellWidth - 1);
            var m_CellCount = cellRowCount * cellRowCount;

            // Create the terrain cell array.
            var TerrainCells = new TerrainGeometryCellsComponent.TerrainCell[m_CellCount];
            try {
                // Loop through and initialize all the terrain cells.
                for (int j = 0; j < cellRowCount; j++) {
                    for (int i = 0; i < cellRowCount; i++) {
                        int index = (cellRowCount * j) + i;
                        TerrainCells[index] = geo.BuildCell(i, j, cellHeight, cellWidth, config.Width);
                    }
                }
            }catch(Exception ex) {
                ex.ToString();
            }

            geo.Cells = TerrainCells;

            return Task.FromResult(0);
        }
    }

    public class TerrainConfigurationComponent : GraphicComponent, ITerrainComponent {
        public System.Drawing.Bitmap Texture { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int TextureRepeat { get; set; }

        public double ElevationPower { get; set; }
        public float Resolution { get; set; }
        public float Correction { get; set; }

        public Perlin Noise { get; set; }
        public Turbulence Turbulence { get; set; }
        public Select Select { get; set; }
        public RidgedMulti Mountain { get; set; }
        public Billow Flat { get; set; }
        public ScaleBias FlatScale { get; set; }

        public TerrainConfigurationComponent() {
            Width = Height = 256;
            TextureRepeat = 8 * 2;// * 4
            IsModified = true;
            Correction = 20;
            ElevationPower = 1;

            Mountain = new RidgedMulti() {
                Seed = 0,
                Frequency = 1,
                Lacunarity = 2,
                OctaveCount = 6
            };

            Flat = new Billow() {
                Frequency = 2,
                Seed = 0,
                Lacunarity = 2,
                OctaveCount = 3
            };

            FlatScale = new ScaleBias() {
                //Scale = 0.125,
                //Bias = -0.75,
                Bias = 0.05,
                Scale = 0.12,
            };

            Noise = new Perlin() {
                Frequency = 0.5,
                Persistence = 0.25,
                Seed = 0,
                OctaveCount = 6
            };

            Select = new Select() {
                LowerBound = 0,
                UpperBound = 1000,
                EdgeFalloff = 0.125,
            };

            Turbulence = new Turbulence() {
                Frequency = 1.6,
                Power = 0.125,
                Roughness = 2,
            };
            
        }

    }
    public class TerrainGeneratorComponent : GraphicComponent, ITerrainComponent {
        public struct TerrainParams {
            public double ElevationPower;
            public float Resolution;
            public float Correction;
            public int Seed;

            public float MountainLacunarity;
            public float MountainFrequency;
            public int MountainOctaveCount;
        }

        public bool IsGenerated { get; private set; }
        public Vector3[] HeightMap { get; private set; }
        public float MaxHeight { get; private set; }
        public float MinHeight { get; private set; }
        public System.Drawing.Bitmap Texture { get; private set; }


        readonly int width;
        readonly int height;
        readonly TerrainConfigurationComponent terrainParams;

        public TerrainGeneratorComponent(int width, int height, TerrainConfigurationComponent terrainParams) {
            this.width = width;
            this.height = height;
            this.terrainParams = terrainParams;
        }

        void Redistribution(ref float e) {
            //Redistribution (make mountain peaks steeper)
            //The noise function gives us values between 0 and 1 (or -1 and +1 depending on which library you’re using). 
            //To make flat valleys, we can raise the elevation to a power.
            //0.1 - 10
            e = (float)Math.Pow(e, terrainParams.ElevationPower);
        }
        static float ReScale(float e) {
            // Rescale from -1.0:+1.0 to 0.0:1.0
            return e / 2.0f + 0.5f;
        }
        static void ReScale(ref float e) {
            // Rescale from -1.0:+1.0 to 0.0:1.0
            e = e / 2.0f + 0.5f;
        }

        /// -1.0:+1.0 | 
        /// -0.2500f| 0.375
        /// 0       | 0.5
        /// 0.0625f | 0.53125
        /// 0.1250f | 0.5625
        /// 0.3750f | 0.6875
        /// 0.7500f | 0.875
        /// 

        void Biome(ref float e) {
            if (e < ReScale(-0.2500f)) {// deep water

            } else if (e < ReScale(0.0000f)) { //shallow water                           
            } else if (e < ReScale(0.0625f)) { //shore                           
                e = (float)Math.Round(e * 19) / 19f;
            } else if (e < ReScale(0.1250f)) {//sand    
                e = (float)Math.Round(e * 19) / 19f;
            } else if (e < ReScale(0.3750f)) { // grass                           
                //e = (float)Math.Round(e * 19) / 19f;
            } else if (e < ReScale(0.7500f)) { //dirt                           
            } else if (e < ReScale(1.0000f)) { //rock

            } else { //snow

            }

            Redistribution(ref e);

            //if (e < 0.1) return WATER;
            //else if (e < 0.2) return BEACH;
            //else if (e < 0.3) return FOREST;
            //else if (e < 0.5) return JUNGLE;
            //else if (e < 0.7) return SAVANNAH;
            //else if (e < 0.9) return DESERT;
            //else return SNOW;
        }

        /*
         * m - height map of moisture texture
         function biome(e, m) {      
  if (e < 0.1) return OCEAN;
  if (e < 0.12) return BEACH;
  
  if (e > 0.8) {
    if (m < 0.1) return SCORCHED;
    if (m < 0.2) return BARE;
    if (m < 0.5) return TUNDRA;
    return SNOW;
  }

  if (e > 0.6) {
    if (m < 0.33) return TEMPERATE_DESERT;
    if (m < 0.66) return SHRUBLAND;
    return TAIGA;
  }

  if (e > 0.3) {
    if (m < 0.16) return TEMPERATE_DESERT;
    if (m < 0.50) return GRASSLAND;
    if (m < 0.83) return TEMPERATE_DECIDUOUS_FOREST;
    return TEMPERATE_RAIN_FOREST;
  }

  if (m < 0.16) return SUBTROPICAL_DESERT;
  if (m < 0.33) return GRASSLAND;
  if (m < 0.66) return TROPICAL_SEASONAL_FOREST;
  return TROPICAL_RAIN_FOREST;
}
             */
        void Generating() {
            //var mountain = BuildHeightMap(
            //   new ImprovedPerlin(),
            //   new SumFractal() {
            //       Frequency = 2,
            //       Lacunarity = 1,
            //       OctaveCount = 6,
            //   }, 0.1f);


            var map = new NoiseMap();
            try {
                var rectangle = new Rectangle(0, 0, width, height);
                var tree = CreateNoiseTree();
                var builder = new PlaneNoiseMapBuilder() {
                    DestNoiseMap = map,
                };
                builder.SourceModule = tree;
                builder.SetDestSize(width, height);
                builder.SetBounds(6.0, 10.0, 1.0, 5.0);
                builder.EnableSeamless = false;

                builder.Build();

                var image = new SharpNoise.Utilities.Imaging.Image();
                var renderer = new ImageRenderer() {
                    SourceNoiseMap = map,
                    DestinationImage = image,
                };
                renderer.BuildTerrainGradient();

                renderer.Render();

                Texture = renderer.DestinationImage.ToGdiBitmap();

            } catch (Exception ex) {
                ex.ToString();
            } finally {
                ClearNoiseTree();
            }

            var resources = Path.Combine("../../../../D3DLab.Wpf.Engine.App/Resources/terrain/");
            // using (var bitmap = new System.Drawing.Bitmap(Path.Combine(resources, "mask_circular_256x256.png"))) {
            HeightMap = new Vector3[height * width];
            var index = HeightMap.Length - 1;

            for (var x = 0; x < width; x++) {
                for (var y = 0; y < height; y++) {
                    //MakeIsland(ref e, x, y, 200f);
                    //var index = height * y + x;

                    var e = map.GetValue(x, y);

                    ReScale(ref e);

                    Redistribution(ref e);

                    HeightMap[index] = new Vector3(x, e * terrainParams.Correction, y);

                    MaxHeight = Math.Max(HeightMap[index].Y, MaxHeight);
                    MinHeight = Math.Min(HeightMap[index].Y, MinHeight);

                    index--;
                }
            }
            //}
            IsGenerated = true;
        }
        Module CreateNoiseTree() {
            var mountainTerrain = terrainParams.Mountain;
            var baseFlatTerrain = terrainParams.Flat;
            var flatTerrain = terrainParams.FlatScale;

            flatTerrain.Source0 = baseFlatTerrain;

            var terrainType = terrainParams.Noise;
            var terrainSelector = terrainParams.Select;

            terrainSelector.Source0 = flatTerrain;
            terrainSelector.Source1 = mountainTerrain;
            terrainSelector.Control = terrainType;

            var finalTerrain = terrainParams.Turbulence;
            finalTerrain.Source0 = terrainSelector;

            return finalTerrain;
        }
        void ClearNoiseTree() {
            terrainParams.FlatScale.Source0 = null;
            terrainParams.Select.Source0 = null;
            terrainParams.Select.Source1 = null;
            terrainParams.Select.Control = null;
            terrainParams.Turbulence.Source0 = null;
        }


        /*
        float[] BuildHeightMap(PrimitiveModule primitive, FilterModule filter, ScaleBias scale = null) {
            //var simplexPerlin = new SimplexPerlin();
            primitive.Quality = NoiseQuality.Fast;
            //
            //filter.Primitive3D = (IModule3D)primitive;



            Module module;
            if (scale != null) {
                scale.SourceModule = filter;
                module = scale;
            } else {
                module = filter;
            }
            var flat = new Billow() {
                Bias = 0.05f,
                Scale = 0.12f,
                Frequency = 2,
                Lacunarity = 2,
                OctaveCount = 3,

            };

            var select = new Select(primitive, flat, filter, 0.0f, 1000, 0.125f);
            
            float bound = 2f;
            var projection = new NoiseMapBuilderPlane(bound, bound * 2, bound, bound * 2, true);
            var noiseMap = new NoiseMap();
            try {
                projection.SetSize(height, width);
                //projection.SourceModule = module;
                projection.SourceModule = select;
                projection.NoiseMap = noiseMap;

                projection.Build();
            } catch (Exception ex) {
                ex.ToString();
            }

            var map = new float[height * width];

            //var hash = new HashSet<float>();
            for (var y = 0; y < height; y++) {
                for (var x = 0; x < width; x++) {
                    var e = noiseMap.GetValue(x, y);
              //      hash.Add(e);
                    map[height * y + x] = e;
                }
            }
            


            var renderer = new LibNoise.Renderer.ImageRenderer();
            renderer.NoiseMap = noiseMap;
            renderer.Gradient = LibNoise.Renderer.GradientColors.Terrain;
            renderer.LightBrightness = 2;
            renderer.LightContrast = 8;
            //renderer.LightEnabled = true;

            var bmpAdaptater = new BitmapAdaptater(width, height);
            renderer.Image = bmpAdaptater;

            renderer.Render();
            Texture = bmpAdaptater.Bitmap;

            return map;

            //btm.Save(@"C:\Storage\projects\sv\3d\d3dlab\D3DLab.Wpf.Engine.App\Resources\terrain\flat.bmp");

            //using (btm) {
            //    for (var y = 0; y < height; y++) {
            //        for (var x = 0; x < width; x++) {
            //            var pixel = btm.GetPixel(x, y);
            //            var e = pixel.R;

            //            map[height * y + x] = e;
            //        }
            //    }
            //}
            //return map;
        }
        */
        void MakeIsland(ref float elevation, float x, float y, float island_size) {
            float distance_x = (float)Math.Abs(x - island_size * 0.5f);
            float distance_y = (float)Math.Abs(y - island_size * 0.5f);
            float distance = (float)Math.Sqrt(distance_x * distance_x + distance_y * distance_y); // circular mask

            float max_width = island_size * 0.5f - 10.0f;
            float delta = distance / max_width;
            float gradient = delta * delta;
            var val = 1.0f - delta;
            if (val < 0) {
            } else if (val > 1) {
                val = 1;
            }
            elevation *= val;// Math.Max(0.0f, 1.0f - gradient);
        }

        public Task StartGeneratingAsync() {
            return Task.Run(Generating);
        }
    }
}
