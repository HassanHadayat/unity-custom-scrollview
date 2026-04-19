using CustomScrollView.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace CustomScrollView.Demo
{
    public abstract class CakeCardViewBase : MonoBehaviour, IScrollCell
    {
        [SerializeField] protected Text NameTxt;
        [SerializeField] protected Image Icon;
        [SerializeField] protected GameObject[] Stars;
        [SerializeField] protected GameObject LockOverlay;
        
        public virtual CakeType CakeType{get;}
        

        public virtual void UpdateView(CakeData data)
        {
            NameTxt.text = data.Name;
            Icon.sprite = data.Icon;
            for (int i = 0; i < Stars.Length; i++)
            {
                Stars[i].SetActive(i<data.Stars);
            }
            LockOverlay.SetActive(data.IsLocked);
        }

        public virtual void OnFill(int section, int index)
        {
            var data = Demo00_Main.Instance.CakeDataProvider.GetData(section, index);
            UpdateView(data);
        }
    }
}