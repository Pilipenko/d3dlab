using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using D3DLab.Debugger.Windows;
using D3DLab.ECS.Shaders;
using D3DLab.Std.Engine.Core.Shaders;

namespace D3DLab.Debugger.Presentation.Avalonia {
    public sealed class ShaderEditorPopup {
        private AvaloniaShaderEditor win;
        public ShaderEditorVM ViewModel { get; }

        public ShaderEditorPopup() {
            win = new AvaloniaShaderEditor();
            ViewModel = (ShaderEditorVM)win.DataContext;
        }
        public void Show() {
            win.Show();
        }
    }

    public class ShaderEditorVM {
        class SaveCommand : ICommand {
            private ShaderEditorVM facade;

            public SaveCommand(ShaderEditorVM shaderEditorViewModel) {
                this.facade = shaderEditorViewModel;
            }

            public event EventHandler CanExecuteChanged = (x, o) => { };
            public bool CanExecute(object parameter) { return true; }
            public void Execute(object parameter) {
                facade.SaveSelected();
            }
        }

        class ShaderTabEditor : NotifyProperty {
            public string Header { get { return Info.Name; } }
            public IShaderInfo Info { get; }
            public string ShaderText { get; set; }

            readonly IRenderTechniquePass pass;

            public ShaderTabEditor(IShaderInfo info, IRenderTechniquePass pass) {
                this.Info = info;
                this.pass = pass;
            }

            public void LoadShaderAsync() {
                var text = Info.ReadText();
                ReLoad(text);
            }

            public void ReLoad(string text) {
                ShaderText = text;
                RisePropertyChanged(nameof(ShaderText));
            }
            internal void MarkAsReCompiled() {
                pass.ClearCache();
            }
        }

        public ICollectionView Tabs { get; }
        private readonly ObservableCollection<ShaderTabEditor> tabs;
        public ICommand Save { get; }
        public ObservableCollection<string> Errors { get; set; }


        readonly EditorHistory history;
        IRenderTechniquePass[] current;
        IShaderCompilator compilator;
        IRenderUpdater updater;

        public ShaderEditorVM() {
            tabs = new ObservableCollection<ShaderTabEditor>();
            Tabs = CollectionViewSource.GetDefaultView(tabs);
            history = new EditorHistory();
            Save = new SaveCommand(this);
            Errors = new ObservableCollection<string>();
        }

        public void LoadShader(IRenderTechniquePass[] pass, IShaderCompilator compilator, IRenderUpdater updater) {
            this.updater = updater;
            current = pass;
            this.compilator = compilator;
            history.Clear();

            foreach (var p in pass) {
                foreach (var sh in p.ShaderInfos) {
                    var tab = new ShaderTabEditor(sh, p);
                    tabs.Add(tab);
                    tab.LoadShaderAsync();
                }
            }
            Tabs.MoveCurrentToFirst();

        }

        private ShaderTabEditor GetSelected() {
            return (ShaderTabEditor)Tabs.CurrentItem;
        }

        public void SaveSelected() {
            Errors.Clear();
            var selected = GetSelected();
            try {
                var text = selected.ShaderText;
                compilator.CompileWithPreprocessing(selected.Info, text);
                selected.Info.WriteText(text);
                selected.MarkAsReCompiled();
                Errors.Insert(0, $"{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss")} Compile: {selected.Info.Stage} succeeded");
                updater.Update();
            } catch (Exception ex) {
                foreach (var line in Regex.Split(ex.Message, Environment.NewLine)) {
                    Errors.Insert(0, line);
                }
                Errors.Add($"{DateTime.Now.TimeOfDay.ToString(@"hh\:mm\:ss")} Compile: {selected.Info.Stage} failed");
            }
        }
    }
}
