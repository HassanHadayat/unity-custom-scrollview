using UnityEngine;

namespace CustomScrollView.Demo
{
    public class CakeData
    {
        public CakeType Type;
        public string Name;
        public Sprite Icon;
        public int Stars;

        public bool IsLocked;

        public CakeData(CakeType type, string name, Sprite icon, int stars, bool isLocked)
        {
            Type = type;
            Name = name;
            Icon = icon;
            Stars = stars;
            IsLocked = isLocked;
        }
        public override string ToString()
        {
            return $"Type: {Type}, Name: {Name}, Stars: {Stars}";
        }
    }

    public enum CakeType
    {
        Round, RoundBig, CupCake, Piece
    }
}