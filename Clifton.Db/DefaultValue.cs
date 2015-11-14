namespace Clifton.Db
{
    public abstract class DefaultValue
    {
        public abstract object GetValue();
        public virtual bool IsNotNull
        {
            get
            {
                // Why would a default value be null?
                return true;
            }
        }
    }
}