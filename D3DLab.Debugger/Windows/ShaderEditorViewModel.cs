using D3DLab.Debugger.IDE;
using D3DLab.ECS.Shaders;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Shaders;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace D3DLab.Debugger.Windows {


    //public sealed class ShaderEditorPopup {
    //    private ShaderEditor win;
    //    public ShaderEditorViewModel ViewModel { get; }

    //    public ShaderEditorPopup() {
    //        win = new ShaderEditor();
    //        ViewModel = (ShaderEditorViewModel)win.DataContext;
    //    }
    //    public void Show() {
    //        win.Show();
    //    }
    //}

    public class EditorHistory {
        public void Undo() {

        }
        public void Redo() {

        }

        public void Clear() {
        }
    }

    public class ShaderSaveCommand : ICommand {
        private ShaderEditorViewModel shaderEditorViewModel;

        public ShaderSaveCommand(ShaderEditorViewModel shaderEditorViewModel) {
            this.shaderEditorViewModel = shaderEditorViewModel;
        }

        public event EventHandler CanExecuteChanged = (x, o) => { };
        public bool CanExecute(object parameter) { return true; }
        public void Execute(object parameter) {
            shaderEditorViewModel.Save();
        }
    }

    public enum IntellisenseTypes {
        AutoComplete,
        Dot,
    }

    public class ShaderTabEditor : INotifyPropertyChanged {

        public class EditorWordSelectedCommand : BaseWPFCommand<TextPoiterSelectionChanged> {
            readonly ShaderDevelopmentEnvironment environment;

            public EditorWordSelectedCommand(ShaderDevelopmentEnvironment environment) {
                this.environment = environment;
            }
            public override void Execute(TextPoiterSelectionChanged selection) {
                try {
                    var world = selection.CaretPointer.GetVariableName();

                    environment.HighlightRelations(world, selection.GetRange());
                } catch (Exception ex) {
                    ex.ToString();
                }

                //if (!string.IsNullOrWhiteSpace(prev)) {
                //    words[prev].ForEach(x => x.Background = Brushes.Transparent);
                //    prev = null;
                //}
                //if (words.TryGetValue(world, out var item)) {
                //    item.ForEach(x => x.Background = Brushes.LightBlue);
                //    prev = world;
                //}
            }
        }
        public class IntellisenseInvokedCommand : BaseWPFCommand {
            readonly ShaderDevelopmentEnvironment environment;

            public IntellisenseInvokedCommand(ShaderDevelopmentEnvironment environment) {
                this.environment = environment;
            }

            public override void Execute(object parameter) {
                var presenter = (IntellisensePopupPresenter)parameter;

                var name = presenter.VariableName;

                IEnumerable<string> result = new string[0];
                if (!string.IsNullOrWhiteSpace(name)) {
                    switch (presenter.Type) {
                        case IntellisenseTypes.Dot:
                            result = environment.GetProperiesOfType(name, presenter.TargetVariable.GetRange());
                            break;
                        case IntellisenseTypes.AutoComplete:
                            result =
                                environment.GetVariablesOfScope(name, presenter.TargetVariable.GetRange())
                                    .Union(environment.GetShaderKeywords(name))
                                    .Union(environment.GetGlobalTypes(name));
                            break;
                    }
                }



                if (!result.Any()) {
                    environment.UnHighlightAll();
                    return;
                }

                presenter.AddRange(result);

                presenter.Show();
            }
        }
        public class DocumentChangedCommand : BaseWPFCommand<TextBoxChangedEventArgs> {
            readonly ShaderTabEditor editor;

            public DocumentChangedCommand(ShaderTabEditor editor) {
                this.editor = editor;
            }
            public override void Execute(TextBoxChangedEventArgs args) {
                var e = args.Args;
                var changes = e.Changes;
                if (!changes.Any()) { return; }
                var change = changes.Last();
                var startPoiter = editor.ShaderDocument.ContentStart.GetPositionAtOffset(change.Offset);
                var endPoiter = startPoiter.GetTextPointAt(change.AddedLength);
                //Document.ContentStart.GetPositionAtOffset(c.Offset + c.AddedLength);
                var range = new TextRange(startPoiter, endPoiter);
                var text = range.Text;

                if (editor.environment.IsKeyword(text)) {
                    //ApplyPropertyValue(TextElement.ForegroundProperty, brush);
                }

                if (text != ";") {
                    return;
                }
                var caret = editor.ShaderDocument.ContentStart.GetOffsetToPosition(args.TextBox.CaretPosition);
                editor.ShaderDocument.Dispatcher.InvokeAsync(() => {
                    editor.ReLoad(new TextRange(editor.ShaderDocument.ContentStart, editor.ShaderDocument.ContentEnd).Text);
                    args.TextBox.CaretPosition = editor.ShaderDocument.ContentStart.GetPositionAtOffset(caret);
                });
                //string text = new TextRange(editor.ShaderDocument.ContentStart, editor.ShaderDocument.ContentEnd).Text;
            }
        }

        public string Header { get { return Info.Name; } }
        public FlowDocument ShaderDocument { get { return environment.ShaderDocument; } }
        public ICommand WordSelected { get; set; }
        public ICommand IntellisenseInvoked { get; set; }
        public ICommand DocumentChanged { get; set; }
        public IShaderInfo Info { get; }
        public ObservableCollection<int> Lines { get; }

        readonly ShaderDevelopmentEnvironment environment;
        readonly IRenderTechniquePass pass;
        public ShaderTabEditor(IShaderInfo info, IRenderTechniquePass pass) {
            this.Info = info;
            this.pass = pass;
            Lines = new ObservableCollection<int>();
            environment = new ShaderDevelopmentEnvironment();
        }
        public void LoadShaderAsync() {
            var text = Info.ReadText();
            ReLoad(text);
        }

        public void ReLoad(string text) {
            IntellisenseInvoked = null;
            WordSelected = null;
            DocumentChanged = null;

            OnPropertyChanged(nameof(IntellisenseInvoked));
            OnPropertyChanged(nameof(WordSelected));
            OnPropertyChanged(nameof(DocumentChanged));

            environment.Read(text);

            IntellisenseInvoked = new IntellisenseInvokedCommand(environment);
            WordSelected = new EditorWordSelectedCommand(environment);
            DocumentChanged = new DocumentChangedCommand(this);

            OnPropertyChanged(nameof(ShaderDocument));
            OnPropertyChanged(nameof(IntellisenseInvoked));
            OnPropertyChanged(nameof(WordSelected));
            OnPropertyChanged(nameof(DocumentChanged));

            var lines = text.Count(x => x == '\n');
            lines.For(x => Lines.Add(x));
        }

        public event PropertyChangedEventHandler PropertyChanged = (x, y) => { };
        void OnPropertyChanged(string name) {
            PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        internal void MarkAsReCompiled() {
            pass.ClearCache();
        }
    }

    public sealed class ShaderEditorViewModel {
        public ICollectionView Tabs { get; }
        private readonly ObservableCollection<ShaderTabEditor> tabs;
        public ICommand SaveCommand { get; }
        public ObservableCollection<string> Errors { get; set; }


        readonly EditorHistory history;
        IRenderTechniquePass[] current;
        IShaderCompilator compilator;
        IRenderUpdater updater;

        public ShaderEditorViewModel() {
            tabs = new ObservableCollection<ShaderTabEditor>();
            Tabs = CollectionViewSource.GetDefaultView(tabs);
            history = new EditorHistory();
            SaveCommand = new ShaderSaveCommand(this);
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

        public void Save() {
            Errors.Clear();
            var selected = GetSelected();
            try {
                var shaderDocument = selected.ShaderDocument;
                string text = new TextRange(shaderDocument.ContentStart, shaderDocument.ContentEnd).Text;

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
