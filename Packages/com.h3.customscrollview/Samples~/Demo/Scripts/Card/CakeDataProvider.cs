using System.Collections.Generic;

namespace CustomScrollView.Demo
{
    public class CakeDataProvider
    {
        private readonly Dictionary<CakeType, List<CakeData>> _data = new ();
        
        public CakeDataProvider(Dictionary<CakeType, List<CakeData>> data)
        {
            if(data is null) return;
            
            _data = data;
        }
        public CakeData GetData(int type, int index)
        {
            return _data[(CakeType)type][index];
        }
        public List<CakeData> GetTypeData(int type)
        {
            return _data[(CakeType)type];
        }
        public int GetTotalCount()
        {
            return _data.Count;
        }
    }
}