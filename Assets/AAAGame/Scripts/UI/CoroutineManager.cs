using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoroutineManager : MonoBehaviour
{
	Queue<IEnumerator> coroutinelist = new();

	public void Init()
	{
		StartCoroutine(ProcessQueue());
	}

	public void Uninit() {
		StopAllCoroutines();
		coroutinelist.Clear();
	}

	public void AddCoroutine(IEnumerator coroutine)
	{
		lock (coroutinelist) {
			coroutinelist.Enqueue(coroutine);
		}
	}

	private IEnumerator ProcessQueue()
	{
		while (true) {
			IEnumerator coroutine = null;
			lock (coroutinelist) {
				if (coroutinelist.Count > 0) {
					coroutine = coroutinelist.Dequeue();
				}
			}
			if (coroutine == null) {
				yield return new WaitForSeconds(0.1f);
			}
			else {
				yield return StartCoroutine(coroutine);
			}
		}
	}

	public bool IsDone() {
		return coroutinelist.Count <= 0;
	}
}
