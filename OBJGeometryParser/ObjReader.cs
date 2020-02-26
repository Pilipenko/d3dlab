using D3DLab.Plugin.Contracts.Parsers;
using D3DLab.Std.Engine.Core.Common;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.MeshFormats;
using D3DLab.Std.Engine.Core.Utilities.Helix;
using D3DLab.ECS.Ext;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text;

namespace OBJGeometryParser {
    using Object3DGroup = System.Collections.Generic.List<Object3D>;
    //public enum MeshFaces {
    //    Default,
    //    QuadPatches,
    //}

    internal struct ModelInfo {
        public MeshFaces Faces { get; set; }
        public bool Normals { get; set; }
        public bool Tangents { get; set; }
    }
    internal class Object3D {
        public AbstractGeometry3D Geometry { get; set; }
        public ObjReader.MaterialDefinition Material { get; set; }
        public Matrix4x4 Transform { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// A Wavefront .obj file reader.
    /// </summary>
    /// <remarks>
    /// See the file format specifications at
    /// http://en.wikipedia.org/wiki/Obj
    /// http://en.wikipedia.org/wiki/Material_Template_Library
    /// http://www.martinreddy.net/gfx/3d/OBJ.spec
    /// http://www.eg-models.de/formats/Format_Obj.html
    /// </remarks>
    internal class ObjReader {
        /// <summary>
        /// Initializes a new instance of the <see cref = "ObjReader" /> class.
        /// </summary>
        public ObjReader() {
            this.IgnoreErrors = false;

            this.IsSmoothingDefault = true;
            this.SkipTransparencyValues = true;

            this.DefaultColor = new Vector4(1, 0, 0, 0);//Red

            this.Points = new List<Vector3>();
            this.TextureCoordinates = new List<Vector2>();
            this.Normals = new List<Vector3>();
            this.Colors = new List<Vector4>();

            this.Groups = new List<Group>();
            this.Materials = new Dictionary<string, MaterialDefinition>();

            this.smoothingGroupMaps = new Dictionary<int, Dictionary<int, int>>();
        }

        /// <summary>
        /// Gets or sets the default color.
        /// </summary>
        /// <value>The default color.</value>
        /// <remarks>
        /// The default value is Colors.Gold.
        /// </remarks>
        public Vector4 DefaultColor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore errors.
        /// </summary>
        /// <value><c>true</c> if errors should be ignored; <c>false</c> if errors should throw an exception.</value>
        /// <remarks>
        /// The default value is on (true).
        /// </remarks>
        public bool IgnoreErrors { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to skip transparency values ("Tr") in the material files.
        /// </summary>
        /// <value>
        /// <c>true</c> if transparency values should be skipped; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// This option is added to allow disabling the "Tr" values in files where it has been defined incorrectly.
        /// The transparency values ("Tr") are interpreted as 0 = transparent, 1 = opaque.
        /// The dissolve values ("d") are interpreted as 0 = transparent, 1=opaque.
        /// </remarks>
        public bool SkipTransparencyValues { get; set; }

        /// <summary>
        /// Sets a value indicating whether smoothing is default.
        /// </summary>
        /// <remarks>
        /// The default value is smoothing=on (true).
        /// </remarks>
        public bool IsSmoothingDefault {
            set {
                this.currentSmoothingGroup = value ? 1 : 0;
            }
        }

        /// <summary>
        /// Gets the groups of the file.
        /// </summary>
        /// <value>The groups.</value>
        public IList<Group> Groups { get; private set; }

        /// <summary>
        /// Gets the materials in the imported material files.
        /// </summary>
        /// <value>The materials.</value>
        public Dictionary<string, MaterialDefinition> Materials { get; private set; }

        /// <summary>
        /// Gets or sets the path to the textures.
        /// </summary>
        /// <value>The texture path.</value>
        public string TexturePath { get; set; }

        /// <summary>
        /// Additional info how to treat the model
        /// </summary>
        public ModelInfo ModelInfo { get; private set; }

        /// <summary>
        /// Reads the model from the specified path.
        /// </summary>
        /// <param name="path">
        /// The path.
        /// </param>
        /// <returns>
        /// The model.
        /// </returns>
        public Object3DGroup Read(string path, ModelInfo info = default(ModelInfo)) {
            this.TexturePath = Path.GetDirectoryName(path);
            this.ModelInfo = info;

            using (var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                return this.Read(s);
            }
        }

        /// <summary>
        /// Reads the model from the specified stream.
        /// </summary>
        /// <param name="s">
        /// The stream.
        /// </param>
        /// <returns>
        /// The model.
        /// </returns>
        public Object3DGroup Read(Stream s, ModelInfo info = default(ModelInfo)) {
            using (this.Reader = new StreamReader(s)) {
                this.currentLineNo = 0;
                while (!this.Reader.EndOfStream) {
                    this.currentLineNo++;
                    var line = this.Reader.ReadLine();
                    if (line == null) {
                        break;
                    }

                    if (line.Contains(@"\")) {
                        line = line.Replace(@"\", Reader.ReadLine().Trim());
                    }

                    line = line.Trim();
                    if (line.StartsWith("#") || line.Length == 0) {
                        continue;
                    }

                    SplitLine(line, out var keyword, out var values);

                    try {

                        switch (keyword.ToLower()) {
                            // Vertex data
                            case "v": // geometric vertices
                                this.AddVertex(values);
                                break;
                            case "vt": // texture vertices
                                this.AddTexCoord(values);
                                break;
                            case "vn": // vertex normals
                                this.AddNormal(values);
                                break;
                            case "vp": // parameter space vertices
                            case "cstype": // rational or non-rational forms of curve or surface type: basis matrix, Bezier, B-spline, Cardinal, Taylor
                            case "degree": // degree
                            case "bmat": // basis matrix
                            case "step": // step size
                                // not supported
                                break;

                            // Elements
                            case "f": // face
                                this.AddFace(values);
                                break;
                            case "p": // point
                            case "l": // line
                            case "curv": // curve
                            case "curv2": // 2D curve
                            case "surf": // surface
                                // not supported
                                break;

                            // Free-form curve/surface body statements
                            case "parm": // parameter name
                            case "trim": // outer trimming loop (trim)
                            case "hole": // inner trimming loop (hole)
                            case "scrv": // special curve (scrv)
                            case "sp":  // special point (sp)
                            case "end": // end statement (end)
                                // not supported
                                break;

                            // Connectivity between free-form surfaces
                            case "con": // connect
                                // not supported
                                break;

                            // Grouping
                            case "g": // group name
                                this.AddGroup(values);
                                break;
                            case "s": // smoothing group
                                this.SetSmoothingGroup(values);
                                break;
                            case "mg": // merging group
                                break;
                            case "o": // object name
                                // not supported
                                break;

                            // Display/render attributes
                            case "mtllib": // material library
                                this.LoadMaterialLib(values);
                                break;
                            case "usemtl": // material name
                                this.EnsureNewMesh();

                                this.SetMaterial(values);
                                break;
                            case "usemap": // texture map name
                                this.EnsureNewMesh();

                                break;
                            case "bevel": // bevel interpolation
                            case "c_interp": // color interpolation
                            case "d_interp": // dissolve interpolation
                            case "lod": // level of detail
                            case "shadow_obj": // shadow casting
                            case "trace_obj": // ray tracing
                            case "ctech": // curve approximation technique
                            case "stech": // surface approximation technique
                                // not supported
                                break;
                        }
                    } catch (Exception) {
                        throw;
                    }
                }
            }

            return this.BuildModel();
        }

        private void SetMaterial(string materialName) {
            this.CurrentGroup.Material.Name = materialName;
        }

        /// <summary>
        /// Reads a GZipStream compressed OBJ file.
        /// </summary>
        /// <param name="path">
        /// The path.
        /// </param>
        /// <returns>
        /// A Model3D object containing the model.
        /// </returns>
        /// <remarks>
        /// This is a file format used by Helix Toolkit only.
        /// Use the GZipHelper class to compress an .obj file.
        /// </remarks>
        public Object3DGroup ReadZ(string path) {
            this.TexturePath = Path.GetDirectoryName(path);
            using (var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                var deflateStream = new GZipStream(s, CompressionMode.Decompress, true);
                return this.Read(deflateStream);
            }
        }



        /// <summary>
        /// The smoothing group maps.
        /// </summary>
        /// <remarks>
        /// The outer dictionary maps from a smoothing group number to a Dictionary&lt;int,int&gt;.
        /// The inner dictionary maps from an obj file vertex index to a vertex index in the current group.
        /// </remarks>
        private readonly Dictionary<int, Dictionary<int, int>> smoothingGroupMaps;

        /// <summary>
        /// The current smoothing group.
        /// </summary>
        private int currentSmoothingGroup;

        /// <summary>
        /// The line number of the line being parsed.
        /// </summary>
        private int currentLineNo;

        /// <summary>
        /// Gets the current group.
        /// </summary>
        private Group CurrentGroup {
            get {
                if (this.Groups.Count == 0) {
                    this.AddGroup("default");
                }

                return this.Groups[this.Groups.Count - 1];
            }
        }

        /// <summary>
        /// Gets or sets the normals.
        /// </summary>
        private IList<Vector3> Normals { get; set; }

        /// <summary>
        /// Gets or sets the points.
        /// </summary>
        private IList<Vector3> Points { get; set; }

        /// <summary>
        /// Gets or sets the points colors.
        /// </summary>
        private IList<Vector4> Colors { get; set; }

        /// <summary>
        /// Gets or sets the stream reader.
        /// </summary>
        private StreamReader Reader { get; set; }

        /// <summary>
        /// Gets or sets the texture coordinates.
        /// </summary>
        private IList<Vector2> TextureCoordinates { get; set; }

        /// <summary>
        /// Parses a color string.
        /// </summary>
        /// <param name="values">
        /// The input.
        /// </param>
        /// <returns>
        /// The parsed color.
        /// </returns>
        private static System.Drawing.Color ColorParse(string values) {
            var fields = Split(values);
            return System.Drawing.Color.FromArgb((byte)(fields[0] * 255), (byte)(fields[1] * 255), (byte)(fields[2] * 255));
        }

        /// <summary>
        /// Parse a string containing a double value.
        /// </summary>
        /// <param name="input">
        /// The input string.
        /// </param>
        /// <returns>
        /// The value.
        /// </returns>
        private static double DoubleParse(string input) {
            return double.Parse(input, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Splits the specified string using whitespace(input) as separators.
        /// </summary>
        /// <param name="input">
        /// The input string.
        /// </param>
        /// <returns>
        /// List of input.
        /// </returns>
        private static IList<double> Split(string input) {
            input = input.Trim();
            var fields = input.SplitOnWhitespace();
            var result = new double[fields.Length];
            for (int i = 0; i < fields.Length; i++) {
                result[i] = DoubleParse(fields[i]);
            }

            return result;
        }

        /// <summary>
        /// Splits a line in keyword and arguments.
        /// </summary>
        /// <param name="line">
        /// The line.
        /// </param>
        /// <param name="keyword">
        /// The keyword.
        /// </param>
        /// <param name="arguments">
        /// The arguments.
        /// </param>
        private static void SplitLine(string line, out string keyword, out string arguments) {
            int idx = line.IndexOf(' ');
            if (idx < 0) {
                keyword = line;
                arguments = null;
                return;
            }

            keyword = line.Substring(0, idx);
            arguments = line.Substring(idx + 1);
        }

        /// <summary>
        /// Adds a group with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        private void AddGroup(string name) {
            this.Groups.Add(new Group(name));
            this.smoothingGroupMaps.Clear();
        }

        /// <summary>
        /// Ensures that a new mesh is created.
        /// </summary>
        private void EnsureNewMesh() {
            if (this.CurrentGroup.MeshBuilder.TriangleIndices.Count != 0) {
                this.CurrentGroup.AddMesh();
                this.smoothingGroupMaps.Clear();
            }
        }

        /// <summary>
        /// Sets the smoothing group number.
        /// </summary>
        /// <param name="values">The group number.</param>
        private void SetSmoothingGroup(string values) {
            if (values == "off") {
                this.currentSmoothingGroup = 0;
            } else {
                if (int.TryParse(values, out var smoothingGroup)) {
                    this.currentSmoothingGroup = smoothingGroup;
                } else {
                    // invalid parameter
                    if (this.IgnoreErrors) {
                        return;
                    }

                    //throw new FileFormatException(string.Format("Invalid smoothing group ({0}) at line {1}.", values, this.currentLineNo));
                }
            }
        }

        /// <summary>
        /// Adds a face.
        /// </summary>
        /// <param name="values">
        /// The input values.
        /// </param>
        /// <remarks>
        /// Adds a polygonal face. The numbers are indexes into the arrays of vertex positions,
        /// texture coordinates, and normals respectively. A number may be omitted if,
        /// for example, texture coordinates are not being defined in the model.
        /// There is no maximum number of vertices that a single polygon may contain.
        /// The .obj file specification says that each face must be flat and convex.
        /// </remarks>
        private void AddFace(string values) {
            var currentGroup = this.CurrentGroup;
            var builder = currentGroup.MeshBuilder;
            var positions = builder.Positions;
            var textureCoordinates = builder.TextureCoordinates;
            var normals = builder.Normals;
            var colors = builder.Colors;
            Dictionary<int, int> smoothingGroupMap = null;

            // If a smoothing group is defined, get the map from obj-file-index to current-group-vertex-index.
            if (this.currentSmoothingGroup != 0) {
                if (!this.smoothingGroupMaps.TryGetValue(this.currentSmoothingGroup, out smoothingGroupMap)) {
                    smoothingGroupMap = new Dictionary<int, int>();
                    this.smoothingGroupMaps.Add(this.currentSmoothingGroup, smoothingGroupMap);
                }
            }

            var fields = values.SplitOnWhitespace();
            var faceIndices = new List<int>();
            foreach (var field in fields) {
                if (string.IsNullOrEmpty(field)) {
                    continue;
                }

                var ff = field.Split('/');
                int vi = int.Parse(ff[0]);
                int vti = ff.Length > 1 && ff[1].Length > 0 ? int.Parse(ff[1]) : -1;
                int vni = ff.Length > 2 && ff[2].Length > 0 ? int.Parse(ff[2]) : -1;

                // Handle relative indices (negative numbers)
                if (vi < 0) {
                    vi = this.Points.Count + vi;
                }

                if (vti < 0) {
                    vti = this.TextureCoordinates.Count + vti;
                }

                if (vni < 0) {
                    vni = this.Normals.Count + vni;
                }

                // Check if the indices are valid
                if (vi - 1 >= this.Points.Count) {
                    if (this.IgnoreErrors) {
                        return;
                    }

                    throw new FormatException(string.Format("Invalid vertex index ({0}) on line {1}.", vi, this.currentLineNo));
                }

                if (vti == -1) {
                    // turn off texture coordinates in the builder
                    //builder.CreateTextureCoordinates = false;
                    builder.TextureCoordinates = null;
                }

                if (vni == -1) {
                    // turn off normals in the builder
                    //builder.CreateNormals = false;
                    builder.Normals = null;
                }

                // check if the texture coordinate index is valid
                if (builder.HasTexCoords && vti - 1 >= this.TextureCoordinates.Count) {
                    if (this.IgnoreErrors) {
                        return;
                    }

                    throw new FormatException(string.Format("Invalid texture coordinate index ({0}) on line {1}.", vti, this.currentLineNo));
                }

                // check if the normal index is valid
                if (builder.HasNormals && vni - 1 >= this.Normals.Count) {
                    if (this.IgnoreErrors) {
                        return;
                    }

                    throw new FormatException(string.Format("Invalid normal index ({0}) on line {1}.", vni, this.currentLineNo));
                }

                bool addVertex = true;

                if (smoothingGroupMap != null) {
                    if (smoothingGroupMap.TryGetValue(vi, out var vix)) {
                        // use the index of a previously defined vertex
                        addVertex = false;
                    } else {
                        // add a new vertex
                        vix = positions.Count;
                        smoothingGroupMap.Add(vi, vix);
                    }

                    faceIndices.Add(vix);
                } else {
                    // if smoothing is off, always add a new vertex
                    faceIndices.Add(positions.Count);
                }

                if (addVertex) {
                    // add vertex
                    positions.Add(this.Points[vi - 1]);
                    if (Colors.Count > 0) {
                        colors.Add(this.Colors[vi - 1]);
                    }

                    // add texture coordinate (if enabled)
                    if (builder.HasTexCoords) {
                        textureCoordinates.Add(this.TextureCoordinates[vti - 1]);
                    }

                    // add normal (if enabled)
                    if (builder.HasNormals) {
                        normals.Add(this.Normals[vni - 1]);
                    }
                }
            }

            try {


                if (faceIndices.Count < 3) {
                    throw new InvalidDataException("Polygon must have at least 3 indices!");
                }


                if (this.ModelInfo.Faces == MeshFaces.QuadPatches) {
                    if (faceIndices.Count == 3) {
                        faceIndices.Add(faceIndices.Last());
                        builder.AddQuad(faceIndices);
                    }
                    if (faceIndices.Count == 4) {
                        builder.AddQuad(faceIndices);
                    } else {
                        // add triangles by cutting ears algorithm
                        // this algorithm is quite expensive...
                        builder.AddPolygonByCuttingEars(faceIndices);
                    }
                } else {
                    if (faceIndices.Count == 3) {
                        builder.AddTriangle(faceIndices);
                    } else if (faceIndices.Count == 4) {
                        //builder.AddQuad(faceIndices);
                        builder.AddTriangleFan(faceIndices);
                    } else {
                        // add triangles by cutting ears algorithm
                        // this algorithm is quite expensive...
                        builder.AddPolygonByCuttingEars(faceIndices);
                    }
                }
            } catch (Exception) {
                //System.Windows.MessageBox.Show(string.Format("Error composing polygonal object: {0}", ex.Message), "Error", MessageBoxButton.OKCancel);
            }
        }

        /// <summary>
        /// Adds a normal.
        /// </summary>
        /// <param name="values">
        /// The input values.
        /// </param>
        private void AddNormal(string values) {
            var fields = Split(values);
            this.Normals.Add(new Vector3((float)fields[0], (float)fields[1], (float)fields[2]));
        }

        /// <summary>
        /// Adds a texture coordinate.
        /// </summary>
        /// <param name="values">
        /// The input values.
        /// </param>
        private void AddTexCoord(string values) {
            var fields = Split(values);
            this.TextureCoordinates.Add(new Vector2((float)fields[0], 1 - (float)fields[1]));
        }

        /// <summary>
        /// Adds a vertex.
        /// </summary>
        /// <param name="values">
        /// The input values.
        /// </param>
        private void AddVertex(string values) {
            var fields = Split(values);
            this.Points.Add(new Vector3((float)fields[0], (float)fields[1], (float)fields[2]));
            if (fields.Count > 4) {
                this.Colors.Add(new Vector4((float)fields[3], (float)fields[4], (float)fields[5], 1f));
            }
        }

        /// <summary>
        /// Builds the model.
        /// </summary>
        /// <returns>
        /// A Model3D object.
        /// </returns>
        private Object3DGroup BuildModel() {
            var modelGroup = new Object3DGroup();
            foreach (var g in this.Groups) {
                foreach (var gm in g.CreateModels(this.ModelInfo)) {
                    modelGroup.Add(gm);
                }
            }

            return modelGroup;
        }

        /// <summary>
        /// Gets the material with the specified name.
        /// </summary>
        /// <param name="materialName">
        /// The material name.
        /// </param>
        /// <returns>
        /// The material.
        /// </returns>
        //private Material GetMaterial(string materialName) {
        //    if (!string.IsNullOrEmpty(materialName) && this.Materials.TryGetValue(materialName, out MaterialDefinition mat)) {
        //        return mat.GetMaterial(this.TexturePath);
        //    }

        //    return PhongMaterials.DefaultVRML;// MaterialHelper.CreateMaterial(new SolidColorBrush(this.DefaultColor));
        //}

        /// <summary>
        /// Loads a material library.
        /// </summary>
        /// <param name="mtlFile">
        /// The mtl file.
        /// </param>
        private void LoadMaterialLib(string mtlFile) {
            var path = Path.Combine(this.TexturePath, mtlFile);
            if (!File.Exists(path)) {
                return;
            }

            using (var mreader = new StreamReader(path)) {
                MaterialDefinition currentMaterial = null;

                while (!mreader.EndOfStream) {
                    var line = mreader.ReadLine();
                    if (line == null) {
                        break;
                    }

                    line = line.Trim();

                    if (line.StartsWith("#") || line.Length == 0) {
                        continue;
                    }

                    SplitLine(line, out var keyword, out var value);

                    switch (keyword.ToLower()) {
                        case "newmtl":
                            if (value != null) {
                                currentMaterial = new MaterialDefinition();
                                this.Materials.Add(value, currentMaterial);
                            }

                            break;
                        case "ka":
                            if (currentMaterial != null && value != null) {
                                currentMaterial.Ambient = value;
                            }

                            break;
                        case "kd":
                            if (currentMaterial != null && value != null) {
                                currentMaterial.Diffuse = value;
                            }

                            break;
                        case "ks":
                            if (currentMaterial != null && value != null) {
                                currentMaterial.Specular = value;
                            }

                            break;
                        case "ns":
                            if (currentMaterial != null && value != null) {
                                currentMaterial.SpecularCoefficient = DoubleParse(value);
                            }

                            break;
                        case "d":
                            if (currentMaterial != null && value != null) {
                                currentMaterial.Dissolved = DoubleParse(value);
                            }

                            break;
                        case "tr":
                            if (!this.SkipTransparencyValues && currentMaterial != null && value != null) {
                                currentMaterial.Dissolved = DoubleParse(value);
                            }

                            break;
                        case "illum":
                            if (currentMaterial != null && value != null) {
                                currentMaterial.Illumination = int.Parse(value);
                            }

                            break;
                        case "map_ka":
                            if (currentMaterial != null) {
                                currentMaterial.AmbientMap = value;
                            }

                            break;
                        case "map_kd":
                            if (currentMaterial != null) {
                                currentMaterial.DiffuseMap = value;
                            }

                            break;
                        case "map_ks":
                            if (currentMaterial != null) {
                                currentMaterial.SpecularMap = value;
                            }

                            break;
                        case "map_d":
                            if (currentMaterial != null) {
                                currentMaterial.AlphaMap = value;
                            }

                            break;
                        case "map_bump":
                        case "bump":
                            if (currentMaterial != null) {
                                currentMaterial.BumpMap = value;
                            }

                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Represents a group in the obj file.
        /// </summary>
        public class Group {
            /// <summary>
            /// List of mesh builders.
            /// </summary>
            private readonly IList<MeshBuilder> meshBuilders;

            /// <summary>
            /// List of materials.
            /// </summary>
            private readonly IList<ObjReader.MaterialDefinition> materials;

            /// <summary>
            /// Initializes a new instance of the <see cref="Group"/> class.
            /// </summary>
            /// <param name="name">
            /// The name of the group.
            /// </param>
            public Group(string name) {
                this.Name = name;
                this.meshBuilders = new List<MeshBuilder>();
                this.materials = new List<ObjReader.MaterialDefinition>();
                this.AddMesh();
            }

            /// <summary>
            /// Sets the material.
            /// </summary>
            /// <value>The material.</value>
            public ObjReader.MaterialDefinition Material {
                set {
                    this.materials[this.materials.Count - 1] = value;
                }
                get {
                    if (this.materials.Count == 0) {
                        this.materials.Add(new MaterialDefinition());
                    }
                    return this.materials[this.materials.Count - 1];
                }
            }

            /// <summary>
            /// Gets the mesh builder for the current mesh.
            /// </summary>
            /// <value>The mesh builder.</value>
            public MeshBuilder MeshBuilder {
                get {
                    return this.meshBuilders[this.meshBuilders.Count - 1];
                }
            }

            public IList<MeshBuilder> MeshBuilders {
                get {
                    return this.meshBuilders;
                }
            }

            /// <summary>
            /// Gets or sets the group name.
            /// </summary>
            /// <value>The name.</value>
            public string Name { get; set; }

            /// <summary>
            /// Adds a mesh.
            /// </summary>
            public void AddMesh() {
                var meshBuilder = new MeshBuilder(true, true);
                this.meshBuilders.Add(meshBuilder);
            }

            /// <summary>
            /// Creates the models of the group.
            /// </summary>
            /// <returns>The models.</returns>
            public IEnumerable<Object3D> CreateModels(ModelInfo info) {
                for (int i = 0; i < this.meshBuilders.Count; i++) {
                    this.meshBuilders[i].ComputeNormalsAndTangents(info.Faces, true);
                    yield return new Object3D {

                        Geometry = this.meshBuilders[i].ToGeometry3D(),
                        //  Material = this.materials[i],
                        //  Transform = Matrix4x4.Identity
                    };
                }
            }
        }

        /// <summary>
        /// A material definition.
        /// </summary>
        /// <remarks>
        /// The file format is documented in http://en.wikipedia.org/wiki/Material_Template_Library.
        /// </remarks>
        public class MaterialDefinition {

            public MaterialDefinition() {
                this.Dissolved = 1.0;
            }

            /// <summary>
            /// Gets or sets the alpha map.
            /// </summary>
            /// <value>The alpha map.</value>
            public string AlphaMap { get; set; }

            /// <summary>
            /// Gets or sets the ambient color.
            /// </summary>
            /// <value>The ambient.</value>
            public string Ambient { get; set; }

            /// <summary>
            /// Gets or sets the ambient map.
            /// </summary>
            /// <value>The ambient map.</value>
            public string AmbientMap { get; set; }

            /// <summary>
            /// Gets or sets the bump map.
            /// </summary>
            /// <value>The bump map.</value>
            public string BumpMap { get; set; }

            /// <summary>
            /// Gets or sets the diffuse color.
            /// </summary>
            /// <value>The diffuse.</value>
            public string Diffuse { get; set; }

            /// <summary>
            /// Gets or sets the diffuse map.
            /// </summary>
            /// <value>The diffuse map.</value>
            public string DiffuseMap { get; set; }

            /// <summary>
            /// Gets or sets the opacity value.
            /// </summary>
            /// <value>The opacity.</value>
            /// <remarks>
            /// 0.0 is transparent, 1.0 is opaque.
            /// </remarks>
            public double Dissolved { get; set; }

            /// <summary>
            /// Gets or sets the illumination.
            /// </summary>
            /// <value>The illumination.</value>
            public int Illumination { get; set; }

            /// <summary>
            /// Gets or sets the specular color.
            /// </summary>
            /// <value>The specular color.</value>
            public string Specular { get; set; }

            /// <summary>
            /// Gets or sets the specular coefficient.
            /// </summary>
            /// <value>The specular coefficient.</value>
            public double SpecularCoefficient { get; set; }

            /// <summary>
            /// Gets or sets the specular map.
            /// </summary>
            /// <value>The specular map.</value>
            public string SpecularMap { get; set; }
            public string Name { get; set; }
        }
    }

    public class ObjSpanReader {
        static Vector4 GetColor(string group) {
            //if (group.StartsWith("I")) {
            //    return V4Colors.Yellow;
            //}
            //if (group.StartsWith("A")) {
            //    return V4Colors.Blue;
            //}
            return V4Colors.Red;
        }
        public readonly AbstractGeometry3D FullGeometry1 = new AbstractGeometry3D();
        public readonly GroupGeometry3D FullGeometry = new GroupGeometry3D();

        Dictionary<string, PartGeometry3D> meshes;

        public void Read(Stream stream) {
            meshes = new Dictionary<string, PartGeometry3D>();

            var group = new ReadOnlySpan<char>(new[] { 'g' });
            var vector = new ReadOnlySpan<char>(new[] { 'v' });
            var texture = new ReadOnlySpan<char>(new[] { 'v','t' });
            var face = new ReadOnlySpan<char>(new[] { 'f' });
            var comm = new ReadOnlySpan<char>(new[] { '#' });
            //Memory<byte> buffer = new Memory<byte>();
            var groupname = "noname";
            PartGeometry3D current = FullGeometry.CreatePart(groupname);
            using (var reader = new StreamReader(stream)) {
                while (!reader.EndOfStream) {
                    var span = reader.ReadLine().AsSpan();
                    if (span.StartsWith(comm) || span.IsWhiteSpace()) {
                        continue;
                    }
                    var part = span.Slice(2, span.Length - 2).Trim();
                    if (span.StartsWith(group)) {
                        var names = part.Trim().ToString().SplitOnWhitespace();
                        groupname = string.Join(" ", names);//[0].ToString();
                        var key  = string.Join(" ", names.Take(names.Length-1));//[0].ToString();
                        current = FullGeometry.CreatePart(groupname);
                    } else if (span.StartsWith(texture)) {

                    } else if (span.StartsWith(vector)) {
                        try {
                            var val = SplitFloat(part, ' ');
                            var v = new Vector3(val[0], val[1], val[2]);

                            FullGeometry1.Positions.Add(v);
                            current.AddPosition(ref v);
                        }catch(Exception ex) {
                            ex.ToString();
                        }

                    } else if (span.StartsWith(face)) {
                        var val = SplitInt(part, ' ');

                        if(new HashSet<int>(val).Count != 3 ) {

                        }

                        FullGeometry1.Indices.AddRange(val);
                        try {
                            current.AddTriangle(val);
                        }catch(Exception ex) {
                            //TODO collect info for displaing in output 
                        }
                    }
                }
            }
        }
        private float[] SplitFloat(ReadOnlySpan<char> span, char separator) {
            var val = new float[3];
            var index = 0;
            while (index < 3) {
                var end = span.IndexOf(' ');
                if (end == -1) {
                    end = span.Length;
                }
                var part = span.Slice(0, end).Trim();
                val[index] = float.Parse(part.ToString(), CultureInfo.InvariantCulture);
                index++;
                span = span.Slice(end, span.Length - end).Trim();
            }
            return val;
        }
        private List<int> SplitInt(ReadOnlySpan<char> span, char separator) {
            var val = new List<int>();
            var index = 0;
            while (!span.IsWhiteSpace()) {
                var end = span.IndexOf(' ');
                if (end == -1) {
                    end = span.Length;
                }
                var part = span.Slice(0, end).Trim();
                var sep = part.IndexOf('/');
                if (sep != -1) {
                    part = span.Slice(0, sep).Trim();
                }
                val.Add(int.Parse(part.ToString(), CultureInfo.InvariantCulture) - 1);
                index++;
                span = span.Slice(end, span.Length - end).Trim();
            }
            if (val.Count > 3) {
                var triangleFan = new List<int>();
                for (int i = 0; i + 2 < val.Count; i++) {
                    triangleFan.Add(val[0]);
                    triangleFan.Add(val[i + 1]);
                    triangleFan.Add(val[i + 2]);
                }
                return triangleFan;
            }

            return val;
        }


    }
    public class ObjStringReader {
        public readonly AbstractGeometry3D FullGeometry = new AbstractGeometry3D();


        readonly List<Vector3> positions = new List<Vector3>();
        readonly Dictionary<int, int> map = new Dictionary<int, int>();

        Dictionary<string, AbstractGeometry3D> meshes;
        public void Read(Stream stream) {

            meshes = new Dictionary<string, AbstractGeometry3D>();

            var group = "g";
            var vector = "v";
            var face = "f";
            //Memory<byte> buffer = new Memory<byte>();
            var groupname = "noname";
            using (var reader = new StreamReader(stream)) {
                while (!reader.EndOfStream) {
                    var span = reader.ReadLine();
                    if (span.StartsWith(group)) {
                        var start = span.IndexOf(' ');
                        groupname = span.Substring(start, span.Length - 1).Trim();
                        if (!meshes.ContainsKey(groupname)) {
                            meshes.Add(groupname, new AbstractGeometry3D());
                        }
                    } else if (span.StartsWith(vector)) {
                        var part = span.Substring(2, span.Length - 2).Trim();
                        var val = SplitFloat(part, ' ');
                        var v = new Vector3(val[0], val[1], val[2]);
                        FullGeometry.Positions.Add(v);
                    } else if (span.StartsWith(face)) {
                        var part = span.Substring(2, span.Length - 2).Trim();
                        var val = SplitInt(part, ' ');
                        //var p = positions [val[0]];
                        //var p1 = positions[val[1]];
                        //var p2 = positions[val[2]];
                        FullGeometry.Indices.Add(val[0] - 1);
                        FullGeometry.Indices.Add(val[1] - 1);
                        FullGeometry.Indices.Add(val[2] - 1);
                    }
                }
            }
        }
        private float[] SplitFloat(string span, char separator) {
            var val = new float[3];
            var index = 0;
            while (index < 3) {
                var end = span.IndexOf(' ');
                if (end == -1) {
                    end = span.Length;
                }
                var part = span.Substring(0, end).Trim();
                val[index] = float.Parse(part, CultureInfo.InvariantCulture);
                index++;
                span = span.Substring(end, span.Length - end).Trim();
            }
            return val;
        }
        private int[] SplitInt(string span, char separator) {
            var val = new int[3];
            var index = 0;
            while (index < 3) {
                var end = span.IndexOf(' ');
                if (end == -1) {
                    end = span.Length;
                }
                var part = span.Substring(0, end).Trim();
                var face = part.Split('/');
                val[index] = int.Parse(face[0], CultureInfo.InvariantCulture);
                index++;
                span = span.Substring(end, span.Length - end).Trim();
            }
            return val;
        }
    }

    [System.ComponentModel.Composition.Export(typeof(IFileParserPlugin))]
    public class OBJParser : IFileParserPlugin {
        public string Name => "Simple OBJ parser";
        const string ex = ".obj";

        public bool IsSupport(string fileExtention) {
            return string.Compare(fileExtention, ex, true) == 0;
        }

        public void Parse(Stream stream, IParseResultVisiter visiter) {
            var r = new ObjSpanReader();
            try {
                var sw = new Stopwatch();
                sw.Start();

                r.Read(stream);

                sw.Stop();
                Trace.WriteLine($"Reader {sw.Elapsed.TotalMilliseconds}");

                //r.FullGeometry1.Color = new Vector4(0, 1, 0, 1);
                //r.FullGeometry1.Normals = r.FullGeometry1.Positions.CalculateNormals(r.FullGeometry.Indices.ToList());

                //var full = r.FullGeometry1;// r.FullGeometry;
                //var c = new SimpleGeometryComponent {
                //    Positions = full.Positions.ToImmutableArray(),
                //    Indices = full.Indices.ToImmutableArray(),
                //    Normals = full.Positions.ToList().CalculateNormals(full.Indices.ToList()).ToImmutableArray(),

                //};


                //    visiter.Handle(c);
                var onlypoints = new List<Vector3>();
                var onlypoints1 = new List<Vector3>();
                r.FullGeometry.Fixed();
                
                var com = new ObjGroupsComponent();
                foreach (var part in r.FullGeometry.Parts) {
                    //com.OrderedGroups.Add(new OrderedObjGroups(part.Name, part.Groups));
                    if (part.Groups.Any(i => i.IndxGroupInfo != null)) { // geo is created only by triangles
                        visiter.Handle(new VirtualGroupGeometryComponent(part));
                    } else {
                        onlypoints.AddRange(part.Positions);
                    }
                    //break;
                }
                //visiter.Handle(com);

            } catch (Exception exc) {
                exc.ToString();
            }
            return;
            var readerA = new ObjReader();
            var res = readerA.Read(stream);
            var meshes = new List<AbstractGeometry3D>();

            var colors = new Vector4[4];
            colors[0] = new Vector4(1, 0, 0, 1);
            colors[1] = new Vector4(0, 1, 0, 1);
            colors[2] = new Vector4(0, 0, 1, 1);
            colors[3] = new Vector4(1, 1, 0, 1);

            for (int i = 0; i < res.Count; i++) {
                Object3D m = res[i];
                var mesh = m.Geometry;
                mesh.Color = colors.Length > i ? colors[i] : colors[0];
                //meshes.Add(mesh);
                var c = new SimpleGeometryComponent {
                    Positions = mesh.Positions.ToImmutableArray(),
                    Indices = mesh.Indices.ToImmutableArray(),
                    Normals = mesh.Positions.ToList().CalculateNormals(mesh.Indices.ToList()).ToImmutableArray(),

                };
                visiter.Handle(c);
            }
            
        }
    }
}
