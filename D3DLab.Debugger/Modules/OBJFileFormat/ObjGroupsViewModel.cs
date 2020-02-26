using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using D3DLab.ECS;
using D3DLab.ECS.Components;
using D3DLab.ECS.Ext;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Common;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Components.Materials;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.GameObjects;
using D3DLab.Std.Engine.Core.MeshFormats;

namespace D3DLab.Debugger.Modules.OBJFileFormat {
    public class CompositeGameObjectFromFile : CompositeGameObject {
        public IGraphicComponent FileComp { get; set; }
        public CompositeGameObjectFromFile(string desc) : base(desc) { }
    }


    public static class ObjDetailsPopup {
        static ObjDetailsWindow win;
        static readonly NullVerificationLock<ObjDetailsWindow> loker;
        static ObjDetailsPopup() {
            loker = new NullVerificationLock<ObjDetailsWindow>();
        }

        static ObjDetailsWindow Create() {
            var win = new ObjDetailsWindow();
            win.Closed += Win_Closed;
            return win;
        }

        static void Win_Closed(object sender, EventArgs e) {
            loker.Destroy(ref win, Cleanup);
        }

        static ObjDetailsWindow Cleanup() {
            win.Closed -= Win_Closed;
            return null;
        }

        public static void Open(CompositeGameObjectFromFile gobj, IEntityManager manager) {
            loker.Create(ref win, Create);
            var vm = ((ObjGroupsViewModel)win.DataContext);
            vm.Fill(gobj, manager);
            win.Show();
        }
        public static void Close() {
            //loker.Destroy(ref win, Cleanup);
            win.Close();
        }
    }

    public class Controler {
        public ICommand Refresh { get; }

        public ICommand CopyGroupName { get; }
        public ICommand VisiblityChanged { get; }

        public ICommand AddNewColorFilter { get; }
        public ICommand RemoveColorFilter { get; }

        public ICommand ShowHideGroup { get; }
        public ICommand HighlightGroup { get; }
        //
        public ICommand ShowHideAll { get; }

        readonly ObjGroupsViewModel facade;

        public Controler(ObjGroupsViewModel facade) {
            this.facade = facade;
            ShowHideGroup = new WpfActionCommand<bool>(OnShowHideGroup);
            HighlightGroup = new WpfActionCommand<bool>(OnHighlightGroup);
            AddNewColorFilter = new WpfActionCommand(OnAddNewColorFilter);

            Refresh = new WpfActionCommand(OnRefresh);
            CopyGroupName = new WpfActionCommand<string>(OnWpfActionCommand);
            VisiblityChanged = new WpfActionCommand<ObjGroupsViewModel.ObjGroupViewItem>(OnVisiblityChanged);
        }

        void OnVisiblityChanged(ObjGroupsViewModel.ObjGroupViewItem item) {
            facade.ShowHideItem(item, item.IsVisible);
        }

        void OnWpfActionCommand(string name) {
            facade.AddNewColorFilter(name, Colors.White);
        }

        void OnRefresh() {
            facade.Refresh();
        }

        void OnAddNewColorFilter() {
            facade.AddNewColorFilter(null, Colors.White);
        }

        void OnHighlightGroup(bool obj) {

        }
        void OnShowHideGroup(bool show) {

        }
    }

    public class ObjGroupsViewModel : NotifyProperty {
        public class ColorFilterViewItem {
            private string filter;
            private string color;

            public string Filter {
                get => filter;
                set {
                    if (string.IsNullOrWhiteSpace(value)) { return; }
                    filter = value;
                }
            }
            public string Color {
                get => color;
                set {
                    if (string.IsNullOrWhiteSpace(value)) { return; }
                    color = value;
                }
            }
        }

        public class ObjGroupViewItem : NotifyProperty {
            public string Name { get; }
            public int Count => Groups.Count;
            public bool IsVisible {
                get => _isVisible;
                set {
                    _isVisible = value;
                    RisePropertyChanged(nameof(IsVisible));
                }
            }

            public readonly List<ObjGroup> Groups;
            public readonly ElementTag Entity;
            private bool _isVisible;

            public ObjGroupViewItem(ElementTag entity, string name) {
                Entity = entity;
                Groups = new List<ObjGroup>();
                IsVisible = true;
                Name = name;
            }
        }


        public int ItemsCount { get; set; }
        public int AllGroupsCount { get; set; }

        public string Filder {
            get => filder;
            set {
                filder = value;
                Filter(filder);
            }
        }
        string filder;

