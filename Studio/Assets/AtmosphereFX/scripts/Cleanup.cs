using UnityEngine;
using System.Collections;

public class Cleanup : MonoBehaviour 
{
	public float TimeBeforeCleanup = 0.0f;
	
	void Awake()
	{
		StartCoroutine( CleanupObject() );
	}
	
	public IEnumerator CleanupObject()
	{
		yield return new WaitForSeconds( TimeBeforeCleanup );
		
		DestroyObject( gameObject );
	}
}
