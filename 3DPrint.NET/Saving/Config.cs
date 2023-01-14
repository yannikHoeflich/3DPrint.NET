namespace _3DPrint.NET.Saving;

public class Config {
    public static Config Current { get; internal set; }  

    private readonly Dictionary<string, object> _properties = new();

    internal Config() {}

    internal Config(IReadOnlyDictionary<string, object> properties) {
        _properties = new Dictionary<string, object>(properties);
    }

    public T Get<T>() where T : new(){
        Type type = typeof(T);
        return (T)_properties.GetOrAdd(type.FullName, () => Activator.CreateInstance(type));
    }

    internal IReadOnlyDictionary<string, object> GetProperties() => _properties;
}
