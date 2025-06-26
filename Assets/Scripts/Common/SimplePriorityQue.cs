#nullable enable
using System;

public class SimplePriorityQue<K, V> where K:IComparable<K>
{
    private K?[] _keys; 
    private V?[] _values;
    
    private int _size;
    
    public int Size => _size;
    
    public bool IsEmpty => _size == 0;
    public bool IsNotEmpty => _size > 0;
    
    public SimplePriorityQue(int maxSize)
    {
        _keys = new K?[maxSize + 1];
        _values = new V?[maxSize + 1];
    }

    public SimplePriorityQue((K, V)[] keyValuePairs)
    {
        _keys = new K?[keyValuePairs.Length + 1];
        _values = new V?[keyValuePairs.Length + 1];

        foreach ((K key, V value) keyValuePair in keyValuePairs)
        {
            Insert(keyValuePair.key, keyValuePair.value);
        }
    }

    public void Insert(K key, V value)
    {
        if (_keys.Length == _size + 1)
        {
            Array.Resize(ref _keys, _keys.Length * 2);
            Array.Resize(ref _values, _values.Length * 2);
        }
        
        _size++;
        _keys[_size] = key;
        _values[_size] = value;
        Swim(_size);
    }

    public V Max()
    {
        return _values[1]!;
    }

    public V DeleteMax()
    {
        V max = _values[1]!;

        Exchange(1, _size);
        _keys[_size] = default;
        _values[_size] = default;
        _size -= 1;
        Sink(1);
        
        if (_keys.Length > 8 &&  _size > 0 &&  _keys.Length / 4 > _size)
        {
            Array.Resize(ref _keys, _keys.Length / 2);
            Array.Resize(ref _values, _values.Length / 2);
        }
        
        return max;
    }

    private bool Less(int i, int j)
    {
        return _keys[i]!.CompareTo(_keys[j]!) < 0;
    }

    private void Exchange(int i, int j)
    {
        K tempKey = _keys[i]!;
        V tempValue = _values[i]!;
        
        _keys[i] = _keys[j];
        _values[i] = _values[j];
        
        _keys[j] = tempKey;
        _values[j] = tempValue;
    }

    private void Swim(int k)
    {
        // if the parent node is less than K we need to swap
        while (k > 1 && Less(k / 2, k))
        {
            Exchange(k/2, k);
            k /= 2;
        }
    }

    private void Sink(int k)
    {
        while (2 * k <= _size)
        {
            int j = 2 * k; // child left
            if (j < _size && Less(j, j + 1))
            {
                j+=1; // the right child j+1 was bigger
            }
            Exchange(k, j);
            k = j;
        }
        
    }
}