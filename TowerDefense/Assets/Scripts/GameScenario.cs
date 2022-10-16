using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class GameScenario : ScriptableObject
{
	[SerializeField, Range(0, 10)]
	int cycles = 1;

	[SerializeField]
	EnemyWave[] waves = { };

	[SerializeField, Range(0f, 1f)]
	float cycleSpeedUp = 0.5f;
	public State Begin() => new State(this);


	[System.Serializable]
	public struct State
	{

		GameScenario scenario;

		int index;
		int cycle;

		EnemyWave.State wave;
		float timeScale;

		public State(GameScenario scenario)
		{
			this.scenario = scenario;
			index = 0;
			cycle = 0;
			Debug.Assert(scenario.waves.Length > 0, "Empty scenario!");
			wave = scenario.waves[0].Begin();
			timeScale = 1f;
		}


		public bool Progress()
		{
			float deltaTime = wave.Progress(timeScale * Time.deltaTime);
			while (deltaTime >= 0f)
			{
				if (++index >= scenario.waves.Length)
				{
					if (++cycle >= scenario.cycles && scenario.cycles > 0)
					{
						return false;
					}	
					index = 0;
					timeScale += scenario.cycleSpeedUp;
				}
				wave = scenario.waves[index].Begin();
				deltaTime = wave.Progress(deltaTime);
			}
			return true;
		}

	}


}
