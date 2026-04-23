using UnityEngine;
using UnityEngine.Splines;
using FortuneValley.Core;

namespace FortuneValley.City.Cars
{
    /// <summary>
    /// Assigns pooled Cars to authored SplineContainer routes and scales the
    /// active car count with total lot tier progress across the city.
    /// Subscribes to OnLotTierChanged so buying and upgrading lots brings
    /// more cars onto the roads.
    /// </summary>
    public class CarSpawner : MonoBehaviour
    {
        [Header("Pool & Routes")]
        [SerializeField] private CarPool _pool;
        [Tooltip("Authored SplineContainer GameObjects along the roads. Each car is assigned one.")]
        [SerializeField] private SplineContainer[] _routes;

        [Header("Count Scaling")]
        [SerializeField] private int _baseCount = 1;
        [Tooltip("Extra cars per unit of total tier progress (sum of every owned lot's tier).")]
        [SerializeField] private float _perTierMultiplier = 0.5f;

        [Header("Playback")]
        [Tooltip("Seconds for a car to traverse its full spline once.")]
        [SerializeField] private float _loopDuration = 30f;

        private readonly CarCountCalculator _calculator = new CarCountCalculator();
        private int _activeCount;

        private void OnEnable()
        {
            GameEvents.OnLotTierChanged += HandleTierChanged;
            GameEvents.OnLotPurchased += HandleLotPurchased;
        }

        private void OnDisable()
        {
            GameEvents.OnLotTierChanged -= HandleTierChanged;
            GameEvents.OnLotPurchased -= HandleLotPurchased;
        }

        private void Start()
        {
            UpdateActiveCount(_baseCount);
        }

        private void HandleLotPurchased(string lotId, FortuneValley.Domain.Enums.Owner owner)
        {
            _calculator.SetLotTier(lotId, 1);
            RecomputeCount();
        }

        private void HandleTierChanged(string lotId, int newTier)
        {
            _calculator.SetLotTier(lotId, newTier);
            RecomputeCount();
        }

        private void RecomputeCount()
        {
            int target = _calculator.ComputeTarget(_baseCount, _perTierMultiplier);
            int clamped = Mathf.Clamp(target, 0, _pool != null ? _pool.Size : 0);
            UpdateActiveCount(clamped);
        }

        private void UpdateActiveCount(int target)
        {
            if (_pool == null || _routes == null || _routes.Length == 0) return;

            for (int i = 0; i < _pool.Size; i++)
            {
                Car car = _pool.GetAt(i);
                if (car == null) continue;

                if (i < target)
                {
                    SplineContainer route = _routes[i % _routes.Length];
                    float startT = (float)i / _pool.Size;
                    car.Show();
                    car.AssignRoute(route, _loopDuration, startT);
                }
                else
                {
                    car.StopAndHide();
                }
            }
            _activeCount = target;
        }
    }
}
