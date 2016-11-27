using WeifenLuo.WinFormsUI.Docking;

namespace Clifton.DockingFormService
{
    public class GenericDockContent : DockContent
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
