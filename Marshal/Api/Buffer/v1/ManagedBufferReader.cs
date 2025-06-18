using System.Reflection;
using System.Runtime.CompilerServices;

namespace LinuxDedicatedServer.Api.Buffer.v1;

public class ManagedBufferReader
{
    private readonly ManagedFactory _factory = new ManagedFactory();

    public static async Task<T> ReadTuple<T>(Stream stream) where T : ITuple
    {
        var reader = new ManagedBufferReader();
        reader.AddTypes(typeof(T).GetGenericArguments());
        
        List<object> constructor = [.. await reader.Open(stream)];

        return (T)Activator.CreateInstance(typeof(T), constructor.ToArray())!;
    }

    public static async Task<T> ReadStruct<T>(Stream stream) where T : struct
    {
        var reader = new ManagedBufferReader();
        var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public);
        int index = 0;

        reader.AddTypes(fields.Select(x => x.FieldType));

        T result = Activator.CreateInstance<T>();

        foreach (var value in await reader.Open(stream))
        {
            fields[index].SetValueDirect(__makeref(result), value);
            index++;
        }

        return result;
    }

    public static async Task<T> ReadClass<T>(Stream stream) where T : class
    {
        var reader = new ManagedBufferReader();
        var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var index = 0;

        reader.AddTypes(properties.Select(x => x.PropertyType));

        T result = Activator.CreateInstance<T>();

        foreach (var value in await reader.Open(stream))
        {
            properties[index].SetValue(result, value);
            index++;
        }

        return result;
    }

    public ManagedBufferReader AddType(Type type)
    {
        _factory.AddType(type);
        return this;
    }

    public ManagedBufferReader AddType<T>()
    {
        return AddType(typeof(T));
    }

    public void AddTypes(IEnumerable<Type> types)
    {
        foreach (var type in types)
        {
            AddType(type);
        }
    }

    private BufferFactory BuildBody(Queue<int> header)
    {
        var factory = new BufferFactory();

        foreach (var type in _factory.BufferTypes)
        {
            if (ManagedTypeResolver.Resolve(type, out var resolver))
            {
                resolver.ReadAddValue(factory, type, header.Dequeue());
            }
            else
            {
                factory.AddType(type);
            }
        }

        return factory;
    }

    public async Task<ReaderEnumerator> OpenEnumerator(Stream stream)
    {
        var header = new Queue<int>();

        var headerReader = await _factory.BuildHeader().ReadAsync(stream);

        for (int i = 0; i < _factory.HeaderLength; i++)
        {
            header.Enqueue(headerReader.Read<int>());
        }

        var bodyReader = await BuildBody(new Queue<int>(header)).ReadAsync(stream);

        return new ReaderEnumerator(_factory, header, bodyReader);
    }

    public async Task<IEnumerable<object>> Open(Stream stream)
    {
        return (await OpenEnumerator(stream)).AsEnumerable();
    }

    public class ReaderEnumerator(ManagedFactory factory, Queue<int> header, BufferReader reader) : IEnumerator<object>
    {
        private readonly ManagedFactory _factory = factory;
        private readonly Queue<int> _lengths = header;
        private readonly BufferReader _reader = reader;
        private int _position = 0;

        public Type CurrentType { get; private set; }
        public object Current { get; private set; }

        public T CastCurrent<T>()
        {
            return (T)Current;
        }

        public void Dispose() { }

        public bool MoveNext()
        {
            if (_position >= _factory.BufferTypes.Count)
            {
                return false;
            }

            CurrentType = _factory.BufferTypes[_position];

            if (ManagedTypeResolver.Resolve(CurrentType, out var resolver))
            {
                Current = resolver.ReadValue(_reader, CurrentType, _lengths.Dequeue());
            }
            else
            {
                Current = _reader.ReflectionRead(CurrentType);
            }

            _position++;

            return true;
        }

        public void Reset()
        {
            throw new NotSupportedException("Cannot reset the position of this enumerator");
        }

        public IEnumerable<object> AsEnumerable()
        {
            while (MoveNext())
            {
                yield return Current;
            }
        }
    }
}