        public ObservableCollection<ColorFilterViewItem> FilterColors { get; }
        public Controler Controler { get; }
        public ICollectionView ObjGroups { get; private set; }
        ObservableCollection<ObjGroupViewItem> items;
        ListCollectionView listView;
        CompositeGameObjectFromFile gobj;
        IEntityManager entityManager;

        public ObjGroupsViewModel() {
            Controler = new Controler(this);
            FilterColors = new ObservableCollection<ColorFilterViewItem>();

            AddNewColorFilter("^A ID*?", Colors.Blue);
            AddNewColorFilter("^C ID*?", Colors.Green);
            AddNewColorFilter("^I ID*?", Colors.Yellow);
        }

        internal void ShowHideItem(ObjGroupViewItem item, bool show) {
            var rendercoms = entityManager
                .GetEntity(item.Entity)
                .GetComponent<IRenderableComponent>()
                .CanRender = show;

            //items.Where(x => x.Entity == item.Entity).ForEach(x => x.IsVisible = show);

            entityManager.PushSynchronization();
        }
        internal void AddNewColorFilter(string filer, Color color) {
            FilterColors.Add(new ColorFilterViewItem {
                Color = color.ToString(),
                Filter = filer,
            });
        }
        internal void Refresh() {
            Refresh(items);
        }

        void Refresh(IEnumerable<ObjGroupViewItem> groups) {
            var regex = new Tuple<Regex, Vector4>[FilterColors.Count];
            for (var i = 0; i < FilterColors.Count; i++) {
                var item = FilterColors[i];
                var color = (Color)ColorConverter.ConvertFromString(item.Color);
                regex[i] = Tuple.Create(
                    new Regex(item.Filter, RegexOptions.Compiled),
                    new Vector4(color.ScR, color.ScG, color.ScB, color.ScA));
            }
            //keep the order of regex
            foreach (var reg in regex) {
                foreach (var item in groups) {
                    var match = reg.Item1.IsMatch(item.Name);
                    if (match) {
                        ChangeColor(item, reg.Item2);
                    }
                }
            }


            //foreach (var item in groups) {
            //    var match = regex.FirstOrDefault(x => x.Item1.IsMatch(item.Name));
            //    if (match.IsNotNull()) {
            //        ChangeColor(item, match.Item2);
            //    }
            //}

            entityManager.PushSynchronization();
        }

        void ChangeColor(ObjGroupViewItem item, Vector4 color) {
            entityManager
                .GetEntity(item.Entity)
                .UpdateComponent(ColorComponent.CreateDiffuse(color));
        }

        ObjGroupViewItem GetCurrentItem() {
            return ObjGroups.CurrentItem as ObjGroupViewItem;
        }

        internal void Fill(CompositeGameObjectFromFile gobj, IEntityManager entityManager) {
            var coms = gobj.GetEntities(entityManager)
                .Select(x => x.GetComponent<VirtualGroupGeometryComponent>());
            if (!coms.Any()) {
                return;
            }

            this.gobj = gobj;
            this.entityManager = entityManager;

            items = new ObservableCollection<ObjGroupViewItem>();

            foreach (var part in coms) {
                var item = new ObjGroupViewItem(part.EntityTag, part.PartGeometry.Name);
                items.Add(item);
                item.Groups.AddRange(part.PartGeometry.Groups);
            }

            ObjGroups = CollectionViewSource.GetDefaultView(items);
            ObjGroups.SortDescriptions.Add(new SortDescription(nameof(ObjGroupViewItem.Count), ListSortDirection.Descending));

            listView = (ListCollectionView)ObjGroups;
            RisePropertyChanged(nameof(ObjGroups));

            UpdateCount();
            Refresh();
        }

        void Filter(string filder) {
            if (ObjGroups.IsNull()) { return; }
            if (string.IsNullOrWhiteSpace(filder)) {
                ObjGroups.Filter = x => true;
            } else {
                try {
                    var f = new Regex(filder, RegexOptions.Compiled);
                    ObjGroups.Filter = x => {
                        var item = (ObjGroupViewItem)x;
                        return f.IsMatch(item.Name);
                    };
                } catch {
                    ObjGroups.Filter = x => {
                        var item = (ObjGroupViewItem)x;
                        return item.Name.Contains(filder);
                    };
                }
            }
            UpdateCount();
        }

        void UpdateCount() {
            ItemsCount = listView.Count;
            RisePropertyChanged(nameof(ItemsCount));

            var count = 0;
            foreach (var i in listView) {
                count += ((ObjGroupViewItem)i).Count;
            }
            AllGroupsCount = count;
            RisePropertyChanged(nameof(AllGroupsCount));
        }


    }

}
