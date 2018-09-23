using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    private Renderer Rend;
    private Environment Env;
    private Environment.IntVector3 Coord;
    public float changeRate = 5f;

    public const float capacity = 100f;
    public float filled = 0f;
    public float filledPercent = 0f;
    public List<Matter> content = new List<Matter>();
    public MatterType currentType;

    public class Matter
    {
        public MatterType type;
        public float amount;
        public float percent;

        public Matter(MatterType _type, float _amount)
        {
            type = _type;
            amount = 0;
            AddAmount(_amount);
        }

        public MatterChange AddAmount(float addAmount)
        {
            MatterChange change = new MatterChange(type, Mathf.Clamp(amount + addAmount, 0, capacity) - amount);
            amount += change.amount;
            percent = amount / Tile.capacity;
            return change;
        }

        public override string ToString()
        {
            return amount + " " + type.ToString() + " (" + percent * 100f + "%)";
        }
    }

    public struct MatterChange
    {
        public MatterType type;
        public float amount;

        public MatterChange(MatterType _type, float _amount)
        {
            type = _type;
            amount = _amount;
        }

        public override string ToString()
        {
            return amount + " " + type.ToString();
        }
    }

    public enum MatterType
    {
        Nothing,
        Hydrogen,
        Oxygen,
        Nitrogen,
        CarbonDioxide
    }

    public void AddMatter(MatterChange newMatter)
    {
        MatterChange clamped = new MatterChange(newMatter.type, Mathf.Clamp(filled + newMatter.amount, 0, capacity) - filled);
        int found = content.FindIndex(m => m.type == clamped.type);
        MatterChange change;
        if (found >= 0)
        {
            change = content[found].AddAmount(clamped.amount);
        }
        else
        {
            content.Add(new Matter(clamped.type, clamped.amount));
            change = clamped;
        }
        filled += change.amount;
        filledPercent = (float)filled / (float)capacity;
    }

    public void Init(Environment env, Environment.IntVector3 coord)
    {
        Env = env;
        Coord = coord;
        Vector3 size = GetComponent<Renderer>().bounds.size;
        Vector3 pos = this.transform.position;
        transform.position = new Vector3(pos.x + (size.x * coord.x), pos.y + (size.y * coord.y), pos.z + (size.z * coord.z));
    }

    private void Awake()
    {
        Rend = GetComponent<Renderer>();
    }

    private void OnMouseOver()
    {
        AddCurrentMatterType();
        Env.selected = Coord;
        Env.CurrentTileText.text = string.Empty;
        content.ForEach(m =>
        {
            Env.CurrentTileText.text += string.Format("{0:0.0000} {1}\n", m.amount, m.type.ToString());
        });
        Env.CurrentTileText.text += string.Format("{0:0.00}%\n", filledPercent * 100);
        Env.CurrentTileText.text += currentType.ToString();
    }

    private void AddCurrentMatterType()
    {
        if (currentType == MatterType.Nothing)
        {
            return;
        }
        if (Input.GetMouseButton(0))
        {
            AddMatter(new MatterChange(currentType, changeRate));
        }
        else if (Input.GetMouseButton(1))
        {
            AddMatter(new MatterChange(currentType, -changeRate));
        }
    }

    private void Update()
    {
        UpdateDisplay();

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetType(MatterType.Hydrogen);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetType(MatterType.Oxygen);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetType(MatterType.Nitrogen);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SetType(MatterType.CarbonDioxide);
        }
    }

    private void SetType(MatterType type)
    {
        currentType = type;
        Env.CurrentTileText.text = currentType.ToString();
    }

    private void UpdateDisplay()
    {
        Color color = new Color();
        float metallic = 0f;
        foreach (Matter m in content)
        {
            switch (m.type)
            {
                case MatterType.Hydrogen:
                    color.r = m.percent;
                    break;
                case MatterType.Oxygen:
                    color.b = m.percent;
                    break;
                case MatterType.Nitrogen:
                    color.g = m.percent;
                    break;
                case MatterType.CarbonDioxide:
                    metallic = m.percent;
                    break;
            }
        }
        color.a = filledPercent * 0.5f;
        Rend.material.color = color;
        Rend.material.SetFloat("_Metallic", metallic);
    }
}
