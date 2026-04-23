using UnityEngine;

namespace FortuneValley.City.Cars
{
    /// <summary>
    /// Fixed-size pool of pre-instantiated Car instances. Drop a Car prefab
    /// into _carPrefab and set _poolSize; the pool instantiates once at
    /// Awake and exposes Get/Return by index.
    /// </summary>
    public class CarPool : MonoBehaviour
    {
        [SerializeField] private Car _carPrefab;
        [SerializeField] private int _poolSize = 16;
        [SerializeField] private Transform _parent;

        private Car[] _cars;

        public int Size => _poolSize;
        public Car GetAt(int index) => _cars[index];

        private void Awake()
        {
            InstantiateAll();
        }

        private void InstantiateAll()
        {
            _cars = new Car[_poolSize];
            Transform parent = _parent != null ? _parent : transform;
            for (int i = 0; i < _poolSize; i++)
            {
                Car instance = Instantiate(_carPrefab, parent);
                instance.name = $"Car_{i}";
                instance.gameObject.SetActive(false);
                _cars[i] = instance;
            }
        }
    }
}
