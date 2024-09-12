namespace CTCommonUI
{
    public interface IContainer
    {
        public T? GetService<T>();
    }
}