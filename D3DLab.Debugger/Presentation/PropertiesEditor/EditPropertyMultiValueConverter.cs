using D3DLab.Debugger.Model;
using D3DLab.ECS.Ext;
using D3DLab.Std.Engine.Core.Ext;
using System;
using System.Globalization;
using System.Windows.Data;

namespace D3DLab.Debugger.Presentation.PropertiesEditor {
    public class PropertyEditedData {
        public ViewProperty Property { get; set; }
        public string EditedText { get; set; }
    }

    public class EditPropertyMultiValueConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            var pr = values[0] as ViewProperty;
            if (pr.IsNull()) {
                return null;
            }
            return new PropertyEditedData {
                Property = pr,
                EditedText = values[1].ToString()
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            return null;
        }
    }
}
