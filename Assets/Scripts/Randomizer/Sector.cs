using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Sector
{
	public string Name { get; private set; }
	public event Action<string> OnNameChanged;
	public Color SectorColor { get; private set; } = Color.red;
	public event Action<Color> OnColorChanged;
	public List<Subsector> Subsectors = new List<Subsector>();
	public int LevelNumber { get; private set; }

	/// <param name="levelNumber">1 based</param>
	public Sector(string name, int levelNumber)
	{
		Name = name;
		LevelNumber = levelNumber;
	}

	public void SetName(string name)
	{
		Name = name;
		OnNameChanged?.Invoke(name);
	}

	public void SetColor(Color col)
	{
		SectorColor = col;
		OnColorChanged?.Invoke(col);
	}
}

