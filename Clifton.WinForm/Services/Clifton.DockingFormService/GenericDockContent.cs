using WeifenLuo.WinFormsUI.Docking;

using Clifton.WinForm.ServiceInterfaces;

namespace Clifton.DockingFormService
{
    public class GenericDockContent : DockContent, IDockDocument
    {
        public string Metadata { get; set; }

        public GenericDockContent(string metadata = "")
        {
            Metadata = metadata;
        }

        protected override string GetPersistString()
        {
            return GetType().ToString() + "," + Metadata;
        }
    }
}
