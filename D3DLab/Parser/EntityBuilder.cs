using D3DLab.Debugger.Modules.OBJFileFormat;
using D3DLab.ECS;
using D3DLab.ECS.Components;
using D3DLab.Plugin.Contracts.Parsers;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Common;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Components.Materials;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.GameObjects;
using D3DLab.Std.Engine.Core.Systems;
using D3DLab.Wpf.Engine.App;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;

namespace D3DLab.Parser {
    class PluginParseVisiter : IParseResultVisiter {
        protected readonly IEntityManager manager;
        protected readonly List<IGeometryComponent> components;
        protected readonly List<IGraphicComponent> others;

        public PluginParseVisiter(IEntityManager manager) {
            this.manager = manager;
            components = new List<IGeometryComponent>();
            others = new List<IGraphicComponent>();
        }
        public void Handle(IGeometryComponent com) {
            components.Add(com);
        }

        public void Handle(IEnumerable<AbstractGeometry3D> mesh) {

        }

        public void Handle(IGraphicComponent com) {
            others.Add(com);
        }
    }

    class GameObjectReBuilder : GameObjectBuilder {
        public GameObjectReBuilder(CompositeGameObjectFromFile go, IEntityManager manager) : base(manager, go) {
        }
    }

    class GameObjectBuilder : PluginParseVisiter {
        readonly CompositeGameObjectFromFile gobj;

        public GameObjectBuilder(IEntityManager manager) : this(manager, new CompositeGameObjectFromFile($"Composite{DateTime.Now.Ticks}")) {
        }
        protected GameObjectBuilder(IEntityManager manager, CompositeGameObjectFromFile gobj) : base(manager) {
            this.gobj = gobj;
        }
        public CompositeGameObjectFromFile Build(Stream stream, IFileParserPlugin parser) {
            parser.Parse(stream, this);
            var colors = new Queue<Vector4>();
            colors.Enqueue(V4Colors.Red);
            colors.Enqueue(V4Colors.Blue);
            colors.Enqueue(V4Colors.Green);
            colors.Enqueue(V4Colors.Yellow);

            foreach (var com in components) {
                var tag = new ElementTag("Obj_" + Guid.NewGuid());//DateTime.Now.Ticks
                var entity = manager.CreateEntity(tag);
                var cc = new List<IGraphicComponent>();
                cc.Add(com);
                cc.Add(EntityBuilders.GetObjGroupsRender());
                cc.Add(EntityBuilders.GetTransformation());
                //var material = new PositionColorsComponent();

                //material.Colors = new Vector4[com.Positions.Length];
                //for (var i =0;i < com.Positions.Length; ++i) {
                //    material.Colors[i] = V4Colors.Red;
                //}

                cc.Add(ColorComponent.CreateDiffuse(V4Colors.Red));
                cc.Add(new ManipulatableComponent());

                entity.AddComponents(cc);
                entity.AddComponents(others);
                gobj.AddEntity(tag);
            }

            return gobj; //new SingleGameObject(tag, info.File.Name);
        }
        public CompositeGameObjectFromFile Build(FileInfo file, IFileParserPlugin parser) {
            using (var str = File.OpenRead(file.FullName)) {
                return Build(str, parser);
            }
        }

    }
}
