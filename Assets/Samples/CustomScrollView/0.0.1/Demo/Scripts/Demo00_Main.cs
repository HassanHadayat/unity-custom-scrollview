using System;
using System.Collections.Generic;
using System.Linq;
using CustomScrollView.Controller;
using CustomScrollView.Data;
using UnityEngine;

namespace CustomScrollView.Demo
{
    /// <summary>
    /// Demo 00: 
    /// </summary>
    public class Demo00_Main : MonoBehaviour
    {
        public static Demo00_Main Instance;
        
        [Header("Unity Scroll View")]
        public GameObject _usv;
        [SerializeField] private RectTransform _usvContent;
        
        [Header("Custom Scroll View")]
        public GameObject _csv;
        [SerializeField] private ScrollViewController _csvController;
        [SerializeField] private float _itemHeight = 120f;
        
        [Header("Data")]
        [SerializeField] private Sprite[] _roundCakeSprites;
        [SerializeField] private Sprite[] _roundBigCakeSprites;
        [SerializeField] private Sprite[] _cupCakeSprites;
        [SerializeField] private Sprite[] _pieceCakeSprites;
        private readonly Dictionary<CakeType, List<Sprite>> _cakesSprites = new ();
        public CakeDataProvider CakeDataProvider;
        
        [Header("Prefabs")]
        [SerializeField] private RoundCakeCardView _roundCakePrefab;
        [SerializeField] private RoundBigCakeCardView _roundBigCakePrefab;
        [SerializeField] private CupCakeCardView _cupCakePrefab;
        [SerializeField] private PieceCakeCardView _pieceCakePrefab;
        private Dictionary<CakeType, CakeCardViewBase> _cakesPrefabs = new ();

        
        private void Awake()
        {
            Application.targetFrameRate = 120;
            
            if (Instance == null)
                Instance = this;
            else
                Destroy(this);
        }

        private void Start()
        {
            Init();
            SwitchScrollView();
        }
        
        // -------------------------------------------------------------------------------------------------------------
        // Initialization
        private void Init()
        {
            InitData();

            InitUnityScrollView();
            InitCustomScrollView();
        }
        private void InitData()
        {
            // Init Sprites
            _cakesSprites[CakeType.Round] = new ();
            _cakesSprites[CakeType.Round].AddRange(_roundCakeSprites.ToList());
            
            _cakesSprites[CakeType.RoundBig] = new ();
            _cakesSprites[CakeType.RoundBig].AddRange(_roundBigCakeSprites.ToList());
            
            _cakesSprites[CakeType.CupCake] = new ();
            _cakesSprites[CakeType.CupCake].AddRange(_cupCakeSprites.ToList());
            
            _cakesSprites[CakeType.Piece] = new ();
            _cakesSprites[CakeType.Piece].AddRange(_pieceCakeSprites.ToList());
            
            // Init Prefabs
            foreach (var kvp in _cakesSprites)
            {
                _cakesPrefabs[kvp.Key] = GetPrefab(kvp.Key);
            }
            
            // Init Data
            
            Dictionary<CakeType, List<CakeData>> cakesData = new ();
            foreach (var kvp in _cakesSprites)
            {
                cakesData[kvp.Key] = new();
                foreach (var sprite in kvp.Value)
                {
                    var data = new CakeData(
                        type: kvp.Key,
                        name: sprite.name,
                        icon: sprite,
                        stars: (int)kvp.Key+1,
                        isLocked: false
                        );
                    cakesData[kvp.Key].Add(data);;
                }
            }

            CakeDataProvider = new CakeDataProvider(cakesData);
        }
        
        // -------------------------------------------------------------------------------------------------------------
        // Unity Scroll View
        private void InitUnityScrollView()
        {
            for (int i = 0; i < CakeDataProvider.GetTotalCount(); i++)
            {
                foreach (var data in CakeDataProvider.GetTypeData(i))
                {
                    var card = Instantiate(GetPrefab(data.Type), _usvContent);
                    if (card.TryGetComponent(out CakeCardViewBase view))
                    {
                        view.UpdateView(data);
                    }
                }
            }
        }
        
        // -------------------------------------------------------------------------------------------------------------
        // Custom Scroll View
        private void InitCustomScrollView()
        {
            var ds = new SimpleDataSource();

            for (int s = 0; s < CakeDataProvider.GetTotalCount(); s++)
            {
                int sec = ds.AddSection();
                ds.AddItems(sec, CakeDataProvider.GetTypeData(sec).Count, _itemHeight);
            }
            
            _csvController.Initialize(ds, ProvideCellPrefab);
        }

        private GameObject ProvideCellPrefab(int sec, int index)
        {
            if (sec < 0 || sec >= Enum.GetNames(typeof(CakeType)).Length)
            {
                return null;
            }
            
            CakeType type = (CakeType)sec;
            return _cakesPrefabs[type].gameObject;
        }
        
        // -------------------------------------------------------------------------------------------------------------
        // UI Switcher
        public void SwitchScrollView()
        {
            bool isCSVEnabled = _csv.activeSelf;
            
            _usv.SetActive(isCSVEnabled);
            _csv.SetActive(!isCSVEnabled);
        }
        
        // -------------------------------------------------------------------------------------------------------------
        // Helper
        private CakeCardViewBase GetPrefab(CakeType type)
        {
            return type switch
            {
                CakeType.Round => _roundCakePrefab,
                CakeType.RoundBig => _roundBigCakePrefab,
                CakeType.CupCake => _cupCakePrefab,
                CakeType.Piece => _pieceCakePrefab,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
        
    }
}