using D3DLab.ECS;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Common;
using D3DLab.Std.Engine.Core.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace D3DLab.Plugin.Contracts.Parsers {
    public interface IParseResultVisiter {
        void Handle(IGeometryComponent com);
        void Handle(IGraphicComponent com);
        void Handle(IEnumerable<AbstractGeometry3D> mesh);
    }

    public interface IFileParserPlugin {
        string Name { get; }
        bool IsSupport(string fileExtention);
        void Parse(Stream stream, IParseResultVisiter visiter);
    }
}
