using System;
using System.Windows.Forms;

using Clifton.Core.ExtensionMethods;
using Clifton.WinForm.ServiceInterfaces;

namespace Clifton.WinForm.ServiceInterfaces
{
    public partial class BaseForm : Form, IBaseForm
    {
        public event EventHandler<ProcessCmdKeyEventArgs> ProcessCmdKeyEvent;

        public BaseForm()
        {
            InitializeComponent();
            IsMdiContainer = true;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool ret = false;
            ProcessCmdKeyEventArgs args = new ProcessCmdKeyEventArgs() { KeyData = keyData };
            ProcessCmdKeyEvent.Fire(this, args);
            ret = args.Handled;

            if (!ret)
            {
                ret = base.ProcessCmdKey(ref msg, keyData);
            }

            return ret;
        }
    }
}
