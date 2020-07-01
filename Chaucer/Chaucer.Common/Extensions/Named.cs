namespace Chaucer.Common.Extensions
{
    public class Named<T>
    {
        public string Name { get; set; }
        public T Value { get; set; }
    }
}