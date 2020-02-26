using System;
using System.Collections.Generic;
using System.Text;
using WPFLab;
using WPFLab.MVVM;

namespace D3DLab.Viewer.Presentation {
    class MainWindowViewModel : BaseNotify {
        readonly IDependencyResolver container;
        public MainWindowViewModel(IDependencyResolver container) {
            this.container = container;

           
        }


    }
}
