using D3DLab.Debugger.Windows;
using D3DLab.ECS.Common;
using D3DLab.ECS.Ext;
using D3DLab.Std.Engine.Core.Ext;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace D3DLab.Debugger.Presentation.PropertiesEditor {
    public class PropertiesEditorPopup {
        private PropertiesEditorWindow win;


        public PropertiesEditorWindowViewModel ViewModel { get; }

        public PropertiesEditorPopup(IRenderUpdater updater) {
            win = new PropertiesEditorWindow();
            ViewModel = (PropertiesEditorWindowViewModel)win.DataContext;
            ViewModel.RenderUpdater = updater;
            win.Closed += Win_Closed;
        }

        private void Win_Closed(object sender, EventArgs e) {
            win.Closed -= Win_Closed;
            ViewModel.Dispose();
        }

        public void Show() {
            win.Show();
        }
    }


    #region view properties

    public abstract class ViewProperty : System.ComponentModel.INotifyPropertyChanged {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged = (x, y) => { };

        public string Key { get; set; }
        public string Title { get; set; }

        public bool IsChanged { get; set; }
        public virtual void UpdateValue(string val) {
            IsChanged = true;
            PropertyChanged.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsChanged)));
        }

        protected void OnPropertyChanged(string name) {
            PropertyChanged.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        }
    }

    public abstract class ViewProperty<TVal> : ViewProperty {
        class ChangedCommand : BaseWPFCommand<string> {
            private ViewProperty<TVal> pr;

            public ChangedCommand(ViewProperty<TVal> viewProperty) {
                this.pr = viewProperty;
            }

            public override void Execute(string parameter) {
                pr.UpdateValue(parameter);
            }
        }
        public ICommand Changed { get; }
        public TVal Value { get; set; }

        readonly Action<string> change;

        public ViewProperty(Action<string> change) {
            Changed = new ChangedCommand(this);
            this.change = change;
        }

        public override void UpdateValue(string val) {
            change(val);
            base.UpdateValue(val);
        }
    }

    public class TextBoxViewProperty : ViewProperty<string> {
        public TextBoxViewProperty(Action<string> change) : base(change) { }
    }

    class ObjUpdaterInArray {
        readonly System.Collections.IList array;
        public readonly int Index;
        readonly object obj;
        readonly PropertyInfo prinfo;
        public ObjUpdaterInArray(System.Collections.IList array, int index) {
            this.array = array;
            this.Index = index;
            this.obj = obj;
            this.prinfo = prinfo;
        }
        public void Update<T>(T val) {
            array[Index] = val;
        }
    }




    public class TextBoxViewProperty<TValue> : TextBoxViewProperty {
        readonly Action<TValue, string> change;

        public TValue Obj { get; }

        public TextBoxViewProperty(TValue obj, Action<TValue, string> change) : base(x=> { }) {
            this.change = change;
            Obj = obj;
        }
        public override void UpdateValue(string val) {
            change(Obj, val);
            base.UpdateValue(val);
        }
    }

    public class ComboBoxViewProperty : ViewProperty<string> {
        public ICollectionView Values { get; }

        string current;
        public ComboBoxViewProperty(Action<string> change, string current, string[] values) : base(change) {
            Value = current;
            this.current = current;
            var obs = new ObservableCollection<string>(values);
            Values = CollectionViewSource.GetDefaultView(obs);
            Values.MoveCurrentToPosition(obs.IndexOf(current));
            Values.CurrentChanged += OnValuesCurrentChanged;
        }

        void OnValuesCurrentChanged(object sender, EventArgs e) {
            var val = Values.CurrentItem.ToString();
            if (val == current) {
                return;
            }
            UpdateValue(val);
            current = val;
        }
    }

    public class Vector3ViewProperty : ViewProperty<Vector3> {
        public Vector3ViewProperty(Vector3 val, Action<Vector3> change) : base(StringToVector3(change)) {
            Value = val;
        }

        static Action<string> StringToVector3(Action<Vector3> change) {
            return str => {
                var parts = str.Split(' ')
                .Select(x => float.Parse(x, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture))
                .ToArray();
                var v = new Vector3(parts[0], parts[1], parts[2]);
                change(v);
            };
        }
    }

    public class ImageReadOnlyViewProperty : ViewProperty {
        public BitmapImage Image { get; private set; }
        public ImageReadOnlyViewProperty(System.Drawing.Bitmap image)  {
            Image = BitmapToImageSource(image);
        }

        static BitmapImage BitmapToImageSource(System.Drawing.Bitmap bitmap) {
            using (var memory = new MemoryStream()) {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }
    }

    #endregion

    public abstract class ViewProperties {
        public ObservableCollection<ViewProperty> Properties {
            get {
                return new ObservableCollection<ViewProperty>(dictionary.Values);
            }
        }

        protected readonly Dictionary<string, ViewProperty> dictionary;
        public ViewProperties() {
            dictionary = new Dictionary<string, ViewProperty>();
        }

        public void AddPrimitive<T>(string key, T val, Action<string> change) {
            dictionary.Add(key, new TextBoxViewProperty(change) {
                Title = key,
                Value = val == null ? string.Empty : val.ToString()
            });
        }
        public void AddVector(string key, Vector3 val, Action<Vector3> change) {
            dictionary.Add(key, new Vector3ViewProperty(val, change) {
                Title = key
            });
        }
    }

    public class GroupViewProperty : ViewProperty<ObservableCollection<ViewProperty>> {
        //static readonly HashSet<int> hash = new HashSet<int>();

        public GroupViewProperty() : base(x => { }) {
            // dictionary = new Dictionary<string, ViewProperty>();
            Value = new ObservableCollection<ViewProperty>();

        }

        public void AddPrimitive<T>(string key, T val, Action<string> change) {
            var pr = new TextBoxViewProperty(change) {
                Title = key,
                Value = val == null ? string.Empty : val.ToString()
            };
            Value.Add(pr);
        }
        public void AddSequence<T>(string key, T val, Action<string> change, string[] values) {
            var pr = new ComboBoxViewProperty(change, val.ToString(), values) {
                Title = key
            };
            //  dictionary.Add(key, pr);
            Value.Add(pr);
        }
        public void AddVector(string key, object obj, Action<Vector3> change) {
            var val = (System.Numerics.Vector3)obj;
            var pr = new Vector3ViewProperty(val, change) {
                Title = key
            };
            //  dictionary.Add(key, pr);
            Value.Add(pr);
        }
        private void AddFileInfo(ObjUpdaterInArray updater, FileInfo fi) {//new ObjUpdater(obj, prinfo)
            var pr = new TextBoxViewProperty<ObjUpdaterInArray>(updater, (o,v) => {
                o.Update(new FileInfo(v));
            });
            pr.Title = updater.Index.ToString();
            pr.Value = fi.FullName;
            Value.Add(pr);
        }
        public void AddImage(System.Drawing.Bitmap bmp) {
            if(bmp == null) {
                return;
            }
            var pr = new ImageReadOnlyViewProperty(bmp);
            Value.Add(pr);
        }

        public void Analyze(object com, HashSet<int> hashed) {
            Analyze(com, this, com.GetType(), hashed);
        }

        void Analyze(object com, GroupViewProperty property, Type type, HashSet<int> hashed) {
            if (com.IsNull()) {
                return;
            }
            foreach (var pr in type.GetProperties()) {
                Analyze(com, property, pr, hashed);
            }
        }


        void AnalyzeValueType(object com, GroupViewProperty property, Type basetype) {
            foreach (var field in basetype.GetFields()) {
                var name = field.Name;
                var val = com.IsNull() ? null : field.GetValue(com);
                var type = field.FieldType;
                if (type.IsPrimitive || Type.GetTypeCode(type) == TypeCode.String) {
                    var converter = GetConverter(type);
                    void change(string x) => field.SetValue(com, converter(x));
                    property.AddPrimitive(name, val, change);
                } else if (type.IsEnum) {
                    var fields = type.GetFields().ToList();
                    fields.RemoveAt(0);
                    void change(string x) => field.SetValue(com, Enum.Parse(type, x));
                    var values = fields.Select(x => x.Name).ToArray();
                    property.AddSequence(name, val, change, values);
                }
            }
        }
        void Analyze(object val, string name, Type type,
            Action<object> setter, GroupViewProperty property, HashSet<int> hashed) {
            try {
                if (type.IsPrimitive || Type.GetTypeCode(type) == TypeCode.String) {
                    var converter = GetConverter(type);
                    Action<string> change = x => setter(converter(x));
                    property.AddPrimitive(name, val, change);
                    return;
                }
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) {
                    var valuetext = val.IsNull() ? "null" : ((System.Collections.ICollection)val).Count.ToString();
                    var value = $"{type.GenericTypeArguments.First().Name}[{valuetext}]";
                    property.AddPrimitive(name, value, x => { });
                    return;
                }
                var group = new GroupViewProperty();
                group.Title = name;

                if (type.IsValueType && !type.IsEnum) {
                    group.AnalyzeValueType(val, group, type);
                } else {
                    group.Analyze(val, group, type, hashed);
                }
                if (group.Value.Any()) {
                    Value.Add(group);
                } else if (type.IsEnum) {
                    var fields = type.GetFields().ToList();
                    fields.RemoveAt(0);
                    void change(string x) => setter(Enum.Parse(type, x));
                    var values = fields.Select(x => x.Name).ToArray();
                    property.AddSequence(name, val, change, values);
                } else {
                    property.AddPrimitive(name, val, x => { });
                }

            } catch (Exception ex) {
                ex.ToString();
            }
        }

        void Analyze(object com, GroupViewProperty property, PropertyInfo pr, HashSet<int> hashed) {
            var name = pr.Name;
            try {
                if (pr.CustomAttributes.Any(x => x.AttributeType == typeof(IgnoreDebugingAttribute))) {
                    return;
                }

                var val = com.IsNull() ? null : pr.GetValue(com);

                if (val.IsNotNull()) {
                    if (hashed.Contains(val.GetHashCode())) {
                        return;
                    }
                    hashed.Add(pr.GetHashCode());
                }

                var type = pr.PropertyType;
                if (type.IsPrimitive || Type.GetTypeCode(type) == TypeCode.String) {
                    var converter = GetConverter(type);
                    Action<string> change = x => pr.SetValue(com, converter(x));
                    switch (Type.GetTypeCode(type)) {
                        case TypeCode.Boolean:
                            property.AddSequence(name, val, change, new[] { bool.TrueString, bool.FalseString });
                            break;
                        default:
                            property.AddPrimitive(name, val, change);
                            break;
                    }                     
                    return;
                }
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) {
                    var valuetext = val.IsNull() ? "null" : ((System.Collections.ICollection)val).Count.ToString();
                    var value = $"{type.GenericTypeArguments.First().Name}[{valuetext}]";
                    property.AddPrimitive(name, value, x => { });
                    return;
                }

                if (type == typeof(System.Drawing.Bitmap)) {
                    property.AddImage((System.Drawing.Bitmap)val);
                    return;
                }

                var group = new GroupViewProperty();
                group.Title = name;

                if (type.IsValueType && !type.IsEnum) {
                    if (NumericsTypes.IsVector3(type)) {
                        group.AddVector(name, val, x => { pr.SetValue(com, x); });
                    } else {
                        group.AnalyzeValueType(val, group, type);
                    }
                    //switch (type.FullName) {
                    //    case "System.Numerics.Vector3":
                    //        group.AddVector(name, val, x => { pr.SetValue(com, x); });
                    //        break;
                    //    default:
                    //        group.AnalyzeValueType(val, group, type);
                    //        break;
                    //}
                } else if (type.IsArray) {
                    var list = (System.Collections.IList)val;
                    if(list.Count < 10) {
                        for (var index = 0; index < list.Count; index++) {
                            var i = list[index];
                            switch (i) {
                                case System.IO.FileInfo fi:
                                    group.AddFileInfo(new ObjUpdaterInArray(list, index), fi);
                                    break;
                                //default:
                                //    group.Analyze(i, group, i.GetType(), hashed);
                                //    break;
                            }
                        }
                    }                    
                } else {
                    group.Analyze(val, group, type, hashed);
                }
                if (group.Value.Any()) {
                    Value.Add(group);
                } else if (type.IsEnum) {
                    var fields = type.GetFields().ToList();
                    fields.RemoveAt(0);
                    void change(string x) => pr.SetValue(com, Enum.Parse(type, x));
                    var values = fields.Select(x => x.GetValue(com).ToString()).ToArray();
                    property.AddSequence(name, val, change, values);
                } else {
                    property.AddPrimitive(name, val, x => { });
                }

            } catch (Exception ex) {
                ex.ToString();
            }
        }

        protected Func<string, object> GetConverter(Type t) {
            switch (Type.GetTypeCode(t)) {
                case TypeCode.Boolean:
                    return _in => bool.Parse(_in);
                case TypeCode.String:
                    return _in => _in;
                case TypeCode.Single:
                    return _in => float.Parse(_in);
                case TypeCode.Double:
                    return _in => double.Parse(_in);
                case TypeCode.Int32:
                    return _in => int.Parse(_in);
                default:
                    return x => null;
            }

        }
        protected bool IsDefaultValue(Type t, object val) {
            switch (Type.GetTypeCode(t)) {
                case TypeCode.String:
                    return string.IsNullOrWhiteSpace(val.ToString());
                case TypeCode.Single:
                    var fval = float.Parse(val.ToString());
                    return fval == 0 || float.NaN == fval;
                default:
                    return false;
            }

        }
    }

    //=====================================================
    public interface IEditingProperties {
        string Titile { get; }
        object TargetObject { get; }
        void MarkAsModified();
    }

    public class PropertiesEditorWindowViewModel : GroupViewProperty, System.ComponentModel.INotifyPropertyChanged {

        public class ApplyCommand : BaseWPFCommand {
            private PropertiesEditorWindowViewModel _this;
            public ApplyCommand(PropertiesEditorWindowViewModel _this) {
                this._this = _this;
            }
            public override void Execute(object parameter) {
                _this.OnApply();
            }
        }

        public ICommand Apply { get; }
        public IRenderUpdater RenderUpdater { get; set; }

        IEditingProperties item;
        // hash for avoid recurvsive while deeping reflection properties
        readonly HashSet<int> hash;
        public PropertiesEditorWindowViewModel() {
            Apply = new ApplyCommand(this);
            hash = new HashSet<int>();
        }

        public void Analyze(IEditingProperties item) {
            this.item = item;
            var com = item.TargetObject;
            Title = item.Titile;

            Analyze(com, hash);

            OnPropertyChanged(nameof(Title));
        }

        public void Dispose() {
            Value.Clear();
            //  dictionary.Clear();
        }

        void OnApply() {
            Value.Clear();
            hash.Clear();
            item.MarkAsModified();
            Analyze(item.TargetObject, hash);
            RenderUpdater.Update();
        }
    }
}
